using System;
using System.Collections.Generic;
using System.Timers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocket4Net;
using SuperSocket.ClientEngine;
using System.Collections.Concurrent;
using System.Xml;
using System.Globalization;

using log4net;

namespace Sharp.Xmpp.Core
{
    internal class WebSocket
    {
        private const int TIMER_WEBSOCKET_ALIVE = 10000;

        private static readonly ILog log = LogManager.GetLogger(typeof(WebSocket));

        public event EventHandler WebSocketOpened;
        public event EventHandler WebSocketClosed;
        public event EventHandler<ExceptionEventArgs> WebSocketError;

        private bool rootElement;
        private string uri;
        private StringBuilder sb;

        private readonly object writeLock = new object();

        private BlockingCollection<string> actionsToPerform;
        private BlockingCollection<string> messagesToSend;
        private BlockingCollection<string> messagesReceived;
        private BlockingCollection<Iq> iqMessagesReceived;
        private HashSet<String> iqIdList;

        private Timer reconnectTimer;

        private WebSocket4Net.WebSocket webSocket4NetClient = null;

        public CultureInfo Language
        {
            get;
            private set;
        }

        public WebSocket(String uri)
        {
            log.Debug("Create Web socket");
            this.uri = uri;
            rootElement = false;

            sb = new StringBuilder();

            actionsToPerform = new BlockingCollection<string>(new ConcurrentQueue<string>());
            messagesToSend = new BlockingCollection<string>(new ConcurrentQueue<string>());
            messagesReceived = new BlockingCollection<string>(new ConcurrentQueue<string>());
            iqMessagesReceived = new BlockingCollection<Iq>(new ConcurrentQueue<Iq>());
            iqIdList = new HashSet<string>();
        }

        public void Open()
        {
            Task.Factory.StartNew(CreateAndManageWebSocket, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);
        }

        private void ReconnectTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (webSocket4NetClient != null)
            {
                if (webSocket4NetClient.State != WebSocketState.Connecting && webSocket4NetClient.State != WebSocketState.Open)
                {
                    log.DebugFormat("[ReconnectTimer_Elapsed] no more opened ...");
                    RaiseWebSocketClosed();
                }
            }
        }

        private void CreateAndManageWebSocket()
        {
            if (webSocket4NetClient == null)
            {
                webSocket4NetClient = new WebSocket4Net.WebSocket(uri);

                if (uri.ToLower().StartsWith("wss"))
                    webSocket4NetClient.Security.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;

                webSocket4NetClient.Opened += new EventHandler(WebSocket4NetClient_Opened);
                webSocket4NetClient.Closed += new EventHandler(WebSocket4NetClient_Closed);
                webSocket4NetClient.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(WebSocket4NetClient_Error);
                webSocket4NetClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(WebSocket4NetClient_MessageReceived);
                webSocket4NetClient.DataReceived += new EventHandler<WebSocket4Net.DataReceivedEventArgs>(WebSocket4NetClient_DataReceived);

                // Use timer to ensure the connection is still alive or not
                reconnectTimer = new Timer
                {
                    Interval = TIMER_WEBSOCKET_ALIVE
                };
                reconnectTimer.Elapsed += ReconnectTimer_Elapsed;

            }
            webSocket4NetClient.Open();

            string message;

            while(true)
            {
                if (webSocket4NetClient != null)
                {
                    switch (webSocket4NetClient.State)
                    {
                        case WebSocketState.Connecting:
                        case WebSocketState.Open:
                            if (messagesToSend.TryTake(out message, 100))
                            {
                                log.DebugFormat("Message send:{0}", message);
                                if (webSocket4NetClient != null)
                                    webSocket4NetClient.Send(message);
                            }
                            break;

                        case WebSocketState.Closed:
                        case WebSocketState.Closing:
                        case WebSocketState.None:
                            return;
                    }
                }
                else
                    return;
            }                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   
        }

        #region Iq stuff
        public void AddExpectedIqId(string id)
        {
            log.DebugFormat("AddExpectedIqId:{0}", id);
            if (!iqIdList.Contains(id))
                iqIdList.Add(id);
        }

        public bool IsExpectedIqId(string id)
        {
            log.DebugFormat("IsExpectedIqId:{0}", id);
            return iqIdList.Contains(id);
        }

        public void QueueExpectedIqMessage(Iq iq)
        {
            log.DebugFormat("QueueExpectedIqMessage :{0}", iq.ToString());
            iqMessagesReceived.Add(iq);
        }

        public Iq DequeueExpectedIqMessage()
        {
            Iq iq = null;
            log.DebugFormat("DequeueExpectedIqMessage - START");
            iq = iqMessagesReceived.Take();
            log.DebugFormat("DequeueExpectedIqMessage - END");
            return iq;

        }
        #endregion

        #region Action to perform
        public void QueueActionToPerform(String action)
        {
            log.DebugFormat("QueueActionToPerform");
            actionsToPerform.Add(action);
        }

        public string DequeueActionToPerform()
        {
            log.DebugFormat("DequeueActionToPerform - START");
            string action = actionsToPerform.Take();
            log.DebugFormat("DequeueActionToPerform - END");
            return action;
        }
        #endregion

        #region Messages to send
        private void QueueMessageToSend(String message)
        {
            messagesToSend.Add(message);
        }

        private string DequeueMessageToSend()
        {
            return messagesToSend.Take();
        }
        #endregion

        #region Messages received
        public void QueueMessageReceived(String message)
        {
            lock (writeLock)
            {
                string xmlMessage;

                sb.Append(message);

                XmlDocument xmlDocument = new XmlDocument();
                try
                {
                    xmlMessage = sb.ToString();

                    // Check if we have a valid XML message - if not an exception is raised
                    xmlDocument.LoadXml(sb.ToString());

                    //Clear string builder
                    sb.Clear();

                    if (rootElement)
                    {
                        // Add XML message in the queue
                        log.DebugFormat("Queue XML message received");
                        messagesReceived.Add(xmlMessage);
                    }
                    else
                        ReadRootElement(xmlDocument);
                }
                catch
                {
                    // Nothing more to do
                }
            }
        }

        public string DequeueMessageReceived()
        {
            log.DebugFormat("Dequeue XML Message Received - START");
            string message = messagesReceived.Take();
            log.DebugFormat("Dequeue XML Message Received - END");
            return message;
        }
        #endregion

        public void Dispose()
        {
            if (webSocket4NetClient != null)
            {
                reconnectTimer.Stop();
                reconnectTimer.Close();

                webSocket4NetClient.Dispose();
                webSocket4NetClient = null;
            }
        }

        public void Close()
        {
            if (webSocket4NetClient != null)
                webSocket4NetClient.Close();

            webSocket4NetClient.Dispose();
        }

        private bool IsConnected()
        {
            if (webSocket4NetClient != null)
                return webSocket4NetClient.State == WebSocketState.Open;
            return false;
        }

        public void Send(string xml)
        {
            QueueMessageToSend(xml);
        }

        private void WebSocket4NetClient_DataReceived(object sender, WebSocket4Net.DataReceivedEventArgs e)
        {
            log.DebugFormat("[WebSocket4NetClient_MessageReceived] Data received ... Not handled yet ...");
        }

        private void WebSocket4NetClient_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            lock (writeLock)
            {
                string xmlMessage;

                sb.Append(e.Message);

                log.DebugFormat("[WebSocket4NetClient_MessageReceived] Message received: {0}", e.Message);

                XmlDocument xmlDocument = new XmlDocument();
                try
                {
                    xmlMessage = sb.ToString();

                    // Check if we have a valid XML message - if not an exception is raised
                    xmlDocument.LoadXml(sb.ToString());

                    //Clear string builder
                    sb.Clear();

                    if (rootElement)
                    {
                        // Add XML message in the queue
                        QueueMessageReceived(xmlMessage);
                    }
                    else
                        ReadRootElement(xmlDocument);
                }
                catch
                {
                    // Nothing more to do
                }
            }
        }

        private void WebSocket4NetClient_Opened(object sender, EventArgs e)
        {
            log.DebugFormat("Web socket opened");
            EventHandler h = this.WebSocketOpened;

            if (h != null)
            {
                try
                {
                    h(this, e);
                }
                catch (Exception ex)
                {
                    //TODO
                }
            }
        }

        private void WebSocket4NetClient_Closed(object sender, EventArgs e)
        {
            log.DebugFormat("[WebSocket4NetClient_Closed]");
            RaiseWebSocketClosed();
        }

        private void RaiseWebSocketClosed()
        {
            log.DebugFormat("Web socket closed");
            EventHandler h = this.WebSocketClosed;

            if (h != null)
            {
                try
                {
                    h(this, new EventArgs());
                }
                catch (Exception)
                {
                    //TODO
                }
            }
        }

        private void WebSocket4NetClient_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            //TODO: enhance error log
            log.DebugFormat("[WebSocket4NetClient_Error] exception message:", e.Exception.Message);
            EventHandler <ExceptionEventArgs> h = this.WebSocketError;
            
            if (h != null)
            {
                try
                {
                    h(this, new ExceptionEventArgs(e.Exception));
                }
                catch (Exception)
                {
                    //TODO
                }
            }
        }

        private void ReadRootElement(XmlDocument xmlDocument)
        {
            XmlElement Open;
            Open = xmlDocument.DocumentElement;

            if (Open == null)
                throw new XmlException("Unexpected XML message received");

            if (Open.Name == "open")
            {
                rootElement = true;
                string lang = Open.GetAttribute("xml:lang");
                if (!String.IsNullOrEmpty(lang))
                    Language = new CultureInfo(lang);
            }
            throw new XmlException("Unexpected XML message received");

        }
    }
}
