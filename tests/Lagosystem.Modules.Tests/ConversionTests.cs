using System.Text;
using Lagosystem.Modules.Conversions;
using Lagosystem.Modules.Validations;

namespace Lagosystem.Modules.Tests;

public sealed class ConversionTests : ModuleTestScope
{
    [Theory]
    [InlineData(true, 1)]
    [InlineData(true, 3)]
    [InlineData(false, 1)]
    [InlineData(false, 3)]
    public void ToBase64ReadsAllChunksFromCurrentPosition(bool canSeek, int chunkSize)
    {
        byte[] input = Encoding.UTF8.GetBytes("prefixo:café 👩‍💻 Lagosystem");
        using ChunkedStream stream = new ChunkedStream(input, canSeek, chunkSize);
        stream.Position = 8;
        string expected = Convert.ToBase64String(input, 8, input.Length - 8);

        Assert.Equal(expected, Conversion.ToBase64(stream));
        Assert.Equal(input.Length, stream.Position);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(true, 3)]
    [InlineData(false, 1)]
    [InlineData(false, 3)]
    public async Task ToBase64AsyncReadsAllChunksFromCurrentPosition(bool canSeek, int chunkSize)
    {
        byte[] input = Encoding.UTF8.GetBytes("prefixo:café 👩‍💻 Lagosystem");
        using ChunkedStream stream = new ChunkedStream(input, canSeek, chunkSize);
        stream.Position = 8;

        string actual = await Conversion.ToBase64Async(stream);

        Assert.Equal(Convert.ToBase64String(input, 8, input.Length - 8), actual);
        Assert.Equal(input.Length, stream.Position);
    }

    [Fact]
    public async Task Base64RoundTripsBytesThroughAllDestinationOverloads()
    {
        byte[] input = new byte[] { 0, 1, 127, 128, 255 };
        string encoded = Conversion.ToBase64(input);
        Assert.Equal("AAF/gP8=", encoded);
        Assert.Equal(input, Conversion.FromBase64(encoded));
        Assert.Equal(input, Conversion.FromBase64("AA F/\r\ngP8="));
        using MemoryStream destination = new MemoryStream();
        Conversion.FromBase64(encoded, destination);
        Assert.Equal(input, destination.ToArray());
        destination.SetLength(0);
        await Conversion.FromBase64Async(encoded, destination);
        Assert.Equal(input, destination.ToArray());
    }

    [Fact]
    public async Task EmptyInputHasNoPayload()
    {
        Assert.Equal(string.Empty, Conversion.ToBase64((byte[]?)null));
        Assert.Equal(string.Empty, Conversion.ToBase64((Stream?)null));
        Assert.Equal(string.Empty, await Conversion.ToBase64Async(null));
        Assert.Empty(Conversion.FromBase64(null));
        using MemoryStream stream = new MemoryStream();
        Assert.Equal(string.Empty, Conversion.ToBase64(stream));
        Assert.Throws<ArgumentNullException>(() => Conversion.FromBase64("YQ==", null!));
    }

    [Theory]
    [InlineData("YQ==", true)]
    [InlineData("YWJj", true)]
    [InlineData("Y Q==\r\n", true)]
    [InlineData("YQ=", false)]
    [InlineData("Y===", false)]
    [InlineData("Y=Q=", false)]
    [InlineData("YQ?=", false)]
    [InlineData("", false)]
    public void Base64ValidationChecksAlphabetAndPadding(string input, bool expected)
    {
        Assert.Equal(expected, Validate.IsValidBase64(input));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ToBase64EnforcesSizeLimit(bool canSeek)
    {
        Settings.MaxBase64StreamSize = 3;
        using ChunkedStream atLimit = new ChunkedStream(new byte[3], canSeek, 1);
        using ChunkedStream overLimit = new ChunkedStream(new byte[4], canSeek, 1);
        Assert.Equal("AAAA", Conversion.ToBase64(atLimit));
        Assert.ThrowsAny<ArgumentException>(() => Conversion.ToBase64(overLimit));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ToBase64AsyncHonorsCancellation(bool canSeek)
    {
        using ChunkedStream stream = new ChunkedStream(new byte[10], canSeek, 1);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Conversion.ToBase64Async(stream, cancellation.Token));
    }

    private sealed class ChunkedStream : MemoryStream
    {
        private readonly bool _canSeek;
        private readonly int _chunkSize;

        public ChunkedStream(byte[] bytes, bool canSeek, int chunkSize) : base(bytes)
        {
            _canSeek = canSeek;
            _chunkSize = chunkSize;
        }

        public override bool CanSeek => _canSeek;

        public override int Read(byte[] buffer, int offset, int count) =>
            base.Read(buffer, offset, Math.Min(count, _chunkSize));

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<int>(cancellationToken);
            return Task.FromResult(Read(buffer, offset, count));
        }
    }
}
