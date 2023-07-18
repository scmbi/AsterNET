using Sufficit.Asterisk;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AsterNET.Helpers;
using System.Linq;

namespace AsterNET.FastAGI
{

    #region Enum - AGIReplyStatuses 

    public enum AGIReplyStatuses
    {
        /// <summary>
        ///     Status code (200) indicating Asterisk successfully processed the AGICommand.
        /// </summary>
        SC_SUCCESS = 200,

        /// <summary>
        ///     Status code (510) indicating Asterisk was unable to process the
        ///     AGICommand because there is no command with the given name available.
        /// </summary>
        SC_INVALID_OR_UNKNOWN_COMMAND = 510,

        /// <summary>
        ///     Status code (511) indicating Asterisk was unable to process the
        ///     AGICommand because the channel is dead.
        /// </summary>
        SC_DEAD_CHANNEL = 511,

        /// <summary>
        ///     Status code (520) indicating Asterisk was unable to process the
        ///     AGICommand because the syntax used was not correct. This is most likely
        ///     due to missing required parameters or additional parameters sent that are
        ///     not understood.<br />
        ///     Ensure proper quoting of the parameters when you receive this status
        ///     code.
        /// </summary>
        SC_INVALID_COMMAND_SYNTAX = 520
    }

    #endregion

    /// <summary>
    ///     Default implementation of the AGIReply interface.
    /// </summary>
    public class AGIReply
    {
        #region Variables

        private readonly string firstLine;
        private readonly string[] _lines;

        /// <summary>Additional attributes contained in this reply, for example endpos.</summary>
        private Dictionary<string, string> attributes;

        private bool attributesCreated;

        /// <summary> The contents of the parenthesis.</summary>
        private string? _extra;


        /// <summary> The result, that is the part directly following the "result=" string.</summary>
        private string result;

        private bool resultCreated;

        /// <summary> The status code.</summary>
        private int status;

        private bool statusCreated;

        /// <summary> In case of status == 520 (invalid command syntax) this attribute contains the synopsis of the command.</summary>
        private string synopsis;

        private bool synopsisCreated;

        /// <summary> In case of status == 520 (invalid command syntax) this attribute contains the usage of the command.</summary>
        private string usage;

        #endregion

        #region Constructor - AGIReply()

        public AGIReply()
        {
        }

        #endregion

        #region Constructor - AGIReply(lines)

        /// <param name="lines">If empty array, means no problems, success</param>
        public AGIReply(string[] lines)
        {
            _lines = lines;
            firstLine = lines.FirstOrDefault() ?? string.Empty;            
        }

        #endregion

        #region FirstLine

        public string FirstLine
        {
            get { return firstLine; }
        }

        #endregion

        #region Lines

        public IList Lines
        {
            get { return _lines; }
        }

        #endregion

        #region ResultCode

        /// <summary>
        ///     Returns the return code (the result as int).
        /// </summary>
        /// <returns>the return code or -1 if the result is not an int.</returns>
        public int ResultCode
        {
            get
            {
                string result = GetResult();                
                if (string.IsNullOrWhiteSpace(result))
                    return 0;

                if (int.TryParse(result, out int resultCode))
                    return resultCode;

                return -1;
            }
        }

        /// <summary>
        ///     Returns the return code as character. 
        /// </summary>
        /// <returns>the return code as character. || char.MaxValue for errors</returns>
        public char ResultCodeAsChar
        {
            get
            {
                int resultCode = ResultCode;
                if (resultCode < 0)
                    return char.MaxValue;
                return (char) resultCode;
            }
        }

        #endregion

        #region Extra

        /// <summary>
        ///     Returns the text in parenthesis contained in this reply.<br />
        ///     The meaning of this property depends on the command sent. Sometimes it
        ///     contains a flag like "timeout" or "hangup" or - in case of the
        ///     GetVariableCommand - the value of the variable.
        /// </summary>
        /// <returns>the text in the parenthesis or null if not set.</returns>
        public string? Extra
        {
            get
            {
                if (GetStatus() != (int)AGIReplyStatuses.SC_SUCCESS)
                    return null;

                if (_extra != null)
                    return _extra;

                var matcher = Common.AGI_PARENTHESIS_PATTERN_NAMED.Match(firstLine);
                if (matcher.Success)                                   
                    //if (matcher.Groups["code"].Value == "200")                   
                        _extra = matcher.Groups["value"].Value;    

                return _extra;
            }
        }

        #endregion

        #region GetResult()

        /// <summary>
        ///     Returns the result, that is the part directly following the "result=" string.
        /// </summary>
        /// <returns>the result.</returns>
        public string GetResult()
        {
            if (resultCreated)
                return result;

            var matcher = Common.AGI_RESULT_PATTERN.Match(firstLine);
            if (matcher.Success)
                result = matcher.Groups[1].Value;

            resultCreated = true;
            return result;
        }

        #endregion

        #region GetStatus()

        /// <summary>
        ///     Returns the status code.<br />
        ///     Supported status codes are:<br />
        ///     200 Success<br />
        ///     510 Invalid or unknown command<br />
        ///     520 Invalid command syntax<br />
        /// </summary>
        /// <returns>the status code.</returns>
        public int GetStatus()
        {
            if (statusCreated)
                return status;
            var matcher = Common.AGI_STATUS_PATTERN.Match(firstLine);
            if (matcher.Success)
                status = Int32.Parse(matcher.Groups[1].Value);
            statusCreated = true;
            return status;
        }

        #endregion

        #region GetAttribute(name)

        /// <summary>
        ///     Returns an additional attribute contained in the reply.<br />
        ///     For example the reply to the StreamFileCommand contains an additional
        ///     endpos attribute indicating the frame where the playback was stopped.
        ///     This can be retrieved by calling getAttribute("endpos") on the corresponding reply.
        /// </summary>
        /// <param name="name">the name of the attribute to retrieve. The name is case insensitive.</param>
        /// <returns>the value of the attribute or null if it is not set.</returns>
        public string GetAttribute(string name)
        {
            if (GetStatus() != (int) AGIReplyStatuses.SC_SUCCESS)
                return null;

            if ("result".ToUpper().Equals(name.ToUpper()))
                return GetResult();

            if (!attributesCreated)
            {
                var matcher = Common.AGI_ADDITIONAL_ATTRIBUTES_PATTERN.Match(firstLine);
                if (matcher.Success)
                {
                    string s;
                    Match attributeMatcher;

                    attributes = new Dictionary<string, string>();
                    s = matcher.Groups[2].Value;
                    attributeMatcher = Common.AGI_ADDITIONAL_ATTRIBUTE_PATTERN.Match(s);
                    while (attributeMatcher.Success)
                    {
                        string key;
                        string value_Renamed;

                        key = attributeMatcher.Groups[1].Value;
                        value_Renamed = attributeMatcher.Groups[2].Value;
                        attributes[key.ToLower(Helper.CultureInfo)] = value_Renamed;
                    }
                }
                attributesCreated = true;
            }

            if (attributes == null || (attributes.Count == 0))
                return null;

            return attributes[name.ToLower(Helper.CultureInfo)];
        }

        #endregion

        #region GetSynopsis()

        /// <summary>
        ///     Returns the synopsis of the command sent if Asterisk expected a different
        ///     syntax (getStatus() == SC_INVALID_COMMAND_SYNTAX).
        /// </summary>
        /// <returns>the synopsis of the command sent, null if there were no syntax errors.</returns>
        public string GetSynopsis()
        {
            if (GetStatus() != (int) AGIReplyStatuses.SC_INVALID_COMMAND_SYNTAX)
                return null;

            if (!synopsisCreated)
            {
                if (_lines.Length > 1)
                {
                    string secondLine;
                    Match synopsisMatcher;

                    secondLine = _lines[1];
                    synopsisMatcher = Common.AGI_SYNOPSIS_PATTERN.Match(secondLine);
                    if (synopsisMatcher.Success)
                        synopsis = synopsisMatcher.Groups[1].Value;
                }
                synopsisCreated = true;

                var sbUsage = new StringBuilder();
                string line;
                for (int i = 2; i < _lines.Length; i++)
                {
                    line = _lines[i];
                    if (line == Common.AGI_END_OF_PROPER_USAGE)
                        break;
                    sbUsage.Append(line.Trim());
                    sbUsage.Append(" ");
                }
                usage = sbUsage.ToString().Trim();
            }
            return synopsis;
        }

        #endregion

        #region GetUsage()

        /// <summary>
        ///     Returns the usage of the command sent if Asterisk expected a different
        ///     syntax (getStatus() == SC_INVALID_COMMAND_SYNTAX).
        /// </summary>
        /// <returns>
        ///     the usage of the command sent,
        ///     null if there were no syntax errors.
        /// </returns>
        public string GetUsage()
        {
            return usage;
        }

        #endregion

        #region ToString()

        public override string ToString()
        {
            return Helper.ToString(this);
        }

        #endregion
    }
}