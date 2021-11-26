using Microsoft.CodeAnalysis;

namespace DIscriminatedUnions;

internal interface IDiagnosticException
{
    Diagnostic ToDiagnostic();
}