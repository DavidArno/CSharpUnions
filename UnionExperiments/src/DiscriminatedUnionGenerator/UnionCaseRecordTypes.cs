namespace DiscriminatedUnionGenerator;

internal record UnionCase(string CaseName, List<string> TypeParameters, List<UnionCaseParameter> CaseParameters);

internal record UnionCaseParameter(
    string ParameterName,
    string TypeName,
    List<string> TypeParameters,
    bool IsNullable);

