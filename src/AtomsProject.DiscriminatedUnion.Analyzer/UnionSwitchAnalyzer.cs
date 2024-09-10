using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace AtomsProject.DiscriminatedUnion.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnionSwitchAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.UNION001Title),
        Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.UNION001MessageFormat),
        Resources.ResourceManager, typeof(Resources));

    public const string DiagnosticId = "UNION001";
    
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // Register for switch statements
        context.RegisterSyntaxNodeAction(AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
        context.RegisterSyntaxNodeAction(AnalyzeSwitchExpression, SyntaxKind.SwitchExpression);
    }

    // Analyzer for switch statements
    private void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context)
    {
        var switchStatement = (SwitchStatementSyntax)context.Node;

        // Analyze the type of the switch expression
        var switchExpressionType = ModelExtensions.GetTypeInfo(context.SemanticModel, switchStatement.Expression).Type;

        if (switchExpressionType != null && HasUnionAttribute(switchExpressionType))
        {
            var unionTypes = GetUnionTypes(switchExpressionType);
            var handledTypes = GetHandledTypesFromSwitch(switchStatement, context.SemanticModel);

            var missingTypes = unionTypes.Except(handledTypes, SymbolEqualityComparer.Default).ToList();
            if (missingTypes.Any())
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    switchStatement.SwitchKeyword.GetLocation(),
                    string.Join(", ", missingTypes.Select(t => t.Name)));

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext context)
    {
        var switchExpression = (SwitchExpressionSyntax)context.Node;

        // Analyze the type of the switch expression
        var switchExpressionType =
            ModelExtensions.GetTypeInfo(context.SemanticModel, switchExpression.GoverningExpression).Type;

        if (switchExpressionType != null && HasUnionAttribute(switchExpressionType))
        {
            var unionTypes = GetUnionTypes(switchExpressionType);
            var handledTypes = GetHandledTypesFromSwitchExpression(switchExpression, context.SemanticModel);

            var missingTypes = unionTypes.Except(handledTypes, SymbolEqualityComparer.Default).ToList();
            if (missingTypes.Any())
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    switchExpression.SwitchKeyword.GetLocation(),
                    string.Join(", ", missingTypes.Select(t => t.Name)));

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    // Helper function to check if a type has the Union attribute
    internal static bool HasUnionAttribute(ITypeSymbol typeSymbol)
    {
        return typeSymbol.GetAttributes().Any(IsUnionAttribute);
    }

    // Helper function to extract union types from the Union attribute
    internal static ImmutableArray<ITypeSymbol> GetUnionTypes(ITypeSymbol typeSymbol)
    {
        var unionAttribute = typeSymbol.GetAttributes().First(IsUnionAttribute);

        var types = unionAttribute.ConstructorArguments[0].Values;
        return types.Where(t => t.Value is not null).Select(t => t.Value).OfType<ITypeSymbol>().ToImmutableArray();
    }

    internal static IEnumerable<ITypeSymbol> GetHandledTypesFromSwitch(SwitchStatementSyntax switchStatement,
        SemanticModel semanticModel)
    {
        var handledTypes = new List<ITypeSymbol>();

        foreach (var section in switchStatement.Sections)
        {
            foreach (var label in section.Labels)
            {
                // Handle case pattern matching (case A a:, case B { Flagged: true }:)
                if (label is CasePatternSwitchLabelSyntax patternLabel)
                {
                    if (patternLabel.Pattern is DeclarationPatternSyntax declarationPattern)
                    {
                        // Get the type of the pattern (e.g., A, B, C)
                        var typeInfo = semanticModel.GetTypeInfo(declarationPattern.Type);
                        if (typeInfo.Type != null)
                        {
                            handledTypes.Add(typeInfo.Type);
                        }
                    }
                    else if (patternLabel.Pattern is RecursivePatternSyntax recursivePattern &&
                             recursivePattern.Type != null)
                    {
                        // Handle recursive patterns like B { Flagged: true }
                        var typeInfo = semanticModel.GetTypeInfo(recursivePattern.Type);
                        if (typeInfo.Type != null)
                        {
                            handledTypes.Add(typeInfo.Type);
                        }
                    }
                }
                // Handle simple case labels (e.g., case A:)
                else if (label is CaseSwitchLabelSyntax caseLabel)
                {
                    var typeInfo = semanticModel.GetTypeInfo(caseLabel.Value);
                    if (typeInfo.Type != null)
                    {
                        handledTypes.Add(typeInfo.Type);
                    }
                }
            }
        }

        // Return distinct types, ignoring nulls
        return handledTypes
            .Distinct(SymbolEqualityComparer.Default)
            .Where(t => t is not null)
            .Cast<ITypeSymbol>();
    }

    // Helper function to handle types in switch expressions
    internal static IEnumerable<ITypeSymbol> GetHandledTypesFromSwitchExpression(SwitchExpressionSyntax switchExpression,
        SemanticModel semanticModel)
    {
        var handledTypes = new List<ITypeSymbol>();

        foreach (var arm in switchExpression.Arms)
        {
            switch (arm.Pattern)
            {
                case ConstantPatternSyntax constantPattern:
                {
                    var typeInfo = semanticModel.GetTypeInfo(constantPattern.Expression);
                    if (typeInfo.Type != null)
                    {
                        handledTypes.Add(typeInfo.Type);
                    }

                    break;
                }
                case DeclarationPatternSyntax declarationPattern:
                {
                    var typeInfo = semanticModel.GetTypeInfo(declarationPattern.Type);
                    if (typeInfo.Type != null)
                    {
                        handledTypes.Add(typeInfo.Type);
                    }

                    break;
                }
                case RecursivePatternSyntax { Type: not null } recursivePattern:
                {
                    var typeInfo = semanticModel.GetTypeInfo(recursivePattern.Type);
                    if (typeInfo.Type != null)
                    {
                        handledTypes.Add(typeInfo.Type);
                    }

                    break;
                }
            }
        }

        return handledTypes
            .Distinct(SymbolEqualityComparer.Default)
            .Where(t => t is not null)
            .Cast<ITypeSymbol>();
    }

    private static bool IsUnionAttribute(AttributeData attr)
    {
        return "UnionAttribute" == attr.AttributeClass?.Name;
    }
}