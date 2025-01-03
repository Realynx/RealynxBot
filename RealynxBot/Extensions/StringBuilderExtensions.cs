using System.Text;

namespace RealynxBot.Extensions {
    public static class StringBuilderExtensions {
        public static StringBuilder Trim(this StringBuilder sb) {
            var start = 0;
            while (char.IsWhiteSpace(sb[start])) {
                start++;
            }

            sb.Remove(0, start);

            var end = 0;
            while (sb.Length - end > 0 && char.IsWhiteSpace(sb[^(end + 1)])) {
                end++;
            }

            return sb.Remove(sb.Length - end, end);
        }
    }
}