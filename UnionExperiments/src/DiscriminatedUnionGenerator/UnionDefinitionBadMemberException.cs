using DIscriminatedUnions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DiscriminatedUnionGenerator;

internal class UnionDefinitionBadMemberException : Exception, IDiagnosticException
{
    private readonly MemberDeclarationSyntax _member;

    public UnionDefinitionBadMemberException(MemberDeclarationSyntax member) => _member = member;

    public Diagnostic ToDiagnostic()
        => Diagnostic.Create(
            new DiagnosticDescriptor(
                "CU0002",
                "Unexpected non-record member found in union definition",
                "{0} are not supported within union definition types. Only record definitions are " +
                "supported as each record definition is mapped to a union case within the resultant " +
                "discriminated union type.",
                "CU#",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),
            _member.GetLocation(),
            MemberDescription(_member));

    private string MemberDescription(MemberDeclarationSyntax member) => member switch {
        EventFieldDeclarationSyntax => "Event fields",
        FieldDeclarationSyntax => "Fields",
        ConstructorDeclarationSyntax => "Constructors",
        DestructorDeclarationSyntax => "Destructors",
        MethodDeclarationSyntax => "Methods",
        ConversionOperatorDeclarationSyntax => "Conversion operators",
        OperatorDeclarationSyntax => "Operators",
        BaseNamespaceDeclarationSyntax => "Namespaces",
        EventDeclarationSyntax => "Events",
        IndexerDeclarationSyntax => "Indexers",
        PropertyDeclarationSyntax => "Properties",
        BaseTypeDeclarationSyntax => "Non-record types",
        DelegateDeclarationSyntax => "Delegates",
        GlobalStatementSyntax => "Global statements",
        _ => "Unknown members (whatever they may be)"
    };
}