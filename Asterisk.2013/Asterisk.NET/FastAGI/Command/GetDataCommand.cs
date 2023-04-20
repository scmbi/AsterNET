using Sufficit.Asterisk;
using System;
using System.Threading;

namespace AsterNET.FastAGI.Command
{
	/// <summary>
	/// Stream the given file, and recieve DTMF data. The user may interrupt the streaming by starting to enter digits.<br/>
	/// Returns the digits recieved from the channel at the other end.<br/>
	/// Input ends when the timeout is reached, the maximum number of digits is read or the user presses #.
	/// </summary>
	public class GetDataCommand : AGICommand
	{
        /// <summary> The name of the file to stream. Must not include extension.</summary>
        public string? File { get; set; }

        /// <summary>
        /// Get/Set the timeout in milliseconds to wait for data. 0 means standard timeout value, -1 means "ludicrous time" (essentially never times out). <br />
        /// *this timeout is used in silence, only if the user stops to enter, every entry restarts the count
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary> The maximum number of digits to read.<br/>
        /// Must be in [1..1024].
        /// </summary>
        public int? MaxDigits { get; set; }

        /// <summary>
        /// Creates a new GetDataCommand with the given timeout and maxDigits.
        /// </summary>
        public GetDataCommand(string? file = null, int? timeout = null, int? maxdigits = null)
		{
			File = file;
			Timeout = timeout;
			MaxDigits = maxdigits;
        }
        				
		public override string BuildCommand()
		{
            // Main file
            var command = $"GET DATA {EscapeAndQuote(File)}";

            if (MaxDigits != null)
            {
                if (MaxDigits < 1 || MaxDigits > 1024)
                    throw new ArgumentException("MaxDigits must be in [1..1024]");

                command += $" {Timeout ?? Common.AGI_DEFAULT_TIMEOUT} {MaxDigits}";
            }
            else if (Timeout != null)
                command += $" {Timeout}";

            return command;
        }

    }
}