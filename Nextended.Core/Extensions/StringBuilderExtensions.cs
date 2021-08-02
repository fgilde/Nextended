using System;
using System.Collections.Generic;
using System.Text;

namespace Nextended.Core.Extensions
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendLineWhen(this StringBuilder builder, string s, Func<string, bool> predicate)
        {
            if (predicate(s))
                builder.AppendLine(s);
            return builder;
        }

        public static StringBuilder AppendLinesWhen(this StringBuilder builder, IEnumerable<string> s, Func<string, bool> predicate)
        {
            foreach (var t in s)
                builder.AppendLineWhen(t, predicate);
            return builder;
        }

        public static StringBuilder AppendLineIfNotEmpty(this StringBuilder builder, string s)
        {
            return builder.AppendLineWhen(s, s1 => !string.IsNullOrWhiteSpace(s1));
        }

        public static StringBuilder AppendLinesIfNotEmpty(this StringBuilder builder, params string[] s)
        {
            foreach (var t in s)
                builder.AppendLineIfNotEmpty(t);
            return builder;
        }

        public static StringBuilder AppendLines(this StringBuilder builder, IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                builder.AppendLine(line);
            }
            return builder;
        }
    }
}