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
using AsterNET.Helpers;
using System.Linq;
using Sufficit.Manager.Events;
using System.Net.Sockets;
using Sufficit.Asterisk.IO;

namespace AsterNET.Manager
{
    /// <summary>
    /// Default implementation of the ManagerConnection interface.
    /// </summary>
    public partial class ManagerConnection : IManagerConnection
    {
        private ISocketConnection? mrSocket;
        protected void SocketDisposing(object? sender, EventArgs args)
        {
            _logger.LogDebug("internal socket is disposing");
            if (mrSocket != null)
            {
                mrSocket.OnDisposing -= SocketDisposing;
                mrSocket = null;
            }

            // what to do ?
        }

        protected void SocketDisconnected(object? sender, string? cause)
        {
            _logger.LogDebug("internal socket was disconnected, cause: {cause}", cause);
            if (mrSocket != null && !keepAlive)
            {
                mrSocket.OnDisconnected -= SocketDisconnected;
                mrSocket = null;
            }

            // what to do ?
        }


        public char[] VAR_DELIMITER = { '|' };

        private readonly ILogger _logger;
        private long actionIdCount = 0;
        private string hostname;
        private int port;
        private string username;
        private string password;

        private Thread mrReaderThread;
        private ManagerReader? mrReader;

        private uint defaultResponseTimeout = 2000;
        private uint defaultEventTimeout = 5000;
        private int sleepTime = 50;
        private bool keepAlive = true;
        private bool keepAliveAfterAuthenticationFailure = false;
        private string protocolIdentifier;
        private AsteriskVersion asteriskVersion;
        private Dictionary<int, IResponseHandler> responseHandlers;
        private Dictionary<int, IResponseHandler> pingHandlers;
        private Dictionary<int, IResponseHandler> responseEventHandlers;
        private int pingInterval = 10000;

        private object lockSocket = new object();
        private object lockSocketWrite = new object();
        private object lockHandlers = new object();

        private bool enableEvents = true;
        private string version = string.Empty;
        private Encoding socketEncoding = Encoding.ASCII;
        private bool reconnected = false;
        private bool reconnectEnable = false;
        private int reconnectCount;

        private Dictionary<int, ConstructorInfo> registeredEventClasses;
        private Dictionary<int, Func<IManagerEvent, bool>> registeredEventHandlers;
        private event EventHandler<IManagerEvent> internalEvent;
        private bool fireAllEvents = false;
        private Thread callerThread;

        /// <summary> Default Fast Reconnect retry counter.</summary>
        private int reconnectRetryFast = 5;
        /// <summary> Default Maximum Reconnect retry counter.</summary>
        private int reconnectRetryMax = 10;
        /// <summary> Default Fast Reconnect interval in milliseconds.</summary>
        private int reconnectIntervalFast = 5000;
        /// <summary> Default Slow Reconnect interval in milliseconds.</summary>
        private int reconnectIntervalMax = 10000;


        /// <summary>
        /// Allows you to specifiy how events are fired. If false (default) then
        /// events will be fired in order. Otherwise events will be fired as they arrive and 
        /// control logic in your application will need to handle synchronization.
        /// </summary>
        public bool UseASyncEvents = false;

        /// <summary>
        /// Permit extensions to log using this object as state
        /// </summary>
        public ILogger Logger => _logger;

        #region Constructor - ManagerConnection()
        /// <summary> Creates a new instance.</summary>
        public ManagerConnection(ILogger<ManagerConnection> logger)
        {
            _logger = logger;

            callerThread = Thread.CurrentThread;

            socketEncoding = Encoding.ASCII;

            responseHandlers = new Dictionary<int, IResponseHandler>();
            pingHandlers = new Dictionary<int, IResponseHandler>();
            responseEventHandlers = new Dictionary<int, IResponseHandler>();
            registeredEventClasses = new Dictionary<int, ConstructorInfo>();

            Helper.Log(logger);
            Helper.RegisterBuiltinEventClasses(registeredEventClasses);

            registeredEventHandlers = new Dictionary<int, Func<IManagerEvent, bool>>();

            #region Event mapping table
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AgentCallbackLoginEvent), arg => fireEvent(AgentCallbackLogin, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AgentCallbackLogoffEvent), arg => fireEvent(AgentCallbackLogoff, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AgentCalledEvent), arg => fireEvent(AgentCalled, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AgentCompleteEvent), arg => fireEvent(AgentComplete, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AgentConnectEvent), arg => fireEvent(AgentConnect, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AgentDumpEvent), arg => fireEvent(AgentDump, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AgentLoginEvent), arg => fireEvent(AgentLogin, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AgentLogoffEvent), arg => fireEvent(AgentLogoff, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AgentRingNoAnswerEvent), arg => fireEvent(AgentRingNoAnswer, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AgentsCompleteEvent), arg => fireEvent(AgentsComplete, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AgentsEvent), arg => fireEvent(Agents, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AlarmClearEvent), arg => fireEvent(AlarmClear, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AlarmEvent), arg => fireEvent(Alarm, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(CdrEvent), arg => fireEvent(Cdr, arg));

            Helper.RegisterEventHandler(registeredEventHandlers, typeof(DBGetResponseEvent), arg => fireEvent(DBGetResponse, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(DialEvent), arg => fireEvent(Dial, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(DNDStateEvent), arg => fireEvent(DNDState, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ExtensionStatusEvent), arg => fireEvent(ExtensionStatus, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(HangupEvent), arg => fireEvent(Hangup, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(HangupRequestEvent), arg => fireEvent(HangupRequest, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(HoldedCallEvent), arg => fireEvent(HoldedCall, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(HoldEvent), arg => fireEvent(Hold, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(JoinEvent), arg => fireEvent(Join, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(LeaveEvent), arg => fireEvent(Leave, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(LinkEvent), arg => fireEvent(Link, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(LogChannelEvent), arg => fireEvent(LogChannel, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(MeetmeJoinEvent), arg => fireEvent(MeetMeJoin, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(MeetmeLeaveEvent), arg => fireEvent(MeetMeLeave, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(MeetmeTalkingEvent), arg => fireEvent(MeetMeTalking, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(MessageWaitingEvent), arg => fireEvent(MessageWaiting, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(NewCallerIdEvent), arg => fireEvent(NewCallerId, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(NewChannelEvent), arg => fireEvent(NewChannel, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(NewExtenEvent), arg => fireEvent(NewExten, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(NewStateEvent), arg => fireEvent(NewState, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(OriginateResponseEvent), arg => fireEvent(OriginateResponse, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ParkedCallEvent), arg => fireEvent(ParkedCall, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ParkedCallGiveUpEvent), arg => fireEvent(ParkedCallGiveUp, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ParkedCallsCompleteEvent), arg => fireEvent(ParkedCallsComplete, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ParkedCallTimeOutEvent), arg => fireEvent(ParkedCallTimeOut, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(PeerEntryEvent), arg => fireEvent(PeerEntry, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(PeerlistCompleteEvent), arg => fireEvent(PeerlistComplete, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(PeerStatusEvent), arg => fireEvent(PeerStatus, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueEntryEvent), arg => fireEvent(QueueEntry, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueMemberAddedEvent), arg => fireEvent(QueueMemberAdded, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueMemberEvent), arg => fireEvent(QueueMember, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueMemberRinginuseEvent), arg => fireEvent(QueueMemberRinginuse, arg)); 
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueMemberPausedEvent), arg => fireEvent(QueueMemberPaused, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueMemberPenaltyEvent), arg => fireEvent(QueueMemberPenalty, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueMemberRemovedEvent), arg => fireEvent(QueueMemberRemoved, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueMemberStatusEvent), arg => fireEvent(QueueMemberStatus, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueParamsEvent), arg => fireEvent(QueueParams, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueStatusCompleteEvent), arg => fireEvent(QueueStatusComplete, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(RegistryEvent), arg => fireEvent(Registry, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueCallerAbandonEvent), arg => fireEvent(QueueCallerAbandon, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(RenameEvent), arg => fireEvent(Rename, arg));

            Helper.RegisterEventHandler(registeredEventHandlers, typeof(StatusCompleteEvent), arg => fireEvent(StatusComplete, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(StatusEvent), arg => fireEvent(Status, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(UnholdEvent), arg => fireEvent(Unhold, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(UnlinkEvent), arg => fireEvent(Unlink, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(UnparkedCallEvent), arg => fireEvent(UnparkedCall, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(UserEvent), arg => fireEvent(UserEvents, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ZapShowChannelsCompleteEvent), arg => fireEvent(ZapShowChannelsComplete, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ZapShowChannelsEvent), arg => fireEvent(ZapShowChannels, arg));

            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ConnectEvent), arg => fireEvent(ConnectionState, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(DisconnectEvent), arg => fireEvent(ConnectionState, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ReloadEvent), arg => fireEvent(Reload, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ShutdownEvent), arg => fireEvent(ConnectionState, arg));

            Helper.RegisterEventHandler(registeredEventHandlers, typeof(BridgeEvent), arg => fireEvent(Bridge, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(TransferEvent), arg => fireEvent(Transfer, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(DTMFEvent), arg => fireEvent(DTMF, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(DTMFBeginEvent), arg => fireEvent(DTMFBegin, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(DTMFEndEvent), arg => fireEvent(DTMFEnd, arg));

            Helper.RegisterEventHandler(registeredEventHandlers, typeof(VarSetEvent), arg => fireEvent(VarSet, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AGIExecEvent), arg => fireEvent(AGIExec, arg));

            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ConfbridgeStartEvent), arg => fireEvent(ConfbridgeStart, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ConfbridgeJoinEvent), arg => fireEvent(ConfbridgeJoin, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ConfbridgeLeaveEvent), arg => fireEvent(ConfbridgeLeave, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ConfbridgeEndEvent), arg => fireEvent(ConfbridgeEnd, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ConfbridgeTalkingEvent), arg => fireEvent(ConfbridgeTalking, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ConfbridgeMuteEvent), arg => fireEvent(ConfbridgeMute, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ConfbridgeUnmuteEvent), arg => fireEvent(ConfbridgeUnmute, arg));

            Helper.RegisterEventHandler(registeredEventHandlers, typeof(FailedACLEvent), arg => fireEvent(FailedACL, arg));

            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ChannelUpdateEvent), arg => fireEvent(ChannelUpdate, arg));

            Helper.RegisterEventHandler(registeredEventHandlers, typeof(CoreShowChannelEvent), arg => fireEvent(CoreShowChannel, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(CoreShowChannelsCompleteEvent), arg => fireEvent(CoreShowChannelsComplete, arg));

            Helper.RegisterEventHandler(registeredEventHandlers, typeof(AttendedTransferEvent), arg => fireEvent(AttendedTransfer, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(BridgeCreateEvent), arg => fireEvent(BridgeCreate, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(BridgeDestroyEvent), arg => fireEvent(BridgeDestroy, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(BridgeEnterEvent), arg => fireEvent(BridgeEnter, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(BridgeLeaveEvent), arg => fireEvent(BridgeLeave, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(BlindTransferEvent), arg => fireEvent(BlindTransfer, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(DialBeginEvent), arg => fireEvent(DialBegin, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(DialEndEvent), arg => fireEvent(DialEnd, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueCallerJoinEvent), arg => fireEvent(QueueCallerJoin, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueCallerLeaveEvent), arg => fireEvent(QueueCallerLeave, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueMemberPauseEvent), arg => fireEvent(QueueMemberPause, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(MusicOnHoldEvent), arg => fireEvent(MusicOnHold, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(MusicOnHoldStartEvent), arg => fireEvent(MusicOnHoldStart, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(MusicOnHoldStopEvent), arg => fireEvent(MusicOnHoldStop, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ChallengeResponseFailedEvent), arg => fireEvent(ChallengeResponseFailed, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(InvalidAccountIDEvent), arg => fireEvent(InvalidAccountID, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(DeviceStateChangeEvent), arg => fireEvent(DeviceStateChanged, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(ChallengeSentEvent), arg => fireEvent(ChallengeSent, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(SuccessfulAuthEvent), arg => fireEvent(SuccessfulAuth, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(QueueSummaryEvent), arg => fireEvent(QueueSummary, arg));
            Helper.RegisterEventHandler(registeredEventHandlers, typeof(InvalidPasswordEvent), arg => fireEvent(InvalidPassword, arg));

            #endregion

            this.internalEvent += new EventHandler<IManagerEvent>(internalEventHandler);
        }
        #endregion

        #region Constructor - ManagerConnection(hostname, port, username, password)
        /// <summary>
        /// Creates a new instance with the given connection parameters.
        /// </summary>
        /// <param name="hostname">the hosname of the Asterisk server to connect to.</param>
        /// <param name="port">the port where Asterisk listens for incoming Manager API connections, usually 5038.</param>
        /// <param name="username">the username to use for login</param>
        /// <param name="password">the password to use for login</param>
        public ManagerConnection(string hostname, int port, string username, string password) 
           : this(new LoggerFactory().CreateLogger<ManagerConnection>())
        {
            this.hostname = hostname;
            this.port = port;
            this.username = username;
            this.password = password;
        }
        #endregion

        public ManagerConnection(ILogger<ManagerConnection> logger, string hostname, int port, string username, string password) 
            : this(logger)
        {
            this.hostname = hostname;
            this.port = port;
            this.username = username;
            this.password = password;
        }

        #region Constructor - ManagerConnection(hostname, port, username, password, Encoding socketEncoding)
        /// <summary>
        /// Creates a new instance with the given connection parameters.
        /// </summary>
        /// <param name="hostname">the hosname of the Asterisk server to connect to.</param>
        /// <param name="port">the port where Asterisk listens for incoming Manager API connections, usually 5038.</param>
        /// <param name="username">the username to use for login</param>
        /// <param name="password">the password to use for login</param>
        /// <param name="socketEncoding">text encoding to asterisk input/output stream</param>
        public ManagerConnection(string hostname, int port, string username, string password, Encoding socketEncoding) 
            : this(new LoggerFactory().CreateLogger<ManagerConnection>())
        {
            this.hostname = hostname;
            this.port = port;
            this.username = username;
            this.password = password;
            this.socketEncoding = socketEncoding;
        }
        #endregion

        /// <summary>
        /// Default Fast Reconnect retry counter.
        /// </summary>
        public int ReconnectRetryFast
        {
            get { return reconnectRetryFast; }
            set { reconnectRetryFast = value; }
        }
        /// <summary> Default Maximum Reconnect retry counter.</summary>
        public int ReconnectRetryMax
        {
            get { return reconnectRetryMax; }
            set { reconnectRetryMax = value; }
        }
        /// <summary> Default Fast Reconnect interval in milliseconds.</summary>
        public int ReconnectIntervalFast
        {
            get { return reconnectIntervalFast; }
            set { reconnectIntervalFast = value; }
        }
        /// <summary> Default Slow Reconnect interval in milliseconds.</summary>
        public int ReconnectIntervalMax
        {
            get { return reconnectIntervalMax; }
            set { reconnectIntervalMax = value; }
        }

        #region CallerThread
        internal Thread CallerThread
        {
            get { return callerThread; }
        }
        #endregion

        #region internalEventHandler(object sender, IManagerEvent e)
        private void internalEventHandler(object sender, IManagerEvent e)
        {
            int eventHash = e.GetType().Name.GetHashCode();
            int userEventHash = typeof(UserEvent).Name.GetHashCode();
            if (registeredEventHandlers.TryGetValue(eventHash, out var currentEvent)
            || (registeredEventHandlers.TryGetValue(userEventHash, out currentEvent) && typeof(UserEvent).IsAssignableFrom(e.GetType())))
            {
                if (currentEvent(e))
                {
                    return;
                }
            }

            if (fireAllEvents)
            {
                fireEvent(UnhandledEvent, e);
            }
        }
        #endregion

        #region FireAllEvents
        /// <summary>
        /// If this property set to <b>true</b> then ManagerConnection send all unassigned events to UnhandledEvent handler,<br/>
        /// if set to <b>false</b> then all unassgned events lost and send only UnhandledEvent.<br/>
        /// Default: <b>false</b>
        /// </summary>
        public bool FireAllEvents
        {
            get { return this.fireAllEvents; }
            set { this.fireAllEvents = value; }
        }
        #endregion

        #region PingInterval
        /// <summary>
        /// Timeout from Ping to Pong. If no Pong received send Disconnect event. Set to zero to disable.
        /// </summary>
        public int PingInterval
        {
            get { return pingInterval; }
            set { pingInterval = value; }
        }
        #endregion

        #region Hostname
        /// <summary> Sets the hostname of the asterisk server to connect to.<br/>
        /// Default is localhost.
        /// </summary>
        public string Hostname
        {
            get { return hostname; }
            set { hostname = value; }
        }
        #endregion

        #region Port
        /// <summary>
        /// Sets the port to use to connect to the asterisk server. This is the port
        /// specified in asterisk's manager.conf file.<br/>
        /// Default is 5038.
        /// </summary>
        public int Port
        {
            get { return port; }
            set { port = value; }
        }
        #endregion

        #region UserName
        /// <summary>
        /// Sets the username to use to connect to the asterisk server. This is the
        /// username specified in asterisk's manager.conf file.
        /// </summary>
        public string Username
        {
            get { return username; }
            set { username = value; }
        }
        #endregion

        #region Password
        /// <summary>
        /// Sets the password to use to connect to the asterisk server. This is the
        /// password specified in asterisk's manager.conf file.
        /// </summary>
        public string Password
        {
            get { return password; }
            set { password = value; }
        }
        #endregion

        #region DefaultResponseTimeout
        /// <summary> Sets the time in milliseconds the synchronous method
        /// will wait for a response before throwing a TimeoutException.<br/>
        /// Default is 2000.
        /// </summary>
        public uint DefaultResponseTimeout
        {
            get { return defaultResponseTimeout; }
            set { defaultResponseTimeout = value; }
        }
        #endregion

        #region DefaultEventTimeout
        /// <summary> Sets the time in milliseconds the synchronous method
        /// will wait for a response and the last response event before throwing a TimeoutException.<br/>
        /// Default is 5000.
        /// </summary>
        public uint DefaultEventTimeout
        {
            get { return defaultEventTimeout; }
            set { defaultEventTimeout = value; }
        }
        #endregion

        #region SleepTime
        /// <summary> Sets the time in milliseconds the synchronous methods
        /// SendAction(Action.ManagerAction) and
        /// SendAction(Action.ManagerAction, long) will sleep between two checks
        /// for the arrival of a response. This value should be rather small.<br/>
        /// The sleepTime attribute is also used when checking for the protocol
        /// identifer.<br/>
        /// Default is 50.
        /// </summary>
        /// <deprecated> this has been replaced by an interrupt based response checking approach.</deprecated>
        public int SleepTime
        {
            get { return sleepTime; }
            set { sleepTime = value; }
        }
        #endregion

        #region KeepAliveAfterAuthenticationFailure
        /// <summary> Set to true to try reconnecting to ther asterisk serve
        /// even if the reconnection attempt threw an AuthenticationFailedException.<br/>
        /// Default is false.
        /// </summary>
        public bool KeepAliveAfterAuthenticationFailure
        {
            set { keepAliveAfterAuthenticationFailure = value; }
            get { return keepAliveAfterAuthenticationFailure; }
        }
        #endregion

        #region KeepAlive
        /// <summary>
        /// Should we attempt to reconnect when the connection is lost?<br/>
        /// This is set to true after successful login and to false after logoff or after an authentication failure when keepAliveAfterAuthenticationFailure is false.
        /// </summary>
        public bool KeepAlive
        {
            get { return keepAlive; }
            set { keepAlive = value; }
        }
        #endregion

        #region Socket Settings

        /// <summary>
        /// Socket Encoding - default ASCII
        /// </summary>
        /// <remarks>
        /// Attention!
        /// <para>
        /// The value of this property must be set before establishing a connection with the Asterisk.
        /// Changing the property doesn't do anything while you are already connected.
        /// </para>
        /// </remarks>
        public Encoding SocketEncoding
        {
            get { return socketEncoding; }
            set { socketEncoding = value; }
        }

        /// <summary>
        /// Socket Receive Buffer Size
        /// </summary>
        /// <remarks>
        /// Attention!
        /// <para>
        /// The value of this property must be set before establishing a connection with the Asterisk.
        /// Changing the property doesn't do anything while you are already connected.
        /// </para>
        /// </remarks>
        public int SocketReceiveBufferSize { get; set;}

        #endregion

        #region Version
        public string Version
        {
            get { return version; }
        }
        #endregion

        #region AsteriskVersion
        public AsteriskVersion AsteriskVersion
        {
            get { return asteriskVersion; }
        }
        #endregion

        #region login(timeout)

        public Task Login(uint? timeout = null)
        {
            var cts = new CancellationTokenSource((int)(timeout ?? defaultResponseTimeout));
            return Login(cts.Token);
        }


        /// <summary>
        /// Logs in to the Asterisk manager using asterisk's MD5 based
        /// challenge/response protocol. The login is delayed until the protocol
        /// identifier has been received by the reader.
        /// </summary>
        /// <throws>  AuthenticationFailedException if the username and/or password are incorrect</throws>
        /// <throws>  TimeoutException if no response is received within the specified timeout period</throws>
        /// <seealso cref="Action.ChallengeAction"/>
        /// <seealso cref="Action.LoginAction"/>
        public async Task Login(CancellationToken cancellationToken)
        {
            
            enableEvents = false;
            if (reconnected)
            {
                string description = "Unable login during reconnect state.";
                var ex = new AuthenticationFailedException(description);
                _logger.LogError(ex, description);
                throw ex;
            }

            reconnectEnable = false;
            DateTime start = DateTime.Now;
            while (string.IsNullOrEmpty(protocolIdentifier))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    disconnect(true);
                    throw new TimeoutException("Timeout waiting for protocol identifier");
                }

                if (connect())
                {
                    // Increase delay after connection up to 500 ms
                    Thread.Sleep(10 * sleepTime);   // 200 milliseconds delay
                }
                try
                {
                    Thread.Sleep(4 * sleepTime);    // 200 milliseconds delay
                }
                catch
                { }
            };

            ChallengeAction challengeAction = new ChallengeAction();
            Response.ManagerResponse response = SendAction(challengeAction, defaultResponseTimeout * 2);
            if (response is ChallengeResponse)
            {
                ChallengeResponse challengeResponse = (ChallengeResponse)response;
                string key, challenge = challengeResponse.Challenge;
                try
                {
                    Util.MD5Support md = Util.MD5Support.GetInstance();
                    if (challenge != null)
                        md.Update(UTF8Encoding.UTF8.GetBytes(challenge));
                    if (password != null)
                        md.Update(UTF8Encoding.UTF8.GetBytes(password));
                    key = Helper.ToHexString(md.DigestData);
                }
                catch (Exception ex)
                {
                    disconnect(true);

                    string description = "Unable to create login key using MD5 Message Digest.";
                    var newException = new AuthenticationFailedException(description, ex);
                    _logger.LogError(newException, description);
                    throw newException;
                }

                var loginAction = new LoginAction(username, "MD5", key);
                ManagerResponse loginResponse = SendAction(loginAction);
                if (loginResponse is Response.ManagerError)
                {
                    disconnect(true);
                    throw new AuthenticationFailedException(loginResponse.Message);
                }

                // successfully logged in so assure that we keep trying to reconnect when disconnected
                reconnectEnable = keepAlive;

                _logger.LogInformation($"Successfully LoggedIn: { loginResponse.ToJson() }");

                asteriskVersion = determineVersion();

                _logger.LogInformation("Determined Asterisk version: " + asteriskVersion);

				enableEvents = true;
				ConnectEvent ce = new ConnectEvent();
				ce.ProtocolIdentifier = this.protocolIdentifier;
				DispatchEvent(ce);
			}
			else if (response is ManagerError)
				throw new ManagerException("Unable login to Asterisk - " + response.Message);
			else
				throw new ManagerException("Unknown response during login to Asterisk - " + response.GetType().Name + " with message " + response.Message);
		}

		#endregion

		#region determineVersion()

        protected internal bool TryVersionByCoreShowVersion(out AsteriskVersion astversion, out string version)
        {
            var command = new CommandAction("core show version");
            var actionResponse = SendAction(command, defaultResponseTimeout * 2);
            if (actionResponse is ManagerError error)
            {
                _logger.LogWarning("'core show version' error: {message}", error.Message);
            }
            else if (actionResponse is CommandResponse response) 
            {
                foreach (string line in response.Result)
                {
                    foreach (Match m in Common.ASTERISK_VERSION.Matches(line))
                    {
                        if (m.Groups.Count >= 2)
                        {
                            version = m.Groups[1].Value;
                            if (version.StartsWith("1.4."))
                            {
                                VAR_DELIMITER = new char[] { '|' };
                                astversion = AsteriskVersion.ASTERISK_1_4;
                                return true;
                            }
                            else if (version.StartsWith("1.6."))
                            {
                                VAR_DELIMITER = new char[] { '|' };
                                astversion = AsteriskVersion.ASTERISK_1_6;
                                return true;
                            }
                            else if (version.StartsWith("1.8."))
                            {
                                VAR_DELIMITER = new char[] { '|' };
                                astversion = AsteriskVersion.ASTERISK_1_8;
                                return true;
                            }
                            else if (version.StartsWith("10."))
                            {
                                VAR_DELIMITER = new char[] { '|' };
                                astversion = AsteriskVersion.ASTERISK_10;
                                return true;
                            }
                            else if (version.StartsWith("11."))
                            {
                                VAR_DELIMITER = new char[] { ',' };
                                astversion = AsteriskVersion.ASTERISK_11;
                                return true;
                            }
                            else if (version.StartsWith("12."))
                            {
                                VAR_DELIMITER = new char[] { ',' };
                                astversion = AsteriskVersion.ASTERISK_12;
                                return true;
                            }
                            else if (version.StartsWith("13."))
                            {
                                VAR_DELIMITER = new char[] { ',' };
                                astversion = AsteriskVersion.ASTERISK_13;
                                return true;
                            }
                            else if (version.StartsWith("14."))
                            {
                                VAR_DELIMITER = new char[] { ',' };
                                astversion = AsteriskVersion.ASTERISK_14;
                                return true;
                            }
                            else if (version.StartsWith("15."))
                            {
                                VAR_DELIMITER = new char[] { ',' };
                                astversion = AsteriskVersion.ASTERISK_15;
                                return true;
                            }
                            else if (version.StartsWith("16."))
                            {
                                VAR_DELIMITER = new char[] { ',' };
                                astversion = AsteriskVersion.ASTERISK_16;
                                return true;
                            }
                            else if (version.StartsWith("17."))
                            {
                                VAR_DELIMITER = new char[] { ',' };
                                astversion = AsteriskVersion.ASTERISK_17;
                                return true;
                            }
                            else if (version.IndexOf('.') >= 2)
                            {
                                VAR_DELIMITER = new char[] { ',' };
                                astversion = AsteriskVersion.ASTERISK_Newer;
                                return true;
                            }
                            else
                                throw new ManagerException("Unknown Asterisk version " + version);
                        }
                    }
                }                
            }
            else
            {
                _logger.LogWarning("'core show version' unknown type response: {message}, {type}", actionResponse.Message, actionResponse.GetType());
            }

            astversion = default;
            version = default;
            return false;
        }

        protected internal bool TryVersionByShowVersionFiles(out AsteriskVersion astversion)
        {
            var command = new CommandAction("show version files");
            var actionResponse = SendAction(command, defaultResponseTimeout * 2);
            if (actionResponse is ManagerError error)
            {
                _logger.LogWarning("'show version files' error: {message}", error.Message);
            }
            else if (actionResponse is CommandResponse response)
            {
                IList showVersionFilesResult = response.Result;
                if (showVersionFilesResult != null && showVersionFilesResult.Count > 0)
                {
                    var line1 = (string)showVersionFilesResult[0];
                    if (line1 != null && line1.StartsWith("File"))
                    {
                        VAR_DELIMITER = new char[] { '|' };
                        astversion = AsteriskVersion.ASTERISK_1_2;
                        return true;
                    }
                }                
            }
            else
            {
                _logger.LogWarning("'show version files' unknown type response: {message}, {type}", actionResponse.Message, actionResponse.GetType());
            }

            astversion = AsteriskVersion.Unknown;
            return false;
        }

        protected internal AsteriskVersion determineVersion()
		{
            if (!TryVersionByCoreShowVersion(out AsteriskVersion asteriskVersion, out string version))
                TryVersionByShowVersionFiles(out asteriskVersion);
            
            this.version = version;
            return asteriskVersion;
		}

		#endregion

		#region connect()
		protected internal bool connect()
		{
			bool result = false;
			bool startReader = false;

			lock (lockSocket)
			{
				if (mrSocket == null)
				{
                    _logger.LogInformation("Connecting to {0}:{1}", hostname, port);
                    try
                    {
                        SocketReceiveBufferSize = 100000;
                        var options = new AGISocketExtendedOptions()
                        {
                            Start = false,
                            BufferSize = (uint)SocketReceiveBufferSize,
                            Encoding = socketEncoding,
                        };                        
                        var client = new TcpClient(hostname, port);
                        mrSocket = new SocketConnection(_logger, options, client.Client);
                        mrSocket.OnDisposing += SocketDisposing;
                        mrSocket.OnDisconnected += SocketDisconnected;

                        result = mrSocket.IsConnected();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("Connect - Exception  : {0}", ex.Message);
                        result = false;
                    }

                    if (result)
                    {
                        if (mrReader == null)
                        {
                            mrReader = new ManagerReader(this);
                            startReader = true;

                            mrReaderThread = new Thread(mrReader.Run) { IsBackground = true, Name = "ManagerReader-" + DateTime.Now.Second };                            
                        }
                       
                        mrReader.Socket = mrSocket;   
                        mrReader.Reinitialize();
                    }
                    else
                    {
                        mrSocket = null;
                    }
                }
            }

            if (startReader)
            {
                mrReaderThread.Start();
            }

            return IsConnected();
        }
        #endregion

        #region disconnect()
        /// <summary> Closes the socket connection.</summary>
        private void disconnect(bool withDie)
        {
            lock (lockSocket)
            {
                if (withDie)
                {
                    reconnectEnable = false;
                    reconnected = false;
                    enableEvents = true;
                }

                if (mrReader != null)
                {
                    if (withDie)
                    {
                        mrReader.Die = true;
                        mrReader = null;
                    }
                    else
                        mrReader.Socket = null;
                }

                if (this.mrSocket != null)
                {
                    mrSocket.Close();
                    mrSocket = null;
                }

                responseEventHandlers.Clear();
                responseHandlers.Clear();
                pingHandlers.Clear();
            }
        }
        #endregion

        #region reconnect(bool init)
        /// <summary>
        /// Reconnects to the asterisk server when the connection is lost.<br/>
        /// While keepAlive is true we will try to reconnect.
        /// Reconnection attempts will be stopped when the logoff() method
        /// is called or when the login after a successful reconnect results in an
        /// AuthenticationFailedException suggesting that the manager
        /// credentials have changed and keepAliveAfterAuthenticationFailure is not set.<br/>
        /// This method is called when a DisconnectEvent is received from the reader.
        /// </summary>
        private void reconnect(bool init)
        {

            _logger.LogWarning("reconnect (init: {0}), reconnectCount:{1}", init, reconnectCount);
            if (init)
                reconnectCount = 0;
            else if (reconnectCount++ > reconnectRetryMax)
                reconnectEnable = false;

            if (reconnectEnable)
            {
                _logger.LogWarning("Try reconnect.");
                enableEvents = false;
                reconnected = true;
                disconnect(false);

                int retryCount = 0;
                while (reconnectEnable && !mrReader.Die)
                {
                    if (retryCount >= reconnectRetryMax)
                        reconnectEnable = false;
                    else
                    {
                        try
                        {
                            if (retryCount < reconnectRetryFast)
                            {
                                // Try to reconnect quite fast for the first times
                                // this succeeds if the server has just been restarted
                                _logger.LogInformation("Reconnect delay : {0}, retry : {1}", reconnectIntervalFast, retryCount);
                                Thread.Sleep(reconnectIntervalFast);
                            }
                            else
                            {
                                // slow down after unsuccessful attempts assuming a shutdown of the server
                                _logger.LogInformation("Reconnect delay : {0}, retry : {1}", reconnectIntervalMax, retryCount);
                                Thread.Sleep(reconnectIntervalMax);
                            }
                        }
                        catch (ThreadInterruptedException)
                        {
                            continue;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation("Reconnect delay exception : ", ex.Message);
                            continue;
                        }

                        try
                        {
                            _logger.LogInformation("Try connect.");
                            if (connect())
                                break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation("Connect exception : ", ex.Message);
                        }
                        retryCount++;
                    }
                }
            }

            if (!reconnectEnable)
            {
                _logger.LogInformation("Can't reconnect.");
                enableEvents = true;
                reconnected = false;
                disconnect(true);
                fireEvent(new DisconnectEvent());
            }
        }
        #endregion

        #region createInternalActionId()
        /// <summary>
        /// Creates a new unique internal action id based on the hash code of this connection and a sequence.
        /// </summary>
        private string createInternalActionId()
        {
            return this.GetHashCode() + "_" + (this.actionIdCount++);
        }
        #endregion

        #region IsConnected()

        /// <summary> Returns true if there is a socket connection to the
        /// asterisk server, false otherwise.
        /// 
        /// </summary>
        /// <returns> true if there is a socket connection to the
        /// asterisk server, false otherwise.
        /// </returns>
        public bool IsConnected()
            => mrSocket?.IsConnected() ?? false;

        #endregion

        #region Logoff()
        /// <summary>
        /// Sends a LogoffAction and disconnects from the server.
        /// </summary>
        public async Task LogOff(CancellationToken cancellationToken = default)
        {
            lock (lockSocket)
            {
                // stop reconnecting when we got disconnected
                reconnectEnable = false;
                if (mrReader != null && mrSocket != null)
                {
                    try
                    {
                        mrReader.IsLogoff = true;
                        var response = SendAction<LogOffAction>();
                        if (response != null && !response.IsSuccess())
                            throw new Exception(response.Message);
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "error on logoff");
                    }
                }
            }
            disconnect(true);
            await Task.CompletedTask;
        }
        #endregion

        #region SendAction(action)

        public ManagerResponse SendAction<T>() where T : ManagerAction, new()
            => SendAction(new T(), defaultResponseTimeout);

        /// <summary>
        /// Send Action with default timeout.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public ManagerResponse SendAction(ManagerAction action)
        {
            return SendAction(action, defaultResponseTimeout);
        }

        #endregion

        #region SendAction(action, timeout)
        /// <summary>
        /// Send action ans with timeout (milliseconds)
        /// </summary>
        /// <param name="action">action to send</param>
        /// <param name="timeout">timeout in milliseconds</param>
        /// <returns></returns>
        public ManagerResponse SendAction(ManagerAction action, uint timeout)
        {
            AutoResetEvent autoEvent = new AutoResetEvent(false);
            ResponseHandler handler = new ResponseHandler(action, autoEvent);

            _ = SendAction(action, handler);
            bool result = autoEvent.WaitOne(timeout <= 0 ? -1 : (int)timeout, true);

            RemoveResponseHandler(handler);

            if (result)
                return handler.Response;
            throw new TimeoutException("Timeout waiting for response to " + action.Action);
        }
        #endregion

        #region SendAction(action, responseHandler)
        /// <summary>
        /// Send action ans with timeout (milliseconds)
        /// </summary>
        /// <param name="action">action to send</param>
        /// <param name="responseHandler">Response Handler</param>
        /// <returns></returns>
        public int SendAction(ManagerAction action, IResponseHandler responseHandler)
        {
            if (action == null)
                throw new ArgumentException("Unable to send action: action is null.");

            if (mrSocket == null)
                throw new SystemException("Unable to send " + action.Action + " action: not connected.");

            // if the responseHandler is null the user is obviously not interested in the response, thats fine.
            string internalActionId = string.Empty;
            if (responseHandler != null)
            {
                internalActionId = createInternalActionId();
                responseHandler.Hash = internalActionId.GetHashCode();
                AddResponseHandler(responseHandler);
            }

            SendToAsterisk(action, internalActionId);

            return responseHandler != null ? responseHandler.Hash : 0;
        }
        #endregion

        #region SendActionAsync(action, timeout)

        public Task<ManagerResponse> SendActionAsync<T>(CancellationToken cancellationToken = default) where T : ManagerAction, new()
            => SendActionAsync(new T(), cancellationToken);

        /// <summary>
        /// Asynchronously send Action async.
        /// </summary>
        /// <param name="action">action to send</param>
        /// <param name="cancellationToken">cancellation Token</param>
        public Task<ManagerResponse> SendActionAsync(ManagerAction action, CancellationToken cancellationToken = default)
        {
          var handler = new TaskResponseHandler(action);
          var source = handler.TaskCompletionSource;

          SendAction(action, handler);
          cancellationToken.Register(() => { source.TrySetCanceled(); });

          return source.Task.ContinueWith(x =>
          {
            RemoveResponseHandler(handler);
            return x.Result;
          });
        }

        #endregion
        #region SendEventGeneratingAction(action)
        public ResponseEvents SendEventGeneratingAction(ManagerActionEvent action)
        {
            return SendEventGeneratingAction(action, defaultEventTimeout);
        }
        #endregion

        #region SendEventGeneratingAction(action, timeout)
        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="timeout">wait timeout in milliseconds</param>
        /// <returns></returns>
        public ResponseEvents SendEventGeneratingAction(ManagerActionEvent action, uint timeout)
        {
            if (action == null)
                throw new ArgumentException("Unable to send action: action is null.");
            else if (action.ActionCompleteEventClass() == null)
                throw new ArgumentException("Unable to send action: ActionCompleteEventClass is null.");
            else if (!typeof(IResponseEvent).IsAssignableFrom(action.ActionCompleteEventClass()))
                throw new ArgumentException("Unable to send action: ActionCompleteEventClass is not a ResponseEvent.");

            if (mrSocket == null)
                throw new SystemException("Unable to send " + action.Action + " action: not connected.");

            AutoResetEvent autoEvent = new AutoResetEvent(false);
            ResponseEventHandler handler = new ResponseEventHandler(this, action, autoEvent);

            string internalActionId = createInternalActionId();
            handler.Hash = internalActionId.GetHashCode();
            AddResponseHandler(handler);
            AddResponseEventHandler(handler);

            SendToAsterisk(action, internalActionId);

            bool result = autoEvent.WaitOne(timeout <= 0 ? -1 : (int)timeout, true);

            RemoveResponseHandler(handler);
            RemoveResponseEventHandler(handler);

            if (result)
                return handler.ResponseEvents;

            throw new EventTimeoutException("Timeout waiting for response or response events to " + action.Action, handler.ResponseEvents);
        }
        #endregion

        #region Response Handler helpers
        private void AddResponseHandler(IResponseHandler handler)
        {
            lock (lockHandlers)
            {
                if (handler.Action is PingAction)
                    pingHandlers[handler.Hash] = handler;
                else
                    responseHandlers[handler.Hash] = handler;
            }
        }

        private void AddResponseEventHandler(IResponseHandler handler)
        {
            lock (lockHandlers)
                responseEventHandlers[handler.Hash] = handler;
        }

        /// <summary>
        /// Delete an instance of a class <see cref="IResponseHandler"/> from handlers list.
        /// </summary>
        /// <param name="handler">Class instance <see cref="IResponseHandler"/>.</param>
		public void RemoveResponseHandler(IResponseHandler handler)
        {
            int hash = handler.Hash;
            if (hash != 0)
                lock (lockHandlers)
                    if (responseHandlers.ContainsKey(hash))
                        responseHandlers.Remove(hash);
        }

        internal void RemoveResponseEventHandler(IResponseHandler handler)
        {
            int hash = handler.Hash;
            if (hash != 0)
                lock (lockHandlers)
                    if (responseEventHandlers.ContainsKey(hash))
                        responseEventHandlers.Remove(hash);
        }
        private IResponseHandler GetRemoveResponseHandler(int hash)
        {
            IResponseHandler handler = null;
            if (hash != 0)
                lock (lockHandlers)
                    if (responseHandlers.ContainsKey(hash))
                    {
                        handler = responseHandlers[hash];
                        responseHandlers.Remove(hash);
                    }
            return handler;
        }

        private IResponseHandler GetRemoveResponseEventHandler(int hash)
        {
            IResponseHandler handler = null;
            if (hash != 0)
                lock (lockHandlers)
                    if (responseEventHandlers.ContainsKey(hash))
                    {
                        handler = responseEventHandlers[hash];
                        responseEventHandlers.Remove(hash);
                    }
            return handler;
        }

        private IResponseHandler GetResponseHandler(int hash)
        {
            IResponseHandler handler = null;
            if (hash != 0)
                lock (lockHandlers)
                    if (responseHandlers.ContainsKey(hash))
                        handler = responseHandlers[hash];
            return handler;
        }

        private IResponseHandler GetResponseEventHandler(int hash)
        {
            IResponseHandler handler = null;
            if (hash != 0)
                lock (lockHandlers)
                    if (responseEventHandlers.ContainsKey(hash))
                        handler = responseEventHandlers[hash];
            return handler;
        }
        #endregion

        #region SendToAsterisk(ManagerAction action, string internalActionId)

        internal void SendToAsterisk(ManagerAction action, string internalActionId)
        {
            string buffer = BuildAction(action, internalActionId);

            _logger.LogDebug("Sent action : '{0}' : {1}", internalActionId, action);

            if (sa == null)
                sa = new SendToAsteriskDelegate(sendToAsterisk);

            sa.Invoke(buffer);
        }

        private delegate void SendToAsteriskDelegate(string buffer);
        private SendToAsteriskDelegate? sa = null;

        private void sendToAsterisk(string buffer)
        {
            if (mrSocket == null)
                throw new SystemException("Unable to send action: socket is null");

            if (!mrSocket.IsConnected())
            {
                mrSocket = null; // setting null to force a reconnect on next time
                throw new SystemException("Unable to send action: tcpclient or network stream null or disposed");
            }

            lock (lockSocketWrite)
            {
                mrSocket.Write(buffer);
            }
        }

        #endregion

        #region BuildAction(action)
        public string BuildAction(Action.ManagerAction action)
        {
            return BuildAction(action, null);
        }
        #endregion

        #region BuildAction(action, internalActionId)

        private static string[] IgnoreKeys = { "class", "action", "actionid", "variable", "dictionary" };

        public string BuildAction(ManagerAction action, string internalActionId)
        {
            MethodInfo getter;
            object value;
            StringBuilder sb = new StringBuilder();
            string valueAsString = string.Empty;

            if (typeof(Action.ProxyAction).IsAssignableFrom(action.GetType()))
                sb.Append(string.Concat("ProxyAction: ", action.Action, Common.LINE_SEPARATOR));
            else
                sb.Append(string.Concat("Action: ", action.Action, Common.LINE_SEPARATOR));

            if (string.IsNullOrEmpty(internalActionId))
                valueAsString = action.ActionId;
            else
                valueAsString = string.Concat(internalActionId, Common.INTERNAL_ACTION_ID_DELIMITER, action.ActionId);

            if (!string.IsNullOrEmpty(valueAsString))
                sb.Append(string.Concat("ActionID: ", valueAsString, Common.LINE_SEPARATOR));
                       
            if (action.Dictionary != null)
            {
                foreach (DictionaryEntry entry in action.Dictionary)
                {
                    string concatItemsValue = Helper.JoinVariables(action.Dictionary, Common.LINE_SEPARATOR, ": ");
                    if (concatItemsValue.Length == 0)
                        continue;

                    sb.Append(concatItemsValue);
                    sb.Append(Common.LINE_SEPARATOR);
                    continue;
                }
            }            

            var getters = Helper.GetGetters(action.GetType());
            foreach (string name in getters.Keys)
            {
                string nameLower = name.ToLower(Helper.CultureInfo);
                if (IgnoreKeys.Contains(nameLower))
                    continue;

                getter = getters[name];
                Type propType = getter.ReturnType;
                if (!(propType == typeof(string)
                    || propType == typeof(bool)
                    || propType == typeof(double)
                    || propType == typeof(DateTime)
                    || propType == typeof(int)
                    || propType == typeof(long)
                    || propType == typeof(Dictionary<string, string>)
                    )
                    )
                    continue;

                try
                {
                    value = getter.Invoke(action, new object[] { });
                }
                catch (UnauthorizedAccessException ex)
                {
					throw new ManagerException("Unable to retrieve property '" + name + "' of " + action.GetType(), ex);
                }
                catch (TargetInvocationException ex)
                {
					throw new ManagerException("Unable to retrieve property '" + name + "' of " + action.GetType(), ex);
                }

                if (value == null)
                    continue;
                if (value is string)
                {
                    valueAsString = (string)value;
                    if (valueAsString.Length == 0)
                        continue;
                }
                else if (value is bool)
                    valueAsString = ((bool)value ? "true" : "false");
                else if (value is DateTime)
                    valueAsString = value.ToString();
                else if (value is IDictionary dictionary)
                {
                    valueAsString = Helper.JoinVariables(dictionary, Common.LINE_SEPARATOR, ": ");
                    if (valueAsString.Length == 0)
                        continue;
                    sb.Append(valueAsString);
                    sb.Append(Common.LINE_SEPARATOR);
                    continue;
                }
                else
                    valueAsString = value.ToString();
                
                sb.Append(string.Concat(name, ": ", valueAsString, Common.LINE_SEPARATOR));
            }

            if (action.Variable != null && action.Variable.Count > 0)
            {
                string concatItemsValue = Helper.JoinVariables(action.Variable, VAR_DELIMITER, "=");
                string concatValue = string.Concat("Variable: ", concatItemsValue);
                sb.Append(concatValue); 
                sb.Append(Common.LINE_SEPARATOR);
            }            

            sb.Append(Common.LINE_SEPARATOR);  
            return sb.ToString();
        }
        #endregion

        #region GetProtocolIdentifier()
        public string GetProtocolIdentifier()
        {
            return this.protocolIdentifier;
        }
        #endregion

        #region RegisterUserEventClass(class)
        /// <summary>
        /// Register User Event Class
        /// </summary>
        /// <param name="userEventClass"></param>
        public void RegisterUserEventClass(Type userEventClass)
        {
            Helper.RegisterEventClass(registeredEventClasses, userEventClass);
        }
        #endregion

        #region DispatchResponse(response)
        /// <summary>
        /// This method is called by the reader whenever a ManagerResponse is
        /// received. The response is dispatched to the associated <see cref="IResponseHandler"/>ManagerResponseHandler.
        /// </summary>
        /// <param name="buffer">the response received by the reader</param>
        /// <seealso cref="ManagerReader" />
        internal void DispatchResponse(Dictionary<string, string> buffer)
        {
            _logger.LogDebug("Dispatch response packet : {0}", Helper.JoinVariables(buffer, ", ", ": "));
            DispatchResponse(buffer, null);
        }

        internal void DispatchResponse(ManagerResponse response)
        {
            _logger.LogTrace("Dispatch response : {0}", response);
            DispatchResponse(null, response);
        }

        internal void DispatchResponse(Dictionary<string, string>? buffer, ManagerResponse? response)
        {
            string responseActionId = string.Empty;
            string actionId = string.Empty;
            IResponseHandler? responseHandler = null;

            if (buffer != null)
            {
                if (buffer["response"].ToLower(Helper.CultureInfo) == "error")
                    response = new ManagerError(buffer);
                else if (buffer.ContainsKey("actionid"))
                    actionId = buffer["actionid"];
            }

            if (response != null)
                actionId = response.ActionId;

            if (!string.IsNullOrEmpty(actionId))
            {
                int hash = Helper.GetInternalActionId(actionId).GetHashCode();
                responseActionId = Helper.StripInternalActionId(actionId);
                responseHandler = GetRemoveResponseHandler(hash);

                if (response != null)
                    response.ActionId = responseActionId;
                if (responseHandler != null)
                {
                    if (response == null)
                    {
                        ManagerActionResponse? action = responseHandler.Action as ManagerActionResponse;
                        if (action == null || (response = action.ActionCompleteResponseClass() as ManagerResponse) == null)
                            response = Helper.BuildResponse(buffer);
                        else
                            Helper.SetAttributes(response, buffer);
                        response.ActionId = responseActionId;
                    }

                    try
                    {
                        responseHandler.HandleResponse(response);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected exception in responseHandler {0}\n{1}", response);
						throw new ManagerException("Unexpected exception in responseHandler " + responseHandler.GetType().FullName, ex);
                    }
                }
            }

            if (response == null && buffer.ContainsKey("ping") && buffer["ping"].ToLower() == "pong")
            {
                response = Helper.BuildResponse(buffer);
                foreach (ResponseHandler pingHandler in pingHandlers.Values)
                    pingHandler.HandleResponse(response);
                pingHandlers.Clear();
            }

            if (!reconnected)
                return;

            if (response == null)
            {
                response = Helper.BuildResponse(buffer);
                response.ActionId = responseActionId;
            }
            _logger.LogInformation("Reconnected - DispatchEvent : " + response);
            #region Support background reconnect
            if (response is ChallengeResponse)
            {
                string key = null;
                if (response.IsSuccess())
                {
                    ChallengeResponse challengeResponse = (ChallengeResponse)response;
                    string challenge = challengeResponse.Challenge;
                    try
                    {
                        Util.MD5Support md = Util.MD5Support.GetInstance();
                        if (challenge != null)
                            md.Update(UTF8Encoding.UTF8.GetBytes(challenge));
                        if (password != null)
                            md.Update(UTF8Encoding.UTF8.GetBytes(password));
                        key = Helper.ToHexString(md.DigestData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unable to create login key using MD5 Message Digest");
                        key = null;
                    }
                }
                bool fail = true;
                if (!string.IsNullOrEmpty(key))
                    try
                    {
                        Action.LoginAction loginAction = new Action.LoginAction(username, "MD5", key);
                        SendAction(loginAction, null);
                        fail = false;
                    }
                    catch { }
                if (fail)
                    if (keepAliveAfterAuthenticationFailure)
                        reconnect(true);
                    else
                        disconnect(true);
            }
            else if (response is ManagerError)
            {
                if (keepAliveAfterAuthenticationFailure)
                    reconnect(true);
                else
                    disconnect(true);
            }
            else if (response is ManagerResponse)
            {
                if (response.IsSuccess())
                {
                    reconnected = false;
                    enableEvents = true;
                    reconnectEnable = keepAlive;
                    ConnectEvent ce = new ConnectEvent();
                    ce.Reconnect = true;
                    ce.ProtocolIdentifier = protocolIdentifier;
                    fireEvent(ce);
                }
                else if (keepAliveAfterAuthenticationFailure)
                    reconnect(true);
                else
                    disconnect(true);
            }
            #endregion
        }
        #endregion

        #region DispatchEvent(...)
        /// <summary>
        /// This method is called by the reader whenever a IManagerEvent is received.
        /// The event is dispatched to all registered IManagerEventHandlers.
        /// </summary>
        /// <seealso cref="ManagerReader"/>
        internal void DispatchEvent(Dictionary<string, string> buffer)
        {
            var e = this.BuildEvent(registeredEventClasses, buffer);
            DispatchEvent(e.Event);
        }

        /// <summary>
        /// Must be identified event before that
        /// </summary>
        /// <param name="e"></param>
        internal void DispatchEvent(IManagerEvent e)
        {
            if (e is IResponseEvent responseEvent)
            {
                if (e is IActionListComplete complete)
                    _logger.LogDebug("Action({actionid}) completed: {eventlist}, items: {items}", responseEvent.ActionId, complete.EventList, complete.ListItems);

                if (!string.IsNullOrEmpty(responseEvent.ActionId) && !string.IsNullOrEmpty(responseEvent.InternalActionId))
                {
                    ResponseEventHandler eventHandler = (ResponseEventHandler)GetResponseEventHandler(responseEvent.InternalActionId.GetHashCode());
                    if (eventHandler != null)
                        try
                        {
                            eventHandler.HandleEvent(e);
                        }
                        catch (SystemException ex)
                        {
                            _logger.LogError(ex, "Unexpected exception");
							throw ex;
                        }
                }
            }

            #region ConnectEvent
            if (e is ConnectEvent)
            {
                string protocol = ((ConnectEvent)e).ProtocolIdentifier;
                _logger.LogInformation("Connected via {0}", protocol);

                if (!string.IsNullOrEmpty(protocol) && protocol.StartsWith("Asterisk Call Manager"))
                {
                    this.protocolIdentifier = protocol;
                }
                else
                {
                    this.protocolIdentifier = (string.IsNullOrEmpty(protocol) ? "Empty" : protocol);
                    _logger.LogWarning("Unsupported protocol version '{0}'. Use at your own risk.", protocol);
                }
                if (reconnected)
                {
                    _logger.LogInformation("Send Challenge action.");
                    ChallengeAction challengeAction = new ChallengeAction();
                    try
                    {
                        SendAction(challengeAction, null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("Send Challenge fail : ", ex.Message);
                        disconnect(true);
                    }
                    return;
                }
            }
            #endregion

            if (reconnected && e is DisconnectEvent)
            {
                ((DisconnectEvent)e).Reconnect = true;
                fireEvent(e);
                reconnect(false);
            }
            else if (!reconnected && reconnectEnable && (e is DisconnectEvent || e is ShutdownEvent))
            {
                ((ConnectionStateEvent)e).Reconnect = true;
                fireEvent(e);
                reconnect(true);
            }
            else
            {
                fireEvent(e);
            }
        }

        private void eventComplete(IAsyncResult result)
        {
        }

        private void fireEvent(IManagerEvent e)
        {
            if (enableEvents && internalEvent != null)
                if (UseASyncEvents)
                    Task.Run(() => internalEvent.Invoke(this, e)).ContinueWith(eventComplete);
                else
                    internalEvent.Invoke(this, e);
        }

        /// <summary>
        /// This method is called when send event to client if subscribed
        /// </summary>
        /// <typeparam name="T">EventHandler argument</typeparam>
        /// <param name="asterEvent">Event delegate</param>
        /// <param name="arg">ManagerEvent or inherited class. Argument of eventHandler.</param>
        private bool fireEvent<T>(EventHandler<T> asterEvent, IManagerEvent arg) where T : IManagerEvent
        {
            if (asterEvent != null)
            {
                asterEvent(this, (T)arg);
                return true;
            }

            return false;
        }

        #endregion
    }
}
