using System.Buffers;
using Lagosystem.Modules.Extensions;
using Lagosystem.Modules.Memory;

namespace Lagosystem.Modules.Tests;

public sealed class MemoryAndSettingsTests : ModuleTestScope
{
    [Fact]
    public void PooledArrayExposesOnlyRequestedLengthAndReturnsOnce()
    {
        TrackingPool<int> pool = new TrackingPool<int>(16);
        PooledArray<int> buffer = new PooledArray<int>(3, clearArray: true, pool: pool);
        Assert.Equal(3, buffer.Length);
        Assert.Equal(16, buffer.Capacity);
        Assert.Equal(3, buffer.AsSpan().Length);
        Assert.Equal(3, buffer.AsMemory().Length);
        buffer.AsSpan()[1] = 42;
        Assert.Equal(42, buffer.GetAsArray[1]);
        Assert.Equal(42, buffer.AsSpan(1, 1)[0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => { buffer.AsSpan(3, 1); });

        buffer.Dispose();
        buffer.Dispose();

        Assert.Equal(1, pool.RentCount);
        Assert.Equal(1, pool.ReturnCount);
        Assert.True(pool.ClearRequested);
        Assert.True(buffer.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => { buffer.AsSpan(); });
        Assert.Throws<ObjectDisposedException>(() => { buffer.AsMemory(); });
        Assert.Throws<ObjectDisposedException>(() => { _ = buffer.GetAsArray; });
    }

    [Fact]
    public void EmptyPooledArrayDoesNotRent()
    {
        TrackingPool<int> pool = new TrackingPool<int>(16);
        using PooledArray<int> buffer = new PooledArray<int>(0, pool: pool);
        Assert.Empty(buffer.GetAsArray);
        Assert.True(buffer.AsSpan().IsEmpty);
        buffer.Dispose();
        Assert.Equal(0, pool.RentCount);
        Assert.Equal(0, pool.ReturnCount);
        Assert.Throws<ArgumentException>(() => new PooledArray<int>(-1));
    }

    [Fact]
    public void NumericSpansHandleFullIntegerRangesAndNull()
    {
        Span<char> destination = stackalloc char[20];
        int count = long.MinValue.ToSpan(destination);
        Assert.Equal("-9223372036854775808", new string(destination[..count]));
        count = ulong.MaxValue.ToSpan(destination);
        Assert.Equal("18446744073709551615", new string(destination[..count]));
        long? missing = null;
        destination.Fill('x');
        Assert.Equal(0, missing.ToSpan(destination));
        Assert.Equal('x', destination[0]);
        Assert.Throws<ArgumentException>(() => 123L.ToSpan(new char[2]));
    }

    [Fact]
    public void BinaryUnitsKeepFractionalValuesAndRoundStreamCapacityUp()
    {
        Assert.Equal(1536m, Settings.GetDataUnit<decimal>(1.5m, DataUnit.Kilobyte, DataUnit.Byte));
        Assert.Equal(1.5m, Settings.GetDataUnit<decimal>(1536m, DataUnit.Byte, DataUnit.Kilobyte));
        Assert.Equal(2, Settings.GetDataUnitForStream(1.1m, DataUnit.Byte));
        Assert.Equal(0, Settings.GetDataUnitForStream(0m, DataUnit.Megabyte));
        Assert.Equal(1, Settings.GetDataUnit<int>(1.9m, DataUnit.Byte, DataUnit.Byte));
        Assert.Throws<ArgumentOutOfRangeException>(() => Settings.GetDataUnitForStream(-1m, DataUnit.Byte));
        Assert.Throws<OverflowException>(() => Settings.GetDataUnitForStream(int.MaxValue, DataUnit.Kilobyte));
        Assert.Throws<OverflowException>(() => Settings.GetDataUnit<byte>(256m, DataUnit.Byte, DataUnit.Byte));
        Assert.Throws<NotSupportedException>(() => Settings.GetDataUnit<bool>(1m, DataUnit.Byte, DataUnit.Byte));
    }

    [Theory]
    [InlineData(126)]
    [InlineData(1025)]
    public void SpanBufferRejectsOutOfRangeValues(short size)
    {
        short original = Settings.DefaultSpanBufferSize;
        Assert.Throws<ArgumentOutOfRangeException>(() => Settings.DefaultSpanBufferSize = size);
        Assert.Equal(original, Settings.DefaultSpanBufferSize);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(2097153)]
    public void StreamBufferRejectsOutOfRangeValues(int size)
    {
        int original = Settings.DefaultStreamBufferSize;
        Assert.Throws<ArgumentOutOfRangeException>(() => Settings.DefaultStreamBufferSize = size);
        Assert.Equal(original, Settings.DefaultStreamBufferSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(209715201)]
    public void Base64LimitRejectsOutOfRangeValues(int size)
    {
        int original = Settings.MaxBase64StreamSize;
        Assert.Throws<ArgumentOutOfRangeException>(() => Settings.MaxBase64StreamSize = size);
        Assert.Equal(original, Settings.MaxBase64StreamSize);
    }

    private sealed class TrackingPool<T> : ArrayPool<T>
    {
        private readonly T[] _buffer;
        public int RentCount { get; private set; }
        public int ReturnCount { get; private set; }
        public bool ClearRequested { get; private set; }

        public TrackingPool(int capacity) => _buffer = new T[capacity];

        public override T[] Rent(int minimumLength)
        {
            Assert.InRange(minimumLength, 0, _buffer.Length);
            RentCount++;
            return _buffer;
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            Assert.Same(_buffer, array);
            ReturnCount++;
            ClearRequested = clearArray;
            if (clearArray) Array.Clear(array);
        }
    }
}
