using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DIscriminatedUnions;

internal class UnionDefinitionNameException : Exception, IDiagnosticException
{
    private readonly TypeDeclarationSyntax _type;

    public UnionDefinitionNameException(TypeDeclarationSyntax type) => _type = type;

    public Diagnostic ToDiagnostic()
        => Diagnostic.Create(
            new DiagnosticDescriptor(
                "CU0001",
                "Unvalid DU definition name",
                "'{0}' is not a valid name for a discriminated union (DU) definition. Names must be of the form " +
                "XxxDefinition, where Xxx is then the name of the resultant DU.",
                "CU#",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),
            _type.Identifier.GetLocation(),
            _type.Identifier.Text);
}