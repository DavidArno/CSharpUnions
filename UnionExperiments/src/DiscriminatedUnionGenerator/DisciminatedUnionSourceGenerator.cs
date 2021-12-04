using DiscriminatedUnionGenerator;
using DiscriminatedUnions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace DIscriminatedUnions;

[Generator]
public class DisciminatedUnionSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Debugger.Launch();

        var unionDefinitionDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (x, _) => IsDecoratedApplicableTypeDeclaration(x),
                transform: static (x, _) => MaybeGetUnionDefinitionDeclarationSyntax(x))
            .Where(static x => x is not null);

        var compilationAndUnionDefinitions =
            context.CompilationProvider.Combine(unionDefinitionDeclarations.Collect());

        context.RegisterSourceOutput(
            compilationAndUnionDefinitions,
            static (context, source) => Execute(source.Right!, context));
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
        var cases = GetUnionCases(type);

        return "";
    }

    private static List<UnionCase> GetUnionCases(TypeDeclarationSyntax type)
    {
        var cases = new List<UnionCase>();

        foreach (var member in type.Members)
        {
            if (member is not RecordDeclarationSyntax record) throw new UnionDefinitionBadMemberException(member);

            var caseName = record.Identifier.ValueText;
            var possibleRawTypeParameters = record.TypeParameterList?.Parameters;
            var possibleRawCaseParameters = record.ParameterList?.Parameters;
            var isStruct = record.Kind() == SyntaxKind.RecordStructDeclaration;

            var typeParameters = new List<string>();
            if (possibleRawTypeParameters is { } rawTypeParameters)
            {
                typeParameters.AddRange(rawTypeParameters.Select(x => x.Identifier.Text));
            }

            var caseParameters = new List<UnionCaseParameter>();
            if (possibleRawCaseParameters is { } rawCaseParameters)
            {
                foreach (var parameter in rawCaseParameters)
                {
                    var parameterName = parameter.Identifier.ValueText;
                    var (name, parameters, isNullable) = GenerateParamTypeDetails(parameter.Type!);

                    caseParameters.Add(new UnionCaseParameter(parameterName, name, parameters, isNullable));
                }
            }

            cases.Add(new UnionCase(caseName, typeParameters, caseParameters));
        }

        return cases;
    }

#pragma warning disable CS8509
    private static (string typeName, List<string> typeParams, bool nullable) GenerateParamTypeDetails(TypeSyntax type)
        => type switch {
            IdentifierNameSyntax s => (s.Identifier.Text, new List<string>(), false),
            GenericNameSyntax s => (
                s.Identifier.Text, 
                s.TypeArgumentList.Arguments.Select(a => ((IdentifierNameSyntax)a).Identifier.Text).ToList(),
                false),
            PredefinedTypeSyntax s => (s.ToString(), new List<string>(), false),
            NullableTypeSyntax s => GenerateParamTypeDetails(s.ElementType) with { nullable = true},
            var s => throw new UnionDefinitionUnknownParameterTypeException(s)
        };
#pragma warning restore CS8509

    private record UnionCaseParameter(
        string ParameterName, 
        string TypeName, 
        List<string> TypeParameters,
        bool isNullable);

    private record UnionCase(string CaseName, List<string> TypeParameters, List<UnionCaseParameter> CaseParameters);

    private static string GenerateHintName(TypeDeclarationSyntax type)
        => $"{CreateUnionNameFromDefinitionName(type)}.generated.cs";

    private static string CreateUnionNameFromDefinitionName(TypeDeclarationSyntax type)
        => type.Identifier.Text is var identifier && identifier.EndsWith("Definition")
            ? identifier.Replace("Definition", "")
            : throw new UnionDefinitionNameException(type);
}
