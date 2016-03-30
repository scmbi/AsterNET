namespace AsterNET.Manager.Action
{
    /// <summary>
    ///     Redirects a given channel (and an optional additional channel) to a new extension.
    /// </summary>
    public class BlindTransferAction : ManagerAction
    {
        /// <summary>
        ///     Creates a new empty RedirectAction.
        /// </summary>
        public BlindTransferAction()
        {
        }

        /// <summary>
        ///     Creates a new RedirectAction that redirects the given channel to the given context, extension, priority triple.
        /// </summary>
        /// <param name="channel">the name of the channel to redirect</param>
        /// <param name="context">the destination context</param>
        /// <param name="exten">the destination extension</param>
        public BlindTransferAction(string channel, string context, string exten)
        {
            this.Channel = channel;
            this.Context = context;
            this.Exten = exten;
        }

        /// <summary>
        ///     Get the name of this action, i.e. "Redirect".
        /// </summary>
        public override string Action
        {
            get { return "BlindTransfer"; }
        }

        /// <summary>
        ///     Get/Set name of the channel to redirect.
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        ///     Get/Set the destination context.
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        ///     Get/Set the destination extension.
        /// </summary>
        public string Exten { get; set; }
    }
}