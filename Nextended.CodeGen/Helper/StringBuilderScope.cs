using System;
using System.Text;

namespace Nextended.CodeGen.Helper;

internal class StringBuilderScope: IDisposable
{
    private readonly StringBuilder _sb;

    public StringBuilderScope(StringBuilder sb, params Action<StringBuilder>[] actions)
    {
        _sb = sb;
        foreach (var action in actions)
        {
            action(_sb);
        }
    }

    public void Dispose()
    {
        _sb.AppendLine("}");
    }
}

internal class ClassScope(StringBuilder sb, bool addFileHeader, string className, string namespaceName, params string[] usings)
    : StringBuilderScope(sb, s => s.AppendClassHeader(addFileHeader, className, namespaceName, usings));

internal class NamespaceScope(StringBuilder sb, string namespaceName)
    : StringBuilderScope(sb, s => s.OpenNamespace(namespaceName));