using System;

namespace Fireteams.Common
{
    public abstract class DisposableBase : IDisposable
    {
        protected bool Disposed { get; private set; }

        ~DisposableBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Disposed = true;
        }
    }
}
