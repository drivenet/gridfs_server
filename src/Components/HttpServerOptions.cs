namespace GridFSServer.Components
{
    internal sealed class HttpServerOptions
    {
        private string _cacheControl;

        public string CacheControl
        {
            get => _cacheControl;
            set
            {
                var cacheControl = value?.Trim();
                if (value != null && cacheControl.Length == 0)
                {
                    cacheControl = null;
                }

                _cacheControl = cacheControl;
            }
        }
    }
}
