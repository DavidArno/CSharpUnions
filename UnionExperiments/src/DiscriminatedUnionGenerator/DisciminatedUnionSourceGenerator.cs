using DiscriminatedUnions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DIscriminatedUnions;

[Generator]
public class DisciminatedUnionSourceGenerator : ISourceGenerator
{
    private SyntaxReceiver? _syntaxReceiver;

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => _syntaxReceiver = new SyntaxReceiver());
        //Debugger.Launch();
    }

    private class SyntaxReceiver : ISyntaxReceiver
    {
        public HashSet<TypeDeclarationSyntax> UnionTypeDeclarations { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TypeDeclarationSyntax declaration &&
                declaration is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax &&
                declaration.AttributeLists
                    .SelectMany(x => x.Attributes)
                    .Where(a => a.Name.ToString() == DiscriminatedUnionAttribute.UnionDefinitionAttributeName)
                    .Any())
            {
                UnionTypeDeclarations.Add(declaration);
            }
        }
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        foreach (var type in _syntaxReceiver!.UnionTypeDeclarations)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                context.AddSource(
                    GenerateHintName(type),
                    SourceText.From(GenerateDiscriminatedUnionFromDefinition(type), Encoding.UTF8));
            }
            catch (Exception exception) when (exception is IDiagnosticException e)
            {
                context.ReportDiagnostic(e.ToDiagnostic());
            }
        }
    }

    private string GenerateDiscriminatedUnionFromDefinition(TypeDeclarationSyntax type)
    {
        return "";
    }

    private string GenerateHintName(TypeDeclarationSyntax type)
        => $"{CreateUnionNameFromDefinitionName(type)}.g.cs";

    private string CreateUnionNameFromDefinitionName(TypeDeclarationSyntax type) 
        => type.Identifier.Text is var identifier && identifier.EndsWith("Definition") 
            ? identifier.Replace("Definition", "")
            : throw new UnionDefinitionNameException(type);
}
