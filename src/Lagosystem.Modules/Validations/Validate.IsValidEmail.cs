
namespace Lagosystem.Modules.Validations
{
    using Modules.Extensions;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Provides high-level validation helpers for e-mail addresses, numeric values and other
    /// domain-specific checks used across the Lagosnack modules.
    /// </summary>
    public static partial class Validate
    {
        private static readonly HashSet<string> CountryTwoLevelSuffixes = new(StringComparer.OrdinalIgnoreCase)
        {
            "com.br", "net.br", "org.br", "gov.br", "edu.br"
        };

        private static readonly HashSet<string> CommonProvidersWithPlusSupport = new(StringComparer.OrdinalIgnoreCase)
        {
            "gmail.com", "hotmail.com", "hotmail.com.br", "outlook.com", "outlook.com.br",
            "live.com", "icloud.com", "protonmail.com", "fastmail.com", "yahoo.com",
            "yahoo.com.br", "zohomail.com"
        };

        #region Email Validations

        /// <summary>
        /// Validates if the given string is a structurally valid e-mail address.
        /// Performs the validation without allocating extra memory (Zero Alloc).
        /// </summary>
        /// <param name="email">The e-mail address string to validate.</param>
        /// <returns><see langword="true"/> if the e-mail is valid; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidEmail(string? email) =>
            !email.IsBlank() && IsValidEmailSyntax(email!.AsSpan());

        /// <summary>
        /// Validates whether the specified string represents a syntactically valid e-mail address and
        /// reports whether the provider/alias combination is only partially supported by the validator.
        /// </summary>
        /// <param name="email">The e-mail address string to validate.</param>
        /// <param name="partialSupport">When this method returns, contains <see langword="true"/> if the e-mail uses a partially supported alias or subdomain; otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the e-mail is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValidEmail(string? email, out bool partialSupport)
        {
            partialSupport = false;

            try
            {
                if (email.IsBlank()) return false;

                // First, do a blazing fast Zero-Alloc syntax check
                if (!IsValidEmailSyntax(email.AsSpan())) return false;

                string cleanedMail = email.Clean();
                int atIndex = cleanedMail.IndexOf('@');

                string localPart = cleanedMail[..atIndex];
                string domainPart = cleanedMail[(atIndex + 1)..];

                bool usesPlus = localPart.Contains('+');
                string domainLower = domainPart.ToLowerInvariant();
                string baseDomain = GetBaseDomain(domainLower);

                bool isFastMail = baseDomain == "fastmail.com";
                bool isKnownProvider = CommonProvidersWithPlusSupport.Contains(baseDomain);
                bool hasSubdomain = !string.Equals(domainLower, baseDomain, StringComparison.OrdinalIgnoreCase);

                if (hasSubdomain)
                {
                    if (isFastMail) return true; // full support
                    if (isKnownProvider) return false; // Known provider using subdomain is rejected
                    partialSupport = true;
                }
                else
                {
                    if (!isKnownProvider) partialSupport = true;
                }

                if (usesPlus && !isKnownProvider)
                    partialSupport = true;

                return true;
            }
            catch (StackOverflowException) { throw; }
            catch (OutOfMemoryException) { throw; }
            catch (ThreadAbortException) { throw; }
            catch (Exception ex)
            {
                LagosystemModulesDiagnostic.ReportError(nameof(IsValidEmail), nameof(Validate), ex,
                    "Error validating advanced email rules.");
                return false;
            }
        }

        /// <summary>Performs an extreme high-performance, regex-free email syntax validation using a character span.</summary>
        /// <param name="email">A read-only span of characters representing the e-mail address.</param>
        /// <returns><see langword="true"/> if the e-mail syntax is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValidEmailSyntax(ReadOnlySpan<char> email)
        {
            int length = email.Length;
            if ((uint)length < 3 || length > 254) return false;

            int domainAt = -1;
            for (int i = 0; i < length; ++i)
            {
                char c = email[i];
                if (char.IsWhiteSpace(c)) return false;
                if (c != '@') continue;
                if (domainAt != -1) return false;
                domainAt = i;
            }

            if (domainAt <= 0 || domainAt == length - 1) return false;

            ReadOnlySpan<char> local = email[..domainAt];
            ReadOnlySpan<char> domain = email[(domainAt + 1)..];

            return IsLocalPartValid(local) && IsEmailDomainValid(domain);
        }

        private static bool IsLocalPartValid(ReadOnlySpan<char> local)
        {
            if ((uint)local.Length > 64u) return false;
            if (local[0] == '.' || local[0] == '+' || local[^1] == '.') return false;

            bool lastWasDot = false;
            for (int i = 0; i < local.Length; i++)
            {
                char c = local[i];
                bool ok = (uint)(c - 'a') <= 25u || (uint)(c - 'A') <= 25u || (uint)(c - '0') <= 9u ||
                          c == '.' || c == '_' || c == '-' || c == '+' || c == '%';
                if (!ok) return false;

                if (c == '.')
                {
                    if (lastWasDot) return false;
                    lastWasDot = true;
                }
                else lastWasDot = false;
            }

            return true;
        }

        private static bool IsEmailDomainValid(ReadOnlySpan<char> domain)
        {
            if (domain.Length < 3 || domain[0] == '.' || domain[^1] == '.') return false;
            if (domain.Contains("..".AsSpan(), StringComparison.Ordinal)) return false;

            int labelStart = 0;
            bool hasDot = false;

            for (int i = 0; i < domain.Length; i++)
            {
                char c = domain[i];
                if (c == '.')
                {
                    int labelLen = i - labelStart;
                    if (labelLen <= 0 || !IsDomainLabelValid(domain.Slice(labelStart, labelLen))) return false;
                    labelStart = i + 1;
                    hasDot = true;
                    continue;
                }

                bool ok = (uint)(c - 'a') <= 25u || (uint)(c - 'A') <= 25u || (uint)(c - '0') <= 9u || c == '-';
                if (!ok) return false;
            }

            int lastLabelLen = domain.Length - labelStart;
            if (lastLabelLen < 2) return false; // TLD must be >= 2

            ReadOnlySpan<char> lastLabel = domain[labelStart..];
            if (!IsDomainLabelValid(lastLabel)) return false;
            if (!hasDot) return false;

            for (int i = 0; i < lastLabel.Length; i++)
            {
                char c = lastLabel[i];
                if (!((uint)(c - 'a') <= 25u || (uint)(c - 'A') <= 25u || (uint)(c - '0') <= 9u)) return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDomainLabelValid(ReadOnlySpan<char> label) =>
            (label.Length is not (0 or > 63)) && (label[0] != '-' && label[^1] != '-');

        private static string GetBaseDomain(string domainLower)
        {
            string[] labels = domainLower.Split('.');
            if (labels.Length < 2) return domainLower;

            int last = labels.Length - 1;
            int secondLast = labels.Length - 2;
            string lastTwo = labels[secondLast] + "." + labels[last];

            if (!CountryTwoLevelSuffixes.Contains(lastTwo)) return lastTwo;

            if (labels.Length == 3) return labels[0] + "." + lastTwo;
            return labels[labels.Length - 3] + "." + lastTwo;
        }

        #endregion

    }
}