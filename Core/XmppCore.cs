﻿using XMPP.Net.Core.Sasl;
using XMPP.Net.Extensions;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.Extensions.Logging;


namespace XMPP.Net.Core
{
    /// <summary>
    /// Implements the core features of the XMPP protocol.
    /// </summary>
    /// <remarks>For implementation details, refer to RFC 3920.</remarks>
    public class XmppCore : IDisposable
    {
        private static readonly ILogger log = LogFactory.CreateLogger<XmppCore>();

        private const String BIND_ID = "bind-0";

        public const String ACTION_CREATE_SESSION = "CREATE_SESSION";
        public const String ACTION_SERVICE_DISCOVERY = "SERVICE_DISCOVERY";
        public const String ACTION_ENABLE_STREAM_MANAGEMENT = "ENABLE_STREAM_MANAGEMENT";
        public const String ACTION_ENABLE_MESSAGE_CARBONS = "ENABLE_MESSAGE_CARBON";
        public const String ACTION_GET_ROSTER = "GET_ROSTER";
        // public const String ACTION_SET_DEFAULT_STATUS = "SET_DEFAULT_STATUS"; // No more used -> Must be done by application itself
        public const String ACTION_FULLY_CONNECTED = "FULLY_CONNECTED";

        public event EventHandler<TextEventArgs> ActionToPerform;
        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatus;
        public event EventHandler<EventArgs> StreamManagementRequestAcknowledgement;

        /// <summary>
        /// The TCP connection to the XMPP server.
        /// </summary>
        private TcpClient tcpClient;

        private WebSocket webSocketClient;

        /// <summary>
        /// The (network) stream used for sending and receiving XML data.
        /// </summary>
        private Stream stream;

        /// <summary>
        /// The parser instance used for parsing incoming XMPP XML-stream data.
        /// </summary>
        private StreamParser parser;

        private bool normalClosure;

        /// <summary>
        /// True if the instance has been disposed of.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The URI to use for the proxy
        /// </summary>
        public Tuple<String, String, String> WebProxyInfo = null;

        /// <summary>
        /// True if web socket is used
        /// </summary>
        private bool useWebSocket = false;

        /// <summary>
        /// URI to use for web socket connection
        /// </summary>
        private string webSocketUri;

        /// <summary>
        /// The port number of the XMPP service of the server.
        /// </summary>
        private int port;

        /// <summary>
        /// The hostname of the XMPP server to connect to.
        /// </summary>
        private string hostname;

        /// <summary>
        /// The XMPP server IP address.
        /// </summary>
        string address;

        /// <summary>
        /// The username with which to authenticate.
        /// </summary>
        private string username;

        /// <summary>
        /// The password with which to authenticate.
        /// </summary>
        private string password;

        /// <summary>
        /// The resource to use for binding.
        /// </summary>
        private string resource;

        SaslMechanism saslMechanism = null;

        /// <summary>
        /// Write lock for the network stream.
        /// </summary>
        private readonly object writeLock = new object();

        /// <summary>
        /// The default Time Out for IQ Requests
        /// </summary>
        private int millisecondsDefaultTimeout = -1;

        /// <summary>
        /// The default value for debugging stanzas is false
        /// </summary>
        private bool debugStanzas = false;

        /// <summary>
        /// A thread-safe dictionary of wait handles for pending IQ requests.
        /// </summary>
        private ConcurrentDictionary<string, AutoResetEvent> waitHandles =
            new ConcurrentDictionary<string, AutoResetEvent>();

        /// <summary>
        /// A thread-safe dictionary of IQ responses for pending IQ requests.
        /// </summary>
        private ConcurrentDictionary<string, Iq> iqResponses =
         new ConcurrentDictionary<string, Iq>();

        /// <summary>
        /// A thread-safe dictionary of callback methods for asynchronous IQ requests.
        /// </summary>
        private ConcurrentDictionary<string, Action<string, Iq>> iqCallbacks =
         new ConcurrentDictionary<string, Action<string, Iq>>();

        /// <summary>
        /// A cancellation token source that is set when the listener threads shuts
        /// down due to an exception.
        /// </summary>
        private CancellationTokenSource cancelIq = new CancellationTokenSource();

        /// <summary>
        /// A FIFO of stanzas waiting to be processed.
        /// </summary>
        private BlockingCollection<Stanza> stanzaQueue = new BlockingCollection<Stanza>();

        private BlockingCollection<Stanza> streamManagementStanzaQueue = new BlockingCollection<Stanza>();

        private BlockingCollection<Stanza>[] fullStanzaQueue;
         

        /// <summary>
        /// A cancellation token source for cancelling the dispatcher, if neccessary.
        /// </summary>
        private CancellationTokenSource cancelDispatch = new CancellationTokenSource();

        /// <summary>
        /// Is web socket used - false by default
        /// </summary>
        public bool UseWebSocket
        {
            get
            {
                return useWebSocket;
            }

            set
            {
                useWebSocket = value;
            }
        }

        /// <summary>
        /// URI to use for web socket connection
        /// </summary>
        public string WebSocketUri
        {
            get
            {
                return webSocketUri;
            }

            set
            {
                webSocketUri = value;
            }
        }

        /// <summary>
        /// The hostname of the XMPP server to connect to.
        /// </summary>
        /// <exception cref="ArgumentNullException">The Hostname property is being
        /// set and the value is null.</exception>
        /// <exception cref="ArgumentException">The Hostname property is being set
        /// and the value is the empty string.</exception>
        public string Hostname
        {
            get
            {
                return hostname;
            }

            set
            {
                value.ThrowIfNullOrEmpty("Hostname");
                hostname = value;
            }
        }

        /// <summary>
        /// The XMPP server IP address.
        /// </summary>
        public string Address
        {
            get { return address; }
            set { address = value; }
        }

        /// <summary>
        /// The port number of the XMPP service of the server.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The Port property is being
        /// set and the value is not between 0 and 65536.</exception>
        public int Port
        {
            get
            {
                return port;
            }

            set
            {
                value.ThrowIfOutOfRange("Port", 0, 65536);
                port = value;
            }
        }

        /// <summary>
        /// The username with which to authenticate. In XMPP jargon this is known
        /// as the 'node' part of the JID.
        /// </summary>
        /// <exception cref="ArgumentNullException">The Username property is being
        /// set and the value is null.</exception>
        /// <exception cref="ArgumentException">The Username property is being set
        /// and the value is the empty string.</exception>
        public string Username
        {
            get
            {
                return username;
            }

            set
            {
                value.ThrowIfNullOrEmpty("Username");
                username = value;
            }
        }

        /// <summary>
        /// The password with which to authenticate.
        /// </summary>
        /// <exception cref="ArgumentNullException">The Password property is being
        /// set and the value is null.</exception>
        public string Password
        {
            get
            {
                return password;
            }

            set
            {
                value.ThrowIfNull("Password");
                password = value;
            }
        }

        /// <summary>
        /// The Default IQ Set /Request message timeout
        /// </summary>
        public int MillisecondsDefaultTimeout
        {
            get { return millisecondsDefaultTimeout; }
            set { millisecondsDefaultTimeout = value; }
        }

        /// <summary>
        /// Print XML stanzas for debugging purposes
        /// </summary>
        public bool DebugStanzas
        {
            get { return debugStanzas; }
            set { debugStanzas = value; }
        }

        /// <summary>
        /// If true the session will be TLS/SSL-encrypted if the server supports it.
        /// </summary>
        public bool Tls
        {
            get;
            set;
        }

        /// <summary>
        /// If true it means that server can manage Stream Management
        /// </summary>
        public bool StreamManagementAvailable
        {
            get;
            set;
        }

        /// <summary>
        /// If true it means that server can manage Stream Management and it was sucessfully enabled
        /// </summary>
        public bool StreamManagementEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// If true the session will enable Stream Management (if server accepts it)
        /// </summary>
        public bool StreamManagementEnable
        {
            get;
            set;
        }

        /// <summary>
        /// If true the session will try to resume Stream Management (if server accepts it)
        /// </summary>
        public bool StreamManagementResume
        {
            get;
            set;
        }

        /// <summary>
        ///  Id to resume Stream Management (if server accepts it)
        /// </summary>
        public String StreamManagementResumeId
        {
            get;
            set;
        }

        /// <summary>
        ///  Delay to resume Stream Management (if server accepts it)
        /// </summary>
        public int StreamManagementResumeDelay
        {
            get;
            set;
        }

        /// <summary>
        ///  Last Stanza received and handled to resume Stream Management (if server accepts it)
        /// </summary>
        public uint StreamManagementLastStanzaReceivedAndHandledByServer
        {
            get;
            set;
        }

        /// <summary>
        ///  Date of Last Stanza to resume Stream Management (if server accepts it)
        /// </summary>
        public DateTime StreamManagementLastStanzaDateReceivedAndHandledByServer
        {
            get;
            set;
        }

        /// <summary>
        ///  Last Stanza sent and handled to resume Stream Management (if server accepts it)
        /// </summary>
        public uint StreamManagementLastStanzaReceivedAndHandledByClient
        {
            get;
            set;
        }

        /// <summary>
        ///  Last Stanza received (but not yet handled) by client (in Stream Management context if server accepts it)
        /// </summary>
        public uint StreamManagementLastStanzaReceivedByClient
        {
            get;
            set;
        }


        /// <summary>
        /// A delegate used for verifying the remote Secure Sockets Layer (SSL)
        /// certificate which is used for authentication.
        /// </summary>
        public RemoteCertificateValidationCallback Validate
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether the session with the server is TLS/SSL encrypted.
        /// </summary>
        public bool IsEncrypted
        {
            get;
            private set;
        }

        /// <summary>
        /// The address of the Xmpp entity.
        /// </summary>
        public Jid Jid
        {
            get;
            private set;
        }

        /// <summary>
        /// The default language of the XML stream.
        /// </summary>
        public CultureInfo Language
        {
            get;
            private set;
        }

        /// <summary>
        /// Determines whether the instance is connected to the XMPP server.
        /// </summary>
        public bool Connected
        {
            get;
            private set;
        }

        /// <summary>
        /// Determines whether the instance has been authenticated.
        /// </summary>
        public bool Authenticated
        {
            get;
            private set;
        }

        /// <summary>
        /// The event that is raised when an unrecoverable error condition occurs.
        /// </summary>
        public event EventHandler<ErrorEventArgs> Error;

        /// <summary>
        /// The event that is raised when an IQ-request stanza has been received.
        /// </summary>
        public event EventHandler<IqEventArgs> Iq;

        /// <summary>
        /// The event that is raised when a Message stanza has been received.
        /// </summary>
        public event EventHandler<MessageEventArgs> Message;

        /// <summary>
        /// The event that is raised when a Presence stanza has been received.
        /// </summary>
        public event EventHandler<PresenceEventArgs> Presence;

        /// <summary>
        /// The event that is raised when a Presence stanza has been received.
        /// </summary>
        public event EventHandler<StreamManagementStanzaEventArgs> StreamManagementStanza;

        public void SetLanguage()
        {
            if(Language == null)
                Language = webSocketClient.Language;
            if (Language == null)
                Language = new CultureInfo("en");
        }

        /// <summary>
        /// Initializes a new instance of the XmppCore class.
        /// </summary>
        /// <param name="address">The XMPP server IP address.</param>
        /// <param name="hostname">The hostname of the XMPP server to connect to.</param>
        /// <param name="username">The username with which to authenticate. In XMPP jargon
        /// this is known as the 'node' part of the JID.</param>
        /// <param name="password">The password with which to authenticate.</param>
        /// <param name="port">The port number of the XMPP service of the server.</param>
        /// <param name="tls">If true the session will be TLS/SSL-encrypted if the server
        /// supports TLS/SSL-encryption.</param>
        /// <param name="validate">A delegate used for verifying the remote Secure Sockets
        /// Layer (SSL) certificate which is used for authentication. Can be null if not
        /// needed.</param>
        /// <exception cref="ArgumentNullException">The hostname parameter or the
        /// username parameter or the password parameter is null.</exception>
        /// <exception cref="ArgumentException">The hostname parameter or the username
        /// parameter is the empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value of the port parameter
        /// is not a valid port number.</exception>
        public XmppCore(string address, string hostname, string username, string password,
            int port = 5222, bool tls = true, RemoteCertificateValidationCallback validate = null)
        {
            if (address == string.Empty)
                Address = hostname;
            else
                Address = address;

            Hostname = hostname;
            Port = port;

            Username = username;
            Password = password;
            Tls = tls;
            Validate = validate;

            fullStanzaQueue = new BlockingCollection<Stanza>[] { streamManagementStanzaQueue, stanzaQueue };

        }

        /// <summary>
        /// Initializes a new instance of the XmppCore class.
        /// </summary>
        /// <param name="hostname">The hostname of the XMPP server to connect to.</param>
        /// <param name="username">The username with which to authenticate. In XMPP jargon
        /// this is known as the 'node' part of the JID.</param>
        /// <param name="password">The password with which to authenticate.</param>
        /// <param name="port">The port number of the XMPP service of the server.</param>
        /// <param name="tls">If true the session will be TLS/SSL-encrypted if the server
        /// supports TLS/SSL-encryption.</param>
        /// <param name="validate">A delegate used for verifying the remote Secure Sockets
        /// Layer (SSL) certificate which is used for authentication. Can be null if not
        /// needed.</param>
        /// <exception cref="ArgumentNullException">The hostname parameter or the
        /// username parameter or the password parameter is null.</exception>
        /// <exception cref="ArgumentException">The hostname parameter or the username
        /// parameter is the empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value of the port parameter
        /// is not a valid port number.</exception>
        public XmppCore(string hostname, string username, string password,
            int port = 5222, bool tls = true, RemoteCertificateValidationCallback validate = null) :
            this(string.Empty, hostname, username, password, port, tls, validate)
        { }

        /// <summary>
        /// Initializes a new instance of the XmppCore class.
        /// </summary>
        /// <param name="hostname">The hostname of the XMPP server to connect to.</param>
        /// <param name="port">The port number of the XMPP service of the server.</param>
        /// <param name="tls">If true the session will be TLS/SSL-encrypted if the server
        /// supports TLS/SSL-encryption.</param>
        /// <param name="validate">A delegate used for verifying the remote Secure Sockets
        /// Layer (SSL) certificate which is used for authentication. Can be null if not
        /// needed.</param>
        /// <exception cref="ArgumentNullException">The hostname parameter is
        /// null.</exception>
        /// <exception cref="ArgumentException">The hostname parameter is the empty
        /// string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value of the port parameter
        /// is not a valid port number.</exception>
        public XmppCore(string hostname, int port = 5222, bool tls = true,
            RemoteCertificateValidationCallback validate = null) :
            this(hostname, string.Empty, string.Empty, port, tls, validate)
        { }

         /// <summary>
        /// Establishes a connection to the XMPP server.
        /// </summary>
        /// <param name="resource">The resource identifier to bind with. If this is null,
        /// it is assigned by the server.</param>
        /// <exception cref="SocketException">An error occurred while accessing the socket
        /// used for establishing the connection to the XMPP server. Use the ErrorCode
        /// property to obtain the specific error code.</exception>
        /// <exception cref="AuthenticationException">An authentication error occured while
        /// trying to establish a secure connection, or the provided credentials were
        /// rejected by the server, or the server requires TLS/SSL and TLS has been
        /// turned off.</exception>
        /// <exception cref="XmppException">An XMPP error occurred while negotiating the
        /// XML stream with the server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <remarks>If a username has been supplied, this method automatically performs
        /// authentication.</remarks>
        public void Connect(string resource)
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            this.resource = resource;
            try
            {
                if(UseWebSocket)
                {
                    if(String.IsNullOrEmpty(WebSocketUri))
                        throw new XmppException("URI not provided for WebSocket connection");

                    // Destroy previous object (if any)
                    if(webSocketClient != null)
                    {
                        webSocketClient.Close();
                        webSocketClient = null;
                    }

                    webSocketClient = new WebSocket(WebSocketUri, WebProxyInfo);
                    webSocketClient.WebSocketOpened += new EventHandler(WebSocketClient_WebSocketOpened);
                    webSocketClient.WebSocketClosed += new EventHandler(WebSocketClient_WebSocketClosed);

                    webSocketClient.Open();
                }
                else
                {
                    Uri proxyUri = null;
                    if ( (WebProxyInfo != null) && (!String.IsNullOrEmpty(WebProxyInfo.Item1)) )
                        proxyUri = new Uri(WebProxyInfo.Item1);

                    if(proxyUri != null)
                        tcpClient = new TcpClient(proxyUri.DnsSafeHost, proxyUri.Port);
                    else
                        tcpClient = new TcpClient();

                    tcpClient.Connect(Address, Port);
                    stream = tcpClient.GetStream();
                    

                    // Sets up the connection which includes TLS and possibly SASL negotiation.
                    SetupConnection(this.resource);

                    // We are connected.
                    Connected = true;
                    RaiseConnectionStatus(true);

                    // Set up the listener and dispatcher tasks.
                    Task.Factory.StartNew(ReadXmlStream, TaskCreationOptions.LongRunning);
                    Task.Factory.StartNew(DispatchEvents, TaskCreationOptions.LongRunning);
                }
            }
            catch (XmlException e)
            {
                throw new XmppException("The XML stream could not be negotiated.", e);
            }
        }

        private void WebSocketClient_WebSocketError(object sender, ExceptionEventArgs e)
        {
            log.LogDebug("[WebSocketClient_WebSocketError] Exception:[{0}]", e.Exception.ToString());
            RaiseConnectionStatus(false);
        }

        private void WebSocketClient_WebSocketClosed(object sender, EventArgs e)
        {
            log.LogDebug("[WebSocketClient_WebSocketClosed]");
            RaiseConnectionStatus(false);
        }

        private void RaiseConnectionStatus(bool connected)
        {
            Connected = connected;
            if (!connected)
            {
                if (webSocketClient != null)
                {
                    try
                    {
                        webSocketClient.WebSocketClosed -= WebSocketClient_WebSocketClosed;
                        webSocketClient.WebSocketOpened -= WebSocketClient_WebSocketOpened;

                        webSocketClient.Close();
                        webSocketClient = null;

                    } catch
                    {
                        // Nothing to do more
                    }
                }
            }

            log.LogDebug("[RaiseConnectionStatus] connected:{0}", connected);
            ConnectionStatus.Raise(this, new ConnectionStatusEventArgs(connected));
        }

        private void WebSocketClient_WebSocketOpened(object sender, EventArgs e)
        {
            log.LogDebug("[WebSocketClient_WebSocketOpened]");

            // We are connected.
            Connected = true;

            // Set up the listener and dispatcher tasks.
            Task.Factory.StartNew(ReadAction, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);
            Task.Factory.StartNew(ReadXmlWebSocketMessage, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);
            Task.Factory.StartNew(DispatchEvents, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);

            var xml = Xml.Element("open", "urn:ietf:params:xml:ns:xmpp-framing")
                .Attr("to", hostname)
                .Attr("version", "1.0")
                .Attr("xmlns:stream", "http://etherx.jabber.org/streams")
                .Attr("xml:lang", CultureInfo.CurrentCulture.Name);
            Send(xml.ToXmlString(xmlDeclaration: true, leaveOpen: false), false);
        }

        /// <summary>
        /// Authenticates with the XMPP server using the specified username and
        /// password.
        /// </summary>
        /// <param name="username">The username to authenticate with.</param>
        /// <param name="password">The password to authenticate with.</param>
        /// <exception cref="ArgumentNullException">The username parameter or the
        /// password parameter is null.</exception>
        /// <exception cref="SocketException">An error occurred while accessing the socket
        /// used for establishing the connection to the XMPP server. Use the ErrorCode
        /// property to obtain the specific error code.</exception>
        /// <exception cref="AuthenticationException">An authentication error occured while
        /// trying to establish a secure connection, or the provided credentials were
        /// rejected by the server, or the server requires TLS/SSL and TLS has been
        /// turned off.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="XmppException">Authentication has already been performed, or
        /// an XMPP error occurred while negotiating the XML stream with the
        /// server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        public void Authenticate(string username, string password)
        {
            AssertValid();
            username.ThrowIfNull("username");
            password.ThrowIfNull("password");
            if (Authenticated)
                throw new XmppException("Authentication has already been performed.");
            // Unfortunately, SASL authentication does not follow the standard XMPP
            // IQ-semantics. At this stage it really is easier to simply perform a
            // reconnect.
            Username = username;
            Password = password;
            Disconnect();
            Connect(this.resource);
        }

        /// <summary>
        /// Sends a Message stanza with the specified attributes and content to the
        /// server.
        /// </summary>
        /// <param name="to">The JID of the intended recipient for the stanza.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="data">he content of the stanza.</param>
        /// <param name="id">The ID of the stanza.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void SendMessage(Jid to = null, Jid from = null, XmlElement data = null,
            string id = null, CultureInfo language = null)
        {
            AssertValid();
            Send(new Message(to, from, data, id, language));
        }

        /// <summary>
        /// Sends the specified message stanza to the server.
        /// </summary>
        /// <param name="message">The message stanza to send to the server.</param>
        /// <exception cref="ArgumentNullException">The message parameter is
        /// null.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void SendMessage(Message message)
        {
            AssertValid();
            message.ThrowIfNull("message");
            if (message.Id == null)
                message.Id = GetId();
            Send(message);
        }

        /// <summary>
        /// Sends a Presence stanza with the specified attributes and content to the
        /// server.
        /// </summary>
        /// <param name="to">The JID of the intended recipient for the stanza.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="data">he content of the stanza.</param>
        /// <param name="id">The ID of the stanza.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void SendPresence(Jid to = null, Jid from = null, string id = null,
            CultureInfo language = null, params XmlElement[] data)
        {
            AssertValid();
            Send(new Presence(to, from, id, language, data));
        }

        /// <summary>
        /// Sends the specified presence stanza to the server.
        /// </summary>
        /// <param name="presence">The presence stanza to send to the server.</param>
        /// <exception cref="ArgumentNullException">The presence parameter
        /// is null.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void SendPresence(Presence presence)
        {
            AssertValid();
            presence.ThrowIfNull("presence");
            Send(presence);
        }

        /// <summary>
        /// Performs an IQ set/get request and blocks until the response IQ comes in.
        /// </summary>
        /// <param name="type">The type of the request. This must be either
        /// IqType.Set or IqType.Get.</param>
        /// <param name="to">The JID of the intended recipient for the stanza.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="data">he content of the stanza.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait
        /// for the arrival of the IQ response or -1 to wait indefinitely.</param>
        /// <returns>The IQ response sent by the server.</returns>
        /// <exception cref="ArgumentException">The type parameter is not
        /// IqType.Set or IqType.Get.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value of millisecondsTimeout
        /// is a negative number other than -1, which represents an indefinite
        /// timeout.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network, or there was a failure reading from the network.</exception>
        /// <exception cref="TimeoutException">A timeout was specified and it
        /// expired.</exception>
        public Iq IqRequest(IqType type, Jid to = null, Jid from = null,
            XmlElement data = null, CultureInfo language = null,
            int millisecondsTimeout = -1)
        {
            AssertValid();
            return IqRequest(new Iq(type, null, to, from, data, language), millisecondsTimeout);
        }

        /// <summary>
        /// Performs an IQ set/get request and blocks until the response IQ comes in.
        /// </summary>
        /// <param name="request">The IQ request to send.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait
        /// for the arrival of the IQ response or -1 to wait indefinitely.</param>
        /// <returns>The IQ response sent by the server.</returns>
        /// <exception cref="ArgumentNullException">The request parameter is null.</exception>
        /// <exception cref="ArgumentException">The type parameter is not IqType.Set
        /// or IqType.Get.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value of millisecondsTimeout
        /// is a negative number other than -1, which represents an indefinite
        /// timeout.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network, or there was a failure reading from the network.</exception>
        /// <exception cref="TimeoutException">A timeout was specified and it
        /// expired.</exception>
        public Iq IqRequest(Iq request, int millisecondsTimeout = -1)
        {
            int timeOut = -1;
            AssertValid();
            request.ThrowIfNull("request");
            if (request.Type != IqType.Set && request.Type != IqType.Get)
                throw new ArgumentException("The IQ type must be either 'set' or 'get'.");
            if (millisecondsTimeout == -1)
            {
                timeOut = millisecondsDefaultTimeout;
            }
            else timeOut = millisecondsTimeout;
            // Generate a unique ID for the IQ request.
            if(request.Id == null)
                request.Id = GetId();

            if (useWebSocket)
            {
                //log.LogDebug("before to send IqRequest:", request.Id);
                webSocketClient.AddExpectedIqId(request.Id);
                Send(request);
                Iq response = webSocketClient.DequeueExpectedIqMessage();
                return response;
            }
            else
            {
                //TODO: need to add time out ...
                AutoResetEvent ev = new AutoResetEvent(false);
                Send(request);
                // Wait for event to be signaled by task that processes the incoming
                // XML stream.
                waitHandles[request.Id] = ev;
                int index = WaitHandle.WaitAny(new WaitHandle[] { ev, cancelIq.Token.WaitHandle },
                    timeOut);
                if (index == WaitHandle.WaitTimeout)
                {
                    //An entity that receives an IQ request of type "get" or "set" MUST reply with an IQ response of type
                    //"result" or "error" (the response MUST preserve the 'id' attribute of the request).
                    //http://xmpp.org/rfcs/rfc3920.html#stanzas
                    //if (request.Type == IqType.Set || request.Type == IqType.Get)

                    //Make sure that its a request towards the server and not towards any client
                    var ping = request.Data["ping"];

                    if (request.To.Domain == Jid.Domain && (request.To.Node == null || request.To.Node == "") && (ping != null && ping.NamespaceURI == "urn:xmpp:ping"))
                    {
                        RaiseConnectionStatus(false);
                        var e = new XmppDisconnectionException("Timeout Disconnection happened at IqRequest");
                        if (!disposed)
                            Error.Raise(this, new ErrorEventArgs(e));
                        //throw new TimeoutException();
                    }

                    //This check is somehow not really needed doue to the IQ must be either set or get
                }
                // Reader task errored out.
                if (index == 1)
                    throw new IOException("The incoming XML stream could not read.");
                // Fetch response stanza.
                Iq response;
                if (iqResponses.TryRemove(request.Id, out response))
                    return response;
                // Shouldn't happen.

                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Performs an IQ set/get request asynchronously and optionally invokes a
        /// callback method when the IQ response comes in.
        /// </summary>
        /// <param name="type">The type of the request. This must be either
        /// IqType.Set or IqType.Get.</param>
        /// <param name="to">The JID of the intended recipient for the stanza.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="data">he content of the stanza.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <param name="callback">A callback method which is invoked once the
        /// IQ response from the server comes in.</param>
        /// <returns>The ID value of the pending IQ stanza request.</returns>
        /// <exception cref="ArgumentException">The type parameter is not IqType.Set
        /// or IqType.Get.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public string IqRequestAsync(IqType type, Jid to = null, Jid from = null,
            XmlElement data = null, CultureInfo language = null,
            Action<string, Iq> callback = null)
        {
            AssertValid();
            return IqRequestAsync(new Iq(type, null, to, from, data, language), callback);
        }

        /// <summary>
        /// Performs an IQ set/get request asynchronously and optionally invokes a
        /// callback method when the IQ response comes in.
        /// </summary>
        /// <param name="request">The IQ request to send.</param>
        /// <param name="callback">A callback method which is invoked once the
        /// IQ response from the server comes in.</param>
        /// <returns>The ID value of the pending IQ stanza request.</returns>
        /// <exception cref="ArgumentNullException">The request parameter is null.</exception>
        /// <exception cref="ArgumentException">The type parameter is not IqType.Set
        /// or IqType.Get.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public string IqRequestAsync(Iq request, Action<string, Iq> callback = null)
        {
            AssertValid();
            request.ThrowIfNull("request");
            if (request.Type != IqType.Set && request.Type != IqType.Get)
                throw new ArgumentException("The IQ type must be either 'set' or 'get'.");
            request.Id = GetId();
            // Register the callback.
            if (callback != null)
                iqCallbacks[request.Id] = callback;
            Send(request);
            return request.Id;
        }

        /// <summary>
        /// Sends an IQ response for the IQ request with the specified id.
        /// </summary>
        /// <param name="type">The type of the response. This must be either
        /// IqType.Result or IqType.Error.</param>
        /// <param name="id">The id of the IQ request.</param>
        /// <param name="to">The JID of the intended recipient for the stanza.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="data">he content of the stanza.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <exception cref="ArgumentException">The type parameter is not IqType.Result
        /// or IqType.Error.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void IqResponse(IqType type, string id, Jid to = null, Jid from = null,
            XmlElement data = null, CultureInfo language = null)
        {
            AssertValid();
            IqResponse(new Iq(type, id, to, from, data, null));
        }

        /// <summary>
        /// Sends an IQ response for the IQ request with the specified id.
        /// </summary>
        /// <param name="response">The IQ response to send.</param>
        /// <exception cref="ArgumentNullException">The response parameter is
        /// null.</exception>
        /// <exception cref="ArgumentException">The Type property of the response
        /// parameter is not IqType.Result or IqType.Error.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void IqResponse(Iq response)
        {
            AssertValid();
            response.ThrowIfNull("response");
            if (response.Type != IqType.Result && response.Type != IqType.Error)
                throw new ArgumentException("The IQ type must be either 'result' or 'error'.");
            Send(response);
        }

        /// <summary>
        /// Closes the connection with the XMPP server. This automatically disposes
        /// of the object.
        /// </summary>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void Close(bool normalClosure = true)
        {
            //FIXME, instead of asert valid I have ifs, only for the closing
            //AssertValid();
            this.normalClosure = normalClosure;
            // Close the XML stream.
            if (Connected) Disconnect();
            if (!disposed) Dispose();
        }

        /// <summary>
        /// Releases all resources used by the current instance of the XmppCore class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the XmppCore
        /// class, optionally disposing of managed resource.
        /// </summary>
        /// <param name="disposing">true to dispose of managed resources, otherwise
        /// false.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                // Indicate that the instance has been disposed.
                disposed = true;
                // Get rid of managed resources.
                if (disposing)
                {
                    if (parser != null)
                    {
                        parser.Close();
                        parser = null;
                    }

                    if (tcpClient != null)
                    {
                        tcpClient.Close();
                        tcpClient = null;
                    }

                    if (webSocketClient != null)
                    {
                        webSocketClient.WebSocketClosed -= WebSocketClient_WebSocketClosed;
                        webSocketClient.WebSocketOpened -= WebSocketClient_WebSocketOpened;

                        webSocketClient.Close(normalClosure);
                        webSocketClient = null;
                    }
                }
                // Get rid of unmanaged resources.
            }
        }

        /// <summary>
        /// Asserts the instance has not been disposed of and is connected to the
        /// XMPP server.
        /// </summary>
        private void AssertValid()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            //FIXME-FIXED: if it is not connected it will be found out by a lower
            //level exception. Dont throw an exception about connection
            if (!Connected)
            {
                System.Diagnostics.Debug.WriteLine("Assert Valid: Client is disconnected, however no exception is thrown");
                //throw new InvalidOperationException("Not connected to XMPP server.");
            }
            //FIXME
        }

        /// <summary>
        /// Negotiates an XML stream over which XML stanzas can be sent.
        /// </summary>
        /// <param name="resource">The resource identifier to bind with. If this is null,
        /// it is assigned by the server.</param>
        /// <exception cref="XmppException">The resource binding process failed.</exception>
        /// <exception cref="XmlException">Invalid or unexpected XML data has been
        /// received from the XMPP server.</exception>
        /// <exception cref="AuthenticationException">An authentication error occured while
        /// trying to establish a secure connection, or the provided credentials were
        /// rejected by the server, or the server requires TLS/SSL and TLS has been
        /// turned off.</exception>
        private void SetupConnection(string resource = null)
        {
            // Request the initial stream.
            XmlElement feats = InitiateStream(Hostname);
            // Server supports TLS/SSL via STARTTLS.
            if (feats["starttls"] != null)
            {
                // TLS is mandatory and user opted out of it.
                if (feats["starttls"]["required"] != null && Tls == false)
                    throw new AuthenticationException("The server requires TLS/SSL.");
                if (Tls)
                    feats = StartTls(Hostname, Validate);
            }
            // If no Username has been provided, don't perform authentication.
            if (Username == null)
                return;
            // Construct a list of SASL mechanisms supported by the server.
            var m = feats["mechanisms"];
            if (m == null || !m.HasChildNodes)
                throw new AuthenticationException("No SASL mechanisms advertised.");
            var mech = m.FirstChild;
            var list = new HashSet<string>();
            while (mech != null)
            {
                list.Add(mech.InnerText);
                mech = mech.NextSibling;
            }
            // Continue with SASL authentication.
            try
            {
                feats = Authenticate(list, Username, Password, Hostname);
                // FIXME: How is the client's JID constructed if the server does not support
                // resource binding?
                if (feats["bind"] != null)
                    Jid = BindResource(resource);
            }
            catch (SaslException e)
            {
                throw new AuthenticationException("Authentication failed.", e);
            }
        }


        /// <summary>
        /// Initiates an XML stream with the specified entity.
        /// </summary>
        /// <param name="hostname">The name of the receiving entity with which to
        /// initiate an XML stream.</param>
        /// <returns>The 'stream:features' XML element as received from the
        /// receiving entity upon stream establishment.</returns>
        /// <exception cref="XmlException">The XML parser has encountered invalid
        /// or unexpected XML data.</exception>
        /// <exception cref="CultureNotFoundException">The culture specified by the
        /// XML-stream in it's 'xml:lang' attribute could not be found.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network, or there was a failure while reading from the network.</exception>
        private XmlElement InitiateStream(string hostname)
        {
            var xml = Xml.Element("stream:stream", "jabber:client")
                .Attr("to", hostname)
                .Attr("version", "1.0")
                .Attr("xmlns:stream", "http://etherx.jabber.org/streams")
                .Attr("xml:lang", CultureInfo.CurrentCulture.Name);
            Send(xml.ToXmlString(xmlDeclaration: true, leaveOpen: true), false);
            // Create a new parser instance.
            if (parser != null)
                parser.Close();
            parser = new StreamParser(stream, true);
            // Remember the default language of the stream. The server is required to
            // include this, but we make sure nonetheless.
            Language = parser.Language ?? new CultureInfo("en");
            // The first element of the stream must be <stream:features>.
            return parser.NextElement("stream:features");
        }

        /// <summary>
        /// Secures the network stream by negotiating TLS-encryption with the server.
        /// </summary>
        /// <param name="hostname">The hostname of the XMPP server.</param>
        /// <param name="validate">A delegate used for verifying the remote Secure
        /// Sockets Layer (SSL) certificate which is used for authentication. Can be
        /// null if not needed.</param>
        /// <returns>The 'stream:features' XML element as received from the
        /// receiving entity upon establishment of a new XML stream.</returns>
        /// <exception cref="AuthenticationException">An
        /// authentication error occured while trying to establish a secure
        /// connection.</exception>
        /// <exception cref="XmlException">The XML parser has encountered invalid
        /// or unexpected XML data.</exception>
        /// <exception cref="CultureNotFoundException">The culture specified by the
        /// XML-stream in it's 'xml:lang' attribute could not be found.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network, or there was a failure while reading from the network.</exception>
        private XmlElement StartTls(string hostname,
            RemoteCertificateValidationCallback validate)
        {
            // Send STARTTLS command and ensure the server acknowledges the request.
            SendAndReceive(Xml.Element("starttls",
                "urn:ietf:params:xml:ns:xmpp-tls"), false, "proceed");
            // Complete TLS negotiation and switch to secure stream.
            SslStream sslStream = new SslStream(stream, false, validate ??
                ((sender, cert, chain, err) => true));
            sslStream.AuthenticateAsClient(hostname);
            stream = sslStream;
            IsEncrypted = true;
            // Initiate a new stream to server.
            return InitiateStream(hostname);
        }

        /// <summary>
        /// Performs SASL authentication.
        /// </summary>
        /// <param name="mechanisms">An enumerable collection of SASL mechanisms
        /// supported by the server.</param>
        /// <param name="username">The username to authenticate with.</param>
        /// <param name="password">The password to authenticate with.</param>
        /// <param name="hostname">The hostname of the XMPP server.</param>
        /// <returns>The 'stream:features' XML element as received from the
        /// receiving entity upon establishment of a new XML stream.</returns>
        /// <remarks>Refer to RFC 3920, Section 6 (Use of SASL).</remarks>
        /// <exception cref="SaslException">A SASL error condition occured.</exception>
        /// <exception cref="XmlException">The XML parser has encountered invalid
        /// or unexpected XML data.</exception>
        /// <exception cref="CultureNotFoundException">The culture specified by the
        /// XML-stream in it's 'xml:lang' attribute could not be found.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network, or there was a failure while reading from the network.</exception>
        private XmlElement Authenticate(IEnumerable<string> mechanisms, string username,
            string password, string hostname)
        {
            string name = SelectMechanism(mechanisms);
            SaslMechanism m = SaslFactory.Create(name);
            m.Properties.Add("Username", username);
            m.Properties.Add("Password", password);
            var xml = Xml.Element("auth", "urn:ietf:params:xml:ns:xmpp-sasl")
                .Attr("mechanism", name)
                .Text(m.HasInitial ? m.GetResponse(String.Empty) : String.Empty);
            Send(xml, false);
            while (true)
            {
                XmlElement ret = parser.NextElement("challenge", "success", "failure");
                if (ret.Name == "failure")
                    throw new SaslException("SASL authentication failed.");
                if (ret.Name == "success" && m.IsCompleted)
                    break;
                // Server has successfully authenticated us, but mechanism still needs
                // to verify server's signature.
                string response = m.GetResponse(ret.InnerText);
                // If the response is the empty string, the server's signature has been
                // verified.
                if (ret.Name == "success")
                {
                    if (response == String.Empty)
                        break;
                    throw new SaslException("Could not verify server's signature.");
                }
                xml = Xml.Element("response",
                    "urn:ietf:params:xml:ns:xmpp-sasl").Text(response);
                Send(xml, false);
            }
            // The instance is now authenticated.
            Authenticated = true;
            // Finally, initiate a new XML-stream.
            return InitiateStream(hostname);
        }

        /// <summary>
        /// Selects the best SASL mechanism that we support from the list of mechanisms
        /// advertised by the server.
        /// </summary>
        /// <param name="mechanisms">An enumerable collection of SASL mechanisms
        /// advertised by the server.</param>
        /// <returns>The IANA name of the selcted SASL mechanism.</returns>
        /// <exception cref="SaslException">No supported mechanism could be found in
        /// the list of mechanisms advertised by the server.</exception>
        private string SelectMechanism(IEnumerable<string> mechanisms)
        {
            // Precedence: SCRAM-SHA-1, DIGEST-MD5, PLAIN.
            //string[] m = new string[] { "SCRAM-SHA-1", "DIGEST-MD5", "PLAIN" };
            string[] m = new string[] { "PLAIN" };
            for (int i = 0; i < m.Length; i++)
            {
                if (mechanisms.Contains(m[i], StringComparer.InvariantCultureIgnoreCase))
                    return m[i];
            }
            throw new SaslException("No supported SASL mechanism found.");
        }

        /// <summary>
        /// Performs resource binding and returns the 'full JID' with which this
        /// session associated.
        /// </summary>
        /// <param name="resourceName">The resource identifier to bind to. If this
        /// is null, the server generates a random identifier.</param>
        /// <returns>The full JID to which this session has been bound.</returns>
        /// <remarks>Refer to RFC 3920, Section 7 (Resource Binding).</remarks>
        /// <exception cref="XmppException">The resource binding process
        /// failed due to an erroneous server response.</exception>
        /// <exception cref="XmlException">The XML parser has encountered invalid
        /// or unexpected XML data.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network, or there was a failure while reading from the network.</exception>
        private Jid BindResource(string resourceName = null)
        {
            var xml = Xml.Element("iq")
                .Attr("type", "set")
                .Attr("id", BIND_ID);
            var bind = Xml.Element("bind", "urn:ietf:params:xml:ns:xmpp-bind");
            if (resourceName != null)
                bind.Child(Xml.Element("resource").Text(resourceName));
            xml.Child(bind);
            XmlElement res = SendAndReceive(xml, true, "iq");
            if (res["bind"] == null || res["bind"]["jid"] == null)
                throw new XmppException("Erroneous server response.");
            return new Jid(res["bind"]["jid"].InnerText);
        }

        /// <summary>
        /// Serializes and sends the specified XML element to the server.
        /// </summary>
        /// <param name="element">The XML element to send.</param>
        /// <exception cref="ArgumentNullException">The element parameter
        /// is null.</exception>
        /// <exception cref="IOException">There was a failure while writing
        /// to the network.</exception>
        internal void Send(XmlElement element, Boolean isStanza)
        {
            element.ThrowIfNull("element");
            Send(element.ToXmlString(), isStanza);
        }

        /// <summary>
        /// Sends the specified string to the server.
        /// </summary>
        /// <param name="xml">The string to send.</param>
        /// <exception cref="ArgumentNullException">The xml parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to
        /// the network.</exception>
        private void Send(string xml, Boolean isStanza)
        {
            xml.ThrowIfNull("xml");
            // XMPP is guaranteed to be UTF-8.
            byte[] buf = Encoding.UTF8.GetBytes(xml);

            if (useWebSocket)
            {
                webSocketClient.Send(xml); 
                if(isStanza && StreamManagementEnabled)
                    StreamManagementRequestAcknowledgement.Raise(this, null);
                return;
            }

            lock (writeLock)
            {
                //FIXME
                //If we have an IOexception immediatelly we make a disconnection, is it correct?
                try
                {
                    stream.Write(buf, 0, buf.Length);
                    if (debugStanzas) System.Diagnostics.Debug.WriteLine(xml);
                }
                catch (IOException e)
                {
                    RaiseConnectionStatus(false);
                    throw new XmppDisconnectionException(e.Message, e);
                }
                //FIXME
            }
        }

        /// <summary>
        /// Sends the specified stanza to the server.
        /// </summary>
        /// <param name="stanza">The stanza to send.</param>
        /// <exception cref="ArgumentNullException">The stanza parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to
        /// the network.</exception>
        private void Send(Stanza stanza)
        {
            stanza.ThrowIfNull("stanza");
            Send(stanza.ToString(), true);
        }

        /// <summary>
        /// Serializes and sends the specified XML element to the server and
        /// subsequently waits for a response.
        /// </summary>
        /// <param name="element">The XML element to send.</param>
        /// <param name="expected">A list of element names that are expected. If
        /// provided, and the read element does not match any of the provided names,
        /// an XmmpException is thrown.</param>
        /// <returns>The XML element read from the stream.</returns>
        /// <exception cref="XmlException">The XML parser has encountered invalid
        /// or unexpected XML data.</exception>
        /// <exception cref="ArgumentNullException">The element parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to
        /// the network, or there was a failure while reading from the network.</exception>
        private XmlElement SendAndReceive(XmlElement element, Boolean isStanza, params string[] expected)
        {
            Send(element, isStanza);
            try
            {
                return parser.NextElement(expected);
            }
            catch (XmppDisconnectionException e)
            {
                RaiseConnectionStatus(false);
                throw e;
            }
        }

        private void ReadAction()
        {
            while (true)
            {
                string action = webSocketClient.DequeueActionToPerform();
                //log.LogDebug("Action dequeued:{0}", action);

                if (ActionToPerform != null)
                {
                    try
                    {
                        ActionToPerform(this, new TextEventArgs(action));
                    }
                    catch (Exception)
                    {
                        //TODO
                    }
                }
            }
        }

        public void QueueActionToPerform(string action)
        {
            webSocketClient.QueueActionToPerform(action);
        }

        /// <summary>
        /// Listens for incoming XML stanzas and raises the appropriate events.
        /// </summary>
        /// <remarks>This runs in the context of a separate thread. In case of an
        /// exception, the Error event is raised and the thread is shutdown.</remarks>
        private void ReadXmlWebSocketMessage()
        {
            try
            {
                while (true)
                {
                    string message = webSocketClient.DequeueMessageReceived();
                    try
                    {
                        XmlDocument xmlDocument;
                        XmlElement elem, subElem;
                        XmlElement xmlResponse;

                        string response;
                        string attribute;

                        xmlDocument = new XmlDocument();
                        xmlDocument.LoadXml(message);
                        
                        elem = xmlDocument.DocumentElement;

                        switch (elem.Name)
                        {
                            case "challenge":
                                //log.LogDebug("challenge received");
                                response = saslMechanism.GetResponse(elem.InnerText);
                                xmlResponse = Xml.Element("response", 
                                    "urn:ietf:params:xml:ns:xmpp-sasl").Text(response);
                                Send(xmlResponse, false);
                                break;

                            case "success":
                                //log.LogDebug("success received");
                                if ( saslMechanism.IsCompleted ||
                                        (saslMechanism.GetResponse(elem.InnerText) == String.Empty) )
                                {
                                    Authenticated = true;

                                    elem = Xml.Element("open", "urn:ietf:params:xml:ns:xmpp-framing")
                                            .Attr("to", hostname)
                                            .Attr("version", "1.0")
                                            .Attr("xmlns:stream", "http://etherx.jabber.org/streams")
                                            .Attr("xml:lang", CultureInfo.CurrentCulture.Name);
                                    Send(elem.ToXmlString(xmlDeclaration: true, leaveOpen: false), false);
                                }
                                break;

                            case "failure":
                                log.LogWarning("Failure received");
                                //TO DOlog.LogDebug
                                break;

                            case "stream:features":
                                //log.LogDebug("stream:features received");

                                subElem = (XmlElement)elem.FirstChild;
                                if (subElem.Name == "mechanisms")
                                {
                                    var mech = subElem.FirstChild;
                                    var list = new HashSet<string>();
                                    while (mech != null)
                                    {
                                        list.Add(mech.InnerText);
                                        mech = mech.NextSibling;
                                    }
                                    string name = SelectMechanism(list);
                                    saslMechanism = SaslFactory.Create(name);
                                    saslMechanism.Properties.Add("Username", username);
                                    saslMechanism.Properties.Add("Password", password);
                                    xmlResponse = Xml.Element("auth", "urn:ietf:params:xml:ns:xmpp-sasl")
                                        .Attr("mechanism", name)
                                        .Text(saslMechanism.HasInitial ? saslMechanism.GetResponse(String.Empty) : String.Empty);
                                    Send(xmlResponse, false);
                                }
                                else if (subElem.Name == "bind")
                                {
                                    // Check if StreamManagement is Supported
                                    var smElement = elem["sm", "urn:xmpp:sm:3"];
                                    StreamManagementAvailable = (smElement != null);

                                    // /!\ Need to RESUME session here (if needed) and AVOID to do a binding in this case
                                    if (StreamManagementAvailable && StreamManagementResume)
                                    {
                                        var xml = Xml.Element("resume", "urn:xmpp:sm:3");
                                        xml.SetAttribute("h", StreamManagementLastStanzaReceivedAndHandledByClient.ToString());
                                        xml.SetAttribute("previd", StreamManagementResumeId);
                                        Send(xml, false);
                                    }
                                    else
                                    {
                                        xmlResponse = Xml.Element("iq")
                                            .Attr("type", "set")
                                            .Attr("id", BIND_ID);
                                        var bind = Xml.Element("bind", "urn:ietf:params:xml:ns:xmpp-bind");
                                        if (resource != null)
                                            bind.Child(Xml.Element("resource").Text(resource));
                                        xmlResponse.Child(bind);
                                        Send(xmlResponse, true);
                                    }
                                }
                                break;

                            case "iq":
                                //log.LogDebug("iq received");

                                attribute = elem.GetAttribute("id");
                                if (attribute == BIND_ID)
                                {
                                    Jid = new Jid(elem["bind"]["jid"].InnerText);
                                    webSocketClient.QueueActionToPerform(ACTION_CREATE_SESSION);
                                    break;
                                }

                                Iq iq = new Iq(elem);
                                //log.LogDebug("iq.Id: " + iq.Id);
                                if (webSocketClient.IsExpectedIqId(iq.Id))
                                {
                                    webSocketClient.QueueExpectedIqMessage(iq);
                                    break;
                                }
                                //else
                                //{
                                //    log.LogDebug("Not an expected Iq Message:{0}", iq.Id);
                                //}


                                if (iq.IsRequest)
                                {
                                    //log.LogDebug("Iq is request:{0}", iq.Id);
                                    stanzaQueue.Add(iq);
                                }
                                else
                                {
                                    //log.LogDebug("Handle Id response:{0}", iq.Id);
                                    HandleIqResponse(iq);
                                }
                                break;

                            case "message":
                                //log.LogDebug("message received");
                                stanzaQueue.Add(new Message(elem));
                                break;

                            case "presence":
                                //log.LogDebug("presence received");
                                stanzaQueue.Add(new Presence(elem));
                                break;

                            case "enabled": // in response to "enable"
                            case "a": // answer to a request
                            case "r": // request
                            case "resumed": // in response to "resume"
                            case "failed": // In case of pb
                                streamManagementStanzaQueue.Add(new StreamManagementStanza(elem));
                                break;

                            case "open":
                                // Nothing special to do
                                break;

                            case "close":
                                // Server has closed the session
                                // We need to cancel any resume info
                                StreamManagementResumeId = "";
                                break;

                            default:
                                log.LogError("ReadXmlWebSocketMessage - not managed:[{0}]", elem.Name);

                                break;
                        }

                    }
                    catch (Exception)
                    {
                        log.LogError("ReadXmlWebSocketMessage - ERROR");
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError("ReadXmlWebSocketMessage - SUB_ERROR");

                // Shut down the dispatcher task.
                cancelDispatch.Cancel();
                cancelDispatch.Dispose();
                cancelDispatch = null;
                cancelDispatch = new CancellationTokenSource();

                // Unblock any threads blocking on pending IQ requests.
                cancelIq.Cancel();
                cancelIq.Dispose();
                cancelIq = null;
                cancelIq = new CancellationTokenSource();

                //Add the failed connection
                if ((e is IOException) || (e is XmppDisconnectionException))
                {
                    RaiseConnectionStatus(false);
                    var ex = new XmppDisconnectionException(e.ToString());
                    e = ex;
                }
                // Raise the error event.
                if (!disposed)
                    Error.Raise(this, new ErrorEventArgs(e));
            }
        }

        /// <summary>
        /// Listens for incoming XML stanzas and raises the appropriate events.
        /// </summary>
        /// <remarks>This runs in the context of a separate thread. In case of an
        /// exception, the Error event is raised and the thread is shutdown.</remarks>
        private void ReadXmlStream()
        {
            try
            {
                while (true)
                {
                    XmlElement elem = parser.NextElement("iq", "message", "presence");
                    log.LogDebug("[ReadXmlStream] elem:[{0}]", elem.ToXmlString());
                    // Parse element and dispatch.
                    switch (elem.Name)
                    {
                        case "iq":
                            Iq iq = new Iq(elem);
                            if (iq.IsRequest)
                                stanzaQueue.Add(iq);
                            else
                                HandleIqResponse(iq);
                            break;

                        case "message":
                            stanzaQueue.Add(new Message(elem));
                            break;

                        case "presence":
                            stanzaQueue.Add(new Presence(elem));
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError("ReadXmlStream - SUB_ERROR");

                // Shut down the dispatcher task.
                cancelDispatch.Cancel();
                cancelDispatch.Dispose();
                cancelDispatch = null;
                cancelDispatch = new CancellationTokenSource();

                // Unblock any threads blocking on pending IQ requests.
                cancelIq.Cancel();
                cancelIq.Dispose();
                cancelIq = null;
                cancelIq = new CancellationTokenSource();
                //Add the failed connection
                if ((e is IOException) || (e is XmppDisconnectionException))
                {
                    RaiseConnectionStatus(false);
                    var ex = new XmppDisconnectionException(e.ToString());
                    e = ex;
                }
                // Raise the error event.
                if (!disposed)
                    Error.Raise(this, new ErrorEventArgs(e));
            }
        }

        private void IncreaseStanzaReceivedAndHandled()
        {
            if (StreamManagementLastStanzaReceivedByClient < uint.MaxValue)
                StreamManagementLastStanzaReceivedByClient++;
            else
                StreamManagementLastStanzaReceivedByClient = 0;
        }

        /// <summary>
        /// Continously removes stanzas from the FIFO of incoming stanzas and raises
        /// the respective events.
        /// </summary>
        /// <remarks>This runs in the context of a separate thread. All stanza events
        /// are streamlined and execute in the context of this thread.</remarks>
        private void DispatchEvents()
        {
            while (true)
            {
                try
                {
                    Stanza stanza;
                    BlockingCollection<Stanza>.TakeFromAny(fullStanzaQueue, out stanza, cancelDispatch.Token);

                    //log.LogDebug("DispatchEvents - message:[{0}]", stanza.ToString());
                    if (stanza is Iq)
                    {
                        Iq.Raise(this, new IqEventArgs(stanza as Iq));
                        IncreaseStanzaReceivedAndHandled();
                    }
                    else if (stanza is Message message)
                    {
                        Message.Raise(this, new MessageEventArgs(message));
                        IncreaseStanzaReceivedAndHandled();
                    }
                    else if (stanza is Presence presence)
                    {
                        Presence.Raise(this, new PresenceEventArgs(presence));
                        IncreaseStanzaReceivedAndHandled();
                    }
                    else if (stanza is StreamManagementStanza sms)
                        StreamManagementStanza.Raise(this, new StreamManagementStanzaEventArgs(sms));
                    else
                        log.LogError("DispatchEvents - not a valide stanza ....");
                }
                catch (OperationCanceledException)
                {
                    // Quit the task if it's been cancelled.
                    log.LogError("DispatchEvents - OperationCanceledException - ERROR");
                    return;
                }
                catch (Exception e)
                {
                    // FIXME: What should we do if an exception is thrown in one of the
                    // event handlers?
                    log.LogError("DispatchEvents - global exception - ERROR");
                    System.Diagnostics.Debug.WriteLine("Error in XMPP Core: " + e.StackTrace + e.ToString());
                    //throw e;
                }
            }
        }

        /// <summary>
        /// Handles incoming IQ responses for previously issued IQ requests.
        /// </summary>
        /// <param name="iq">The received IQ response stanza.</param>
        private void HandleIqResponse(Iq iq)
        {
            string id = iq.Id;
            AutoResetEvent ev;
            Action<string, Iq> cb;
            iqResponses[id] = iq;
            // Signal the event if it's a blocking call.
            if (waitHandles.TryRemove(id, out ev))
                ev.Set();
            // Call the callback if it's an asynchronous call.
            else if (iqCallbacks.TryRemove(id, out cb))
                Task.Factory.StartNew(() => { cb(id, iq); });
        }

        /// <summary>
        /// Generates a unique id.
        /// </summary>
        /// <returns>A unique id.</returns>
        public static string GetId()
        {
            return "Sharp.Ws.Xmpp." + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Disconnects from the XMPP server.
        /// </summary>
        private void Disconnect()
        {
            if (!Connected)
                return;


            // Close the XML stream.
            if (normalClosure)
                Send("</stream:stream>", false);

            //if (useWebSocket)
            //{
            //    if (webSocketClient != null)
            //    {
            //        webSocketClient.Close(normalClosure);
            //    }
            //}
            
            Connected = false;
            Authenticated = false;
        }
    }
}