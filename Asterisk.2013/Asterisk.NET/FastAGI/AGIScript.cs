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
        /// <summary>
        ///     Invokes dispose for ensure the closure at inheritances classes
        /// </summary>
        ~AGIScript() => Dispose();
                
        /// <summary>
		///     Default asyncronous executing starting point
		/// </summary>
        public virtual ValueTask ExecuteAsync(AGIScriptParameters parameters, CancellationToken cancellationToken)
            => ExecuteAsync(parameters.Request, parameters.Channel, cancellationToken);

        /// <summary>
        ///     You should prefer to override <see cref="AGIScript.ExecuteAsync(AGIScriptParameters, CancellationToken)"/>
        /// </summary>
        public virtual async ValueTask ExecuteAsync(AGIRequest request, AGIChannel channel, CancellationToken cancellationToken)
            => await Task.Run(() => Execute(request, channel));

        /// <summary>
        ///     Default sincronous executing starting point
        /// </summary>
        protected virtual void Execute(AGIRequest request, AGIChannel channel)
            => throw new NotImplementedException();
        
        #region IMPLEMENT IDISPOSABLE

        protected bool disposed = false;

        public void Dispose()
        {
            // Check to see if Dispose has already been called.
            if (!disposed)
            {
                // Note disposing has been done.
                disposed = true;
                Dispose(true);

                GC.SuppressFinalize(this);
            }            
        }

        protected virtual void Dispose (bool disposing) { }

        #endregion
    }
}
