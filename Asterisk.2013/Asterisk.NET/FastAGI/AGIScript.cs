using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsterNET.FastAGI
{
	/// <summary>
	/// The BaseAGIScript provides some convinience methods to make it easier to
	/// write custom AGIScripts.<br/>
	/// Just extend it by your own AGIScripts.
	/// </summary>
	public abstract class AGIScript
	{		
		/// <summary>
		/// Default sincronous executing starting point
		/// </summary>
		/// <param name="request"></param>
		/// <param name="channel"></param>
		/// <exception cref="NotImplementedException"></exception>
		protected virtual void Execute(AGIRequest request, AGIChannel channel)
        {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Default asyncronous executing starting point
		/// </summary>
		/// <param name="request"></param>
		/// <param name="channel"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public virtual Task ExecuteAsync(AGIRequest request, AGIChannel channel, CancellationToken cancellationToken)
        {
			return Task.Run(() => Execute(request, channel), cancellationToken);
        }
    }
}
