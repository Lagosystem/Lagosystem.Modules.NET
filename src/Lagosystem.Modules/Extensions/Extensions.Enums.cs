

namespace Lagosystem.Modules.Extensions
{
    /// <summary>Provides extension methods for enumeration types used across the Lagosystem modules.</summary>
    public static class EnumsExtensions
    {
        /// <summary>
        /// Intelligently formats a digital storage value into a human-readable string, 
        /// automatically scaling to the most appropriate unit based on the current <see cref="DataUnit"/>.
        /// </summary>
        /// <param name="fromUnit">The original <see cref="DataUnit"/> of the provided value.</param>
        /// <param name="value">The numeric value to format.</param>
        /// <param name="decimals">The number of decimal places to include in the formatted string. Default is 2.</param>
        /// <returns>A formatted string containing the optimized numeric value and the best-fitting unit name.</returns>
        /// <remarks>
        /// This is a convenient extension method wrapper around <see cref="Lagosystem.Modules.Formatting.Format.FormatSize"/>.
        /// </remarks>
        public static string Format(this DataUnit fromUnit, double value, short decimals = 2) => Lagosystem.Modules.Formatting.Format.FormatSize(value, fromUnit, decimals);
    }
}

