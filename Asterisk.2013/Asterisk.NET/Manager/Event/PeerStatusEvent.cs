using Sufficit.Asterisk;
using Sufficit.Asterisk.Events;
using Sufficit.Asterisk.Manager;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AsterNET.Manager.Event
{
	
	/// <summary>
	/// A PeerStatusEvent is triggered when a SIP or IAX client attempts to registrer at this asterisk server.<br/>
	/// This event is implemented in channels/chan_iax2.c and channels/chan_sip.c
	/// </summary>
	public class PeerStatusEvent : IManagerEvent, PeerStatusEventInterface
	{
        #region IMPLEMENT INTERFACE PARSE SUPPORT

        public Dictionary<string, string> Attributes { get; set; }

		/// <summary>
		/// Unknown properties parser
		/// </summary>
		/// <param name="key">key name</param>
		/// <param name="value">key value</param>
		/// <returns>true - value parsed, false - can't parse value</returns>
		public virtual bool Parse(string key, string value)
		{
			if (Attributes == null)
			{
				Attributes = new Dictionary<string, string>();
			}

			if (Attributes.ContainsKey(key))
			{
				Attributes[key] += string.Concat(Common.LINE_SEPARATOR, value); // Key already presents, add with delimiter
			}
			else
			{
				Attributes.Add(key, value);
			}

			return true;
		}

		/// <summary>
		/// Unknown properties parser.
		/// </summary>
		/// <param name="attributes">dictionary</param>
		/// <returns>updated dictionary</returns>
		public virtual Dictionary<string, string> ParseSpecial(Dictionary<string, string> attributes)
		{
			return attributes;
		}

		#endregion

		#region IMPLEMENT INTERFACE MANAGER EVENT

		string IManagerEvent.Privilege => "system";

		public string Server { get; set; }

		public double Timestamp { get; set; }

		public string UniqueId { get; set; }

		[JsonIgnore]
		public IManagerConnection Source { get; set; }


		#endregion

		public PeerStatusEvent(IManagerConnection connection)
		{
			this.DateReceived = DateTime.Now;
			Source = connection;
		}

		public string Channel { get; set; }

		public DateTime DateReceived { get; set; }


		public const PrivilegeEnum Privilege = PrivilegeEnum.system;

		private string address;
		private string peer;
		private string cause;
		private int time;

		/// <summary>
		/// Returns the name of the peer that registered. The peer's name starts with "IAX2/" if it is an
		/// IAX client or "SIP/" if it is a SIP client. It is followed by the username that is used for
		/// registration.
		/// </summary>
		/// <summary> Sets the name of the peer that registered.</summary>
		public string Peer
		{
			get { return peer ?? string.Empty; }
			set { this.peer = value; }
		}

		/// <summary> Returns the registration state.<br/>
		/// This may be one of
		/// <ul>
		/// <li>Registered</li>
		/// <li>Unregistered</li>
		/// <li>Reachable</li>
		/// <li>Lagged</li>
		/// <li>Unreachable</li>
		/// <li>Rejected (IAX only)</li>
		/// </ul>
		/// </summary>
		/// <summary> Sets the registration state.</summary>
		public PeerStatus PeerStatus { get; set; }


		/// <summary>
		/// Returns the cause of a rejection or unregistration.<br/>
		/// For IAX peers this is set only if the status equals "Rejected".<br/>
		/// For SIP peers this is set if the status equals "Unregistered" and the peer was unregistered
		/// due to an expiration. In that case the cause is set to "Expired".
		/// </summary>
		/// <summary> Sets the cause of the rejection or unregistration.</summary>
		public PeerStatusCauseEnum Cause { get; set; }

		/// <summary>
		/// Returns the ping time of the client if status equals "Reachable" or "Lagged"; if the status
		/// equals "Unreachable" it returns how long the last response took (in ms) for IAX peers or -1
		/// for SIP peers.
		/// </summary>
		public int Time { get; set; }

		public string Address
		{
			get { return this.address ?? string.Empty; }
			set { this.address = value; }
		}
    }
}