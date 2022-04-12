using Sufficit.Asterisk;
using Sufficit.Asterisk.Events;

namespace AsterNET.Manager.Event
{

    /// <summary>
    /// Raised when a request successfully authenticates with a service..<br />
    /// </summary>
    public class SuccessfulAuthEvent : ManagerEvent, SuccessfulAuthEventInterface
    {
        public const PrivilegeEnum Privilege = PrivilegeEnum.security;

        public SuccessfulAuthEvent(ManagerConnection source)
            : base(source)
        {
        }

        public Severity Severity { get; set; }
        public string AccountId { get; set; }
        public string RemoteAddress { get; set; }
        public bool UsingPassword { get; set; }
        public string Service { get; set; }
    }
}