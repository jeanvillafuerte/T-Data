#if !(NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
using System.Text;

namespace System.Text
{
    internal static class StringBuilderExtension
    {
        public static StringBuilder AppendJoin(this StringBuilder builder, char separator, string[] values)
        {
            if (values == null || values.Length == 0)
                return builder;

            builder.Append(values[0]);

            for (int i = 1; i < values.Length; i++)
            {
                builder.Append(separator).Append(values[i]);
            }

            return builder;
        }
    }
}
#endif