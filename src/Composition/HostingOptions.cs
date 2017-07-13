namespace GridFSServer.Composition
{
    internal sealed class HostingOptions
    {
        private string _listen;

        public string Listen
        {
            get => _listen;
            set
            {
                var listen = value?.Trim();
                if (value != null && listen.Length == 0)
                {
                    listen = null;
                }

                _listen = listen;
            }
        }

        public ushort MaxConcurrentConnections { get; set; }
    }
}
