
namespace Lagosystem.Modules.Formatting
{
    using System.Text;
    using Modules.Extensions;
    using Modules.Validations;
    using Lagosystem.Modules.Conversions;
    
    /// <summary>Provides high-performance, low-allocation methods for formatting Brazilian documents and postal codes.</summary>
    public static partial class Format
    {
        /// <summary>
        /// Intelligently formats a digital storage value into a human-readable string, 
        /// automatically selecting the most appropriate unit (e.g., converting 934000 Kilobytes to "890.67 Megabyte").
        /// </summary>
        /// <param name="value">The numeric value to format.</param>
        /// <param name="fromUnit">The original unit of the provided value.</param>
        /// <param name="decimals">The number of decimal places to include in the formatted string. Default is 2.</param>
        /// <returns>A formatted string containing the optimized numeric value and the unit name (from the <see cref="DataUnit"/> enum).</returns>
        public static string FormatSize(double value, DataUnit fromUnit, short decimals = 2)
        {
            double bytes = Math.Abs(value * (long)fromUnit);
            DataUnit targetUnit = bytes switch
            {
                >= (long)DataUnit.Exabyte => DataUnit.Exabyte,
                >= (long)DataUnit.Petabyte => DataUnit.Petabyte,
                >= (long)DataUnit.Terabyte => DataUnit.Terabyte,
                >= (long)DataUnit.Gigabyte => DataUnit.Gigabyte,
                >= (long)DataUnit.Megabyte => DataUnit.Megabyte,
                >= (long)DataUnit.Kilobyte => DataUnit.Kilobyte,
                _ => DataUnit.Byte
            };
            double finalValue = Conversion.ConvertSize(value, fromUnit, targetUnit);
            return $"{Math.Round(finalValue, decimals)} {targetUnit}";
        }
    }    
}