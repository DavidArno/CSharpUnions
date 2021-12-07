using Microsoft.CodeAnalysis;

namespace DiscriminatedUnionGenerator;

internal interface IDiagnosticException
{
    Diagnostic ToDiagnostic();
}