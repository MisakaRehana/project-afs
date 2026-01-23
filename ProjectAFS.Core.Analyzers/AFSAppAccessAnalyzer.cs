using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ProjectAFS.Core.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class AFSAppAccessAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "AFS0001";
		private const string Title = "Unless absolutely necessary, avoid directly referencing or converting Application.Current";
		private const string MessageFormat = "Avoid direct access or conversion of Application.Current to prevent tight coupling with AFSApp. " +
		                                     "Use Dependency Injection for type AFSApp to enhance testability and maintainability instead as AFSApp is the first service to be added in the Generic Host DI container.";
		private const string Description = "Directly accessing or converting Application.Current to AFSApp can lead to tight coupling, making the code less testable and harder to maintain. " +
		                                   "Consider using Dependency Injection to obtain an instance of AFSApp.";
		private const string Category = "Usage";
		
		private readonly static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId, Title, MessageFormat, Category,
			DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext ctx)
		{
			ctx.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			ctx.EnableConcurrentExecution();
			
			// ctx.RegisterSyntaxNodeAction(AnalyzeCast, SyntaxKind.CastExpression);
			// ctx.RegisterSyntaxNodeAction(AnalyzeAs, SyntaxKind.AsExpression);
			ctx.RegisterCompilationStartAction(compCtx =>
			{
				var afsAppType = compCtx.Compilation.GetTypeByMetadataName("ProjectAFS.Core.AFSApp");
				var avaAppType = compCtx.Compilation.GetTypeByMetadataName("Avalonia.Application");
				if (afsAppType == null || avaAppType == null) return;
				
				compCtx.RegisterSyntaxNodeAction(c => AnalyzeCast(c, afsAppType, avaAppType), SyntaxKind.CastExpression);
				compCtx.RegisterSyntaxNodeAction(c => AnalyzeAs(c, afsAppType, avaAppType), SyntaxKind.AsExpression);
			});
		}

		private void AnalyzeCast(SyntaxNodeAnalysisContext ctx, INamedTypeSymbol typeAFSApp, INamedTypeSymbol typeAvaloniaApp)
		{
			var castExpr = (CastExpressionSyntax)ctx.Node;
			CheckTypeAndReport(ctx, castExpr.Expression, castExpr, typeAFSApp, typeAvaloniaApp);
		}
		
		private void AnalyzeAs(SyntaxNodeAnalysisContext ctx, INamedTypeSymbol typeAFSApp, INamedTypeSymbol typeAvaloniaApp)
		{
			var asExpr = (BinaryExpressionSyntax)ctx.Node;
			CheckTypeAndReport(ctx, asExpr.Left, asExpr, typeAFSApp, typeAvaloniaApp);
		}

		private static void CheckTypeAndReport(SyntaxNodeAnalysisContext ctx, SyntaxNode nodeExpression, SyntaxNode nodeTargetType, INamedTypeSymbol typeAFSApp, INamedTypeSymbol typeAvaloniaApp)
		{
			var targetTypeInfo = ctx.SemanticModel.GetTypeInfo(nodeTargetType);
			if (!SymbolEqualityComparer.Default.Equals(targetTypeInfo.Type, typeAFSApp))
			{
				return;
			}
			
			var symbolInfo = ctx.SemanticModel.GetSymbolInfo(nodeExpression);
			var symbol = symbolInfo.Symbol;
			
			if (symbol is IPropertySymbol property && property.Name == "Current" && SymbolEqualityComparer.Default.Equals(property.ContainingType, typeAvaloniaApp))
			{
				var diagnostic = Diagnostic.Create(Rule, nodeTargetType.GetLocation());
				ctx.ReportDiagnostic(diagnostic);
			}
		}
	}
}