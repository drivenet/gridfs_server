using System.Threading;
using System.Threading.Tasks;

namespace GridFSServer.Components
{
    internal interface IFileSource
    {
        Task<IFileInfo> FetchFileAsync(string filename, CancellationToken cancellationToken);
    }
}
