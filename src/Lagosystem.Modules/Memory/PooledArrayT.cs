namespace Lagosystem.Modules.Memory;
using System.Buffers;
using System.Runtime.CompilerServices;
using Lagosystem.Modules.Helpers;

// TODO: Document the class and methods

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class PooledArray<T> : IDisposable
{
    private T[]? _buffer;
    private readonly ArrayPool<T> _pool;
    private readonly bool _clearArray;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="minimumLength"></param>
    /// <param name="clearArray"></param>
    /// <param name="pool"></param>
    public PooledArray(int minimumLength, bool clearArray = false, ArrayPool<T>? pool = null)
    {
        ThrowHelper.ThrowIfNegativeValue(minimumLength, nameof(minimumLength));
        Length = minimumLength;
        _clearArray = clearArray;
        _pool = pool ?? ArrayPool<T>.Shared;
        if (minimumLength > 0) _buffer = _pool.Rent(minimumLength);
    }

    /// <summary>
    /// 
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// 
    /// </summary>
    public int Capacity => _buffer?.Length ?? 0;

    /// <summary>
    /// 
    /// </summary>
    public bool IsDisposed => _buffer is null && Length > 0;

    /// <summary>
    /// 
    /// </summary>
    public T[] GetAsArray
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return _buffer ?? Array.Empty<T>();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public Span<T> GetAsSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return _buffer is null ? Span<T>.Empty : _buffer.AsSpan(0, Length);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public Memory<T> GetAsMemory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return _buffer?.AsMemory(0, Length) ?? Memory<T>.Empty;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan() => GetAsSpan;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int start) => GetAsSpan[start..];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int start, int length) => GetAsSpan.Slice(start, length);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<T> AsMemory() => GetAsMemory;

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        T[]? buffer = Interlocked.Exchange(ref _buffer, null);
        if (buffer is null) return;
        _pool.Return(buffer, _clearArray);
    }
    
    private void ThrowIfDisposed()
    {
        if (_buffer is null && Length > 0)
            throw new ObjectDisposedException(nameof(PooledArray<T>));
    }
}