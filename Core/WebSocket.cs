using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using WebSocketSharp;

using NLog;
using System.Security.Authentication;

namespace Sharp.Xmpp.Core
{
    internal class WebSocket
    {
        private static readonly NLog.Logger log = LogConfigurator.GetLogger(typeof(WebSocket));

        public event EventHandler WebSocketOpened;
        public event EventHandler WebSocketClosed;
        public event EventHandler<ExceptionEventArgs> WebSocketError;

        private bool webSocketOpened = false;

        private bool rootElement;
        private string uri;
        private StringBuilder sb;

        private readonly object writeLock = new object();

        private BlockingCollection<string> actionsToPerform;
        private BlockingCollection<string> messagesToSend;
        private BlockingCollection<string> messagesReceived;
        private BlockingCollection<Iq> iqMessagesReceived;
        private HashSet<String> iqIdList;

        private Tuple<String, String, String> webProxyInfo = null;

        private WebSocketSharp.WebSocket webSocketSharp = null;

        public CultureInfo Language
        {
            get;
            private set;
        }

        public WebSocket(String uri, Tuple<String, String, String> webProxyInfo)
        {
            log.Debug("Create Web socket");
            this.uri = uri;
            rootElement = false;

            this.webProxyInfo = webProxyInfo;

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
            if (webSocketSharp == null)
            {
                webSocketSharp = new WebSocketSharp.WebSocket(uri);

                webSocketSharp.SslConfiguration.EnabledSslProtocols = SslProtocols.Default | SslProtocols.Tls12 ;
                webSocketSharp.SslConfiguration.ServerCertificateValidationCallback =  (sender, certificate, chain, sslPolicyErrors) => {
                    return true; // If the server certificate is valid.
                };

                webSocketSharp.EmitOnPing = true;
                webSocketSharp.EnableRedirection = true;
                webSocketSharp.WaitTime = TimeSpan.FromSeconds(2);

                webSocketSharp.OnOpen += WebSocketSharp_Opened;
                webSocketSharp.OnClose += WebSocketSharp_OnClose;
                webSocketSharp.OnError += WebSocketSharp_OnError;
                webSocketSharp.OnMessage += WebSocketSharp_OnMessage;
            }

            // Set proxy info
            if (webProxyInfo == null)
                webSocketSharp.SetProxy(null, null, null);
            else
            {
                // Set Ip End point
                log.Debug("[CreateAndManageWebSocket] Web Proxy Info:[{0}]", webProxyInfo?.Item1);
                webSocketSharp.SetProxy(webProxyInfo.Item1, webProxyInfo.Item2, webProxyInfo.Item3);
            }

            webSocketSharp.Connect();

            string message;

            while (true)
            {
                if (webSocketSharp != null)
                {
                    if(!webSocketSharp.IsAlive)
                    {
                        if (webSocketOpened)
                        {
                            webSocketOpened = false;
                            log.Debug("[webSocketSharp] Web Socket is not alive ...");
                            WebSocketError?.Invoke(this, new ExceptionEventArgs(new Exception("Web Socket is not alive")));
                            return;
                        }
                    }

                    switch (webSocketSharp.ReadyState)
                    {
                        case WebSocketSharp.WebSocketState.Connecting:
                        case WebSocketSharp.WebSocketState.Open:
                            if (messagesToSend.TryTake(out message, 50))
                            {
                                log.Debug("[Message_Send]: {0}", message);
                                if (webSocketSharp != null)
                                    webSocketSharp.Send(message);
                            }
                            else
                            {
                                //log.Debug("[Message_Send] TryTake failed ...");
                            }
                            break;

                        case WebSocketSharp.WebSocketState.Closed:
                        case WebSocketSharp.WebSocketState.Closing:
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
            if (webSocketSharp != null)
                webSocketSharp = null;
        }

        public void Close()
        {
            if (webSocketSharp != null)
                webSocketSharp.Close();
        }

        private bool IsConnected()
        {
            if (webSocketSharp != null)
                return webSocketSharp.ReadyState == WebSocketSharp.WebSocketState.Open;
            return false;
        }

        public void Send(string xml)
        {
            QueueMessageToSend(xml);
        }

        private void WebSocketSharp_Opened(object sender, EventArgs e)
        {
            log.Debug("Web socket opened");
            webSocketOpened = true;
            EventHandler h = this.WebSocketOpened;

            if (h != null)
            {
                try
                {
                    h(this, e);
                }
                catch (Exception)
                {
                    log.Error("WebSocketSharp_Opened - ERROR");
                }
            }
        }

        private void WebSocketSharp_OnMessage(object sender, WebSocketSharp.MessageEventArgs e)
        {
            lock (writeLock)
            {
                if (e.IsText)
                {
                    string xmlMessage;
                    sb.Append(e.Data);

                    log.Debug("[Message_Received]: {0}", e.Data);

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
                else if (e.IsPing)
                {
                    log.Debug("[Message_Received]: Ping received");
                }
                else 
                {
                    log.Debug("[Message_Received]: message type not managed");
                }
            }
        }

        private void WebSocketSharp_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            if (webSocketOpened)
            {
                webSocketOpened = false;

                if (!String.IsNullOrEmpty(e.Message))
                    log.Debug("[WebSocketSharp_OnError] Message:[{0}]", e.Message);

                if (e.Exception != null)
                    log.Debug("[WebSocketSharp_OnError] Exception:[{0}]", Util.SerializeException(e.Exception));

                EventHandler<ExceptionEventArgs> h = this.WebSocketError;

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
        }

        private void WebSocketSharp_OnClose(object sender, CloseEventArgs e)
        {
            webSocketOpened = false;
            log.Debug("[WebSocketSharp_OnClose]");
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
