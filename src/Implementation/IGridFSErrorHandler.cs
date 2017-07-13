using System;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSServer.Implementation
{
    internal interface IGridFSErrorHandler
    {
        Task<TResult> HandleErrors<TResult>(Func<Task<TResult>> action, string filename, CancellationToken cancellationToken);
    }
}
