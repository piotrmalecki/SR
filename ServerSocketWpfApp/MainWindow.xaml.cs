

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
        public AsyncCallback pfnWorkerCallBack;
        public AsyncCallback pfnWorkerNodeCallBack;
        public List<string> allNodesIps = new List<string>();
        public List<IPEndPoint> ipPointList = new List<IPEndPoint>();
        string myIpAddress = null;
        public int elNo = 0;
        public int port = 4511;
        public int portNode = 4512;
        private Socket m_mainSocket;
        private Socket m_nodeSocket;
        private Socket[] m_nodeWorkerSocket = new Socket[10];
        private Socket[] m_workerSocket = new Socket[10];
        private int m_clientCount = 0;
        private int m_nodeCount = 0;
        public Socket handler;
        public List<Client> clientList = new List<Client>();
        public List<int> portstClient = new List<int>();
        public String result = null;
        public String resultNode = null;

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
                m_nodeSocket = new Socket(AddressFamily.InterNetwork,
                       SocketType.Stream,
                       ProtocolType.Tcp);
                m_mainSocket = new Socket(
                       ipAddr.AddressFamily,
                       SocketType.Stream,
                       ProtocolType.Tcp
                       );
                var portip = new IPEndPoint(ipAddr, port);

                /*TO BE UNCOMMENTED WHEN OTHER NODES ARE
                 * foreach (DictionaryEntry entry in resourceSet)
                {
                 * //nie łączyć się ze sobą ani nie dodawc sienie do ilPoinList
                    var nodeEndPoint1 = new IPEndPoint(Dns.Resolve(entry.Value.ToString()).AddressList[0], portNode);
                    ipPointList.Add(nodeEndPoint1);
                    m_nodeSocket.Bind(nodeEndPoint1);
                    allNodesIps.Add(entry.Value.ToString());
                }

                m_nodeSocket.BeginAccept(new AsyncCallback(OnNodeConnect), null);*/


                m_mainSocket.Bind(portip);
                m_mainSocket.Listen(15);
                m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
                // Associates a Socket with a local endpoint 

                tbStatus.Text = "Server started.";

                Start_Button.IsEnabled = false;

            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var item in ipPointList)
                {
                    m_nodeSocket.Connect(item);
                }

            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }

        private void OnNodeConnect(IAsyncResult asyn)
        {
            try
            {   // ktoś się dołączył, trzeba sprawdzić czy jestem serwerem
                server = false;

                m_nodeWorkerSocket[m_nodeCount] = m_nodeSocket.EndAccept(asyn);
                nodesConnected.Add(new NodeWorker(m_workerSocket[m_clientCount].RemoteEndPoint.ToString().Split(':')[0], m_nodeWorkerSocket[m_nodeCount]));
                WaitForNodeData(m_nodeWorkerSocket[m_nodeCount]);
                ++m_nodeCount;

    
                m_nodeSocket.BeginAccept(new AsyncCallback(OnNodeConnect), null);



                if (Convert.ToInt32(myIpAddress.Split('.')[3]) <  Convert.ToInt32(nodesConnected.Min(i=>i.ip.Split('.')[3])) )//ForEach(i => Convert.ToInt32(i.ip.Split(new char[] { ' ' })[3])).Min() > 5)
                           {
                               Election election = new Election("election", new List<Member> { new Member(myIpAddress, elNo) });
                               m_nodeWorkerSocket[0].Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(election)));
                            //connect = JsonConvert.SerializeObject(election);
                           }
	                
                
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
                                   pfnWorkerCallBack,
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
            string sendText = null;
            byte[] msg = null;
            int bytesSend = 0;
            JObject json = null;
            try
            {
                resultNode += content;
                json = JObject.Parse(result);
            }
            catch (Exception)
            {
                return;
            }
            switch ((string)json["type"])
            {

                case "election":
                    {
                        Election deserializedElection = JsonConvert.DeserializeObject<Election>(resultNode);

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
                                     if (item.ip.Equals(myIpAddress)) { item.received = true; }
                                 }
                                
                            }
                            else
                            {
                                deserializedElection.members.Add(new Member(myIpAddress, elNo));
                                Socket sock = nodesConnected.Where(i => !deserializedElection.members.Select(q=>q.ip).Contains(i.ip)).Select(i=>i.socket).FirstOrDefault();

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
                        foreach (var item in deserializedElection.members)
                        {
                            
                        }
                        //sprawdz czy jestem na liscie
                        //wyslij election-ack lub break
                        

                        resultNode = null;
                    }
                    break;
                case "election-ack":
                    {
                        //zdjąć timeout
                    }
                    break;
                case "election-break":
                    {
                        //zdjąć timeout
                    }
                    break;
                case "election-done":
                    {
                        //zdjąć timeout
                    }
                    break;
                case "node-update":
                    {
                        //zdjąć timeout
                    }
                    break;
                case "node-bye":
                    {
                        //zdjąć timeout
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
                            handler.Send(Encoding.ASCII.GetBytes(msgFailure));
                        }
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
