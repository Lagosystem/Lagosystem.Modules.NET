
namespace Lagosystem.Modules.Extensions;

using ca = System.Diagnostics.CodeAnalysis;
using jba = JetBrains.Annotations;
using System.Runtime.CompilerServices;
using System.ComponentModel;

/// <summary>
/// Provides extension methods for <see cref="IEnumerable{T}"/> collections.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Determines whether a sequence of strings contains a specified element by using a specified <see cref="StringComparison"/>.
    /// </summary>
    /// <param name="source">A sequence of strings in which to locate a value.</param>
    /// <param name="value">The string to locate in the sequence.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules to use in the comparison.</param>
    /// <returns>
    /// <see langword="true"/> if the source sequence contains an element that has the specified value; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool Contains(this IEnumerable<string> source, string value, StringComparison comparisonType) =>
        !value.IsBlank() && source.Any(str => str.Equals(value, comparisonType));
    
    /// <summary>
    /// Executes an action on each element of the sequence.
    /// </summary>
    /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">The sequence of elements to execute the action on.</param>
    /// <param name="action">The action to execute on each element.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="action"/> is null.</exception>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (action == null) throw new ArgumentNullException(nameof(action));

        foreach (T item in source) action(item);
    }
    
    /// <summary>
    /// Filters a sequence of values based on a predicate only if the specified condition is true.
    /// </summary>
    /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">The sequence to filter.</param>
    /// <param name="condition">The boolean condition that determines whether the predicate should be applied.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> that contains elements from the input sequence that satisfy the condition, 
    /// or the original sequence if the condition is false.
    /// </returns>
    public static IEnumerable<T> WhereIf<T>([ca.NotNull, jba.InstantHandle] this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        return condition ? source.Where(predicate) : source;
    }
    
    /// <summary>
    /// Determines whether no elements of a sequence satisfy a condition.
    /// </summary>
    /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">The sequence to check.</param>
    /// <param name="predicate">A function to test each element for a condition. If null, the method checks if the sequence is empty.</param>
    /// <returns>
    /// <see langword="true"/> if no elements in the sequence satisfy the condition (or if the sequence is empty); otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsNone<T>(
        [ca.NotNull, jba.InstantHandle] this IEnumerable<T> source, 
        [jba.CanBeNull, jba.InstantHandle] Func<T, bool>? predicate = null
    ) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return predicate == null ? !source.Any() : !source.Any(predicate);
    }
    
    /// <summary>
    /// Splits the sequence into two sequences based on a predicate.
    /// </summary>
    /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">The sequence to partition.</param>
    /// <param name="predicate">The function used to determine which partition an element belongs to.</param>
    /// <returns>
    /// A <see cref="PartitionResult{T}"/> containing elements that satisfy the predicate 
    /// and elements that do not.
    /// </returns>
    public static PartitionResult<T> Partition<T>([ca.NotNull, jba.InstantHandle] this IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        List<T> matches = new List<T>();
        List<T> nonMatches = new List<T>();

        foreach (T item in source)
            (predicate(item) ? matches : nonMatches).Add(item);

        return new PartitionResult<T>(matches, nonMatches);
    }
    
    /// <summary>
    /// Splits a sequence of strings into two sequences based on equality with a specified value.
    /// </summary>
    /// <param name="source">The sequence to partition.</param>
    /// <param name="value">The string value to compare against each element in the sequence.</param>
    /// <param name="comparison">The string comparison rules to use.</param>
    /// <returns>
    /// A <see cref="PartitionResult{String}"/> containing strings that match the value 
    /// and strings that do not. If the provided value is blank, all elements are returned as non-matches.
    /// </returns>
    public static PartitionResult<string> Partition([ca.NotNull, jba.InstantHandle] this IEnumerable<string> source, string value, StringComparison comparison)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        List<string> matches = new List<string>();
        List<string> nonMatches = new List<string>();
        if (value.IsBlank())
        {
            nonMatches.AddRange(source);
            return new PartitionResult<string>(matches, nonMatches);
        }

        foreach (string item in source)
            (value.Equals(item, comparison) ? matches : nonMatches).Add(item);

        return new PartitionResult<string>(matches, nonMatches);
    }

    /// <summary>
    /// Concatenates the members of a collection, using the specified separator between each member.
    /// </summary>
    /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">A collection that contains the objects to concatenate.</param>
    /// <param name="separator">The string to use as a separator.</param>
    /// <returns>
    /// A string that consists of the elements of the collection delimited by the separator string.
    /// </returns>
    /// <summary>
    /// Concatenates the members of a collection, using the specified separator between each member.
    /// If no separator is provided or if it is blank, it defaults to the current culture's list separator.
    /// </summary>
    /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">A collection that contains the objects to concatenate.</param>
    /// <param name="separator">The string to use as a separator. Defaults to the system's regional list separator if null or blank.</param>
    /// <returns>
    /// A string that consists of the elements of the collection delimited by the separator string.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
    public static string ToDelimitedString<T>([ca.NotNull, jba.InstantHandle] this IEnumerable<T> source, [jba.CanBeNull] string? separator = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (separator.IsBlank()) separator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        return string.Join(separator, source);
    }
    
    /// <summary>
    /// Indicates whether the specified enumerable is null or empty.
    /// </summary>
    /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">The sequence to check.</param>
    /// <returns>
    /// <see langword="true"/> if the source sequence is null or has no elements; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsNullOrEmpty<T>([jba.CanBeNull, jba.InstantHandle] this IEnumerable<T>? source) =>
        source == null || !source.Any();
    
    /// <summary>
    /// Hides the generic IEnumerable implementation for strings to prevent conflicts and performance hits.
    /// </summary>
    [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Do not use this enumerable extension for strings. Use the native 'string.IsNullOrEmpty()' or your custom 'IsBlank()' method instead.", error: true)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty([jba.CanBeNull] this string? source)
    {
        // Se por algum milagre alguém conseguir contornar o erro do compilador via reflection, 
        // ainda assim rodará o método nativo de alta performance.
        return string.IsNullOrEmpty(source);
    }
}

/// <summary>
/// Represents the result of a partition operation on an <see cref="IEnumerable{T}"/>.
/// </summary>
/// <typeparam name="T">The type of elements in the partitioned sequences.</typeparam>
public readonly struct PartitionResult<T>
{
    private readonly IReadOnlyCollection<T>? _matches;
    private readonly IReadOnlyCollection<T>? _nonMatches;

    /// <summary>
    /// Gets the sequence of elements that satisfy the partition predicate.
    /// </summary>
    public IReadOnlyCollection<T> Matches => _matches ?? Array.Empty<T>();

    /// <summary>
    /// Gets the sequence of elements that do not satisfy the partition predicate.
    /// </summary>
    public IReadOnlyCollection<T> NonMatches => _nonMatches ?? Array.Empty<T>();
    
    internal PartitionResult(IReadOnlyCollection<T> matches, IReadOnlyCollection<T> nonMatches)
    {
        _matches = matches ?? throw new ArgumentNullException(nameof(matches));
        _nonMatches = nonMatches ?? throw new ArgumentNullException(nameof(nonMatches));
    }
}