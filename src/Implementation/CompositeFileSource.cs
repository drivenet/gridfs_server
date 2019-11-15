using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSServer.Implementation
{
    internal sealed class CompositeFileSource : Components.IFileSource
    {
        private readonly IEnumerable<Components.IFileSource> _sources;

        public CompositeFileSource(IEnumerable<Components.IFileSource> sources)
        {
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
        }

        public async Task<Components.IFileInfo> FetchFile(string filename, CancellationToken cancellationToken)
        {
            foreach (var source in _sources)
            {
                if (source == null)
                {
                    throw new InvalidDataException("Null file source encountered.");
                }

                var fileInfo = await source.FetchFile(filename, cancellationToken);
                if (fileInfo != null)
                {
                    return fileInfo;
                }
            }

            return null;
        }
    }
}
