using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DiscriminatedUnionGenerator;

internal class DiscriminatedUnionDefinitionReader
{
    internal static List<UnionCase> GetUnionCases(TypeDeclarationSyntax type)
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


    internal static string GetNameSpace(SyntaxNode? node) => node switch {
        null => "",
        FileScopedNamespaceDeclarationSyntax s => FormatNamespace(s.Name),
        NamespaceDeclarationSyntax s => FormatNamespace(s.Name),
        _ => GetNameSpace(node.Parent)
    };

    internal static Usings GetUsings(SyntaxNode? node, Usings usingsSoFar)
    {
        return node switch {
            null => usingsSoFar,
            NamespaceDeclarationSyntax s => GetUsings(s.Parent, CaptureNameSpaceUsings(s, usingsSoFar)),
            CompilationUnitSyntax s => GetUsings(s.Parent, CaptureCompilationUnitUsings(s, usingsSoFar)),
            var n => GetUsings(n.Parent, usingsSoFar)
        };
    }

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
    
    private static string FormatNamespace(NameSyntax name) => $"namespace {name}\n{{\n";

    private static Usings CaptureCompilationUnitUsings(CompilationUnitSyntax compilationUnit, Usings usingsSoFar) 
        => usingsSoFar.SetCompilationUnitUsings(compilationUnit.Usings.Select(x => x.ToString()));

    private static Usings CaptureNameSpaceUsings(
        NamespaceDeclarationSyntax namespaceDeclaration, 
        Usings usingsFoundSoFar) 
        => usingsFoundSoFar.SetNamespaceUsings(namespaceDeclaration.Usings.Select(x => x.ToString()));
}
