using Lagosystem.Modules.Conversions;

namespace Lagosystem.Modules.Tests;

public sealed class DiagnosticsTests : ModuleTestScope
{
    [Fact]
    public void OperationalFailureIsReportedOnceAndOriginalExceptionSurvives()
    {
        IOException failure = new IOException("falha de leitura simulada");
        int calls = 0;
        Exception? reported = null;
        string? method = null;
        string? className = null;
        Diagnostics.SetErrorHandler((string sourceMethod, string sourceClass, Exception? exception, string? message) =>
        {
            calls++;
            reported = exception;
            method = sourceMethod;
            className = sourceClass;
        });
        using FailingStream stream = new FailingStream(failure);
        Assert.Same(failure, Assert.Throws<IOException>(() => Conversion.ToBase64(stream)));
        Assert.Same(failure, Assert.Throws<IOException>(() => Conversion.ToBase64(stream)));
        Assert.Equal(1, calls);
        Assert.Same(failure, reported);
        Assert.Equal(nameof(Conversion.ToBase64), method);
        Assert.Equal(nameof(Conversion), className);
    }

    [Fact]
    public void FailingHandlerDoesNotReplaceOriginalOperationalFailure()
    {
        IOException failure = new IOException("erro original");
        Diagnostics.SetErrorHandler((string method, string className, Exception? exception, string? message) =>
            throw new InvalidOperationException("erro do observador"));
        using FailingStream stream = new FailingStream(failure);
        Assert.Same(failure, Assert.Throws<IOException>(() => Conversion.ToBase64(stream)));
    }

    [Fact]
    public void ClearRemovesHandlerAndProgrammerErrorsAreNotReported()
    {
        int calls = 0;
        Diagnostics.SetErrorHandler((string method, string className, Exception? exception, string? message) => calls++);
        Assert.Throws<ArgumentNullException>(() => Conversion.FromBase64("YQ==", null!));
        Assert.Equal(0, calls);
        Diagnostics.Clear();
        using FailingStream stream = new FailingStream(new IOException("outro erro"));
        Assert.Throws<IOException>(() => Conversion.ToBase64(stream));
        Assert.Equal(0, calls);
    }

    private sealed class FailingStream : MemoryStream
    {
        private readonly IOException _failure;
        public FailingStream(IOException failure) => _failure = failure;
        public override bool CanSeek => false;
        public override int Read(byte[] buffer, int offset, int count) => throw _failure;
    }
}
