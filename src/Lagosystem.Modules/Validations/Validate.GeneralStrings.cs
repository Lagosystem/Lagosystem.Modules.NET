
namespace Lagosystem.Modules.Validations
{
    using System.Buffers;
    using System.Runtime.CompilerServices;
    using Modules;

    public static partial class Validate
    {
        #region String Similarity

        /// <summary>
        /// Calculates the similarity rate between two character sequences, case-insensitive.
        /// Optimized for critical performance scenarios (Zero Alloc on strings &lt;&#61; 256 chars).
        /// </summary>
        /// <param name="str1">The first string to compare.</param>
        /// <param name="str2">The second string to compare.</param>
        /// <param name="algorithm">The algorithm to use for the similarity calculation. Defaults to <see cref="StringSimilarityAlgorithm.LevenshteinGlobal"/>.</param>
        /// <returns>A double-precision floating-point number between 0.0 (completely different) and 1.0 (identical).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CalculateStringSimilarity(string? str1, string? str2,
            StringSimilarityAlgorithm algorithm = StringSimilarityAlgorithm.LevenshteinGlobal) =>
            CalculateStringSimilarity((str1 ?? string.Empty).AsSpan(), (str2 ?? string.Empty).AsSpan(), algorithm);

        /// <summary>Calculates the similarity rate between two character sequences using Spans (Zero Alloc up to 256 chars).</summary>
        /// <param name="str1">A read-only span representing the first string to compare.</param>
        /// <param name="str2">A read-only span representing the second string to compare.</param>
        /// <param name="algorithm">The algorithm to use for the similarity calculation. Defaults to <see cref="StringSimilarityAlgorithm.LevenshteinGlobal"/>.</param>
        /// <returns>A double-precision floating-point number between 0.0 (completely different) and 1.0 (identical).</returns>
        public static double CalculateStringSimilarity(ReadOnlySpan<char> str1, ReadOnlySpan<char> str2,
            StringSimilarityAlgorithm algorithm = StringSimilarityAlgorithm.LevenshteinGlobal)
        {
            try
            {
                if (str1.Length == 0 && str2.Length == 0) return 1.0;
                if (str1.Length == 0 || str2.Length == 0) return 0.0;

                return algorithm switch
                {
                    StringSimilarityAlgorithm.LevenshteinGlobal => ComputeLevenshtein(str1, str2),
                    StringSimilarityAlgorithm.LevenshteinPerWord => ComputeLevenshteinPerWord(str1, str2),
                    StringSimilarityAlgorithm.JaroWinkler => ComputeJaroWinkler(str1, str2),
                    _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
                };
            }
            catch (StackOverflowException) { throw; }
            catch (OutOfMemoryException) { throw; }
            catch (ThreadAbortException) { throw; }
            catch (Exception ex)
            {
                Diagnostics.ReportError(nameof(CalculateStringSimilarity), nameof(Validate), ex,
                    "Error calculating string similarity");
                return 0.0;
            }
        }

        private static double ComputeLevenshteinPerWord(ReadOnlySpan<char> str1, ReadOnlySpan<char> str2)
        {
            double similaritySum = 0;
            int totalWords = 0;

            ReadOnlySpan<char> remainder1 = str1;
            ReadOnlySpan<char> remainder2 = str2;

            while (!remainder1.IsEmpty || !remainder2.IsEmpty)
            {
                ReadOnlySpan<char> word1 = ExtractNextWord(ref remainder1);
                ReadOnlySpan<char> word2 = ExtractNextWord(ref remainder2);

                similaritySum += ComputeLevenshtein(word1, word2);
                totalWords++;
            }

            return totalWords == 0 ? 1.0 : similaritySum / totalWords;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<char> ExtractNextWord(ref ReadOnlySpan<char> input)
        {
            input = input.TrimStart(' ');

            if (input.IsEmpty) return Span<char>.Empty;
            int spaceIndex = input.IndexOf(' ');
            if (spaceIndex == -1)
            {
                ReadOnlySpan<char> lastWord = input;
                input = default;
                return lastWord;
            }

            ReadOnlySpan<char> generatedWord = input[..spaceIndex];
            input = input[(spaceIndex + 1)..];
            return generatedWord;
        }

        private static double ComputeLevenshtein(ReadOnlySpan<char> str1, ReadOnlySpan<char> str2)
        {
            if (str1.Length == 0 && str2.Length == 0) return 1.0;
            if (str1.Length == 0 || str2.Length == 0) return 0.0;

            if (str1.Length < str2.Length)
            {
                ReadOnlySpan<char> temp = str1;
                str1 = str2;
                str2 = temp;
            }

            int len1 = str1.Length;
            int len2 = str2.Length;

            int maxStackAlloc = Settings.DefaultSpanBufferSize;
            int[]? rentedArray = null;

            if (len2 > maxStackAlloc) rentedArray = ArrayPool<int>.Shared.Rent(len2 + 1);
            Span<int> distances = len2 <= maxStackAlloc ? stackalloc int[len2 + 1] : rentedArray;

            try
            {
                for (int indexDistance = 0; indexDistance <= len2; indexDistance++) 
                    distances[indexDistance] = indexDistance;

                for (int i1 = 1; i1 <= len1; i1++)
                {
                    int prevDiagonal = distances[0];
                    distances[0] = i1;
                    char c1 = char.ToLowerInvariant(str1[i1 - 1]);

                    for (int i2 = 1; i2 <= len2; i2++)
                    {
                        int savedDiagonal = distances[i2];
                        char c2 = char.ToLowerInvariant(str2[i2 - 1]);

                        if (c1 == c2)
                            distances[i2] = prevDiagonal;
                        else
                        {
                            int min = distances[i2 - 1];
                            if (distances[i2] < min) min = distances[i2];
                            if (prevDiagonal < min) min = prevDiagonal;
                            distances[i2] = min + 1;
                        }

                        prevDiagonal = savedDiagonal;
                    }
                }

                int distance = distances[len2];
                int maxLen = Math.Max(len1, len2);
                return 1.0 - ((double)distance / maxLen);
            }
            finally { if (rentedArray != null) ArrayPool<int>.Shared.Return(rentedArray); }
        }

        private static double ComputeJaroWinkler(ReadOnlySpan<char> str1, ReadOnlySpan<char> str2)
        {
            int len1 = str1.Length;
            int len2 = str2.Length;

            if (len1 == 0 && len2 == 0) return 1.0;
            if (len1 == 0 || len2 == 0) return 0.0;

            int searchRange = Math.Max(0, Math.Max(len1, len2) / 2 - 1);

            int maxStackAlloc = Settings.DefaultSpanBufferSize;
            bool[]? rentedArray1 = null;
            bool[]? rentedArray2 = null;

            if (len1 > maxStackAlloc) rentedArray1 = ArrayPool<bool>.Shared.Rent(len1);
            if (len2 > maxStackAlloc) rentedArray2 = ArrayPool<bool>.Shared.Rent(len2);

            Span<bool> matched1 = len1 <= maxStackAlloc ? stackalloc bool[len1] : rentedArray1;
            Span<bool> matched2 = len2 <= maxStackAlloc ? stackalloc bool[len2] : rentedArray2;

            try
            {
                matched1.Clear();
                matched2.Clear();

                int matches = 0;

                for (int i = 0; i < len1; i++)
                {
                    int start = Math.Max(0, i - searchRange);
                    int end = Math.Min(i + searchRange + 1, len2);
                    char c1 = char.ToLowerInvariant(str1[i]);

                    for (int j = start; j < end; j++)
                    {
                        if (matched2[j]) continue;
                        if (c1 != char.ToLowerInvariant(str2[j])) continue;
                        matched1[i] = true;
                        matched2[j] = true;
                        matches++;
                        break;
                    }
                }

                if (matches == 0) return 0.0;

                int transpositions = 0;
                int k = 0;
                for (int i = 0; i < len1; i++)
                {
                    if (!matched1[i]) continue;
                    while (!matched2[k]) k++;
                    if (char.ToLowerInvariant(str1[i]) != char.ToLowerInvariant(str2[k]))
                        transpositions++;
                    k++;
                }

                double dMatches = matches;
                double jaro = (dMatches / len1 + dMatches / len2 + (dMatches - transpositions / 2.0) / dMatches) / 3.0;

                int prefix = 0;
                int maxPrefix = Math.Min(4, Math.Min(len1, len2));
                for (int i = 0; i < maxPrefix; i++)
                {
                    if (char.ToLowerInvariant(str1[i]) == char.ToLowerInvariant(str2[i])) prefix++;
                    else break;
                }

                return jaro + prefix * 0.1 * (1.0 - jaro);
            }
            finally
            {
                if (rentedArray1 != null) ArrayPool<bool>.Shared.Return(rentedArray1);
                if (rentedArray2 != null) ArrayPool<bool>.Shared.Return(rentedArray2);
            }
        }

        #endregion

    }

    
}