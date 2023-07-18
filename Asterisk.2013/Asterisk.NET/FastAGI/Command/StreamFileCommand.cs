using System;
namespace AsterNET.FastAGI.Command
{

    /// <summary>
    /// Plays the given file, allowing playback to be interrupted by the given digits, if any.<br/>
    /// If offset is provided then the audio will seek to sample offset before play starts.<br/>
    /// Returns 0 if playback completes without a digit being pressed, or the ASCII
    /// numerical value of the digit if one was pressed, or -1 on error or if the
    /// channel was disconnected. <br/>
    /// Remember, the file extension must not be included in the filename.
    /// </summary>
    /// <remarks>
    /// It sets the following channel variables upon completion: <br/>
	/// PLAYBACKSTATUS = SUCCESS | FAILED, 
    /// </remarks>
    public class StreamFileCommand : AGICommand
	{
		/// <summary>
		/// Get/Set the name of the file to stream.
		/// The name of the file to stream, must not include extension.
		/// </summary>
		public string File { get; set; }


		/// <summary>
		/// Get/Set the digits that allow the user to interrupt this command.
		/// </summary>
		public string EscapeDigits { get; set; }

		/// <summary>
		/// Get/Set the offset samples to skip before streaming.
		/// </summary>
		public int? Offset { get; set; }
				
		/// <summary>
		/// Creates a new StreamFileCommand, streaming from the given offset.
		/// </summary>
		/// <param name="file">the name of the file to stream, must not include extension.</param>
		/// <param name="escapeDigits">contains the digits that allow the user to interrupt this command.
		/// Maybe null if you don't want the user to interrupt.
		/// </param>
		/// <param name="offset">the offset samples to skip before streaming.</param>
		public StreamFileCommand(string file, string escapeDigits = "", int? offset = null)
		{
            File = file;
            EscapeDigits = escapeDigits;
            Offset = offset;
		}
		
		public override string BuildCommand()
		{
			// Main file
			var command = $"STREAM FILE {EscapeAndQuote(File)}";
			
			// Escaping
			command += $" {EscapeAndQuote(EscapeDigits)}";

			// Start Offset
			if (Offset != null)
				command += $" {Offset}";
            
            return command;
		}
	}
}