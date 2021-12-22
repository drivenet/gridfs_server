using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSServer.Components;

internal interface IFileInfo : IAsyncDisposable
{
    string Filename { get; }

    Task<bool> CopyTo(Stream stream, CancellationToken cancellationToken);
}
