
namespace Lagosystem.Modules;

using Lagosystem.Modules.Helpers;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;
using System.ComponentModel;

/// <summary>
/// Provides global configuration settings for the Lagosystem library. 
/// This class allows users to define internal behaviors, such as the maximum allocation limits for 
/// <see langword="stackalloc"/> and read/write buffer sizes for <see cref="System.IO.Stream"/> operations.
/// </summary>
public static class Settings
{
    private static short _defaultSpanBufferSize = 512;
    private static int _defaultStreamBufferSize = 80 * (int)DataUnit.Kilobyte;
    private static int _maxBase64StreamSize = 50 * (int)DataUnit.Megabyte;
    private static int _partitionInitialCapacity = 0;

    /// <summary>
    /// Gets or sets the maximum allowed size (in bytes) for a stream to be loaded into memory for Base64 string
    /// conversion. The default is 50 MB.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the provided value is less than 1 byte or greater than 200 Megabytes.</exception>
    [Range(1 * (int)DataUnit.Byte, 200 * (int)DataUnit.Megabyte)]
    [ValueRange(1 * (int)DataUnit.Byte, 200 * (int)DataUnit.Megabyte)]
    public static int MaxBase64StreamSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref _maxBase64StreamSize);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowHelper.ThrowIfLessThan(value, 1 * (int)DataUnit.Byte);
            ThrowHelper.ThrowIfGreaterThan(value, 200 * (int)DataUnit.Megabyte);
            Volatile.Write(ref _maxBase64StreamSize, value);
        }
    }

    /// <summary>
    /// Converts the specified digital storage size to its equivalent value in bytes, suitable for use with stream-related settings.
    /// </summary>
    /// <param name="requestedSize">The requested size expressed in <paramref name="dataUnit"/>.</param>
    /// <param name="dataUnit">The unit in which <paramref name="requestedSize"/> is expressed.</param>
    /// <returns>The equivalent size in bytes, rounded up to the nearest whole byte.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="requestedSize"/> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the resulting number of bytes exceeds the range supported by <see cref="int"/>.
    /// </exception>
    /// <remarks>
    /// For example, converting <c>19.8</c> <see cref="DataUnit.Megabyte"/> to bytes returns <c>20,761,805</c> bytes
    /// after rounding the fractional result up to the nearest whole byte. The resulting value can then be used as
    /// the buffer size, expressed in bytes.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetDataUnitForStream(decimal requestedSize, DataUnit dataUnit)
    {
        ThrowHelper.ThrowIfLessThan(requestedSize, 0, nameof(requestedSize));
        decimal bytes = GetDataUnit<decimal>(requestedSize, dataUnit, DataUnit.Byte);
        return checked((int)Math.Ceiling(bytes));
    }

    /// <summary>
    /// Converts a digital storage value from one <see cref="DataUnit"/> to another
    /// and returns the result as the specified numeric type. 
    /// </summary> 
    /// <typeparam name="TResult"> 
    /// The numeric type used to return the converted value. 
    /// Supported types include <see cref="sbyte"/>, <see cref="byte"/>, 
    /// <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, 
    /// <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, 
    /// <see cref="float"/>, <see cref="double"/>, and <see cref="decimal"/>. 
    /// </typeparam> 
    /// <param name="dataUnitIn">The numeric value to be converted, expressed in <paramref name="dataUnitModeIn"/>.</param> 
    /// <param name="dataUnitModeIn">The unit in which <paramref name="dataUnitIn"/> is currently expressed.</param> 
    /// <param name="dataUnitModeResult">The unit to which <paramref name="dataUnitIn"/> will be converted.</param> 
    /// <returns>The converted storage value represented as <typeparamref name="TResult"/>.</returns> 
    /// <remarks> 
    /// The conversion is performed by first converting the input value to its byte-based 
    /// equivalent using the multiplier represented by <paramref name="dataUnitModeIn"/>, 
    /// and then dividing it by the multiplier represented by 
    /// <paramref name="dataUnitModeResult"/>. 
    /// <para>For example, converting <c>2</c> from <see cref="DataUnit.Gigabyte"/> to <see cref="DataUnit.Megabyte"/> returns <c>2048</c>.</para> 
    /// <para> 
    /// When <typeparamref name="TResult"/> is an integral numeric type, any fractional 
    /// portion of the converted value is discarded during the numeric conversion. 
    /// Use <see cref="float"/>, <see cref="double"/>, or <see cref="decimal"/> when 
    /// fractional results must be preserved. 
    /// </para> 
    /// </remarks> 
    /// <exception cref="OverflowException"> 
    /// Thrown when the converted value is outside the valid range of 
    /// <typeparamref name="TResult"/>. 
    /// </exception> 
    /// <exception cref="NotSupportedException"> 
    /// Thrown when <typeparamref name="TResult"/> is not one of the supported numeric types. 
    /// </exception>
    public static TResult GetDataUnit<TResult>(decimal dataUnitIn, DataUnit dataUnitModeIn, DataUnit dataUnitModeResult)
        where TResult : struct, IConvertible
    {
        decimal result = dataUnitIn * (long)dataUnitModeIn / (long)dataUnitModeResult;

        object convertedResult = Type.GetTypeCode(typeof(TResult)) switch
        {
            TypeCode.SByte   => checked((sbyte)result),
            TypeCode.Byte    => checked((byte)result),
            TypeCode.Int16   => checked((short)result),
            TypeCode.UInt16  => checked((ushort)result),
            TypeCode.Int32   => checked((int)result),
            TypeCode.UInt32  => checked((uint)result),
            TypeCode.Int64   => checked((long)result),
            TypeCode.UInt64  => checked((ulong)result),
            TypeCode.Single  => checked((float)result),
            TypeCode.Double  => checked((double)result),
            TypeCode.Decimal => result,

            _ => throw new NotSupportedException($"The type '{typeof(TResult).Name}' is not supported.")
        };

        return (TResult)convertedResult;
    }

    /// <summary>Gets or sets the default buffer size used for <see langword="stackalloc"/> operations within the library.</summary>
    /// <remarks>
    /// This value defines the threshold for stack allocations. If a required buffer exceeds this defined size, 
    /// the library methods will typically fall back to using heap allocation or array pools to prevent stack overflow exceptions.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the provided value is less than or equal to 127 bytes (<see cref="sbyte.MaxValue"/>), 
    /// or greater than 1 Kilobyte.
    /// </exception>
    [ValueRange(sbyte.MaxValue * (int)DataUnit.Byte, 1 * (short)DataUnit.Kilobyte)]
    [Range(sbyte.MaxValue * (int)DataUnit.Byte, 1 * (short)DataUnit.Kilobyte)]
    public static short DefaultSpanBufferSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref _defaultSpanBufferSize);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowHelper.ThrowIfLessThan(value, sbyte.MaxValue * (int)DataUnit.Byte);
            ThrowHelper.ThrowIfGreaterThan(value, 1 * (int)DataUnit.Kilobyte);
            Volatile.Write(ref _defaultSpanBufferSize, value);
        }
    }

    /// <summary>
    /// Gets or sets the default buffer size used for reading and writing operations across various 
    /// <see cref="System.IO.Stream"/> objects (e.g., MemoryStream, FileStream, NetworkStream).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the provided value is less than 16 bytes, or greater than 2 Megabytes (2,097,152 bytes).
    /// </exception>
    [ValueRange(16 * (int)DataUnit.Byte, 2 * (int)DataUnit.Megabyte)]
    [Range(16 * (int)DataUnit.Byte, 2 * (int)DataUnit.Megabyte)]
    public static int DefaultStreamBufferSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref _defaultStreamBufferSize);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowHelper.ThrowIfLessThan(value, 16 * (int)DataUnit.Byte);
            ThrowHelper.ThrowIfGreaterThan(value, 2 * (int)DataUnit.Megabyte /*1mb = 1.048.576*/);
            Volatile.Write(ref _defaultStreamBufferSize, value);
        }
    }
    

    /// <summary>Gets or sets the default initial capacity allocated for each partition (to use in methods in <see cref="Extensions.EnumerableExtensions"/>).</summary>
    [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("This prop will be use in future")]
    public static int PartitionInitialCapacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref _partitionInitialCapacity);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Volatile.Write(ref _partitionInitialCapacity, value);
        }
    }
}