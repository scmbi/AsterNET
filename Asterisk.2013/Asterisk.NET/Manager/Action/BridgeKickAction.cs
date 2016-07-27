namespace AsterNET.Manager.Action
{
    public class BridgeKickAction : ManagerAction
    {
        public override string Action
        {
            get { return "BridgeKick"; }
        }

        public string BridgeUniqueid { get; set; }
        public string Channel { get; set; }
    }
}