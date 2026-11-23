using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace DapperQueryGuard.CodeFixes;

/// <summary>Offers a DbString conversion only when the analyzer supplied a bounded ANSI column length.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Dqg001CodeFixProvider))]
public sealed class Dqg001CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("DQG001");
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.FirstOrDefault(); if (diagnostic is null || !diagnostic.Properties.TryGetValue("length", out var lengthText) || !int.TryParse(lengthText, out var length) || length <= 0) return;
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false); if (root is null) return;
        var node = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<AnonymousObjectMemberDeclaratorSyntax>(); if (node is null || node.Expression is null) return;
        context.RegisterCodeFix(Microsoft.CodeAnalysis.CodeActions.CodeAction.Create("Use Dapper.DbString for ANSI column", cancellationToken => ApplyAsync(context.Document, node, length, cancellationToken), "Dqg001DbString"), diagnostic);
    }
    private static async Task<Document> ApplyAsync(Document document, AnonymousObjectMemberDeclaratorSyntax member, int length, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var value = member.Expression!;
        var objectCreation = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("Dapper.DbString"))
            .WithNewKeyword(SyntaxFactory.Token(SyntaxKind.NewKeyword))
            .WithInitializer(SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, SyntaxFactory.SeparatedList<ExpressionSyntax>(new SyntaxNodeOrToken[]
            {
                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("Value"), value), SyntaxFactory.Token(SyntaxKind.CommaToken),
                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("IsAnsi"), SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)), SyntaxFactory.Token(SyntaxKind.CommaToken),
                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("Length"), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(length)))
            })));
        editor.ReplaceNode(value, objectCreation.WithTriviaFrom(value));
        return editor.GetChangedDocument();
    }
}
