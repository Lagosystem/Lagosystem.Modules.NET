using System.Globalization;
using Lagosystem.Modules;

// Settings e callbacks de diagnóstico pertencem ao processo inteiro.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Lagosystem.Modules.Tests;

public abstract class ModuleTestScope : IDisposable
{
    private readonly short _spanBufferSize = Settings.DefaultSpanBufferSize;
    private readonly int _streamBufferSize = Settings.DefaultStreamBufferSize;
    private readonly int _maxBase64Size = Settings.MaxBase64StreamSize;
    private readonly bool _throwInDebugMode = Diagnostics.ThrowInDebugMode;
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

    protected ModuleTestScope()
    {
        Diagnostics.Clear();
        Diagnostics.ThrowInDebugMode = false;
    }

    public void Dispose()
    {
        Settings.DefaultSpanBufferSize = _spanBufferSize;
        Settings.DefaultStreamBufferSize = _streamBufferSize;
        Settings.MaxBase64StreamSize = _maxBase64Size;
        Diagnostics.Clear();
        Diagnostics.ThrowInDebugMode = _throwInDebugMode;
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
        GC.SuppressFinalize(this);
    }
}
