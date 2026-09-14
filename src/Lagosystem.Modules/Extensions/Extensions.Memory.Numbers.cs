


namespace Lagosystem.Modules.Extensions
{
    using Lagosystem.Modules.Helpers;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Provides extension methods for memory and span-related operations, optimizing the formatting of primitive types.
    /// </summary>
    public static class MemoryExtensions
    {
        /// <summary>Formats the specified <see cref="long"/> value directly into the provided character span.</summary>
        /// <param name="value">The <see cref="long"/> numerical value to format.</param>
        /// <param name="destination">The <see cref="Span{T}"/> of characters to write the formatted value into.</param>
        /// <returns>The number of characters successfully written to the destination span.</returns>
        /// <exception cref="ArgumentException">Thrown when the destination span is too small to accommodate the formatted value.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToSpan(this long value, Span<char> destination) =>
            !value.TryFormat(destination, out int charsWritten)
                ? throw ThrowHelper.ArgumentInsufficientBufferFromParam(nameof(destination))
                : charsWritten;

        /// <summary>Formats the specified unsigned <see cref="ulong"/> value directly into the provided character span.</summary>
        /// <param name="value">The <see cref="ulong"/> numerical value to format.</param>
        /// <param name="destination">The <see cref="Span{T}"/> of characters to write the formatted value into.</param>
        /// <returns>The number of characters successfully written to the destination span.</returns>
        /// <exception cref="ArgumentException">Thrown when the destination span is too small to accommodate the formatted value.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToSpan(this ulong value, Span<char> destination) =>
            !value.TryFormat(destination, out int charsWritten)
                ? throw ThrowHelper.ArgumentInsufficientBufferFromParam(nameof(destination))
                : charsWritten;

        /// <summary>Formats the specified nullable <see cref="long"/> value directly into the provided character span.</summary>
        /// <param name="value">The nullable <see cref="long"/> numerical value to format. If <see langword="null"/>, no formatting occurs.</param>
        /// <param name="destination">The <see cref="Span{T}"/> of characters to write the formatted value into.</param>
        /// <returns>The number of characters written to the destination span, or <c>0</c> if the value is <see langword="null"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToSpan(this long? value, Span<char> destination) => value?.ToSpan(destination) ?? 0;

        /// <summary>Formats the specified nullable unsigned <see cref="ulong"/> value directly into the provided character span.</summary>
        /// <param name="value">The nullable <see cref="ulong"/> numerical value to format. If <see langword="null"/>, no formatting occurs.</param>
        /// <param name="destination">The <see cref="Span{T}"/> of characters to write the formatted value into.</param>
        /// <returns>The number of characters written to the destination span, or <c>0</c> if the value is <see langword="null"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToSpan(this ulong? value, Span<char> destination) => value?.ToSpan(destination) ?? 0;
    }
}