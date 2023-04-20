using System.Text;
using AsterNET.Helpers;

namespace AsterNET.FastAGI.Command
{
	public abstract class AGICommand
	{
		/// <summary>
		/// Change the default timeout for wait a valid response
		/// </summary>
		public int? ReadTimeOut { get; set; }

		public abstract string BuildCommand();

		protected internal string EscapeAndQuote(string? s)
		{
			if (string.IsNullOrWhiteSpace(s))
				return "\"\"";

			string tmp = s!;
			tmp = tmp.Replace("\\\"", "\\\\\"");		// escape quotes
			tmp = tmp.Replace("\\\n", "");				// filter newline
			return "\"" + tmp + "\"";					// add quotes
		}

		public override string ToString()
		{
			return Helper.ToString(this);
		}
	}
}
