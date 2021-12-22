using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSServer.Implementation;

internal sealed class CompositeFileSource : Components.IFileSource
{
    private readonly IEnumerable<Components.IFileSource> _sources;

    public CompositeFileSource(IEnumerable<Components.IFileSource> sources)
    {
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
    }

    public async Task<Components.IFileInfo?> FetchFile(string filename, CancellationToken cancellationToken)
    {
        foreach (var source in _sources)
        {
            var fileInfo = await source.FetchFile(filename, cancellationToken);
            if (fileInfo is object)
            {
                return fileInfo;
            }
        }

        return null;
    }
}
