using System;

namespace AppParaUniversidad.Common;

public static class StringSimilarity
{
    // Levenshtein distance simple implementation for short strings.
    public static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var lenA = a.Length;
        var lenB = b.Length;
        var dp = new int[lenA + 1, lenB + 1];

        for (int i = 0; i <= lenA; i++) dp[i, 0] = i;
        for (int j = 0; j <= lenB; j++) dp[0, j] = j;

        for (int i = 1; i <= lenA; i++)
        {
            for (int j = 1; j <= lenB; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }
        return dp[lenA, lenB];
    }
}
