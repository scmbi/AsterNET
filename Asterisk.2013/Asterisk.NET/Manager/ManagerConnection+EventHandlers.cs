using System;
using System.Threading;
using System.Collections;
using AsterNET.Manager.Action;
using AsterNET.Manager.Event;
using AsterNET.Manager.Response;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using AsterNET.IO;
using System.Threading.Tasks;
using Sufficit.Asterisk.Manager;
using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager.Events;
using Microsoft.Extensions.Logging;

namespace AsterNET.Manager
{
    /// <summary>
    /// Default implementation of the ManagerConnection interface.
    /// </summary>
    public partial class ManagerConnection : IManagerConnection
    {      
        #region Events

        /// <summary>
        /// An UnhandledEvent is triggered on unknown event.
        /// </summary>
        public event EventHandler<IManagerEvent> UnhandledEvent;
        /// <summary>
        /// An AgentCallbackLogin is triggered when an agent is successfully logged in.
        /// </summary>
        public event EventHandler<AgentCallbackLoginEvent> AgentCallbackLogin;
        /// <summary>
        /// An AgentCallbackLogoff is triggered when an agent that previously logged in is logged of.<br/>
        /// </summary>
        public event EventHandler<AgentCallbackLogoffEvent> AgentCallbackLogoff;
        /// <summary>
        /// An AgentCalled is triggered when an agent is ring.<br/>
        /// To enable AgentCalled you have to set eventwhencalled = yes in queues.conf.<br/>
        /// </summary>
        public event EventHandler<AgentCalledEvent> AgentCalled;
        /// <summary>
        /// An AgentCompleteEvent is triggered when at the end of a call if the caller was connected to an agent.
        /// </summary>
        public event EventHandler<AgentCompleteEvent> AgentComplete;
        /// <summary>
        /// An AgentConnectEvent is triggered when a caller is connected to an agent.
        /// </summary>
        public event EventHandler<AgentConnectEvent> AgentConnect;
        /// <summary>
        /// An AgentDumpEvent is triggered when an agent dumps the caller while listening to the queue announcement.
        /// </summary>
        public event EventHandler<AgentDumpEvent> AgentDump;
        /// <summary>
        /// An AgentLoginEvent is triggered when an agent is successfully logged in using AgentLogin.
        /// </summary>
        public event EventHandler<AgentLoginEvent> AgentLogin;
        /// <summary>
        /// An AgentCallbackLogoffEvent is triggered when an agent that previously logged in using AgentLogin is logged of.
        /// </summary>
        public event EventHandler<AgentLogoffEvent> AgentLogoff;
        /// <summary>
        /// An AgentRingNoAnswer is triggered when an agent was rang and did not answer.<br/>
        /// To enable AgentRingNoAnswer you have to set eventwhencalled = yes in queues.conf.
        /// </summary>
        public event EventHandler<AgentRingNoAnswerEvent> AgentRingNoAnswer;
        /// <summary>
        /// An AgentsCompleteEvent is triggered after the state of all agents has been reported in response to an AgentsAction.
        /// </summary>
        public event EventHandler<AgentsCompleteEvent> AgentsComplete;
        /// <summary>
        /// An AgentsEvent is triggered for each agent in response to an AgentsAction.
        /// </summary>
        public event EventHandler<AgentsEvent> Agents;
        /// <summary>
        /// An AlarmEvent is triggered when a Zap channel leaves alarm state.
        /// </summary>
        public event EventHandler<AlarmClearEvent> AlarmClear;
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<BridgeEvent> Bridge;
        /// <summary>
        /// An AlarmEvent is triggered when a Zap channel enters or changes alarm state.
        /// </summary>
        public event EventHandler<AlarmEvent> Alarm;
        /// <summary>
        /// A CdrEvent is triggered when a call detail record is generated, usually at the end of a call.
        /// </summary>
        public event EventHandler<CdrEvent> Cdr;
        public event EventHandler<DBGetResponseEvent> DBGetResponse;
        /// <summary>
        /// A Dial is triggered whenever a phone attempts to dial someone.<br/>
        /// </summary>
        public event EventHandler<DialEvent> Dial;
        public event EventHandler<DTMFEvent> DTMF;
        /// <summary>
        /// An DTMFBeginEvent is triggered when a DTMF digit has started on a channel.
        /// </summary>
        public event EventHandler<DTMFBeginEvent> DTMFBegin;
        /// <summary>
        /// An DTMFEndEvent is triggered when a DTMF digit has ended on a channel.
        /// </summary>
        public event EventHandler<DTMFEndEvent> DTMFEnd;
        /// <summary>
        /// A DNDStateEvent is triggered by the Zap channel driver when a channel enters or leaves DND (do not disturb) state.
        /// </summary>
        public event EventHandler<DNDStateEvent> DNDState;
        /// <summary>
        /// An ExtensionStatus is triggered when the state of an extension changes.<br/>
        /// </summary>
        public event EventHandler<ExtensionStatusEvent> ExtensionStatus;
        /// <summary>
        /// A Hangup is triggered when a channel is hung up.<br/>
        /// </summary>
        public event EventHandler<HangupEvent> Hangup;
        /// <summary>
        /// A HangupRequestEvent is raised when a channel is hang up.<br/>
        /// </summary>
        public event EventHandler<HangupRequestEvent> HangupRequest;
        /// <summary>
        /// A HoldedCall is triggered when a channel is put on hold.<br/>
        /// </summary>
        public event EventHandler<HoldedCallEvent> HoldedCall;
        /// <summary>
        /// A Hold is triggered by the SIP channel driver when a channel is put on hold.<br/>
        /// </summary>
        public event EventHandler<HoldEvent> Hold;
        /// <summary>
        /// A Join is triggered when a channel joines a queue.<br/>
        /// </summary>
        public event EventHandler<JoinEvent> Join;
        /// <summary>
        /// A Leave is triggered when a channel leaves a queue.<br/>
        /// </summary>
        public event EventHandler<LeaveEvent> Leave;
        /// <summary>
        /// A Link is triggered when two voice channels are linked together and voice data exchange commences.<br/>
        /// Several Link events may be seen for a single call. This can occur when Asterisk fails to setup a
        /// native bridge for the call.This is when Asterisk must sit between two telephones and perform
        /// CODEC conversion on their behalf.
        /// </summary>
        public event EventHandler<LinkEvent> Link;
        /// <summary>
        /// A LogChannel is triggered when logging is turned on or off.<br/>
        /// </summary>
        public event EventHandler<LogChannelEvent> LogChannel;
        /// <summary>
        /// A MeetMeJoin is triggered if a channel joins a meet me conference.<br/>
        /// </summary>
        public event EventHandler<MeetmeJoinEvent> MeetMeJoin;
        /// <summary>
        /// A MeetMeLeave is triggered if a channel leaves a meet me conference.<br/>
        /// </summary>
        public event EventHandler<MeetmeLeaveEvent> MeetMeLeave;
        // public event EventHandler<MeetMeStopTalkingEvent> MeetMeStopTalking;
        /// <summary>
        /// A MeetMeTalkingEvent is triggered when a user starts talking in a meet me conference.<br/>
        /// To enable talker detection you must pass the option 'T' to the MeetMe application.
        /// </summary>
        public event EventHandler<MeetmeTalkingEvent> MeetMeTalking;
        /// <summary>
        /// A MessageWaiting is triggered when someone leaves voicemail.<br/>
        /// </summary>
        public event EventHandler<MessageWaitingEvent> MessageWaiting;
        /// <summary>
        /// A NewCallerId is triggered when the caller id of a channel changes.<br/>
        /// </summary>
        public event EventHandler<NewCallerIdEvent> NewCallerId;
        /// <summary>
        /// A NewChannel is triggered when a new channel is created.<br/>
        /// </summary>
        public event EventHandler<NewChannelEvent> NewChannel;
        /// <summary>
        /// A NewExten is triggered when a channel is connected to a new extension.<br/>
        /// </summary>
        public event EventHandler<NewExtenEvent> NewExten;
        /// <summary>
        /// A NewState is triggered when the state of a channel has changed.<br/>
        /// </summary>
        public event EventHandler<NewStateEvent> NewState;
        // public event EventHandler<OriginateEvent> Originate;
        /// <summary>
        /// An OriginateFailure is triggered when the execution of an OriginateAction failed.
        /// </summary>
        // public event EventHandler<OriginateFailureEvent> OriginateFailure;
        /// <summary>
        /// An OriginateSuccess is triggered when the execution of an OriginateAction succeeded.
        /// </summary>
        // public event EventHandler<OriginateSuccessEvent> OriginateSuccess;
        /// <summary>
        /// An OriginateResponse is triggered when the execution of an Originate.
        /// </summary>
        public event EventHandler<OriginateResponseEvent> OriginateResponse;
        /// <summary>
        /// A ParkedCall is triggered when a channel is parked (in this case no
        /// action id is set) and in response to a ParkedCallsAction.<br/>
        /// </summary>
        public event EventHandler<ParkedCallEvent> ParkedCall;
        /// <summary>
        /// A ParkedCallGiveUp is triggered when a channel that has been parked is hung up.<br/>
        /// </summary>
        public event EventHandler<ParkedCallGiveUpEvent> ParkedCallGiveUp;
        /// <summary>
        /// A ParkedCallsComplete is triggered after all parked calls have been reported in response to a ParkedCallsAction.
        /// </summary>
        public event EventHandler<ParkedCallsCompleteEvent> ParkedCallsComplete;
        /// <summary>
        /// A ParkedCallTimeOut is triggered when call parking times out for a given channel.<br/>
        /// </summary>
        public event EventHandler<ParkedCallTimeOutEvent> ParkedCallTimeOut;
        /// <summary>
        /// A PeerEntry is triggered in response to a SIPPeersAction or SIPShowPeerAction and contains information about a peer.<br/>
        /// </summary>
        public event EventHandler<PeerEntryEvent> PeerEntry;
        /// <summary>
        /// A PeerlistComplete is triggered after the details of all peers has been reported in response to an SIPPeersAction or SIPShowPeerAction.<br/>
        /// </summary>
        public event EventHandler<PeerlistCompleteEvent> PeerlistComplete;
        /// <summary>
        /// A PeerStatus is triggered when a SIP or IAX client attempts to registrer at this asterisk server.<br/>
        /// </summary>
        public event EventHandler<PeerStatusEvent> PeerStatus;
        /// <summary>
        /// A QueueEntryEvent is triggered in response to a QueueStatusAction and contains information about an entry in a queue.
        /// </summary>
        public event EventHandler<QueueCallerAbandonEvent> QueueCallerAbandon;
        /// <summary>
        /// A QueueEntryEvent is triggered in response to a QueueStatusAction and contains information about an entry in a queue.
        /// </summary>
        public event EventHandler<QueueEntryEvent> QueueEntry;
        /// <summary>
        /// A QueueMemberAddedEvent is triggered when a queue member is added to a queue.
        /// </summary>
        public event EventHandler<QueueMemberAddedEvent> QueueMemberAdded;
        /// <summary>
        /// A QueueMemberEvent is triggered in response to a QueueStatusAction and contains information about a member of a queue.
        /// </summary>
        public event EventHandler<QueueMemberEvent> QueueMember;
        public event EventHandler<QueueMemberPenaltyEvent> QueueMemberPenalty;
        public event EventHandler<QueueMemberRinginuseEvent> QueueMemberRinginuse;
        /// <summary>
        /// A QueueMemberPausedEvent is triggered when a queue member is paused or unpaused.
        /// <b>Replaced by : </b> <see cref="QueueMemberPauseEvent"/> since <see href="https://wiki.asterisk.org/wiki/display/AST/Asterisk+12+Documentation" target="_blank" alt="Asterisk 12 wiki docs">Asterisk 12</see>.<br/>
        /// <b>Removed since : </b> <see href="https://wiki.asterisk.org/wiki/display/AST/Asterisk+13+Documentation" target="_blank" alt="Asterisk 13 wiki docs">Asterisk 13</see>.<br/>
        /// </summary>
        public event EventHandler<QueueMemberPausedEvent> QueueMemberPaused;
        /// <summary>
        /// A QueueMemberRemovedEvent is triggered when a queue member is removed from a queue.
        /// </summary>
        public event EventHandler<QueueMemberRemovedEvent> QueueMemberRemoved;
        /// <summary>
        /// A QueueMemberStatusEvent shows the status of a QueueMemberEvent.
        /// </summary>
        public event EventHandler<QueueMemberStatusEvent> QueueMemberStatus;
        /// <summary>
        /// A QueueParamsEvent is triggered in response to a QueueStatusAction and contains the parameters of a queue.
        /// </summary>
        public event EventHandler<QueueParamsEvent> QueueParams;
        /// <summary>
        /// A QueueStatusCompleteEvent is triggered after the state of all queues has been reported in response to a QueueStatusAction.
        /// </summary>
        public event EventHandler<QueueStatusCompleteEvent> QueueStatusComplete;
        /// <summary>
        /// A Registry is triggered when this asterisk server attempts to register
        /// as a client at another SIP or IAX server.<br/>
        /// </summary>
        public event EventHandler<RegistryEvent> Registry;
        /// <summary>
        /// A RenameEvent is triggered when the name of a channel is changed.
        /// </summary>
        public event EventHandler<RenameEvent> Rename;
        /// <summary>
        /// A StatusCompleteEvent is triggered after the state of all channels has been reported in response to a StatusAction.
        /// </summary>
        public event EventHandler<StatusCompleteEvent> StatusComplete;
        /// <summary>
        /// A StatusEvent is triggered for each active channel in response to a StatusAction.
        /// </summary>
        public event EventHandler<StatusEvent> Status;
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<TransferEvent> Transfer;
        /// <summary>
        /// An UnholdEvent is triggered by the SIP channel driver when a channel is no longer put on hold.
        /// </summary>
        public event EventHandler<UnholdEvent> Unhold;
        /// <summary>
        /// An UnlinkEvent is triggered when a link between two voice channels is discontinued, for example, just before call completion.
        /// </summary>
        public event EventHandler<UnlinkEvent> Unlink;
        /// <summary>
        /// A UnparkedCallEvent is triggered when a channel that has been parked is resumed.
        /// </summary>
        public event EventHandler<UnparkedCallEvent> UnparkedCall;
        /// <summary>
        /// A ZapShowChannelsEvent is triggered on UserEvent in dialplan.
        /// </summary>
        public event EventHandler<UserEvent> UserEvents;
        /// <summary>
        /// A ZapShowChannelsCompleteEvent is triggered after the state of all zap channels has been reported in response to a ZapShowChannelsAction.
        /// </summary>
        public event EventHandler<ZapShowChannelsCompleteEvent> ZapShowChannelsComplete;
        /// <summary>
        /// A ZapShowChannelsEvent is triggered in response to a ZapShowChannelsAction and shows the state of a zap channel.
        /// </summary>
        public event EventHandler<ZapShowChannelsEvent> ZapShowChannels;
        /// <summary>
        /// A ConnectionState is triggered after Connect/Disconnect/Shutdown events. <br />
        /// Not a manager event
        /// </summary>
        public event EventHandler<ConnectionStateEvent> ConnectionState;

        /// <summary>
        /// A Reload is triggered after Reload events.
        /// </summary>
        public event EventHandler<ReloadEvent> Reload;

        /// <summary>
        /// When a variable is set
        /// </summary>
        public event EventHandler<VarSetEvent> VarSet;

        /// <summary>
        /// AgiExec is execute
        /// </summary>
        public event EventHandler<AGIExecEvent> AGIExec;

        /// <summary>
        /// This event is sent when the first user requests a conference and it is instantiated
        /// </summary>
        public event EventHandler<ConfbridgeStartEvent> ConfbridgeStart;

        /// <summary>
        /// This event is sent when a user joins a conference - either one already in progress or as the first user to join a newly instantiated bridge.
        /// </summary>
        public event EventHandler<ConfbridgeJoinEvent> ConfbridgeJoin;

        /// <summary>
        /// This event is sent when a user leaves a conference.
        /// </summary>
        public event EventHandler<ConfbridgeLeaveEvent> ConfbridgeLeave;

        /// <summary>
        /// This event is sent when the last user leaves a conference and it is torn down.
        /// </summary>
        public event EventHandler<ConfbridgeEndEvent> ConfbridgeEnd;

        /// <summary>
        /// This event is sent when the conference detects that a user has either begin or stopped talking.
        /// </summary>
        public event EventHandler<ConfbridgeTalkingEvent> ConfbridgeTalking;

        /// <summary>
        /// This event is sent when a Confbridge participant mutes.
        /// </summary>
        public event EventHandler<ConfbridgeMuteEvent> ConfbridgeMute;

        /// <summary>
        /// This event is sent when a Confbridge participant unmutes.
        /// </summary>
        public event EventHandler<ConfbridgeUnmuteEvent> ConfbridgeUnmute;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<FailedACLEvent> FailedACL;

        public event EventHandler<AttendedTransferEvent> AttendedTransfer;
        public event EventHandler<BlindTransferEvent> BlindTransfer;

        public event EventHandler<BridgeCreateEvent> BridgeCreate;
        public event EventHandler<BridgeDestroyEvent> BridgeDestroy;
        public event EventHandler<BridgeEnterEvent> BridgeEnter;
        public event EventHandler<BridgeLeaveEvent> BridgeLeave;

        /// <summary>
        /// Raised when a dial action has started.<br/>
        /// </summary>
        public event EventHandler<DialBeginEvent> DialBegin;

        /// <summary>
        /// Raised when a dial action has completed.<br/>
        /// </summary>
        public event EventHandler<DialEndEvent> DialEnd;

        /// <summary>
        /// Raised when a caller joins a Queue.<br/>
        /// </summary>
        public event EventHandler<QueueCallerJoinEvent> QueueCallerJoin;

        /// <summary>
        /// Raised when a caller leaves a Queue.<br/>
        /// </summary>
        public event EventHandler<QueueCallerLeaveEvent> QueueCallerLeave;

        /// <summary>
        /// A QueueMemberPauseEvent is triggered when a queue member is paused or unpaused.<br />
        /// <b>Available since : </b> <see href="https://wiki.asterisk.org/wiki/display/AST/Asterisk+12+Documentation" target="_blank" alt="Asterisk 12 wiki docs">Asterisk 12</see>.
        /// </summary>
        public event EventHandler<QueueMemberPauseEvent> QueueMemberPause;

        /// <summary>
        ///    Raised when music on hold has started/stopped on a channel.<br />
        ///    <b>Available since : </b> Asterisk 1.6.
        /// </summary>
        public event EventHandler<MusicOnHoldEvent> MusicOnHold;

        /// <summary>
        ///    Raised when music on hold has started on a channel.<br />
        ///    <b>Available since : </b> <see href="https://wiki.asterisk.org/wiki/display/AST/Asterisk+12+Documentation" target="_blank" alt="Asterisk 12 wiki docs">Asterisk 12</see>.
        /// </summary>
        public event EventHandler<MusicOnHoldStartEvent> MusicOnHoldStart;

        /// <summary>
        ///    Raised when music on hold has stopped on a channel.<br />
        ///    <b>Available since : </b> <see href="https://wiki.asterisk.org/wiki/display/AST/Asterisk+12+Documentation" target="_blank" alt="Asterisk 12 wiki docs">Asterisk 12</see>.
        /// </summary>
        public event EventHandler<MusicOnHoldStopEvent> MusicOnHoldStop;

        /// <summary>
        /// A ChallengeResponseFailed is triggered when a request's attempt to authenticate has been challenged, and the request failed the authentication challenge.
        /// </summary>
        public event EventHandler<ChallengeResponseFailedEvent> ChallengeResponseFailed;

        /// <summary>
        /// A InvalidAccountID is triggered when a request fails an authentication check due to an invalid account ID.
        /// </summary>
        public event EventHandler<InvalidAccountIDEvent> InvalidAccountID;

        /// <summary>
        /// A DeviceStateChanged is triggered when a device state changes.
        /// </summary>
        public event EventHandler<DeviceStateChangeEvent> DeviceStateChanged;

        /// <summary>
        /// A ChallengeSent is triggered when an Asterisk service sends an authentication challenge to a request..
        /// </summary>
        public event EventHandler<ChallengeSentEvent> ChallengeSent;

        /// <summary>
        /// A SuccessfulAuth is triggered when a request successfully authenticates with a service.
        /// </summary>
        public event EventHandler<SuccessfulAuthEvent> SuccessfulAuth;

        /// <summary>
        /// Raised when call queue summary
        /// </summary>
        public event EventHandler<QueueSummaryEvent> QueueSummary;

        /// <summary>
        /// Raised when a request provides an invalid password during an authentication attempt
        /// </summary>
        public event EventHandler<InvalidPasswordEvent> InvalidPassword;

        #endregion
    }
}
