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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using ProjectAFS.Core.Analyzers;

namespace ProjectAFS.Core.CodeFix
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AFSAppAccessCodeFixProvider)), Shared]
	public sealed class AFSAppAccessCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AFSAppAccessAnalyzer.DiagnosticId);
		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;
			
			var declaration = root?.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
			if (declaration == null) return;
			
			context.RegisterCodeFix(CodeAction.Create(
				title: "Use Dependency Injection to obtain AFSApp instance",
				createChangedDocument: c => MoveToConstructorInjectionAsync(context.Document, declaration, c),
				equivalenceKey: nameof(AFSAppAccessCodeFixProvider)),
				diagnostic);
		}

		private static async Task<Document> MoveToConstructorInjectionAsync(Document document, SyntaxNode expression, CancellationToken cancellationToken = default)
		{
			var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
			
			var classDeclaration = expression.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
			if (classDeclaration == null) return document;

			const string fieldName = "_app";
			const string paramName = "app";
			const string typeName = "AFSApp";
			
			bool hasField = classDeclaration.Members.OfType<FieldDeclarationSyntax>()
				.Any(f => f.Declaration.Variables.Any(v => v.Identifier.Text == fieldName));

			if (!hasField)
			{
				var newField = (FieldDeclarationSyntax)editor.Generator.FieldDeclaration(
					fieldName,
					editor.Generator.IdentifierName(typeName),
					Accessibility.Private,
					DeclarationModifiers.ReadOnly);
				
				editor.InsertBefore(classDeclaration.Members.First(), newField);
			}
			else
			{
				for (int i = 1; ; i++)
				{
					var newFieldName = $"_{fieldName}{i}";
					bool conflict = classDeclaration.Members.OfType<FieldDeclarationSyntax>()
						.Any(f => f.Declaration.Variables.Any(v => v.Identifier.Text == newFieldName));
					if (!conflict)
					{
						expression = expression.ReplaceNode(
							expression,
							SyntaxFactory.IdentifierName(newFieldName).WithTriviaFrom(expression));
						break;
					}
				}
			}
			
			var ctor = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
			if (ctor == null)
			{
				var newCtor = (ConstructorDeclarationSyntax)editor.Generator.ConstructorDeclaration(
					classDeclaration.Identifier.Text,
					parameters: new[] { editor.Generator.ParameterDeclaration(paramName, editor.Generator.IdentifierName(typeName)) },
					statements: new[] { editor.Generator.AssignmentStatement(editor.Generator.IdentifierName(fieldName), editor.Generator.IdentifierName(paramName)) },
					accessibility: Accessibility.Public); // Dependency Injection requires public constructor
				
				editor.InsertAfter(editor.OriginalRoot.FindNode(classDeclaration.Members.OfType<FieldDeclarationSyntax>().LastOrDefault()?.Span ?? classDeclaration.Span), newCtor);
			}
			else
			{
				if (ctor.ParameterList.Parameters.All(p => p.Identifier.Text != paramName))
				{
					var newParam = (ParameterSyntax)editor.Generator.ParameterDeclaration(paramName, editor.Generator.IdentifierName(typeName));
					editor.AddParameter(ctor, newParam);
					
					var assignment = (ExpressionSyntax)editor.Generator.AssignmentStatement(
						editor.Generator.IdentifierName(fieldName),
						editor.Generator.IdentifierName(paramName));
					
					var assignmentStatement = SyntaxFactory.ExpressionStatement(assignment);

					if (ctor.Body != null)
					{
						var firstStatement = ctor.Body.Statements.FirstOrDefault();
						if (firstStatement == null)
						{
							editor.ReplaceNode(ctor.Body, ctor.Body.AddStatements(assignmentStatement));
						}
						else
						{
							editor.InsertBefore(firstStatement, assignmentStatement);
						}
					}
					else if (ctor.ExpressionBody != null)
					{
						// Convert expression-bodied constructor to block-bodied
						var originalBodyExpr = SyntaxFactory.ExpressionStatement(ctor.ExpressionBody.Expression);
						
						var statements = SyntaxFactory.List(new[] { assignmentStatement, originalBodyExpr });
						var newBlock = SyntaxFactory.Block(statements);

						var newCtor = ctor
							.WithExpressionBody(null)
							.WithSemicolonToken(default)
							.WithBody(newBlock)
							.WithAdditionalAnnotations(Formatter.Annotation);
						
						editor.ReplaceNode(ctor, newCtor);
					}
				}
			}
			
			editor.ReplaceNode(expression, SyntaxFactory.IdentifierName(fieldName).WithTriviaFrom(expression));
			return editor.GetChangedDocument();
		}
	}
}