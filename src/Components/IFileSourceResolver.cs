using Microsoft.AspNetCore.Http;

namespace GridFSServer.Components;

internal interface IFileSourceResolver
{
    IFileSource Resolve(HostString host);
}
