using DiscriminatedUnions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DiscriminatedUnionGenerator;

namespace DIscriminatedUnions;

[Generator]
public class DisciminatedUnionSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var unionDefinitionDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (x, _) => IsDecoratedApplicableTypeDeclaration(x),
                transform: static (x, _) => MaybeGetUnionDefinitionDeclarationSyntax(x))
            .Where(static x => x is not null);

        var compilationAndUnionDefinitions = 
            context.CompilationProvider.Combine(unionDefinitionDeclarations.Collect());

        context.RegisterSourceOutput(
            compilationAndUnionDefinitions,
            static (context, source) => Execute(source.Left, source.Right!, context));
    }
    private static bool IsDecoratedApplicableTypeDeclaration(SyntaxNode syntaxNode)
        => syntaxNode is TypeDeclarationSyntax declaration &&
           declaration is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax &&
           declaration.AttributeLists.Any();

    private static TypeDeclarationSyntax? MaybeGetUnionDefinitionDeclarationSyntax(GeneratorSyntaxContext context)
    {
        var syntaxNode = (TypeDeclarationSyntax)context.Node;

        foreach (var attributeList in syntaxNode.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.Name.ToString() == DiscriminatedUnionAttribute.UnionDefinitionAttributeName)
                {
                    return syntaxNode;
                }
            }
        }

        return null;
    }

    private static void Execute(
        Compilation compilation, 
        ImmutableArray<TypeDeclarationSyntax> unionDefinitions, 
        SourceProductionContext context)
    {
        if (unionDefinitions.IsDefaultOrEmpty) return;


        var cancellationToken = context.CancellationToken;

        foreach (var type in unionDefinitions.Distinct())
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

    private static string GenerateDiscriminatedUnionFromDefinition(TypeDeclarationSyntax type)
    {
        foreach (var member in type.Members)
        {
            if (member is not RecordDeclarationSyntax record) throw new UnionDefinitionBadMemberException(member);

            var name = record.Identifier.ValueText;
            var parameters = record.ParameterList?.Parameters;
        }

        return "";
    }

    private static string GenerateHintName(TypeDeclarationSyntax type)
        => $"{CreateUnionNameFromDefinitionName(type)}.generated.cs";

    private static string CreateUnionNameFromDefinitionName(TypeDeclarationSyntax type) 
        => type.Identifier.Text is var identifier && identifier.EndsWith("Definition") 
            ? identifier.Replace("Definition", "")
            : throw new UnionDefinitionNameException(type);
}
