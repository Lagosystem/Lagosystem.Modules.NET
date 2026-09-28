

using Lagosystem.Modules.Extensions;

namespace Lagosystem.Modules.Validations
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Globalization;
    using System.Numerics;
    using System.Text;

    /// <summary>
    /// Provides high-level validation helpers for e-mail addresses, numeric values and other
    /// domain-specific checks used across the Lagosnack modules.
    /// </summary>
    public static partial class Validate
    {
        #region URLs

        /// <summary>
        /// Validates whether the specified string represents a syntactically valid HTTP or HTTPS URL.
        /// </summary>
        /// <param name="url">The URL string to validate.</param>
        /// <returns><see langword="true"/> if the URL is valid; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidUrl(string? url) => IsValidUrl(url, true);

        /// <summary>Validates whether the specified string represents a syntactically valid absolute URL.</summary>
        /// <param name="url">The URL string to validate.</param>
        /// <param name="requireHttpOrHttps">If set to <see langword="true"/>, the URL must use the HTTP or HTTPS scheme.</param>
        /// <returns><see langword="true"/> if the URL is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValidUrl(string? url, bool requireHttpOrHttps)
        {
            try
            {
                if (url.IsBlank()) return false;

                string trimmed = url.Trim();
                if (trimmed.Length > 2048) return false;
                if (HasInvalidUrlCharacters(trimmed)) return false;

                if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri)) return false;
                if (uri.Scheme.IsBlank() || uri.Host.IsBlank()) return false;

                if (!requireHttpOrHttps) return true;
                string scheme = uri.Scheme.ToLowerInvariant();
                return scheme == Uri.UriSchemeHttp || scheme == Uri.UriSchemeHttps;
            }
            catch (StackOverflowException) { throw; }
            catch (OutOfMemoryException) { throw; }
            catch (ThreadAbortException) { throw; }
            catch (Exception ex)
            {
                Diagnostics.ReportError(nameof(IsValidUrl), nameof(Validate), ex, "Error parsing URL");
                return false;
            }
        }

        private static bool HasInvalidUrlCharacters(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (char.IsControl(ch) || ch == ' ') return true;
                if (ch is '"' or '<' or '>' or '\\' or '|' or '^' or '`' or '{' or '}') return true;
            }

            return false;
        }

        #endregion

    }
}