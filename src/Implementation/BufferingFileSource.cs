using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.IO;

namespace GridFSServer.Implementation
{
    internal sealed class BufferingFileSource : Components.IFileSource
    {
        private readonly Components.IFileSource _inner;
        private readonly RecyclableMemoryStreamManager _streamManager;

        public BufferingFileSource(Components.IFileSource inner, RecyclableMemoryStreamManager streamManager)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        }

        public async Task<Components.IFileInfo?> FetchFile(string filename, CancellationToken cancellationToken)
        {
            var fileInfo = await _inner.FetchFile(filename, cancellationToken);
            if (fileInfo is null)
            {
                return null;
            }

            fileInfo = new BufferingFileInfo(fileInfo, _streamManager);
            return fileInfo;
        }
    }
}
