// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace Amqp.Net.Protocol.Framing;

/// <summary>
/// Manages pooled byte buffers for frame encoding/decoding.
/// Uses ArrayPool for efficient memory reuse.
/// </summary>
public sealed class FrameBufferPool
{
    private readonly ArrayPool<byte> _pool;

    /// <summary>
    /// Creates a new frame buffer pool.
    /// </summary>
    /// <param name="pool">The underlying array pool.</param>
    /// <param name="maxFrameSize">Maximum frame size to support.</param>
    public FrameBufferPool(ArrayPool<byte> pool, int maxFrameSize)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        MaxFrameSize = maxFrameSize;
    }

    /// <summary>
    /// Default instance using shared array pool.
    /// </summary>
    public static FrameBufferPool Shared { get; } = new(ArrayPool<byte>.Shared, 1024 * 1024); // 1MB max

    /// <summary>
    /// Gets the maximum frame size supported by this pool.
    /// </summary>
    public int MaxFrameSize { get; }

    /// <summary>
    /// Rents a buffer of at least the specified size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] Rent(int minimumSize)
    {
        if (minimumSize > MaxFrameSize)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSize),
                $"Requested size {minimumSize} exceeds maximum frame size {MaxFrameSize}");
        }
        return _pool.Rent(minimumSize);
    }

    /// <summary>
    /// Returns a buffer to the pool.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(byte[] buffer, bool clearArray = false)
    {
        _pool.Return(buffer, clearArray);
    }

    /// <summary>
    /// Rents a buffer wrapped in a disposable handle.
    /// </summary>
    public RentedBuffer RentBuffer(int minimumSize) => new(this, Rent(minimumSize));
}

/// <summary>
/// A rented buffer that returns itself to the pool when disposed.
/// </summary>
public readonly struct RentedBuffer : IDisposable
{
    private readonly FrameBufferPool _pool;

    internal RentedBuffer(FrameBufferPool pool, byte[] buffer)
    {
        _pool = pool;
        Array = buffer;
    }

    /// <summary>
    /// Gets the underlying buffer.
    /// </summary>
    public byte[] Array { get; }

    /// <summary>
    /// Gets a span over the buffer.
    /// </summary>
    public Span<byte> Span => Array.AsSpan();

    /// <summary>
    /// Gets a memory over the buffer.
    /// </summary>
    public Memory<byte> Memory => Array.AsMemory();

    /// <summary>
    /// Returns the buffer to the pool.
    /// </summary>
    public void Dispose()
    {
        if (Array != null)
        {
            _pool.Return(Array);
        }
    }
}

/// <summary>
/// A writer that accumulates frame data into a pooled buffer.
/// </summary>
public sealed class FrameWriter : IDisposable
{
    private readonly FrameBufferPool _pool;
    private byte[] _buffer;
    private bool _disposed;

    /// <summary>
    /// Creates a new frame writer with the specified initial capacity.
    /// </summary>
    public FrameWriter(FrameBufferPool pool, int initialCapacity = 512)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        _buffer = pool.Rent(initialCapacity);
        Position = 0;
    }

    /// <summary>
    /// Gets the current write position.
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    /// Gets the current capacity.
    /// </summary>
    public int Capacity => _buffer.Length;

    /// <summary>
    /// Gets a span of the written data.
    /// </summary>
    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, Position);

    /// <summary>
    /// Gets a memory of the written data.
    /// </summary>
    public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, Position);

    /// <summary>
    /// Disposes the writer and returns the buffer to the pool.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _pool.Return(_buffer);
            _buffer = null!;
            _disposed = true;
        }
    }

    /// <summary>
    /// Gets a span for writing at the current position.
    /// </summary>
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(Position + Math.Max(sizeHint, 1));
        return _buffer.AsSpan(Position);
    }

    /// <summary>
    /// Advances the write position.
    /// </summary>
    public void Advance(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        Position += count;
    }

    /// <summary>
    /// Writes bytes to the buffer.
    /// </summary>
    public void Write(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(Position + data.Length);
        data.CopyTo(_buffer.AsSpan(Position));
        Position += data.Length;
    }

    /// <summary>
    /// Writes a single byte to the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        EnsureCapacity(Position + 1);
        _buffer[Position++] = value;
    }

    /// <summary>
    /// Resets the writer for reuse.
    /// </summary>
    public void Reset()
    {
        Position = 0;
    }

    /// <summary>
    /// Ensures the buffer has at least the specified capacity.
    /// </summary>
    private void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _buffer.Length)
        {
            return;
        }
        var newCapacity = Math.Max(_buffer.Length * 2, requiredCapacity);
        var newBuffer = _pool.Rent(newCapacity);
        _buffer.AsSpan(0, Position).CopyTo(newBuffer);
        _pool.Return(_buffer);
        _buffer = newBuffer;
    }
}
