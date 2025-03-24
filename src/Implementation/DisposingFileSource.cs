using System;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSServer.Implementation;

internal sealed class DisposingFileSource : Components.IFileSource, IDisposable
{
    private readonly Components.IFileSource _inner;
    private readonly IDisposable _disposable;

    public DisposingFileSource(Components.IFileSource inner, IDisposable disposable)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _disposable = disposable ?? throw new ArgumentNullException(nameof(disposable));
    }

    public void Dispose() => _disposable.Dispose();

    public Task<Components.IFileInfo?> FetchFile(string filename, CancellationToken cancellationToken) => _inner.FetchFile(filename, cancellationToken);
}
