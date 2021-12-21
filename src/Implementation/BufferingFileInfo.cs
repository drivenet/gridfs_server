using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.IO;

namespace GridFSServer.Implementation
{
    internal sealed class BufferingFileInfo : Components.IFileInfo
    {
        private readonly Components.IFileInfo _inner;
        private readonly RecyclableMemoryStreamManager _streamManager;

        public BufferingFileInfo(Components.IFileInfo inner, RecyclableMemoryStreamManager streamManager)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        }

        public string Filename => _inner.Filename;

        public async Task<bool> CopyTo(Stream stream, CancellationToken cancellationToken)
        {
            using var buffer = _streamManager.GetStream();
            if (!await _inner.CopyTo(buffer, cancellationToken))
            {
                return false;
            }

            await buffer.CopyToAsync(stream, cancellationToken);
            return true;
        }

        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }
}
