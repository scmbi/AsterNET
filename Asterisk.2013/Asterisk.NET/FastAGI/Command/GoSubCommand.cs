using System;
namespace AsterNET.FastAGI.Command
{
    /// <summary>
    /// Cause the channel to execute the specified dialplan subroutine. <br/>
    /// Returning to the dialplan with execution of a Return().
    /// </summary>
    /// <remarks>GOSUB CONTEXT EXTENSION PRIORITY OPTIONAL-ARGUMENT</remarks>
    public class GoSubCommand : AGICommand
	{
		public string Context { get; }
        public string Extension { get; }
        public string Priority { get; }
        public string? Argument { get; }

        /// <summary>
        /// Creates a new ExecCommand.
        /// </summary>
        /// <param name="application">the name of the application to execute.</param>
        public GoSubCommand(string context, string extension, string priority, string? argument = null)
		{
			this.Context = context;
            this.Extension = extension;
			this.Priority = priority;
			this.Argument = argument;
        }		
		
		public override string BuildCommand()
		{
            var argument = Argument != null ? EscapeAndQuote(Argument) : string.Empty;
            return $"GOSUB {EscapeAndQuote(Context)} {EscapeAndQuote(Extension)} {EscapeAndQuote(Priority)} {argument}";
		}
	}
}