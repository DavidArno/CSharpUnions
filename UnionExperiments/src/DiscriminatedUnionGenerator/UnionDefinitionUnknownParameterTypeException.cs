using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DiscriminatedUnionGenerator;

internal class UnionDefinitionUnknownParameterTypeException : Exception, IDiagnosticException
{
    private readonly TypeSyntax _type;

    public UnionDefinitionUnknownParameterTypeException(TypeSyntax type) => _type = type;

    public Diagnostic ToDiagnostic()
        => Diagnostic.Create(
            new DiagnosticDescriptor(
                "CU0003",
                "Unknown parameter type encountered in a union case definition",
                "CSharpUnions does not know how to handle a case parameter of type '{0}'. " +
                "Please log an issue at xxx with an code example if possible that details this parameter type so it " +
                "can be considered for support in a future release.",
                "CU#",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),
            _type.GetLocation(),
            _type.ToString());
}