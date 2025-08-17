namespace OmniscanAdmin.Models
{
    public static class SystemConfig
    {
        private static bool _allowDisable = false;
        private static readonly object _lock = new object();

        public static bool AllowDisable
        {
            get
            {
                lock (_lock)
                {
                    return _allowDisable;
                }
            }
            set
            {
                lock (_lock)
                {
                    _allowDisable = value;
                }
            }
        }
    }
}
