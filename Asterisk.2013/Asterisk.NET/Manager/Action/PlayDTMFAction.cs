using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsterNET.Manager.Action
{
    /// <summary>
    ///     The PlayDTMFAction plays DTMF in channel.<br />
    ///     It is implemented in apps/app_senddtmf.c
    /// </summary>
    public class PlayDTMFAction : ManagerAction
    {
        /// <summary>
        ///     Creates a new empty PlayDTMFAction.
        /// </summary>
        public PlayDTMFAction()
        {
        }

        /// <summary>
        ///     Creates a new PlayDTMFAction that plays a tones in specificed channel.
        /// </summary>
        /// <param name="channel">Channel name to send digit to.</param>
        /// <param name="digit">The DTMF digit to play.</param>
        /// <param name="duration">The duration, in milliseconds, of the digit to be played.</param>
        public PlayDTMFAction(string channel, string digit, string duration)
        {
            this.Channel = channel;
            this.Digit = digit;
            this.Duration = duration;
        }

        /// <summary>
        ///     Creates a new PlayDTMFAction that plays a tones in specificed channel.
        /// </summary>
        /// <param name="channel">Channel name to send digit to.</param>
        /// <param name="digit">The DTMF digit to play.</param>
        public PlayDTMFAction(string channel, string digit)
        {
            this.Channel = channel;
            this.Digit = digit;
        }

        /// <summary>
        ///     Get the name of this action, i.e. "PlayDTMF".
        /// </summary>
        public override string Action
        {
            get { return "PlayDTMF"; }
        }

        /// <summary>
        ///     Get/Set the name of the Channel name to send digit to.<br />
        ///     This property is mandatory.
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        ///     Get/Set the DTMF digit to play.<br />
        ///     This property is mandatory.
        /// </summary>
        public string Digit { get; set; }

        /// <summary>
        ///     Get/Set the duration, in milliseconds, of the digit to be played.
        /// </summary>
        public string Duration { get; set; }

    }
}