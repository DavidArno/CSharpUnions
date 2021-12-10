using System.Text;

namespace DiscriminatedUnionGenerator;

internal sealed class Usings
{
    private IEnumerable<string> _compilationUnitUsings = new List<string>();
    private IEnumerable<string> _namespaceUsings = new List<string>();
    private bool _systemRuntimeInteropServicesSpecified;

    internal Usings SetCompilationUnitUsings(IEnumerable<string> usings)
    {
        _compilationUnitUsings = usings;
        UpdateUsingsSpecifiedStatus(usings);
        return this;
    }

    internal Usings SetNamespaceUsings(IEnumerable<string> usings)
    {
        _namespaceUsings = usings;
        UpdateUsingsSpecifiedStatus(usings);
        return this;
    }

    internal void UpdateUsingsSpecifiedStatus(IEnumerable<string> usings)
    {
        foreach (var usingStatement in usings)
        {
            if (usingStatement == "using System.Runtime.InteropServices;")
            {
                _systemRuntimeInteropServicesSpecified = true;
            }
        }
    }

    internal string GetFormattedCompilationUnitUsings() => $"{string.Join("\n", _compilationUnitUsings)}\n\n";

    internal string GetFormattedNamespaceUsings()
    {
        var builder = new StringBuilder();

        foreach (var usingStatement in _namespaceUsings)
        {
            builder.Append($"    {usingStatement}\n");
        }

        if (!_systemRuntimeInteropServicesSpecified)
        {
            builder.Append("    using System.Runtime.InteropServices;\n");
        }

        builder.Append("\n");

        return builder.ToString();
    }
}

