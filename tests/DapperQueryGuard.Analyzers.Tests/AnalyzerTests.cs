using System.Collections.Immutable;
using DapperQueryGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace DapperQueryGuard.Analyzers.Tests;

public sealed class AnalyzerTests
{
    [Fact]
    public async Task Reports_ansi_column_with_plain_string_parameter()
    {
        var diagnostics = await AnalyzeAsync("connection.Query<Customer>(\"SELECT * FROM dbo.Customers WHERE CustomerCode = @Code\", new { Code = code });");
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("DQG001", diagnostic.Id);
    }

    [Theory]
    [InlineData("SELECT * FROM dbo.Customers WHERE CustomerCode = @Code", true)]
    [InlineData("SELECT * FROM dbo.Customers WHERE Name = @Code", false)]
    [InlineData("SELECT * FROM dbo.Customers WHERE CustomerCode > @Code", false)]
    public async Task Only_supported_ansi_equality_is_reported(string sql, bool expected)
    {
        var diagnostics = await AnalyzeAsync($"connection.Query<Customer>(\"{sql}\", new {{ Code = code }});");
        Assert.Equal(expected, diagnostics.Any(x => x.Id == "DQG001"));
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string invocation)
    {
        var source = "using System.Collections.Generic; using Dapper;\nnamespace Dapper { public static class SqlMapper { public static IEnumerable<T> Query<T>(this object c, string sql, object p) => null!; } }\nclass Customer { }\nclass Demo { void Run(object connection, string code) { " + invocation + " } }";
        var compilation = CSharpCompilation.Create("Test", new[] { CSharpSyntaxTree.ParseText(source) },
            ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty).Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var options = new AnalyzerOptions(ImmutableArray.Create<AdditionalText>(new SnapshotText()));
        var result = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new Dqg001Analyzer()), options).GetAnalyzerDiagnosticsAsync();
        return result;
    }

    private sealed class SnapshotText : AdditionalText
    {
        public override string Path => "dapperguard.schema.json";
        public override SourceText? GetText(CancellationToken cancellationToken = default) => SourceText.From("{\"schemaVersion\":1,\"tables\":[{\"schema\":\"dbo\",\"name\":\"Customers\",\"columns\":[{\"name\":\"CustomerCode\",\"type\":\"Varchar\",\"length\":50,\"isNullable\":false}],\"indexes\":[]}]}");
    }
}
