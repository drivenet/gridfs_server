using System;

using Microsoft.AspNetCore.Http;

namespace GridFSServer.Implementation
{
    internal sealed class DefaultGridFSFileSourceResolver : Components.IFileSourceResolver
    {
        private readonly IMongoUrlResolver _urlResolver;

        private readonly IGridFSFileSourceResolver _fileSourceResolver;

        public DefaultGridFSFileSourceResolver(IMongoUrlResolver urlResolver, IGridFSFileSourceResolver fileSourceResolver)
        {
            _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));
            _fileSourceResolver = fileSourceResolver ?? throw new ArgumentNullException(nameof(fileSourceResolver));
        }

        public Components.IFileSource Resolve(HostString host)
        {
            var url = _urlResolver.Resolve(host);
            if (url == null)
            {
                return EmptyFileSource.Value;
            }

            return _fileSourceResolver.Resolve(url);
        }
    }
}
