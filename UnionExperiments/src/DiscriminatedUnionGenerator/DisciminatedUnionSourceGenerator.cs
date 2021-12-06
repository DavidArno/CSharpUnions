using System.Collections.Immutable;
using System.Text;
using DiscriminatedUnions;
using DIscriminatedUnions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DiscriminatedUnionGenerator;

[Generator]
public class DiscriminatedUnionSourceGenerator : IIncrementalGenerator
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
                    SourceText.From(
                        GenerateDiscriminatedUnionFromDefinition(CreateUnionNameFromDefinitionName(type), type),
                        Encoding.UTF8));
            }
            catch (Exception exception) when (exception is IDiagnosticException e)
            {
                context.ReportDiagnostic(e.ToDiagnostic());
            }
        }
    }

    private static string GenerateDiscriminatedUnionFromDefinition(string unionName, TypeDeclarationSyntax type)
    {
        var cases = GetUnionCases(type);
        var casesTypes = GenerateCasesTypes(cases);
        var casesEnum = GenerateCasesEnum(cases);
        var (caseTypeFields, offset) = GenerateCaseTypeFieldsAndOffsets(cases);

        var casesValuesFieldsOffset = (offset / 8 + 1) * 8;
        var caseFields = GenerateCasesValueFields(cases, casesValuesFieldsOffset);
        var constructors = GenerateConstructors(unionName, cases);
        var initializers = GenerateInitializers(unionName, cases);

        return "";
    }

    private static string GenerateCaseValueProperties
    private static string GenerateCaseTypeProperty(string unionName, List<UnionCase> cases)
    {
        var builder = new StringBuilder();

        builder.Append("    public object Case => _validCase switch {\n");
        foreach (var caseName in cases.Select(x => x.CaseName))
        {
            builder.Append($"        Cases.{caseName}Case => _type{caseName},\n");
        }

        builder.Append("        Cases.NotCorrectlyInitialized or _ =>\n");
        builder.Append(
            "            throw new InvalidOperationException(\"Incorrectly initialized " +
            $"{unionName} with no valid case\")");
        builder.Append("    };\n\n");

        return builder.ToString();
    }

    private static string GenerateInitializers(string unionName, List<UnionCase> cases)
    {
        var builder = new StringBuilder();

        foreach (var unionCase in cases)
        {
            var parameters = string.Join(", ", unionCase.CaseParameters.Select(
                x => GenerateLowerCaseType(x.TypeName, x.TypeParameters, x.isNullable)));

            var arguments = string.Join(", ", unionCase.CaseParameters.Select(x => CamelCase(x.ParameterName)));

            builder.Append($"    public static {unionName} As{unionCase.CaseName}({parameters}) => " +
                           $"new (new {unionCase.CaseName}({arguments}));\n");
        }

        builder.Append("\n");

        return builder.ToString();
    }

    private static string GenerateCasesEnum(List<UnionCase> cases)
    {
        return $"    private enum Cases : byte {{ {string.Join(", ", EnumValues(cases))} }}\n\n" +
                "    [FieldOffset(0)] private readonly Cases _validCase;";

        static IEnumerable<string> EnumValues(List<UnionCase> cases)
        {
            yield return "NotCorrectlyInitialized";

            foreach (var unionCase in cases)
            {
                yield return $"{unionCase.CaseName}Case";
            }
        }
    }

    private static (string code, int offset) GenerateCaseTypeFieldsAndOffsets(List<UnionCase> cases)
    {
        var builder = new StringBuilder();
        var offset = 0;

        foreach (var name in cases.Select(x => x.CaseName))
        {
            builder.Append(
                $"    [FieldOffset({++offset})] private readonly Type<{name}> _type{name} = " +
                $"Type<{name}>.Value;\n");
        }

        builder.Append("/n");

        return (builder.ToString(), offset);
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

    private static string GenerateCasesTypes(List<UnionCase> cases)
    {
        var builder = new StringBuilder();
        foreach (var unionCase in cases)
        {
            builder.Append(GenerateCaseType(unionCase));
        }

        return builder.ToString();

        static string GenerateCaseType(UnionCase unionCase)
        {
            var builder = new StringBuilder();

            builder.Append($"    readonly record struct ");
            builder.Append($"{GenerateType(unionCase.CaseName, unionCase.TypeParameters, false)}");
            builder.Append($"({GenerateCaseParameterList(unionCase.CaseParameters)});\n\n");

            return builder.ToString();
        }

        static string GenerateCaseParameterList(IEnumerable<UnionCaseParameter> unionCaseParameters)
            => string.Join(
                ", ",
                unionCaseParameters.Select(
                    p => $"{GenerateType(p.TypeName, p.TypeParameters, p.isNullable)} {p.ParameterName}"));
    }

    private static string GenerateCasesValueFields(List<UnionCase> cases, int offset)
    {
        var builder = new StringBuilder();

        foreach (var name in cases.Select(x => x.CaseName))
        {
            builder.Append($"    [FieldOffset({offset})] private readonly {name} _case{name};\n");
        }

        builder.Append("\n");

        return builder.ToString();
    }

    private static string GenerateConstructors(string unionName, List<UnionCase> cases)
    {
        var builder = new StringBuilder();

        foreach (var name in cases.Select(x => x.CaseName))
        {
            builder.Append($"    private {unionName}({name} case{name})\n");
            builder.Append("    {\n");

            foreach (var otherName in cases.Select(x => x.CaseName))
            {
                if (otherName == name) continue;

                builder.Append($"        _case{otherName} = default;\n");
            }

            builder.Append($"        _case{name} = case{name};\n\n");
            builder.Append($"        _validCase = Cases.{name}Case;\n");
            builder.Append("    }\n\n");
        }

        return builder.ToString();
    }

    private static string GenerateType(string typeName, IList<string> typeParameters, bool isNullable)
        => GenerateType(typeName, typeParameters, isNullable, x => x);

    private static string GenerateLowerCaseType(string typeName, IList<string> typeParameters, bool isNullable)
        => GenerateType(typeName, typeParameters, isNullable, CamelCase);

    private static string GenerateType(
        string typeName, 
        IList<string> typeParameters, 
        bool isNullable,
        Func<string, string> styleTypeName)
    {
        var genericParameters = typeParameters.Any() ? $"<{string.Join(", ", typeParameters)}>" : "";
        var nullableSymbol = isNullable ? "?" : "";

        return $"{styleTypeName(typeName)}{genericParameters}{nullableSymbol}";
    }


    private static string CamelCase(string term) => char.ToLowerInvariant(term[0]) + term.Substring(1);

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
