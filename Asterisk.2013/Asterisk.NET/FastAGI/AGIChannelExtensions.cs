using AsterNET.FastAGI.Command;
using AsterNET.IO;
using Sufficit.Asterisk;
using System;

namespace AsterNET.FastAGI
{
    /// <summary>
    ///     Default implementation of the AGIChannel interface.
    /// </summary>
    public static class AGIChannelExtensions
    {
		#region Answer()

		/// <summary>
		/// Answers the channel.
		/// </summary>
		public static void Answer(this AGIChannel source)
		{
			source.SendCommand(new AnswerCommand());
		}

		#endregion

		#region Hangup()

		/// <summary>
		/// Hangs the channel up.
		/// </summary>
		public static void Hangup(this AGIChannel source)
		{
			source.SendCommand(new HangupCommand());
		}

		#endregion

		#region SetAutoHangup

		/// <summary>
		/// Cause the channel to automatically hangup at the given number of seconds in the future.<br/>
		/// 0 disables the autohangup feature.
		/// </summary>
		public static void SetAutoHangup(this AGIChannel source, int time)
		{
			source.SendCommand(new SetAutoHangupCommand(time));
		}

		#endregion

		#region SetCallerId

		/// <summary>
		/// Sets the caller id on the current channel.<br/>
		/// The raw caller id to set, for example "John Doe&lt;1234&gt;".
		/// </summary>
		public static void SetCallerId(this AGIChannel source, string callerId)
		{
			source.SendCommand(new SetCallerIdCommand(callerId));
		}

		#endregion

		#region PlayMusicOnHold()

		/// <summary>
		/// Plays music on hold from the default music on hold class.
		/// </summary>
		public static void PlayMusicOnHold(this AGIChannel source)
		{
			source.SendCommand(new SetMusicOnCommand());
		}

        #endregion

        #region PlayMusicOnHold(string musicOnHoldClass)

        /// <summary>
        /// Plays music on hold from the given music on hold class.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="musicOnHoldClass">the music on hold class to play music from as configures in Asterisk's &lt;musiconhold.conf/code$gt;.</param>
        public static void PlayMusicOnHold(this AGIChannel source, string musicOnHoldClass)
		{
			source.SendCommand(new SetMusicOnCommand(musicOnHoldClass));
		}

		#endregion

		#region StopMusicOnHold()

		/// <summary>
		/// Stops playing music on hold.
		/// </summary>
		public static void StopMusicOnHold(this AGIChannel source)
		{
			source.SendCommand(new SetMusicOffCommand());
		}

		#endregion

		#region GetChannelStatus

		/// <summary>
		/// Returns the status of the channel.<br/>
		/// Return values:
		/// <ul>
		/// <li>0 Channel is down and available</li>
		/// <li>1 Channel is down, but reserved</li>
		/// <li>2 Channel is off hook</li>
		/// <li>3 Digits (or equivalent) have been dialed</li>
		/// <li>4 Line is ringing</li>
		/// <li>5 Remote end is ringing</li>
		/// <li>6 Line is up</li>
		/// <li>7 Line is busy</li>
		/// </ul>
		/// </summary>
		/// <returns> the status of the channel.
		/// </returns>
		public static int GetChannelStatus(this AGIChannel source)
		{
			AGIReply lastReply = source.SendCommand(new ChannelStatusCommand());
			return lastReply.ResultCode;
		}

        #endregion

        #region GetData(string file)

        /// <summary>
        /// Plays the given file and waits for the user to enter DTMF digits until he
        /// presses '#'. The user may interrupt the streaming by starting to enter
        /// digits.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="file">the name of the file to play</param>
        /// <returns> a String containing the DTMF the user entered</returns>
        public static string GetData(this AGIChannel source, string file)
		{
			var lastReply = source.SendCommand(new GetDataCommand(file));
			return lastReply.GetResult();
		}

        #endregion

        #region GetData(string file, int timeout)

        /// <summary>
        /// Plays the given file and waits for the user to enter DTMF digits until he
        /// presses '#' or the timeout occurs. The user may interrupt the streaming
        /// by starting to enter digits.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="file">the name of the file to play</param>
        /// <param name="timeout">the timeout in milliseconds to wait for user input.<br/>
        /// 0 means standard timeout value, -1 means "ludicrous time"
        /// (essentially never times out).</param>
        /// <returns> a String containing the DTMF the user entered</returns>
        public static string GetData(this AGIChannel source, string file, long timeout)
		{
			var lastReply = source.SendCommand(new GetDataCommand(file, timeout));
			return lastReply.GetResult();
		}

        #endregion

        #region GetData(string file, int timeout, int maxDigits)

        /// <summary>
        /// Plays the given file and waits for the user to enter DTMF digits until he
        /// presses '#' or the timeout occurs or the maximum number of digits has
        /// been entered. The user may interrupt the streaming by starting to enter
        /// digits.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="file">the name of the file to play</param>
        /// <param name="timeout">the timeout in milliseconds to wait for user input.<br/>
        /// 0 means standard timeout value, -1 means "ludicrous time"
        /// (essentially never times out).</param>
        /// <param name="maxDigits">the maximum number of digits the user is allowed to enter</param>
        /// <returns> a String containing the DTMF the user entered</returns>
        public static string GetData(this AGIChannel source, string file, long timeout, int maxDigits)
		{
			var lastReply = source.SendCommand(new GetDataCommand(file, timeout, maxDigits));
			return lastReply.GetResult();
		}

        #endregion

        #region GetOption(string file, string escapeDigits)

        /// <summary>
        /// Plays the given file, and waits for the user to press one of the given
        /// digits. If none of the esacpe digits is pressed while streaming the file
        /// it waits for the default timeout of 5 seconds still waiting for the user
        /// to press a digit.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="file">the name of the file to stream, must not include extension.</param>
        /// <param name="escapeDigits">contains the digits that the user is expected to press.</param>
        /// <returns> the DTMF digit pressed or 0x0 if none was pressed.</returns>
        public static char GetOption(this AGIChannel source, string file, string escapeDigits)
		{
			AGIReply lastReply = source.SendCommand(new GetOptionCommand(file, escapeDigits));
			return lastReply.ResultCodeAsChar;
		}
        #endregion

        #region GetOption(string file, string escapeDigits, int timeout)

        /// <summary>
        /// Plays the given file, and waits for the user to press one of the given
        /// digits. If none of the esacpe digits is pressed while streaming the file
        /// it waits for the specified timeout still waiting for the user to press a
        /// digit.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="file">the name of the file to stream, must not include extension.</param>
        /// <param name="escapeDigits">contains the digits that the user is expected to press.</param>
        /// <param name="timeout">the timeout in seconds to wait if none of the defined esacpe digits was presses while streaming.</param>
        /// <returns> the DTMF digit pressed or 0x0 if none was pressed.</returns>
        public static char GetOption(this AGIChannel source, string file, string escapeDigits, int timeout)
		{
			var lastReply = source.SendCommand(new GetOptionCommand(file, escapeDigits, timeout));
			return lastReply.ResultCodeAsChar;
		}

        #endregion

        #region Exec(string application)

        /// <summary>
        /// Executes the given command.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="application">the name of the application to execute, for example "Dial".</param>
        /// <returns> the return code of the application of -2 if the application was not found.</returns>
        public static int Exec(this AGIChannel source, string application)
		{
			var lastReply = source.SendCommand(new ExecCommand(application));
			return lastReply.ResultCode;
		}

        #endregion

        #region Exec(string application, string options)

        /// <summary>
        /// Executes the given command.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="application">the name of the application to execute, for example "Dial".</param>
        /// <param name="options">the parameters to pass to the application, for example "SIP/123".</param>
        /// <returns> the return code of the application of -2 if the application was not found.</returns>
        public static int Exec(this AGIChannel source, string application, string options)
		{
			var lastReply = source.SendCommand(new ExecCommand(application, options));
			return lastReply.ResultCode;
		}

		#endregion

		#region SetContext

		/// <summary>
		/// Sets the context for continuation upon exiting the application.
		/// </summary>
		public static void SetContext(this AGIChannel source, string context)
		{
			source.SendCommand(new SetContextCommand(context));
		}

		#endregion

		#region SetExtension

		/// <summary>
		/// Sets the extension for continuation upon exiting the application.
		/// </summary>
		public static void SetExtension(this AGIChannel source, string extension)
		{
			source.SendCommand(new SetExtensionCommand(extension));
		}

		#endregion

		#region  SetPriority(int priority)

		/// <summary>
		/// Sets the priority for continuation upon exiting the application.
		/// </summary>
		public static void SetPriority(this AGIChannel source, int priority)
		{
			source.SendCommand(new SetPriorityCommand(priority));
		}

        #endregion

        #region  SetPriority(string label priority)

        /// <summary>
        /// Sets the label for continuation upon exiting the application.
        /// </summary>
        public static void SetPriority(this AGIChannel source, string label)
		{
			source.SendCommand(new SetPriorityCommand(label));
		}

        #endregion

        #region StreamFile(string file)

        /// <summary>
        /// Plays the given file.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="file">name of the file to play.</param>
        /// <returns>failure: -1; failure on open: 0; success: 0; digit pressed: (digit) > 0.</returns>
        public static int StreamFile(this AGIChannel source, string file)
		{
			AGIReply lastReply = source.SendCommand(new StreamFileCommand(file));
			return lastReply.ResultCode;
		}

        #endregion

        #region StreamFile(string file, string escapeDigits)

        /// <summary>
        /// Plays the given file and allows the user to escape by pressing one of the given digit.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="file">name of the file to play.</param>
        /// <param name="escapeDigits">a String containing the DTMF digits that allow the user to escape.</param>
        /// <returns> the DTMF digit pressed or 0x0 if none was pressed.</returns>
        public static char StreamFile(this AGIChannel source, string file, string escapeDigits)
		{
			AGIReply lastReply = source.SendCommand(new StreamFileCommand(file, escapeDigits));
			return lastReply.ResultCodeAsChar;
		}

        #endregion

        #region SayDigits(string digits)

        /// <summary>
        /// Says the given digit string.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="digits">the digit string to say.</param>
        public static void SayDigits(this AGIChannel source, string digits)
		{
			source.SendCommand(new SayDigitsCommand(digits));
		}

        #endregion

        #region SayDigits(string digits, string escapeDigits)

        /// <summary>
        /// Says the given number, returning early if any of the given DTMF number
        /// are received on the channel.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="digits">the digit string to say.</param>
        /// <param name="escapeDigits">a String containing the DTMF digits that allow the user to escape.</param>
        /// <returns> the DTMF digit pressed or 0x0 if none was pressed.</returns>
        public static char SayDigits(this AGIChannel source, string digits, string escapeDigits)
		{
			AGIReply lastReply = source.SendCommand(new SayDigitsCommand(digits, escapeDigits));
			return lastReply.ResultCodeAsChar;
		}
        #endregion

        #region SayNumber(string number)

        /// <summary>
        /// Says the given number.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="number">the number to say.</param>
        public static void SayNumber(this AGIChannel source, string number)
		{
			source.SendCommand(new SayNumberCommand(number));
		}
        #endregion

        #region SayNumber(string number, string escapeDigits)

        /// <summary>
        /// Says the given number, returning early if any of the given DTMF number
        /// are received on the channel.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="number">the number to say.</param>
        /// <param name="escapeDigits">a String containing the DTMF digits that allow the user to escape.</param>
        /// <returns> the DTMF digit pressed or 0x0 if none was pressed.</returns>
        public static char SayNumber(this AGIChannel source, string number, string escapeDigits)
		{
			AGIReply lastReply = source.SendCommand(new SayNumberCommand(number, escapeDigits));
			return lastReply.ResultCodeAsChar;
		}

        #endregion

        #region SayPhonetic(string text)

        /// <summary>
        /// Says the given character string with phonetics.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text">the text to say.</param>
        public static void SayPhonetic(this AGIChannel source, string text)
		{
			source.SendCommand(new SayPhoneticCommand(text));
		}

        #endregion
        #region SayPhonetic(string text, string escapeDigits)

        /// <summary>
        /// Says the given character string with phonetics, returning early if any of
        /// the given DTMF number are received on the channel.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text">the text to say.</param>
        /// <param name="escapeDigits">a String containing the DTMF digits that allow the user to escape.</param>
        /// <returns> the DTMF digit pressed or 0x0 if none was pressed.</returns>
        public static char SayPhonetic(this AGIChannel source, string text, string escapeDigits)
		{
			AGIReply lastReply = source.SendCommand(new SayPhoneticCommand(text, escapeDigits));
			return lastReply.ResultCodeAsChar;
		}

        #endregion
        #region SayAlpha(string text)

        /// <summary>
        /// Says the given character string.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text">the text to say.</param>
        public static void SayAlpha(this AGIChannel source, string text)
		{
			source.SendCommand(new SayAlphaCommand(text));
		}

        #endregion
        #region SayAlpha(string text, string escapeDigits)

        /// <summary>
        /// Says the given character string, returning early if any of the given DTMF
        /// number are received on the channel.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text">the text to say.</param>
        /// <param name="escapeDigits">a String containing the DTMF digits that allow the user to escape.</param>
        /// <returns> the DTMF digit pressed or 0x0 if none was pressed.</returns>
        public static char SayAlpha(this AGIChannel source, string text, string escapeDigits)
		{
			AGIReply lastReply = source.SendCommand(new SayAlphaCommand(text, escapeDigits));
			return lastReply.ResultCodeAsChar;
		}

        #endregion
        #region SayTime(long time)

        /// <summary>
        /// Says the given time.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="time">the time to say in seconds since 00:00:00 on January 1, 1970.</param>
        public static void SayTime(this AGIChannel source, long time)
		{
			source.SendCommand(new SayTimeCommand(time));
		}

        #endregion
        #region SayTime(long time, string escapeDigits)

        /// <summary>
        /// Says the given time, returning early if any of the given DTMF number are
        /// received on the channel.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="time">the time to say in seconds since 00:00:00 on January 1, 1970.</param>
        /// <param name="escapeDigits">a String containing the DTMF digits that allow the user to escape.</param>
        /// <returns> the DTMF digit pressed or 0x0 if none was pressed.</returns>
        public static char SayTime(this AGIChannel source, long time, string escapeDigits)
		{
			AGIReply lastReply = source.SendCommand(new SayTimeCommand(time, escapeDigits));
			return lastReply.ResultCodeAsChar;
		}

        #endregion
        #region GetVariable(string name)

        /// <summary>
        /// Returns the value of the given channel variable.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="name">the name of the variable to retrieve.</param>
        /// <returns> the value of the given variable or null if not set.</returns>
        public static string? GetVariable(this AGIChannel source, string name)
		{
			AGIReply lastReply = source.SendCommand(new GetVariableCommand(name));
			if (lastReply.ResultCode != 1)
				return null;
			return lastReply.Extra;
		}

        #endregion
        #region SetVariable(string name, string value_Renamed)

        /// <summary>
        /// Sets the value of the given channel variable to a new value.
        /// </summary>
		/// <param name="source"></param>
        /// <param name="name">the name of the variable to retrieve.</param>
        /// <param name="val">the new value to set.</param>
        public static void SetVariable(this AGIChannel source, string name, string? val = default)
		{
			source.SendCommand(new SetVariableCommand(name, val));
		}

        public static void GoSub(this AGIChannel source, string context, string extension, string priority, string? args = null)
        {
            source.SendCommand(new GoSubCommand(context, extension, priority, args));
        }

        #endregion
        #region SetVariable(string name, string value_Renamed)

        /// <summary>
        /// Sets the value for return to asterisk
        /// </summary>
        public static void SetReturn(this AGIChannel source, string? val = default)
        {
            source.SendCommand(new SetVariableCommand(Common.AGI_DEFAULT_RETURN_VALUE, val));
        }

        #endregion
        #region WaitForDigit(int timeout)

        /// <summary>
        /// Waits up to 'timeout' milliseconds to receive a DTMF digit.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="timeout">timeout the milliseconds to wait for the channel to receive a DTMF digit, -1 will wait forever.</param>
        /// <returns> the DTMF digit pressed or 0x0 if none was pressed.</returns>
        public static char WaitForDigit(this AGIChannel source, int timeout)
		{
			AGIReply lastReply = source.SendCommand(new WaitForDigitCommand(timeout));
			return lastReply.ResultCodeAsChar;
		}

        #endregion
        #region GetFullVariable(string name)
        /// <summary>
        /// Returns the value of the current channel variable, unlike getVariable()
        /// this method understands complex variable names and builtin variables.<br/>
        /// Available since Asterisk 1.2.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="name">the name of the variable to retrieve.</param>
        /// <returns>the value of the given variable or null if not et.</returns>
        public static string? GetFullVariable(this AGIChannel source, string name)
		{
			AGIReply lastReply = source.SendCommand(new GetFullVariableCommand(name));
			if (lastReply.ResultCode != 1)
				return null;
			return lastReply.Extra;
		}

        #endregion
        #region GetFullVariable(string name, string channel)

        /// <summary>
        /// Returns the value of the given channel variable.<br/>
        /// Available since Asterisk 1.2.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="name">the name of the variable to retrieve.</param>
        /// <param name="channel">the name of the channel.</param>
        /// <returns>the value of the given variable or null if not set.</returns>
        public static string? GetFullVariable(this AGIChannel source, string name, string channel)
		{
			AGIReply lastReply = source.SendCommand(new GetFullVariableCommand(name, channel));
			if (lastReply.ResultCode != 1)
				return null;
			return lastReply.Extra;
		}

        #endregion
        #region SayDateTime(...)

        /// <summary>
        /// Says the given time.<br/>
        /// Available since Asterisk 1.2.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="time">the time to say in seconds elapsed since 00:00:00 on January 1, 1970, Coordinated Universal Time (UTC)</param>
        public static void SayDateTime(this AGIChannel source, long time)
		{
			source.SendCommand(new SayDateTimeCommand(time));
		}

        /// <summary>
        /// Says the given time and allows interruption by one of the given escape digits.<br/>
        /// Available since Asterisk 1.2.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="time">the time to say in seconds elapsed since 00:00:00 on January 1, 1970, Coordinated Universal Time (UTC)</param>
        /// <param name="escapeDigits">the digits that allow the user to interrupt this command or null for none.</param>
        /// <returns>the DTMF digit pressed or 0x0 if none was pressed.</returns>
        public static char SayDateTime(this AGIChannel source, long time, string escapeDigits)
		{
			AGIReply lastReply = source.SendCommand(new SayDateTimeCommand(time, escapeDigits));
			return lastReply.ResultCodeAsChar;
		}

        /// <summary>
        /// Says the given time in the given format and allows interruption by one of the given escape digits.<br/>
        /// Available since Asterisk 1.2.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="time">the time to say in seconds elapsed since 00:00:00 on January 1, 1970, Coordinated Universal Time (UTC)</param>
        /// <param name="escapeDigits">the digits that allow the user to interrupt this command or null for none.</param>
        /// <param name="format">the format the time should be said in</param>
        /// <returns>the DTMF digit pressed or 0x0 if none was pressed.</returns>
        public static char SayDateTime(this AGIChannel source, long time, string escapeDigits, string format)
		{
			AGIReply lastReply = source.SendCommand(new SayDateTimeCommand(time, escapeDigits, format));
			return lastReply.ResultCodeAsChar;
		}

        /// <summary>
        /// Says the given time in the given format and timezone and allows interruption by one of the given escape digits.<br/>
        /// Available since Asterisk 1.2.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="time">the time to say in seconds elapsed since 00:00:00 on January 1, 1970, Coordinated Universal Time (UTC)</param>
        /// <param name="escapeDigits">the digits that allow the user to interrupt this command or null for none.</param>
        /// <param name="format">the format the time should be said in</param>
        /// <param name="timezone">the timezone to use when saying the time, for example "UTC" or "Europe/Berlin".</param>
        /// <returns>the DTMF digit pressed or 0x0 if none was pressed.</returns>
        public static char SayDateTime(this AGIChannel source, long time, string escapeDigits, string format, string timezone)
		{
			AGIReply lastReply = source.SendCommand(new SayDateTimeCommand(time, escapeDigits, format, timezone));
			return lastReply.ResultCodeAsChar;
		}

        #endregion
        #region DatabaseGet(string family, string key)

        /// <summary>
        /// Retrieves an entry in the Asterisk database for a given family and key.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="family">the family of the entry to retrieve.</param>
        /// <param name="key">key the key of the entry to retrieve.</param>
        /// <return>the value of the given family and key or null if there is no such value.</return>
        public static string? DatabaseGet(this AGIChannel source, string family, string key)
		{
			AGIReply lastReply = source.SendCommand(new DatabaseGetCommand(family, key));
			if (lastReply.ResultCode != 1)
				return null;
			return lastReply.Extra;
		}

        #endregion
        #region DatabasePut(string family, string key, string value)

        /// <summary>
        /// Adds or updates an entry in the Asterisk database for a given family, key and value.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="family">the family of the entry to add or update.</param>
        /// <param name="key">the key of the entry to add or update.</param>
        /// <param name="value">the new value of the entry.</param>
        public static void DatabasePut(this AGIChannel source, string family, string key, string value)
		{
			source.SendCommand(new DatabasePutCommand(family, key, value));
		}

        #endregion
        #region DatabaseDel(string family, string key)

        /// <summary>
        /// Deletes an entry in the Asterisk database for a given family and key.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="family">the family of the entry to delete.</param>
        /// <param name="key">the key of the entry to delete.</param>
        public static void DatabaseDel(this AGIChannel source, string family, string key)
		{
			source.SendCommand(new DatabaseDelCommand(family, key));
		}

        #endregion
        #region DatabaseDelTree(String family)

        /// <summary>
        /// Deletes a whole family of entries in the Asterisk database.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="family">the family to delete.</param>
        public static void DatabaseDelTree(this AGIChannel source, string family)
		{
			source.SendCommand(new DatabaseDelTreeCommand(family));
		}

        #endregion
        #region DatabaseDelTree(string family, string keytree)

        /// <summary>
        /// Deletes all entries of a given family in the Asterisk database that have a key that starts with a given prefix.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="family">the family of the entries to delete.</param>
        /// <param name="keytree">the prefix of the keys of the entries to delete.</param>
        public static void DatabaseDelTree(this AGIChannel source, string family, string keytree)
		{
			source.SendCommand(new DatabaseDelTreeCommand(family, keytree));
		}

        #endregion
        #region Verbose(string message, int level)

        /// <summary>
        /// Sends a message to the Asterisk console via the verbose message system.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message">the message to send</param>
        /// <param name="level">the verbosity level to use. Must be in [1..4]</param>
        public static void Verbose(this AGIChannel source, string message, int level)
		{
			source.SendCommand(new VerboseCommand(message, level));
		}

        #endregion
        #region RecordFile(...)

        /// <summary>
        /// Record to a file until a given dtmf digit in the sequence is received.<br/>
        /// Returns -1 on hangup or error.<br/>
        /// The format will specify what kind of file will be recorded. The timeout is
        /// the maximum record time in milliseconds, or -1 for no timeout. Offset samples
        /// is optional, and if provided will seek to the offset without exceeding the
        /// end of the file. "maxSilence" is the number of seconds of maxSilence allowed
        /// before the function returns despite the lack of dtmf digits or reaching
        /// timeout.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="file">the name of the file to stream, must not include extension.</param>
        /// <param name="format">the format of the file to be recorded, for example "wav".</param>
        /// <param name="escapeDigits">contains the digits that allow the user to end recording.</param>
        /// <param name="timeout">the maximum record time in milliseconds, or -1 for no timeout.</param>
        /// <returns>result code</returns>
        public static int RecordFile(this AGIChannel source, string file, string format, string escapeDigits, int timeout)
		{
			AGIReply lastReply = source.SendCommand(new RecordFileCommand(file, format, escapeDigits, timeout));
			return lastReply.ResultCode;
		}

        /// <summary>
        /// Record to a file until a given dtmf digit in the sequence is received.<br/>
        /// Returns -1 on hangup or error.<br/>
        /// The format will specify what kind of file will be recorded. The timeout is
        /// the maximum record time in milliseconds, or -1 for no timeout. Offset samples
        /// is optional, and if provided will seek to the offset without exceeding the
        /// end of the file. "maxSilence" is the number of seconds of maxSilence allowed
        /// before the function returns despite the lack of dtmf digits or reaching
        /// timeout.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="file">the name of the file to stream, must not include extension.</param>
        /// <param name="format">the format of the file to be recorded, for example "wav".</param>
        /// <param name="escapeDigits">contains the digits that allow the user to end recording.</param>
        /// <param name="timeout">the maximum record time in milliseconds, or -1 for no timeout.</param>
        /// <param name="offset">the offset samples to skip.</param>
        /// <param name="beep">true if a beep should be played before recording.</param>
        /// <param name="maxSilence">The amount of silence (in seconds) to allow before returning despite the lack of dtmf digits or reaching timeout.</param>
        /// <returns>result code</returns>
        public static int RecordFile(this AGIChannel source, string file, string format, string escapeDigits, int timeout, int offset, bool beep, int maxSilence)
		{
			AGIReply lastReply = source.SendCommand(new RecordFileCommand(file, format, escapeDigits, timeout, offset, beep, maxSilence));
			return lastReply.ResultCode;
		}

        #endregion
        #region ControlStreamFile(...)

        /// <summary>
        /// Plays the given file, allowing playback to be interrupted by the given
        /// digits, if any, and allows the listner to control the stream.<br/>
        /// If offset is provided then the audio will seek to sample offset before play
        /// starts.<br/>
        /// Returns 0 if playback completes without a digit being pressed, or the ASCII
        /// numerical value of the digit if one was pressed, or -1 on error or if the
        /// channel was disconnected. <br/>
        /// Remember, the file extension must not be included in the filename.<br/>
        /// Available since Asterisk 1.2
        /// </summary>
        /// <param name="source"></param>
        /// <seealso cref="Command.ControlStreamFileCommand"/>
        /// <param name="file">the name of the file to stream, must not include extension.</param>
        /// <returns>result code</returns>
        public static int ControlStreamFile(this AGIChannel source, string file)
		{
			AGIReply lastReply = source.SendCommand(new ControlStreamFileCommand(file));
			return lastReply.ResultCode;
		}
        /// <summary>
        /// Plays the given file, allowing playback to be interrupted by the given
        /// digits, if any, and allows the listner to control the stream.<br/>
        /// If offset is provided then the audio will seek to sample offset before play
        /// starts.<br/>
        /// Returns 0 if playback completes without a digit being pressed, or the ASCII
        /// numerical value of the digit if one was pressed, or -1 on error or if the
        /// channel was disconnected. <br/>
        /// Remember, the file extension must not be included in the filename.<br/>
        /// Available since Asterisk 1.2
        /// </summary>
        /// <seealso cref="Command.ControlStreamFileCommand"/>
        /// <param name="source"></param>
        /// <param name="file">the name of the file to stream, must not include extension.</param>
        /// <param name="escapeDigits">contains the digits that allow the user to interrupt this command.</param>
        /// <returns>result code</returns>
        public static int ControlStreamFile(this AGIChannel source, string file, string escapeDigits)
		{
			AGIReply lastReply = source.SendCommand(new ControlStreamFileCommand(file, escapeDigits));
			return lastReply.ResultCode;
		}
        /// <summary>
        /// Plays the given file, allowing playback to be interrupted by the given
        /// digits, if any, and allows the listner to control the stream.<br/>
        /// If offset is provided then the audio will seek to sample offset before play
        /// starts.<br/>
        /// Returns 0 if playback completes without a digit being pressed, or the ASCII
        /// numerical value of the digit if one was pressed, or -1 on error or if the
        /// channel was disconnected. <br/>
        /// Remember, the file extension must not be included in the filename.<br/>
        /// Available since Asterisk 1.2
        /// </summary>
        /// <seealso cref="Command.ControlStreamFileCommand"/>
        /// <param name="source"></param>
        /// <param name="file">the name of the file to stream, must not include extension.</param>
        /// <param name="escapeDigits">contains the digits that allow the user to interrupt this command.</param>
        /// <param name="offset">the offset samples to skip before streaming.</param>
        /// <returns>result code</returns>
        public static int ControlStreamFile(this AGIChannel source, string file, string escapeDigits, int offset)
		{
			AGIReply lastReply = source.SendCommand(new ControlStreamFileCommand(file, escapeDigits, offset));
			return lastReply.ResultCode;
		}
        /// <summary>
        /// Plays the given file, allowing playback to be interrupted by the given
        /// digits, if any, and allows the listner to control the stream.<br/>
        /// If offset is provided then the audio will seek to sample offset before play
        /// starts.<br/>
        /// Returns 0 if playback completes without a digit being pressed, or the ASCII
        /// numerical value of the digit if one was pressed, or -1 on error or if the
        /// channel was disconnected. <br/>
        /// Remember, the file extension must not be included in the filename.<br/>
        /// Available since Asterisk 1.2
        /// </summary>
        /// <seealso cref="ControlStreamFileCommand"/>
        /// <param name="source"></param>
        /// <param name="file">the name of the file to stream, must not include extension.</param>
        /// <param name="escapeDigits">contains the digits that allow the user to interrupt this command.</param>
        /// <param name="offset">the offset samples to skip before streaming.</param>
        /// <param name="forwardDigit">the digit for fast forward.</param>
        /// <param name="rewindDigit">the digit for rewind.</param>
        /// <param name="pauseDigit">the digit for pause and unpause.</param>
        /// <returns>result code</returns>
        public static int ControlStreamFile(this AGIChannel source, string file, string escapeDigits, int offset, string forwardDigit, string rewindDigit, string pauseDigit)
		{
			AGIReply lastReply = source.SendCommand(new ControlStreamFileCommand(file, escapeDigits, offset, forwardDigit, rewindDigit, pauseDigit));
			return lastReply.ResultCode;
		}
		#endregion
	}
}