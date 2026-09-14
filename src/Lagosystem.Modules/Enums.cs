
namespace Lagosystem.Modules
{
    /// <summary>Specifies units of digital information storage, representing their respective multiplier in bytes.</summary>
    /// <remarks>
    /// This enumeration uses a <see cref="long"/> base type to safely accommodate the large numerical values 
    /// required for high-capacity units (up to Exabytes) without overflowing. It uses the binary (base-2) 
    /// definition where each step is a multiple of 1,024.
    /// </remarks>
    public enum DataUnit : long
    {
        /// <summary>Represents a single Byte.</summary>
        Byte = 1L,
        
        /// <summary>Represents a Kilobyte (KB), equivalent to 1,024 Bytes.</summary>
        Kilobyte = Byte * 1024L,

        /// <summary>Represents a Megabyte (MB), equivalent to 1,048,576 Bytes (1,024 KB).</summary>
        Megabyte = Kilobyte * 1024L,

        /// <summary>Represents a Gigabyte (GB), equivalent to 1,073,741,824 Bytes (1,024 MB).</summary>
        Gigabyte = Megabyte * 1024L,

        /// <summary>Represents a Terabyte (TB), equivalent to 1,099,511,627,776 Bytes (1,024 GB).</summary>
        Terabyte = Gigabyte * 1024L,

        /// <summary>Represents a Petabyte (PB), equivalent to 1,125,899,906,842,624 Bytes (1,024 TB).</summary>
        Petabyte = Terabyte * 1024L,

        /// <summary>Represents an Exabyte (EB), equivalent to 1,152,921,504,606,846,976 Bytes (1,024 PB).</summary>
        Exabyte = Petabyte * 1024L,
    }
    
    /// <summary>
    /// Algorithms to be used in
    /// <see cref="Lagosystem.Modules.Validations.Validate.CalculateStringSimilarity(string?,string?,StringSimilarityAlgorithm)"/>.</summary>
    public enum StringSimilarityAlgorithm
    {
        /// <summary>Evaluates the entire string at once. Strict with character positions.</summary>
        LevenshteinGlobal,
        /// <summary>Compares word by word sequentially and extracts the average. Ideal for descriptions with abbreviations.</summary>
        LevenshteinPerWord,
        /// <summary>Evaluates the entire string, but gives a bonus for identical starting words.</summary>
        JaroWinkler
    }

}