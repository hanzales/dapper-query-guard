using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DapperQueryGuard.Analyzers;

/// <summary>Reports sufficiently supported ANSI/Unicode Dapper parameter mismatches.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Dqg001Analyzer : DiagnosticAnalyzer
{
    /// <summary>DQG001 descriptor.</summary>
    public static readonly DiagnosticDescriptor Rule = new("DQG001", "Potential ANSI/Unicode parameter mismatch", "Dapper parameter '{0}' may be sent as Unicode while SQL Server column '{1}' is ANSI; consider Dapper.DbString only when the value and length semantics are safe", "Performance", DiagnosticSeverity.Warning, true, "Implicit conversion can affect a query plan depending on collation, indexes, statistics and data distribution; this diagnostic is not a guaranteed slowdown.");
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
    public override void Initialize(AnalysisContext context) { context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None); context.EnableConcurrentExecution(); context.RegisterCompilationStartAction(start => { var snapshot = LoadSnapshot(start.Options.AdditionalFiles); if (snapshot.Count == 0) return; start.RegisterSyntaxNodeAction(c => AnalyzeInvocation(c, snapshot), Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression); }); }

    private static Dictionary<string, ColumnInfo> LoadSnapshot(ImmutableArray<AdditionalText> files)
    {
        var result = new Dictionary<string, ColumnInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files.Where(x => x.Path.EndsWith("dapperguard.schema.json", StringComparison.OrdinalIgnoreCase) || x.Path.EndsWith("schema.json", StringComparison.OrdinalIgnoreCase)))
        {
            try { using var document = JsonDocument.Parse(file.GetText()?.ToString() ?? string.Empty); foreach (var table in document.RootElement.GetProperty("tables").EnumerateArray()) foreach (var column in table.GetProperty("columns").EnumerateArray()) { var type = column.GetProperty("type").GetString() ?? string.Empty; result[$"{table.GetProperty("schema").GetString()}.{table.GetProperty("name").GetString()}.{column.GetProperty("name").GetString()}"] = new(type, column.TryGetProperty("length", out var l) && l.ValueKind != JsonValueKind.Null ? l.GetInt32() : null, column.GetProperty("isNullable").GetBoolean()); } } catch (JsonException) { }
        }
        return result;
    }
    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, Dictionary<string, ColumnInfo> snapshot)
    {
        var invocation = (InvocationExpressionSyntax)context.Node; if (invocation.Expression is not MemberAccessExpressionSyntax member) return;
        var name = member.Name.Identifier.ValueText; if (name is not ("Query" or "QueryAsync" or "QueryFirst" or "QueryFirstOrDefault" or "QuerySingle" or "QuerySingleOrDefault" or "Execute" or "ExecuteAsync")) return;
        var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol; if (symbol?.ContainingType.Name != "SqlMapper") return;
        if (invocation.ArgumentList.Arguments.Count < 2 || invocation.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax sqlLiteral || !sqlLiteral.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression) || invocation.ArgumentList.Arguments[1].Expression is not AnonymousObjectCreationExpressionSyntax parameters) return;
        var sql = sqlLiteral.Token.ValueText; var match = Regex.Match(sql, @"(?is)^\s*select\s+.+?\s+from\s+(?:(?<schema>\[[^]]+\]|[\w]+)\.)?(?<table>\[[^]]+\]|[\w]+)(?:\s+(?:as\s+)?(?<alias>\w+))?\s+where\s+(?:(?<wherealias>\w+)\.)?(?<column>\[[^]]+\]|[\w]+)\s*=\s*@(?<param>\w+)\s*;?\s*$"); if (!match.Success) return;
        var schema = Unquote(match.Groups["schema"].Success ? match.Groups["schema"].Value : "dbo"); var table = Unquote(match.Groups["table"].Value); var column = Unquote(match.Groups["column"].Value); var alias = match.Groups["alias"].Success ? match.Groups["alias"].Value : string.Empty; if (match.Groups["wherealias"].Success && !string.Equals(match.Groups["wherealias"].Value, alias, StringComparison.OrdinalIgnoreCase)) return;
        var key = $"{schema}.{table}.{column}"; if (!snapshot.TryGetValue(key, out var metadata) || metadata.Type is not ("Varchar" or "Char") || metadata.Nullable) return;
        var parameterName = match.Groups["param"].Value; var property = parameters.Initializers.FirstOrDefault(x => string.Equals(x.NameEquals?.Name.Identifier.ValueText ?? x.Expression.ToString(), parameterName, StringComparison.OrdinalIgnoreCase)); if (property is null) return;
        var type = context.SemanticModel.GetTypeInfo(property.Expression).Type; if (type?.Name == "DbString" || type?.ToDisplayString().Contains("Dapper.DbString", StringComparison.Ordinal) == true) return;
        var properties = ImmutableDictionary<string, string?>.Empty.Add("length", metadata.Length?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        context.ReportDiagnostic(Diagnostic.Create(Rule, property.GetLocation(), properties, parameterName, $"{schema}.{table}.{column}"));
    }
    private static string Unquote(string value) => value.Trim('[', ']');
    private sealed record ColumnInfo(string Type, int? Length, bool Nullable);
}
