using System.Threading;
using System.Threading.Tasks;

namespace GridFSServer.Implementation
{
    internal sealed class EmptyFileSource : Components.IFileSource
    {
        private EmptyFileSource()
        {
        }

        public static EmptyFileSource Value { get; } = new EmptyFileSource();

        public Task<Components.IFileInfo?> FetchFile(string filename, CancellationToken cancellationToken)
            => Task.FromResult((Components.IFileInfo?)null);
    }
}
