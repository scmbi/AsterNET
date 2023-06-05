using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace AsterNET.FastAGI
{
	/// <summary>
	/// The BaseAGIScript provides some convinience methods to make it easier to
	/// write custom AGIScripts.<br/>
	/// Just extend it by your own AGIScripts.
	/// </summary>
	public abstract class AGIScript : IDisposable
	{
        ~AGIScript()
        {            
            Dispose(disposing: false);
        }

        protected bool disposed = false;

        /// <summary>
        /// Default sincronous executing starting point
        /// </summary>
        protected virtual void Execute(AGIRequest request, AGIChannel channel)
        {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Default asyncronous executing starting point
		/// </summary>
		public virtual async ValueTask ExecuteAsync(AGIRequest request, AGIChannel channel, CancellationToken cancellationToken)
            => await Task.Run(() => Execute(request, channel));

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // Note disposing has been done.
                disposed = true;
            }
        }
    }
}
