using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using WebSocket4Net;

using NLog;
using SuperSocket.ClientEngine.Proxy;

namespace Sharp.Xmpp.Core
{
    internal class WebSocket
    {
        private static readonly Logger log = LogConfigurator.GetLogger(typeof(WebSocket));

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

        private IPEndPoint ipEndPoint = null;
        private Uri proxy = null;

        private WebSocket4Net.WebSocket webSocket4NetClient = null;

        public CultureInfo Language
        {
            get;
            private set;
        }

        public WebSocket(String uri, IPEndPoint ipEndPoint, Uri proxy)
        {
            log.Debug("Create Web socket");
            this.uri = uri;
            rootElement = false;

            this.ipEndPoint = ipEndPoint;
            this.proxy = proxy;

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

        private void CreateAndManageWebSocket()
        {
            if (webSocket4NetClient == null)
            {
                webSocket4NetClient = new WebSocket4Net.WebSocket(uri);

                // Set Ip End point
                log.Debug("[CreateAndManageWebSocket] IpEndPoint:[{0}] - Proxy:[{1}]", ipEndPoint?.ToString(), proxy?.ToString());

                HttpConnectProxy proxyWS = null;
                if (proxy != null)
                    proxyWS = new HttpConnectProxy(new IPEndPoint(IPAddress.Parse(proxy.Host), proxy.Port));

                webSocket4NetClient.Proxy = (SuperSocket.ClientEngine.IProxyConnector)proxyWS;
                webSocket4NetClient.LocalEndPoint = ipEndPoint;

                if (uri.ToLower().StartsWith("wss"))
                    webSocket4NetClient.Security.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;

                webSocket4NetClient.EnableAutoSendPing = true;
                webSocket4NetClient.AutoSendPingInterval = 1; // in seconds

                webSocket4NetClient.Opened += new EventHandler(WebSocket4NetClient_Opened);
                webSocket4NetClient.Closed += new EventHandler(WebSocket4NetClient_Closed);
                webSocket4NetClient.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(WebSocket4NetClient_Error);
                webSocket4NetClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(WebSocket4NetClient_MessageReceived);
                webSocket4NetClient.DataReceived += new EventHandler<WebSocket4Net.DataReceivedEventArgs>(WebSocket4NetClient_DataReceived);
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
                                log.Debug("[Message_Send]{0}", message);
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
            //log.Debug("AddExpectedIqId:{0}", id);
            if (!iqIdList.Contains(id))
                iqIdList.Add(id);
        }

        public bool IsExpectedIqId(string id)
        {
            //log.Debug("IsExpectedIqId:{0}", id);
            return iqIdList.Contains(id);
        }

        public void QueueExpectedIqMessage(Iq iq)
        {
            //log.Debug("QueueExpectedIqMessage :{0}", iq.ToString());
            iqMessagesReceived.Add(iq);
        }

        public Iq DequeueExpectedIqMessage()
        {
            Iq iq = null;
            //log.Debug("DequeueExpectedIqMessage - START");
            iq = iqMessagesReceived.Take();
            //log.Debug("DequeueExpectedIqMessage - END");
            return iq;

        }
        #endregion

        #region Action to perform
        public void QueueActionToPerform(String action)
        {
            //log.Debug("QueueActionToPerform");
            actionsToPerform.Add(action);
        }

        public string DequeueActionToPerform()
        {
            //log.Debug("DequeueActionToPerform - START");
            string action = actionsToPerform.Take();
            //log.Debug("DequeueActionToPerform - END");
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
                        //log.Debug("Queue XML message received");
                        messagesReceived.Add(xmlMessage);
                    }
                    else
                        ReadRootElement(xmlDocument);
                }
                catch
                {
                    log.Error("QueueMessageReceived - ERROR");
                }
            }
        }

        public string DequeueMessageReceived()
        {
            //log.Debug("Dequeue XML Message Received - START");
            string message = messagesReceived.Take();
            //log.Debug("Dequeue XML Message Received - END");
            return message;
        }
        #endregion

        public void Dispose()
        {
            if (webSocket4NetClient != null)
            {
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
            log.Debug("[WebSocket4NetClient_DataReceived] Not handled yet ...");
        }

        private void WebSocket4NetClient_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            lock (writeLock)
            {
                string xmlMessage;

                sb.Append(e.Message);

                log.Debug("[Message_Received]: {0}", e.Message);

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
                catch (Exception)
                {
                    log.Error("WebSocket4NetClient_MessageReceived - ERROR");
                }
            }
        }

        private void WebSocket4NetClient_Opened(object sender, EventArgs e)
        {
            log.Debug("Web socket opened");
            EventHandler h = this.WebSocketOpened;

            if (h != null)
            {
                try
                {
                    h(this, e);
                }
                catch (Exception)
                {
                    log.Error("WebSocket4NetClient_Opened - ERROR");
                }
            }
        }

        private void WebSocket4NetClient_Closed(object sender, EventArgs e)
        {
            log.Debug("[WebSocket4NetClient_Closed]");
            RaiseWebSocketClosed();
        }

        private void RaiseWebSocketClosed()
        {
            log.Debug("[RaiseWebSocketClosed]");
            EventHandler h = this.WebSocketClosed;

            if (h != null)
            {
                try
                {
                    h(this, new EventArgs());
                }
                catch (Exception)
                {
                    log.Error("RaiseWebSocketClosed - ERROR");
                }
            }
        }

        private void WebSocket4NetClient_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            //TODO: enhance error log
            log.Debug("[WebSocket4NetClient_Error] exception message:", e.Exception.Message);
            EventHandler <ExceptionEventArgs> h = this.WebSocketError;
            
            if (h != null)
            {
                try
                {
                    h(this, new ExceptionEventArgs(e.Exception));
                }
                catch (Exception)
                {
                    log.Error("WebSocket4NetClient_Error - ERROR");
                }
            }
        }

        private void ReadRootElement(XmlDocument xmlDocument)
        {
            XmlElement Open;
            Open = xmlDocument.DocumentElement;

            if (Open == null)
            {
                log.Error("ReadRootElement - Unexpected XML message received");
            }

            if (Open.Name == "open")
            {
                rootElement = true;
                string lang = Open.GetAttribute("xml:lang");
                if (!String.IsNullOrEmpty(lang))
                    Language = new CultureInfo(lang);
            }
            else
            {
                log.Error("ReadRootElement - ERROR");
            }
        }
    }
}
