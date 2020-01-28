using System;

namespace WebValidation
{
    // integration test for testing any REST API or web site
    public partial class Test : IDisposable
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
                _client.Dispose();
            }

            // Free any unmanaged objects
            disposed = true;
        }
    }
}
