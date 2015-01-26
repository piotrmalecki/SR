

using System;
using System.Text;
using System.Linq;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using WpfApplication1.Comminication;
using System.Collections.Generic;
using Newtonsoft.Json;
using ServerSocketWpfApp.Comminication;
using System.Resources;
using System.Globalization;
using System.Collections;

namespace ServerSocketWpfApp
{

    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public partial class MainWindow : Window
    {
        public bool server = true;
        public string serverIP = null;

        const int MAX_CLIENTS = 10;
        public List<ClientWorker> clientsConnected = new List<ClientWorker>();
        public List<NodeWorker> nodesConnected = new List<NodeWorker>();
        public List<ServerWorkerer> serverClients = new List<ServerWorkerer>();
        public AsyncCallback pfnWorkerCallBack;
        public AsyncCallback pfnWorkerNodeCallBack;
        public List<string> allNodesIps = new List<string>();
        public List<IPEndPoint> ipPointList = new List<IPEndPoint>();
        string myIpAddress = null;
        public int elNo = 0;
        public int port = 4511;
        public int portNode = 45000;
        private Socket m_mainSocket;
        private Socket m_nodeSocketListener;
        private Socket m_nodeSocketConnectorSingle;
        private Socket[] m_nodeSocketConnector = new Socket[10];
        private Socket[] m_nodeWorkerSocket = new Socket[10];
        private Socket[] m_workerSocket = new Socket[10];
        private int m_clientCount = 0;
        private int m_nodeCount = 0;
        public Socket handler;
        public List<Client> clientList = new List<Client>();
        public List<int> portstClient = new List<int>();
        public String result = null;
        public String resultNode = null;
        public Timer timer;

        private TextBox tbAux = new TextBox();

        public MainWindow()
        {
            InitializeComponent();
            tbAux.SelectionChanged += tbAux_SelectionChanged;

            Start_Button.IsEnabled = true;
            Send_Button.IsEnabled = false;
            Close_Button.IsEnabled = false;
        }

        private void tbAux_SelectionChanged(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
            {
                tbMsgReceived.Text = tbAux.Text;
            }
            );
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                myIpAddress = Helpers.getMyIPAddress();
                // Resolves a host name to an IPHostEntry instance 
                IPHostEntry ipHost = Dns.GetHostEntry("");

                // Gets first IP address associated with a localhost 
                IPAddress ipAddr = Dns.Resolve(Helpers.getMyIPAddress()).AddressList[0];
                ResourceSet resourceSet = IPadressess.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);

                // Creates a network endpoint 
                m_nodeSocketListener = new Socket(AddressFamily.InterNetwork,
                       SocketType.Stream,
                       ProtocolType.Tcp);
                m_nodeSocketConnectorSingle = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);

                m_mainSocket = new Socket(
                       ipAddr.AddressFamily,
                       SocketType.Stream,
                       ProtocolType.Tcp
                       );
                var portip = new IPEndPoint(ipAddr, port);



                //nie łączyć się ze sobą ani nie dodawc sienie do ilPoinList



                var nodeEndPoint1 = new IPEndPoint(IPAddress.Parse(myIpAddress), portNode);

                m_nodeSocketListener.Bind(nodeEndPoint1);
                // allNodesIps.Add(entry.Value.ToString());

                foreach (DictionaryEntry entry in resourceSet)
                {
                    if (entry.Value.ToString() != "")
                    {
                        var nodeEndPoint2 = new IPEndPoint(IPAddress.Parse(entry.Value.ToString()), portNode);
                        ipPointList.Add(nodeEndPoint2);
                    }
                }
                m_nodeSocketListener.Listen(100);
                m_nodeSocketListener.BeginAccept(new AsyncCallback(OnNodeConnect), null);

                m_mainSocket.Bind(portip);
                m_mainSocket.Listen(100);
                m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
                // Associates a Socket with a local endpoint 

                tbStatus.Text = "Server started.";

                Start_Button.IsEnabled = false;

            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
        //godzina
        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            bool isError = false;
            try
            {
                // starting element int a =
                var next = Helpers.GetNextIPAdressIPEndPoint(ipPointList, myIpAddress);
                while (true)
                {
                    try
                    {   //sparwdzic czy jest polaczenie
                        if (!next.Address.ToString().Equals(myIpAddress.ToString())) m_nodeSocketConnectorSingle.Connect(next);
                        else break;
                        if (m_nodeSocketConnectorSingle.Connected)
                        {
                            Election election = new Election("election", new List<Member> { new Member(myIpAddress, elNo) });
                            m_nodeSocketConnectorSingle.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(election)));
                            addLogComment("Election send : " + m_nodeSocketConnectorSingle.RemoteEndPoint.ToString() + "ElNo: " + elNo + "\n");
                            server = false;
                            break;
                        }
                        else { server = true; }
                    }
                    catch (SocketException)
                    {
                        next = Helpers.GetNextIPAdressIPEndPoint(ipPointList, next.Address.ToString());
                        isError = true;
                    }
                    if (isError) continue;
                }
                if (server)
                {
                    addLogComment("Jestem sam jestem serwerem\n");

                }
            }
            catch (Exception se)
            {
                MessageBox.Show(se.Message);
            }
        }

        private void addLogComment(string Comment)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
            {
                tbMsgReceived.Text += Comment;

            });
        }

        private void OnNodeConnect(IAsyncResult asyn)
        {
            try
            {   // ktoś się dołączył, trzeba sprawdzić czy jestem serwerem
                server = false;
                Socket soc = m_nodeSocketListener.EndAccept(asyn);
                m_nodeWorkerSocket[m_nodeCount] = soc;
                var tmp = m_nodeWorkerSocket[m_nodeCount].RemoteEndPoint.ToString().ToString().Split(':')[0];
                nodesConnected.Add(new NodeWorker(tmp, soc));
                WaitForNodeData(soc);
                ++m_nodeCount;


                m_nodeSocketListener.BeginAccept(new AsyncCallback(OnNodeConnect), null);



                //if (Convert.ToInt32(myIpAddress.Split('.')[3]) <  Convert.ToInt32(nodesConnected.Min(i=>i.ip.Split('.')[3])))//ForEach(i => Convert.ToInt32(i.ip.Split(new char[] { ' ' })[3])).Min() > 5)
                //      {

                //timer = new Timer(OnTimer, null, 1000, 0);
                //connect = JsonConvert.SerializeObject(election);
                //    }


            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\n OnClientConnection: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }

        public void OnClientConnect(IAsyncResult asyn)
        {
            try
            {
                // Here we complete/end the BeginAccept() asynchronous call
                // by calling EndAccept() - which returns the reference to
                // a new Socket object
                m_workerSocket[m_clientCount] = m_mainSocket.EndAccept(asyn);
                // Let the worker Socket do the further processing for the 
                // just connected client
                //string addr = m_workerSocket[m_clientCount].RemoteEndPoint.ToString().Split(':')[0];
                WaitForData(m_workerSocket[m_clientCount]);
                // Now increment the client count
                ++m_clientCount;
                // Since the main Socket is now free, it can go back and wait for
                // other clients who are attempting to connect
                m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\n OnClientConnection: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }

        }

        private void WaitForNodeData(Socket soc)
        {
            try
            {
                if (pfnWorkerNodeCallBack == null)
                {
                    pfnWorkerNodeCallBack = new AsyncCallback(OnNodeDataReceived);
                }
                SocketPacket theSocPkt = new SocketPacket();
                theSocPkt.m_currentSocket = soc;
                // Start receiving any data written by the connected client
                // asynchronously
                soc.BeginReceive(theSocPkt.dataBuffer, 0,
                                   theSocPkt.dataBuffer.Length,
                                   SocketFlags.None,
                                   pfnWorkerNodeCallBack,
                                   theSocPkt);
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }


        public class SocketPacket
        {
            public System.Net.Sockets.Socket m_currentSocket;
            public byte[] dataBuffer = new byte[2048];
        }
        // Start waiting for data from the client
        public void WaitForData(System.Net.Sockets.Socket soc)
        {
            try
            {
                if (pfnWorkerCallBack == null)
                {
                    // Specify the call back function which is to be 
                    // invoked when there is any write activity by the 
                    // connected client
                    pfnWorkerCallBack = new AsyncCallback(OnDataReceived);
                }
                SocketPacket theSocPkt = new SocketPacket();
                theSocPkt.m_currentSocket = soc;
                // Start receiving any data written by the connected client
                // asynchronously
                soc.BeginReceive(theSocPkt.dataBuffer, 0,
                                   theSocPkt.dataBuffer.Length,
                                   SocketFlags.None,
                                   pfnWorkerCallBack,
                                   theSocPkt);
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }

        }
        private void OnNodeDataReceived(IAsyncResult asyn)
        {
            try
            {
                SocketPacket socketData = (SocketPacket)asyn.AsyncState;

                int iRx = 0;

                iRx = socketData.m_currentSocket.EndReceive(asyn);
                char[] chars = new char[iRx + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(socketData.dataBuffer,
                                         0, iRx, chars, 0);
                System.String szData = new System.String(chars);


                NodeLogic(socketData.m_currentSocket, szData);
                // Continue the waiting for data on the Socket
                WaitForNodeData(socketData.m_currentSocket);

            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nOnDataReceived: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }


        // This the call back function which will be invoked when the socket
        // detects any client writing of data on the stream
        public void OnDataReceived(IAsyncResult asyn)
        {
            try
            {
                SocketPacket socketData = (SocketPacket)asyn.AsyncState;

                int iRx = 0;
                // Complete the BeginReceive() asynchronous call by EndReceive() method
                // which will return the number of characters written to the stream 
                // by the client
                iRx = socketData.m_currentSocket.EndReceive(asyn);
                char[] chars = new char[iRx + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(socketData.dataBuffer,
                                         0, iRx, chars, 0);
                System.String szData = new System.String(chars);
                //richTextBoxReceivedMsg.AppendText(szData);

                Logic(socketData.m_currentSocket, szData);
                // Continue the waiting for data on the Socket
                WaitForData(socketData.m_currentSocket);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nOnDataReceived: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }
        private void NodeLogic(Socket handler, string content)
        {
            JObject json = null;
            try
            {
                //resultNode += content;
                json = JObject.Parse(content);
                resultNode = content;
            }
            catch (Exception)
            {
                return;
            }
            switch ((string)json["type"])
            {

                case "election":
                    { // dodac timeout
                        Election deserializedElection = JsonConvert.DeserializeObject<Election>(resultNode);
                        addLogComment("f");
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
                        {
                            //tbMsgReceived.Text += "Odebrany election : " + deserializedElection.members.ForEach(i=>i.)ToString() + "\n";

                        });
                        if (elNo <= deserializedElection.members.Max(i => i.elNo))
                        {
                            StringBuilder sb = Helpers.typeJson("type", "election-ack");
                            handler.Send(Encoding.ASCII.GetBytes(sb.ToString()));

                            if (deserializedElection.members.Select(i => i.ip).Contains(myIpAddress))
                            {
                                //election done
                                elNo++;
                                Election electionDone = new Election("election-done", deserializedElection.members);
                                // electionDone.members.Where(i => i.ip.Equals(myIpAddress)).Select(i => i.received).FirstOrDefault() = true;
                                foreach (var item in electionDone.members)
                                {
                                    if (item.ip.Equals(myIpAddress)) { item.received = true; item.elNo = elNo; }
                                }
                                var sendTo = electionDone.members.Where(i => !i.received).Select(i => i.ip).FirstOrDefault();
                                if (sendTo != null)
                                {
                                    nodesConnected.Where(i => i.ip.Equals(sendTo)).Select(i => i.socket).FirstOrDefault().Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(electionDone)));
                                }
                                var tmp = electionDone.members.Max(i => Convert.ToInt32(i.ip.Split('.')[3]));
                                serverIP = nodesConnected.Where(i => Convert.ToInt32(i.ip.Split('.')[3]) == tmp).Select(i => i.ip).FirstOrDefault();
                                //nodesConnected.Where(i=>i.)
                            }
                            else
                            {
                                deserializedElection.members.Add(new Member(myIpAddress, elNo));
                                //KOLEJNY Z LISTY !!
                                Socket sock = nodesConnected.Where(i => !deserializedElection.members.Select(q => q.ip).Contains(i.ip)).Select(i => i.socket).FirstOrDefault();

                                if (sock == null)
                                {
                                    // odsylam do pierwszego
                                    nodesConnected.Select(i => i.socket).FirstOrDefault().Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deserializedElection)));

                                }
                                else
                                {
                                    //do takiego u którego jeszcze nie bylo
                                    sock.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deserializedElection)));
                                }
                            }
                        }
                        else
                        {
                            ElectionBreak eBreak = new ElectionBreak("election-break", elNo, serverIP);
                            handler.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(eBreak)));
                        }

                        //sprawdz czy jestem na liscie
                        //wyslij election-ack lub break


                        resultNode = null;
                    }
                    break;
                case "election-ack":
                    {
                        //zdjąć timeout
                        resultNode = null;
                    }
                    break;
                case "election-break":
                    {
                        ElectionBreak deserializedElection = JsonConvert.DeserializeObject<ElectionBreak>(resultNode);
                        serverIP = deserializedElection.ipSerwer;
                        elNo = deserializedElection.elNo;
                        //rejestracja u serwera
                        resultNode = null;
                        //tiemout
                    }
                    break;
                case "election-done":
                    {
                        Election deserializedElection = JsonConvert.DeserializeObject<Election>(resultNode);
                        Election electionDone = new Election("election-done", deserializedElection.members);

                        foreach (var item in electionDone.members)
                        {
                            if (item.ip.Equals(myIpAddress)) { item.received = true; item.elNo = elNo; }
                        }
                        var sendTo = electionDone.members.Where(i => !i.received).Select(i => i.ip).FirstOrDefault();
                        if (sendTo != null)
                        {
                            nodesConnected.Where(i => i.ip.Equals(sendTo)).Select(i => i.socket).FirstOrDefault().Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(electionDone)));
                        }
                        var tmp = electionDone.members.Max(i => Convert.ToInt32(i.ip.Split('.')[3]));
                        serverIP = nodesConnected.Where(i => Convert.ToInt32(i.ip.Split('.')[3]) == tmp).Select(i => i.ip).FirstOrDefault();
                        //zdjąć timeout

                        if (myIpAddress == serverIP)
                        {
                            //send ping
                            Ping ping = new Ping("ping", elNo);
                            foreach (var item in nodesConnected.Select(i => i.socket))
                            {
                                item.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(ping)));
                            }
                        }

                        resultNode = null;
                    }
                    break;
                case "ping":
                    {
                        ClientConnect clientsConnect = new ClientConnect("node-update", clientList);
                        clientsConnect.elNo = elNo;
                        handler.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(clientsConnect)));
                        //zdjąć timeout

                        resultNode = null;
                    }
                    break;
                case "node-update":
                    {
                        //wykona sie tylko dla serwera
                        ClientConnect clientsConnect = JsonConvert.DeserializeObject<ClientConnect>(resultNode);

                        foreach (var item in clientsConnect.list)
                        {
                            item.node = handler.RemoteEndPoint.ToString().Split(':')[0].ToString();
                        }
                        serverClients.Add(new ServerWorkerer(handler.RemoteEndPoint.ToString().Split(':')[0].ToString(), handler, clientsConnect.list));

                        handler.Send(Encoding.ASCII.GetBytes(Helpers.typeJson("type", "node-update-ack").ToString()));
                        //zdjąć timeout

                        resultNode = null;
                    }
                    break;
                case "node-update-ack":
                    {
                        //zdjąć timeout
                        resultNode = null;
                    }
                    break;
                case "node-bye":
                    {
                        //pod przyciskiem !!
                        //zdjąć timeout
                        resultNode = null;
                    }
                    break;
                case "get-clients":
                    {
                        //jestem serwerem 
                        //pod przyciskiem updateClients strzal do serwera !!
                        var allClients = new List<Client>();
                        foreach (var item in serverClients.Select(i => i.listClients))
                        {
                            allClients.Concat(item);

                        }
                        allClients.Distinct();

                        var clientConnect = new ClientConnect("clients-list", allClients);
                        handler.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(clientConnect)));
                        //zdjąć timeout
                        resultNode = null;
                    }
                    break;
                case "clients-list":
                    {
                        ClientConnect deserializedClient = JsonConvert.DeserializeObject<ClientConnect>(resultNode);
                        clientList = deserializedClient.list;
                        //pod przyciskiem
                        //zdjąć timeout
                        resultNode = null;
                    }
                    break;
                case "message":
                    {
                        Message deserializedMessage = JsonConvert.DeserializeObject<Message>(resultNode);
                        var client = clientsConnected.Where(i => i.id == deserializedMessage.clientTo.id).Select(i => i.socket).FirstOrDefault();
                        string msgAck = JsonConvert.SerializeObject(new Message("message-ack", deserializedMessage.clientFrom, deserializedMessage.clientTo, null, Helpers.GetTimestamp(DateTime.Now)));
                        string msgFailure = JsonConvert.SerializeObject(new Message("message-fail", deserializedMessage.clientFrom, deserializedMessage.clientTo, null, Helpers.GetTimestamp(DateTime.Now)));

                        if (client != null)
                        {
                            client.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deserializedMessage)));
                            handler.Send(Encoding.ASCII.GetBytes(msgAck));
                        }
                        else
                        {
                            handler.Send(Encoding.ASCII.GetBytes(msgFailure));
                        }
                        //pod przyciskiem
                        //zdjąć timeout
                        resultNode = null;
                    }
                    break;
            }
        }

        private void Logic(Socket handler, string content)
        {
            ClientConnect clientConnect = null;
            string sendText = null;
            byte[] msg = null;
            int bytesSend = 0;
            JObject json = null;
            try
            {
                result += content;
                json = JObject.Parse(result);
            }
            catch (Exception)
            {
                return;
            }

            switch ((string)json["type"])
            {

                case "connect":
                    {
                        ClientConnect deserializedClient = JsonConvert.DeserializeObject<ClientConnect>(result);
                        foreach (var item in deserializedClient.list)
                        {
                            clientList.Add(item);
                            clientsConnected.Add(new ClientWorker(item.id, handler));
                        }

                        clientConnect = new ClientConnect("clients-list", clientList);

                        sendText = JsonConvert.SerializeObject(clientConnect);
                        msg = Encoding.ASCII.GetBytes(sendText);
                        bytesSend = handler.Send(msg);
                        result = null;
                    }
                    break;
                case "get-clients":
                    {
                        clientConnect = new ClientConnect("clients-list", clientList);
                        sendText = JsonConvert.SerializeObject(clientConnect);
                        msg = Encoding.ASCII.GetBytes(sendText);
                        bytesSend = handler.Send(msg);
                        result = null;
                    }
                    break;
                case "message":
                    {
                        Message deserializedMessage = JsonConvert.DeserializeObject<Message>(result);
                        //deserializedMessage.
                        string msgAck = JsonConvert.SerializeObject(new Message("message-ack", deserializedMessage.clientFrom, deserializedMessage.clientTo, null, Helpers.GetTimestamp(DateTime.Now)));
                        string msgFailure = JsonConvert.SerializeObject(new Message("message-fail", deserializedMessage.clientFrom, deserializedMessage.clientTo, null, Helpers.GetTimestamp(DateTime.Now)));
                        var client = clientsConnected.Where(i => i.id == deserializedMessage.clientTo.id).Select(i => i.socket).FirstOrDefault();
                        if (client != null)
                        {
                            client.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deserializedMessage)));
                            handler.Send(Encoding.ASCII.GetBytes(msgAck));
                        }
                        else
                        {
                            var node = clientList.Where(i => i.id == deserializedMessage.clientTo.id).Select(i => i.node).FirstOrDefault();
                            Socket clientAll = null;
                            if (node != null)
                            {
                                clientAll = nodesConnected.Where(i => i.ip == node).Select(i => i.socket).FirstOrDefault();
                                deserializedMessage.elNo = elNo;
                                clientAll.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deserializedMessage)));
                            }

                            //handler.Send(Encoding.ASCII.GetBytes(msgFailure));
                        }
                        result = null;

                    }
                    break;
                case "message-ack":
                    {
                        Message deserializedMessageACK = JsonConvert.DeserializeObject<Message>(result);
                        var client = clientsConnected.Where(i => i.id == deserializedMessageACK.clientTo.id).Select(i => i.socket).FirstOrDefault();
                        client.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deserializedMessageACK)));
                        result = null;
                    }
                    break;
                case "message-fail":
                    {
                        Message deserializedMessageFailure = JsonConvert.DeserializeObject<Message>(result);
                        var client = clientsConnected.Where(i => i.id == deserializedMessageFailure.clientTo.id).Select(i => i.socket).FirstOrDefault();
                        client.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deserializedMessageFailure)));
                        result = null;
                    }
                    break;
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Convert byte array to string 
                string str = null;/// = tbMsgToSend.Text;

                // Prepare the reply message 
                byte[] byteData =
                    Encoding.ASCII.GetBytes(str);

                // Sends data asynchronously to a connected Socket 
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), handler);

                Send_Button.IsEnabled = false;
                Close_Button.IsEnabled = true;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public void SendCallback(IAsyncResult ar)
        {
            try
            {
                // A Socket which has sent the data to remote host 
                Socket handler = (Socket)ar.AsyncState;

                // The number of bytes sent to the Socket 
                int bytesSend = handler.EndSend(ar);
                Console.WriteLine(
                    "Sent {0} bytes to Client", bytesSend);
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //if (sListener.Any(i => i.Connected))
                //{
                //    sListener.ForEach(i => i.Shutdown(SocketShutdown.Receive));
                //    sListener.ForEach(i => i.Close());
                //}
                Close_Button.IsEnabled = false;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
    }
}
