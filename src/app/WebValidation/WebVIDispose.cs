using System;

namespace WebValidation
{
    public partial class WebV : IDisposable
    {
        private bool disposed = false;

        // iDisposable::Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_client != null)
                {
                    _client.Dispose();
                }
                if (_config != null)
                {
                    _config.Dispose();
                }
            }

            // Free any unmanaged objects
            disposed = true;
        }
    }
}
