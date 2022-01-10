namespace GridFSServer.Components;

internal sealed class HttpServerOptions
{
    public const uint DefaultMaxBufferedLength = 16U << 20;

    private string? _cacheControl;

    public string? CacheControl
    {
        get => _cacheControl;
        set
        {
            var cacheControl = value?.Trim();
            if (cacheControl is object && cacheControl.Length == 0)
            {
                cacheControl = null;
            }

            _cacheControl = cacheControl;
        }
    }

    public uint MaxBufferedLength { get; set; } = DefaultMaxBufferedLength;

    public bool LogRequests { get; set; }
}
