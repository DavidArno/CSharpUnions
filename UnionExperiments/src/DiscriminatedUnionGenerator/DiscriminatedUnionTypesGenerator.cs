using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DiscriminatedUnionGenerator;

internal class DiscriminatedUnionTypesGenerator
{
    internal static string GenerateDiscriminatedUnionFromDefinition(
        string unionName, 
        List<UnionCase> cases, 
        string namespaceName, 
        Usings usings)
    {
        var (casesTypes, typeParameters) = GenerateCasesTypes(cases);
        var isGeneric = typeParameters.Any();
        var casesEnumName = isGeneric ? $"{unionName}.Cases" : "Cases";

        var unionTypeParameters = isGeneric ? $"<{string.Join(", ", typeParameters)}>" : "";
        var unionType = $"{unionName}{unionTypeParameters}";

        var casesEnum = GenerateCasesEnum(cases);
        var validCaseField = GenerateValidCaseField(casesEnumName, false);

        var (caseTypeFields, offset) = GenerateCaseTypeFieldsAndOffsets(cases);

        var casesValuesFieldsOffset = (offset / 8 + 1) * 8;
        var caseFields = GenerateCasesValueFields(cases, casesValuesFieldsOffset, false);
        var constructors = GenerateConstructors(unionName, casesEnumName, cases);
        var initializers = GenerateInitializers(unionType, cases);
        var caseTypeProperty = GenerateCaseTypeProperty(unionName, casesEnumName, cases);
        var caseValueProperties = GenerateCaseValueProperties(casesEnumName, cases);

        var builder = new StringBuilder();
        builder.Append(usings.GetFormattedCompilationUnitUsings());
        builder.Append(namespaceName);
        builder.Append(usings.GetFormattedNamespaceUsings());
        builder.Append(casesTypes);

        //builder.Append("    [StructLayout(LayoutKind.Explicit)]\n");
        builder.Append($"    public readonly struct {unionType}\n    {{\n");

        builder.Append(isGeneric ? "" : casesEnum);
        builder.Append(validCaseField);
        builder.Append(isGeneric ? "" : $"{caseTypeFields}\n");
        builder.Append(caseFields);
        builder.Append(constructors);
        builder.Append(initializers);
        builder.Append(caseTypeProperty);
        builder.Append(caseValueProperties);

        builder.Append("    }\n");

        if (isGeneric)
        {
            builder.Append($"\n    public readonly struct {unionName}\n    {{\n");
            builder.Append(casesEnum);
            builder.Append(caseTypeFields);
            builder.Append("    }\n");
        }

        builder.Append("}\n");

        return builder.ToString();
    }

    private static string GenerateCaseValueProperties(string casesEnumName, List<UnionCase> cases)
    {
        var builder = new StringBuilder();
        foreach (var unionCase in cases)
        {
            var name = unionCase.CaseName;
            var type = GenerateType(name, unionCase.TypeParameters, false);

            builder.Append(
                $"        public {type} {name} => _validCase is {casesEnumName}.{name}Case ? _case{name} : default;\n");
        }

        return builder.ToString();
    }

    private static string GenerateCaseTypeProperty(string unionName, string casesEnumName, List<UnionCase> cases)
    {
        var builder = new StringBuilder();

        builder.Append($"        public {casesEnumName} Case => _validCase switch {{\n");
        builder.Append(
            $"            ({string.Join(" or ", cases.Select(x => $"{casesEnumName}.{ x.CaseName}Case"))}" +
            $") and var x => x,\n");
        
        builder.Append(
            "            _ => throw new InvalidOperationException(\"Incorrectly initialized " +
            $"{unionName} with no valid case\")\n");
        builder.Append("        };\n\n");

        return builder.ToString();
    }

    private static string GenerateInitializers(string unionType, List<UnionCase> cases)
    {
        var builder = new StringBuilder();

        foreach (var unionCase in cases)
        {
            var parameters = string.Join(", ", unionCase.CaseParameters.Select(
                x => $"{GenerateType(x.TypeName, x.TypeParameters, x.IsNullable)} {CamelCase(x.ParameterName)}"));

            var arguments = string.Join(", ", unionCase.CaseParameters.Select(x => CamelCase(x.ParameterName)));

            var type = GenerateType(unionCase.CaseName, unionCase.TypeParameters, false);
            builder.Append($"        public static {unionType} As{unionCase.CaseName}({parameters}) => " +
                           $"new(new {type}({arguments}));\n");
        }

        builder.Append("\n");

        return builder.ToString();
    }

    private static string GenerateCasesEnum(List<UnionCase> cases)
    {
        return $"        public enum Cases : byte {{ {string.Join(", ", EnumValues(cases))} }}\n\n";

        static IEnumerable<string> EnumValues(List<UnionCase> cases)
        {
            yield return "NotCorrectlyInitialized";

            foreach (var unionCase in cases)
            {
                yield return $"{unionCase.CaseName}Case";
            }
        }
    }

    private static string GenerateValidCaseField(string casesEnumName, bool explicitLayout)
    {
        var layout = explicitLayout ? "[FieldOffset(0)] " : "";

        return $"        {layout}private readonly {casesEnumName} _validCase;\n";
    }

    private static (string code, int offset) GenerateCaseTypeFieldsAndOffsets(List<UnionCase> cases)
    {
        var builder = new StringBuilder();
        var offset = 0;

        foreach (var unionCase in cases)
        {
            var name = unionCase.CaseName;
            builder.Append($"        public const Cases {name}Case = Cases.{name}Case;\n");
        }

        return (builder.ToString(), offset);
    }

    private static (string typeDefinitions, List<string> typeParameters) GenerateCasesTypes(List<UnionCase> cases)
    {
        var builder = new StringBuilder();
        var typeParameters = new List<string>();

        foreach (var unionCase in cases)
        {
            builder.Append(GenerateCaseType(unionCase));

            typeParameters.AddRange(unionCase.TypeParameters);
        }

        return (builder.ToString(), typeParameters);

        static string GenerateCaseType(UnionCase unionCase)
        {
            var builder = new StringBuilder();

            builder.Append($"    public readonly record struct ");
            builder.Append($"{GenerateType(unionCase.CaseName, unionCase.TypeParameters, false)}");
            builder.Append($"({GenerateCaseParameterList(unionCase.CaseParameters)});\n\n");

            return builder.ToString();
        }

        static string GenerateCaseParameterList(IEnumerable<UnionCaseParameter> unionCaseParameters)
            => string.Join(
                ", ",
                unionCaseParameters.Select(
                    p => $"{GenerateType(p.TypeName, p.TypeParameters, p.IsNullable)} {p.ParameterName}"));
    }

    private static string GenerateCasesValueFields(List<UnionCase> cases, int offset, bool explicitLayout)
    {
        var builder = new StringBuilder();

        foreach (var unionCase in cases)
        {
            var name = unionCase.CaseName;
            var type = GenerateType(name, unionCase.TypeParameters, false);
            var layout = explicitLayout ? $"[FieldOffset({offset})] " : "";

             builder.Append($"        {layout}private readonly {type} _case{name};\n");
        }

        builder.Append("\n");

        return builder.ToString();
    }

    private static string GenerateConstructors(string unionName, string casesEnumName, List<UnionCase> cases)
    {
        var builder = new StringBuilder();

        foreach (var unionCase in cases)
        {
            var name = unionCase.CaseName;
            var type = GenerateType(name, unionCase.TypeParameters, false);

            builder.Append($"        private {unionName}({type} case{name})\n");
            builder.Append("        {\n");

            foreach (var otherName in cases.Select(x => x.CaseName))
            {
                if (otherName == name) continue;

                builder.Append($"            _case{otherName} = default;\n");
            }

            builder.Append($"            _case{name} = case{name};\n");
            builder.Append($"            _validCase = {casesEnumName}.{name}Case;\n");
            builder.Append("        }\n\n");
        }

        return builder.ToString();
    }

    private static string GenerateType(
        string typeName, 
        IList<string> typeParameters, 
        bool isNullable)
    {
        var genericParameters = typeParameters.Any() ? $"<{string.Join(", ", typeParameters)}>" : "";
        var nullableSymbol = isNullable ? "?" : "";

        return $"{typeName}{genericParameters}{nullableSymbol}";
    }


    private static string CamelCase(string term) => char.ToLowerInvariant(term[0]) + term.Substring(1);

    private static string CreateUnionNameFromDefinitionName(TypeDeclarationSyntax type)
        => type.Identifier.Text is var identifier && identifier.EndsWith("Definition")
            ? identifier.Replace("Definition", "")
            : throw new UnionDefinitionNameException(type);
}
