namespace AsterNET.Manager.Event
{
    /// <summary>
    /// A EndpointDetailEvent is triggered in response to a PJSIPShowEndpointAction and contains information about the endpoint.<br/>
    /// It is implemented in channels/chan_pjsip.c
    /// </summary>
    public class EndpointDetailEvent : ResponseEvent
	{
        public string ObjectType {get; set;}
        public string ObjectName {get; set;}
        public string Context {get; set;}
        public string Disallow {get; set;}
        public string Allow {get; set;}
        public string DtmfMode {get; set;}
        public string RtpIpv6 {get; set;}
        public string RtpSymmetric {get; set;}
        public string IceSupport {get; set;}
        public string UsePtime {get; set;}
        public string ForceRport {get; set;}
        public string RewriteContact {get; set;}
        public string Transport {get; set;}
        public string OutboundProxy {get; set;}
        public string MohSuggest {get; set;}
        public string Timers {get; set;}
        public string TimersMinSe {get; set;}
        public string TimersSessExpires {get; set;}
        public string Auth {get; set;}
        public string OutboundAuth {get; set;}
        public string Aors {get; set;}
        public string MediaAddress {get; set;}
        public string IdentifyBy {get; set;}
        public string DirectMedia {get; set;}
        public string DirectMediaMethod {get; set;}
        public string ConnectedLineMethod {get; set;}
        public string DirectMediaGlareMitigation {get; set;}
        public string DisableDirectMediaOnNat {get; set;}
        public string Callerid {get; set;}
        public string CalleridPrivacy {get; set;}
        public string CalleridTag {get; set;}
        public string TrustIdInbound {get; set;}
        public string TrustIdOutbound {get; set;}
        public string SendPai {get; set;}
        public string SendRpid {get; set;}
        public string SendDiversion {get; set;}
        public string Mailboxes {get; set;}
        public string AggregateMwi {get; set;}
        public string MediaEncryption {get; set;}
        public string MediaEncryptionOptimistic {get; set;}
        public string UseAvpf {get; set;}
        public string ForceAvp {get; set;}
        public string MediaUseReceivedTransport {get; set;}
        public string OneTouchRecording {get; set;}
        public string InbandProgress {get; set;}
        public string CallGroup {get; set;}
        public string PickupGroup {get; set;}
        public string NamedCallGroup {get; set;}
        public string NamedPickupGroup {get; set;}
        public string DeviceStateBusyAt {get; set;}
        public string T38Udptl {get; set;}
        public string T38UdptlEc {get; set;}
        public string T38UdptlMaxdatagram {get; set;}
        public string FaxDetect {get; set;}
        public string T38UdptlNat {get; set;}
        public string T38UdptlIpv6 {get; set;}
        public string ToneZone {get; set;}
        public string Language {get; set;}
        public string RecordOnFeature {get; set;}
        public string RecordOffFeature {get; set;}
        public string AllowTransfer {get; set;}
        public string UserEqPhone {get; set;}
        public string SdpOwner {get; set;}
        public string SdpSession {get; set;}
        public string TosAudio {get; set;}
        public string TosVideo {get; set;}
        public string CosAudio {get; set;}
        public string CosVideo {get; set;}
        public string AllowSubscribe {get; set;}
        public string SubMinExpiry {get; set;}
        public string FromUser {get; set;}
        public string FromDomain {get; set;}
        public string MwiFromUser {get; set;}
        public string RtpEngine {get; set;}
        public string DtlsVerify {get; set;}
        public string DtlsRekey {get; set;}
        public string DtlsCertFile {get; set;}
        public string DtlsPrivateKey {get; set;}
        public string DtlsCipher {get; set;}
        public string DtlsCaFile {get; set;}
        public string DtlsCaPath {get; set;}
        public string DtlsSetup {get; set;}
        public string SrtpTag32 {get; set;}
        public string RedirectMethod {get; set;}
        public string SetVar {get; set;}
        public string MessageContext {get; set;}
        public string Accountcode {get; set;}
        public string DeviceState {get; set;}
        public string ActiveChannels {get; set;}
        public string SubscribeContext {get; set;}
        //public string 100rel {get; set;}

    /// <summary>
    /// Get/Set the status of this peer.<br/>
    /// For SIP peers this is one of:
    /// <dl>
    /// <dt>"UNREACHABLE"</dt>
    /// <dd></dd>
    /// <dt>"LAGGED (%d ms)"</dt>
    /// <dd></dd>
    /// <dt>"OK (%d ms)"</dt>
    /// <dd></dd>
    /// <dt>"UNKNOWN"</dt>
    /// <dd></dd>
    /// <dt>"Unmonitored"</dt>
    /// <dd></dd>
    /// </dl>
    /// </summary>
    /*
    public string Status
    {
        get { return this.status; }
        set { this.status = value; }
    }
    */

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public EndpointDetailEvent(ManagerConnection source)
			: base(source)
		{
		}
	}
}