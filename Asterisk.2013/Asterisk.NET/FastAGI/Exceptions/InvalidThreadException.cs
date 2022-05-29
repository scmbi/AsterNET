using System;
namespace AsterNET.FastAGI
{
	/// <summary>
	/// The AGIHangupException is thrown if the channel has been hang up while processing the AGIRequest.
	/// </summary>
	public class InvalidThreadException : AGIException
	{
		public InvalidThreadException()
			: base("Trying to send command from an invalid thread")
		{
		}
	}
}