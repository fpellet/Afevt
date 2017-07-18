using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Afevt
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AfevtAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            "Afevt",
            "Avoid default constructor",
            "Default constructor is prohibited, because {0} has others constructors",
            "Struct",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Default constructor is prohibited");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var node = (ObjectCreationExpressionSyntax)context.Node;
            if (node.ArgumentList?.Arguments == null || node.ArgumentList.Arguments.Any()) return;

            var typeCreated = context.SemanticModel.GetSymbolInfo(node.Type).Symbol as INamedTypeSymbol;
            if (typeCreated?.TypeKind != TypeKind.Struct) return;

            var namespaceName = typeCreated.ContainingNamespace.ToString();
            if (namespaceName.StartsWith("System") || namespaceName.StartsWith("Microsoft")) return;

            if (typeCreated.Constructors.Length == 1) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), typeCreated.Name));
        }
    }
}
