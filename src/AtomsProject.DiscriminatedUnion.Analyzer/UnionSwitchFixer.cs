using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AtomsProject.DiscriminatedUnion.Analyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnionSwitchFixer)), Shared]
public class UnionSwitchFixer : CodeFixProvider
{
    private static readonly LocalizableString Title =
        new LocalizableResourceString(nameof(Resources.UNION001FixTitle), Resources.ResourceManager,
            typeof(Resources));

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(UnionSwitchAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the switch statement or expression identified by the diagnostic
        var statementNode = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<SwitchStatementSyntax>()
            .FirstOrDefault();
        var expressionNode = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<SwitchExpressionSyntax>()
            .FirstOrDefault();

        if (statementNode != null && diagnostic.Id == UnionSwitchAnalyzer.DiagnosticId)
        {
            // Register code fix for switch statement
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title.ToString(),
                    createChangedDocument: token =>
                        AddMissingTypesToSwitchStatement(context.Document, statementNode, token),
                    equivalenceKey: Title.ToString()),
                diagnostic);
        }

        if (expressionNode != null && diagnostic.Id == UnionSwitchAnalyzer.DiagnosticId)
        {
            // Register code fix for switch expression
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title.ToString(),
                    createChangedDocument: token =>
                        AddMissingTypesToSwitchExpression(context.Document, expressionNode, token),
                    equivalenceKey: Title.ToString()),
                diagnostic);
        }
    }

    private async Task<Document> AddMissingTypesToSwitchStatement(Document document,
        SwitchStatementSyntax switchStatement, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null) return document;
        // Get the type of the switch expression
        var switchExpressionType = semanticModel.GetTypeInfo(switchStatement.Expression).Type;

        if (switchExpressionType == null || !UnionSwitchAnalyzer.HasUnionAttribute(switchExpressionType))
        {
            return document; // No changes if the type is not a union
        }

        // Get union types and handled types
        var unionTypes = UnionSwitchAnalyzer.GetUnionTypes(switchExpressionType);
        var handledTypes = UnionSwitchAnalyzer.GetHandledTypesFromSwitch(switchStatement, semanticModel);

        // Calculate the missing types
        var missingTypes = unionTypes
            .Except(handledTypes, SymbolEqualityComparer.Default)
            .Where(t => t is not null)
            .Cast<ITypeSymbol>()
            .ToList();

        if (!missingTypes.Any())
        {
            return document; // No missing types, so return the original document
        }

        // Create new switch sections for the missing types
        var newSections = missingTypes.Select(missingType =>
            SyntaxFactory.SwitchSection(
                SyntaxFactory.List<SwitchLabelSyntax>(new[]
                {
                    SyntaxFactory.CaseSwitchLabel(SyntaxFactory.IdentifierName(missingType.Name))
                }),
                SyntaxFactory.List<StatementSyntax>(new[] { SyntaxFactory.BreakStatement() })
            )
        ).ToArray();

        // Check if the last section is a default case
        var sections = switchStatement.Sections;
        var lastSection = sections.LastOrDefault();
        if (lastSection != null && lastSection.Labels.Any(label => label.IsKind(SyntaxKind.DefaultSwitchLabel)))
        {
            // Insert the new sections just before the default case
            sections = sections.InsertRange(sections.Count - 1, newSections);
        }
        else
        {
            // Add the new sections at the end
            sections = sections.AddRange(newSections);
        }

        // Update the switch statement with the new sections
        var updatedSwitchStatement = switchStatement.WithSections(sections);
        var newRoot = root.ReplaceNode(switchStatement, updatedSwitchStatement);
        return document.WithSyntaxRoot(newRoot);
    }

    private async Task<Document> AddMissingTypesToSwitchExpression(Document document,
        SwitchExpressionSyntax switchExpression, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null) return document;

        // Get the type of the switch expression
        var switchExpressionType = semanticModel.GetTypeInfo(switchExpression.GoverningExpression).Type;

        if (switchExpressionType == null || !UnionSwitchAnalyzer.HasUnionAttribute(switchExpressionType))
        {
            return document; // No changes if the type is not a union
        }

        // Get union types and handled types
        var unionTypes = UnionSwitchAnalyzer.GetUnionTypes(switchExpressionType);
        var handledTypes = UnionSwitchAnalyzer.GetHandledTypesFromSwitchExpression(switchExpression, semanticModel);

        // Calculate the missing types
        var missingTypes = unionTypes
            .Except(handledTypes, SymbolEqualityComparer.Default)
            .Where(t => t is not null)
            .Cast<ITypeSymbol>()
            .ToList();

        if (!missingTypes.Any())
        {
            return document; // No missing types, so return the original document
        }

        // Get the return type of the switch expression
        var switchReturnType = semanticModel.GetTypeInfo(switchExpression).ConvertedType;

        // Create new switch expression arms for the missing types
        var newArms = missingTypes.Select(missingType =>
            SyntaxFactory.SwitchExpressionArm(
                pattern: SyntaxFactory.ConstantPattern(SyntaxFactory.IdentifierName(missingType.Name)),
                expression: SyntaxFactory.DefaultExpression(
                    SyntaxFactory.ParseTypeName(switchReturnType?.ToDisplayString() ?? "object"))
            )
        ).ToArray();

        // Check if the last arm is the default arm
        var arms = switchExpression.Arms;
        var lastArm = arms.LastOrDefault();
        if (lastArm != null && lastArm.Pattern.IsKind(SyntaxKind.DiscardPattern))
        {
            // Insert the new arms just before the default case
            arms = arms.InsertRange(arms.Count - 1, newArms);
        }
        else
        {
            // Add the new arms at the end
            arms = arms.AddRange(newArms);
        }

        // Update the switch expression with the new arms
        var updatedSwitchExpression = switchExpression.WithArms(arms);
        var newRoot = root.ReplaceNode(switchExpression, updatedSwitchExpression);
        return document.WithSyntaxRoot(newRoot);
    }
}