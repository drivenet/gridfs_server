using System.Threading;
using System.Threading.Tasks;

namespace GridFSServer.Components
{
    internal interface IFileSource
    {
        Task<IFileInfo> FetchFile(string filename, CancellationToken cancellationToken);
    }
}
