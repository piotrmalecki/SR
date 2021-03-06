﻿

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
using System.Linq.Dynamic;
using MoreLinq;

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
        List<string> ipList = new List<string> { "192.168.0.17",
                                                  "192.168.0.20","192.168.0.21", "192.168.0.22"};
        private TextBox tbAux = new TextBox();

        public MainWindow()
        {
            InitializeComponent();
            tbAux.SelectionChanged += tbAux_SelectionChanged;

            Start_Button.IsEnabled = true;
            Clear_Button.IsEnabled = true;
            Close_Button.IsEnabled = true;
        }

        private void tbAux_SelectionChanged(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
            {
                tbMsgReceived.Text = tbAux.Text;
                tbMsgReceived.Focus();
                tbMsgReceived.CaretIndex = tbMsgReceived.Text.Length;
                tbMsgReceived.ScrollToEnd();
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

                //m_nodeSocketListener.SocketClosed += socket_SocketClosed;
                //m_nodeSocketListener.EventsEnabled = true;
                //m_nodeSocketListener.Soc += socket_SocketClosed;
               // m_nodeSocketListener.EventsEnabled = true;
                var portip = new IPEndPoint(ipAddr, port);

                //nie łączyć się ze sobą ani nie dodawc sienie do ilPoinList

                var nodeEndPoint1 = new IPEndPoint(IPAddress.Parse(myIpAddress), portNode);

                m_nodeSocketListener.Bind(nodeEndPoint1);
                // allNodesIps.Add(entry.Value.ToString());

                foreach (var item in ipList)
                {
                    if (item.ToString() != "")
                    {
                        var nodeEndPoint2 = new IPEndPoint(IPAddress.Parse(item.ToString()), portNode);
                        ipPointList.Add(nodeEndPoint2);
                    }
                }
                // result  = ipPointList.OrderByDescending(i=> Convert.ToInt32(i.Address.ToString().Split('.')[3]));
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

            try
            {
                // starting element int a =
                startElection();
            }
            catch (Exception se)
            {
                MessageBox.Show(se.Message);
            }
        }

        private void startElection()
        {
            bool isError = false;
            var next = Helpers.GetNextIPAdressIPEndPoint(ipPointList, myIpAddress);
            while (true)
            {
                try
                {   //sparwdzic czy jest polaczenie
                    if (!next.Address.ToString().Equals(myIpAddress.ToString())) m_nodeSocketConnectorSingle.Connect(next);
                    //m_nodeSocketConnectorSingle.BeginConnect(next, new AsyncCallback(ConnectCallback), m_nodeSocketConnectorSingle);
                    else break;
                    if (m_nodeSocketConnectorSingle.Connected)
                    {
                        Election election = new Election("election", new List<Member> { new Member(myIpAddress, elNo) });
                        WaitForNodeData(m_nodeSocketConnectorSingle);
                        m_nodeSocketConnectorSingle.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(election)));
                        //m_nodeSocketConnectorSingle.SocketClosed += socket_SocketClosed;
                       // m_nodeSocketConnectorSingle.EventsEnabled = true;
                        nodesConnected.Add(new NodeWorker(next.Address.ToString(), m_nodeSocketConnectorSingle));
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

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);
            }
            catch (Exception e)
            {

                MessageBox.Show(e.Message);
            }
        }

        private void addLogComment(string Comment)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
            {
                tbMsgReceived.Text += Comment + "\r\n";

            });
        }

        private void OnNodeConnect(IAsyncResult asyn)
        {
            try
            {   // ktoś się dołączył, trzeba sprawdzić czy jestem serwerem
                server = false;
                Socket soc = m_nodeSocketListener.EndAccept(asyn);
                m_nodeWorkerSocket[m_nodeCount] = (Socket)soc;
                //m_nodeWorkerSocket[m_nodeCount].SocketClosed += socket_SocketClosed;
                //m_nodeWorkerSocket[m_nodeCount].EventsEnabled = true;
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
                m_workerSocket[m_clientCount] = (Socket)m_mainSocket.EndAccept(asyn);
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
            { // TUTAJ exception connection SHUTDOWN BY REMOTE HOST
              // jak jestes serwerem to wykryc ze ktos sie odłączył i zuppdatowac client list

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
            catch (ObjectDisposedException e)
            {
                MessageBox.Show(e.Message);
                //System.Diagnostics.Debugger.Log(0, "1", "\nOnDataReceived: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                //tutaj ten exception przy probie wys
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
                        addLogComment("Odebrany election od : " + handler.RemoteEndPoint.ToString());

                        if (elNo <= deserializedElection.members.Max(i => i.elNo))
                        {
                            StringBuilder sb = Helpers.typeJson("type", "election-ack");
                            handler.Send(Encoding.ASCII.GetBytes(sb.ToString()));
                            addLogComment("Wysłany election ACK do : " + handler.RemoteEndPoint.ToString());

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
                                //var sendTo = electionDone.members.Where(i => !i.received).Select(i => i.ip).FirstOrDefault();
                                var sendTo = Helpers.NextSocket(electionDone.members, myIpAddress);
                                if (sendTo != null)
                                {
                                    electionDone.elNo = elNo;
                                    nodesConnected.Where(i => i.ip.Equals(sendTo)).Select(i => i.socket).FirstOrDefault().Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(electionDone)));
                                    addLogComment("Wysłany election done do : " + sendTo.ToString());
                                }
                                var tmp = electionDone.members.Max(i => Convert.ToInt32(i.ip.Split('.')[3]));
                                serverIP = ipPointList.Where(i => Convert.ToInt32(i.Address.ToString().Split('.')[3]) == tmp).Select(i => i.Address.ToString()).FirstOrDefault();
                                // serverIP = nodesConnected.Where(i => Convert.ToInt32(i.ip.Split('.')[3]) == tmp).Select(i => i.ip).FirstOrDefault();
                                if (myIpAddress == serverIP)
                                {
                                    //send ping
                                    Ping ping = new Ping("ping", elNo);
                                    foreach (var item in nodesConnected.Select(i => i.socket))
                                    {
                                        item.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(ping)));
                                        addLogComment("Jestem serwerem, wysyłam pingi do " + item.RemoteEndPoint.ToString());
                                    }
                                    //zmiana z nocy do testow
                                    /*foreach (var item in clientList)
                                    {
                                        item.node = handler.LocalEndPoint.ToString().Split(':')[0].ToString();
                                    }
                                    serverClients.Add(new ServerWorkerer(serverIP, handler, clientList));*/
                                }
                                //nodesConnected.Where(i=>i.)
                            }
                            else
                            {
                                deserializedElection.members.Add(new Member(myIpAddress, elNo));
                                //KOLEJNY Z LISTY !!
                                var next = Helpers.GetNextIPAdressIPEndPoint(ipPointList, myIpAddress);
                                var isError = false;
                                while (true)
                                {
                                    try
                                    {
                                        var socketFromList = nodesConnected.Where(i => i.ip == next.Address.ToString()).Select(i => i.socket).FirstOrDefault();
                                        if (socketFromList == null)
                                        {
                                            if (!next.Address.ToString().Equals(myIpAddress.ToString())) m_nodeSocketConnectorSingle.Connect(next);
                                            else break;

                                            if (m_nodeSocketConnectorSingle.Connected)
                                            {
                                                m_nodeSocketConnectorSingle.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deserializedElection)));
                                                addLogComment("Wysłany election do : " + m_nodeSocketConnectorSingle.RemoteEndPoint.ToString());
                                                //m_nodeSocketConnectorSingle.SocketClosed += socket_SocketClosed;
                                                //m_nodeSocketConnectorSingle.EventsEnabled = true;
                                                nodesConnected.Add(new NodeWorker(next.Address.ToString(), m_nodeSocketConnectorSingle));
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            socketFromList.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deserializedElection)));
                                            addLogComment("Wysłany election do : " + socketFromList.RemoteEndPoint.ToString());
                                            break;
                                        }
                                    }
                                    catch (SocketException)
                                    {
                                        next = Helpers.GetNextIPAdressIPEndPoint(ipPointList, next.Address.ToString());
                                        isError = true;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        next = Helpers.GetNextIPAdressIPEndPoint(ipPointList, next.Address.ToString());
                                        isError = true;
                                    }

                                    if (isError) continue;

                                }
                            }
                        }
                        else
                        {
                            ElectionBreak eBreak = new ElectionBreak("election-break", elNo, serverIP);
                            handler.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(eBreak)));
                            addLogComment("Wysłany election-break do : " + handler.RemoteEndPoint.ToString());

                        }

                        //sprawdz czy jestem na liscie
                        //wyslij election-ack lub break


                        resultNode = null;
                    }
                    break;
                case "election-ack":
                    {
                        //zdjąć timeout
                        addLogComment("Odebrany election-ack od " + handler.RemoteEndPoint.ToString());
                        resultNode = null;
                    }
                    break;
                case "election-break":
                    {
                        addLogComment("Odebrany election-break od " + handler.RemoteEndPoint.ToString());
                        ElectionBreak deserializedElection = JsonConvert.DeserializeObject<ElectionBreak>(resultNode);

                        serverIP = deserializedElection.server;
                        elNo = deserializedElection.elNo;
                        addLogComment("Znam serwer  " + serverIP);
                        //rejestracja u serwera

                        ClientConnect clientsConnect = new ClientConnect("node-update", clientList);
                        var sentTo = nodesConnected.Where(i => i.ip == serverIP).Select(i => i.socket).FirstOrDefault();
                        if (sentTo != null)
                        {
                            sentTo.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(clientsConnect)));

                        }
                        else
                        {
                            try
                            {
                                var newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                                                    ProtocolType.Tcp);
                                IPAddress ipAddr = IPAddress.Parse(serverIP);
                                newSocket.Connect(ipAddr, portNode);

                                if (newSocket.Connected)
                                {
                                    ClientConnect nodesUpdate = new ClientConnect("node-update", clientList);
                                    nodesUpdate.elNo = elNo;
                                    WaitForNodeData(newSocket);
                                    nodesConnected.Add(new NodeWorker(serverIP, (Socket)newSocket));
                                    newSocket.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(nodesUpdate)));
                                    addLogComment("Wysłałem node-update do serwera do " + newSocket.RemoteEndPoint.ToString());
                                }

                            }
                            catch (SocketException ę)
                            {
                                //robie elekcje
                                startElection();
                            }
                        }

                        resultNode = null;
                        //tiemout
                    }
                    break;
                case "election-done":
                    {
                        addLogComment("Odebrany election-done od " + handler.RemoteEndPoint.ToString());
                        Election deserializedElection = JsonConvert.DeserializeObject<Election>(resultNode);
                        Election electionDone = new Election("election-done", deserializedElection.members);

                        //zmiana Łuczka
                        elNo = deserializedElection.elNo;
                        foreach (var item in electionDone.members)
                        {
                            if (item.ip.Equals(myIpAddress)) { item.received = true; item.elNo = elNo; }
                        }
                        var sendTo = Helpers.NextSocket(electionDone.members, myIpAddress);
                        //var sendTo = electionDone.members.Where(i => !i.received).Select(i => i.ip).FirstOrDefault();
                        if (sendTo != null)
                        {
                            electionDone.elNo = elNo;
                            nodesConnected.Where(i => i.ip.Equals(sendTo)).Select(i => i.socket).FirstOrDefault().Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(electionDone)));
                            addLogComment("Wysłany election-done do " + handler.RemoteEndPoint.ToString());
                        }
                        var tmp = electionDone.members.Max(i => Convert.ToInt32(i.ip.Split('.')[3]));
                        //serverIP = nodesConnected.Where(i => Convert.ToInt32(i.ip.Split('.')[3]) == tmp).Select(i => i.ip).FirstOrDefault();
                        serverIP = ipPointList.Where(i => Convert.ToInt32(i.Address.ToString().Split('.')[3]) == tmp).Select(i => i.Address.ToString()).FirstOrDefault();
                        //zdjąć timeout

                        if (myIpAddress == serverIP)
                        {
                            //send ping
                            Ping ping = new Ping("ping", elNo);
                            foreach (var item in nodesConnected.Select(i => i.socket))
                            {
                                item.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(ping)));
                                addLogComment("Jestem serwerem, wysyłam pingi do " + item.RemoteEndPoint.ToString());
                            }
                            //jak powiedziec 
                            // zmiana z nocy do testów
                            /*
                            foreach (var item in clientList)
                            {
                                item.node = serverIP;
                            }
                            serverClients.Add(new ServerWorkerer(serverIP, handler, clientList));*/
                        }

                        resultNode = null;
                    }
                    break;
                case "ping":
                    {
                        addLogComment("Dostałem ping od " + handler.RemoteEndPoint.ToString());
                        ClientConnect clientsConnect = new ClientConnect("node-update", clientList);
                        clientsConnect.elNo = elNo;
                        handler.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(clientsConnect)));
                        addLogComment("Wysłałem clientsConnected do serwera do " + handler.RemoteEndPoint.ToString());
                        //zdjąć timeout

                        resultNode = null;
                    }
                    break;
                case "node-update":
                    {
                        //wykona sie tylko dla serwera
                        addLogComment("Dostałem node-update od " + handler.RemoteEndPoint.ToString());
                        ClientConnect clientsConnect = JsonConvert.DeserializeObject<ClientConnect>(resultNode);

                        if (clientsConnect.clients != null)
                        {
                            foreach (var item in clientsConnect.clients)
                            {
                                item.node = handler.RemoteEndPoint.ToString().Split(':')[0].ToString();
                            }
                            serverClients.Add(new ServerWorkerer(handler.RemoteEndPoint.ToString().Split(':')[0].ToString(), handler, clientsConnect.clients));
                        }
                        if (clientList != null)
                        {

                            serverClients.Add(new ServerWorkerer(handler.RemoteEndPoint.ToString().Split(':')[0].ToString(), handler, clientList));
                        }
                        handler.Send(Encoding.ASCII.GetBytes(Helpers.typeJson("type", "node-update-ack").ToString()));
                        addLogComment("Wysłałem node-update-ack do " + handler.RemoteEndPoint.ToString());
                        //zdjąć timeout

                        resultNode = null;
                    }
                    break;
                case "node-update-ack":
                    {
                        //zdjąć timeout
                        addLogComment("Dostałem node-update-ack od " + handler.RemoteEndPoint.ToString());
                        resultNode = null;
                    }
                    break;
                case "node-bye":
                    {
                        //pod przyciskiem !!!!!!!!!!!!!!!
                        //zdjąć timeout
                        addLogComment("Dostałem node-bye od " + handler.RemoteEndPoint.ToString());
                        resultNode = null;
                    }
                    break;
                case "get-clients":
                    {
                        //jestem serwerem 
                        addLogComment("Dostałem get-clients od " + handler.RemoteEndPoint.ToString());
                        var allClients = new List<Client>();
                        foreach (var item in serverClients)
                        {
                            foreach (var item2 in item.listClients)
                            {
                                allClients.Add(item2);
                            }
                        }
                        foreach (var item in clientList)
                        {
                            allClients.Add(item);
                        }
                    
                        //var result = MoreLinq.MoreEnumerable.DistinctBy(allClients, x=>x.id, null);
                        var clientConnect = new ClientConnect("clients-list", allClients.DistinctBy(x=>x.name).ToList());
                        clientConnect.elNo = elNo;
                        
                        handler.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(clientConnect)));
                        addLogComment("Wyslalem clients-list do " + handler.RemoteEndPoint.ToString());
                        //zdjąć timeout
                        resultNode = null;
                    }
                    break;
                case "clients-list":
                    {
                        addLogComment("Otrzymałem clients-list od " + handler.RemoteEndPoint.ToString());
                        ClientConnect deserializedClient = JsonConvert.DeserializeObject<ClientConnect>(resultNode);
                        clientList = deserializedClient.clients;
                        //pod przyciskiem
                        //zdjąć timeout
                        resultNode = null;
                    }
                    break;
                case "message":
                    {
                        addLogComment("Otrzymałem message od " + handler.RemoteEndPoint.ToString());
                        Message deserializedMessage = JsonConvert.DeserializeObject<Message>(resultNode);
                        deserializedMessage.elNo = elNo;
                        var client = clientsConnected.Where(i => i.id == deserializedMessage.clientTo.id).Select(i => i.socket).FirstOrDefault();
                        var clientID = clientsConnected.Where(i => i.id == deserializedMessage.clientTo.id).Select(i => i.id).FirstOrDefault();
                        var msgACK = new Message("message-ack", deserializedMessage.clientTo, deserializedMessage.clientFrom, null, Helpers.GetCurrentUnixTimestampMillis().ToString());
                        msgACK.elNo = elNo;
                        string msgAck = JsonConvert.SerializeObject(msgACK);
                        var msgFail = new Message("message-fail", deserializedMessage.clientTo, deserializedMessage.clientFrom, null, Helpers.GetCurrentUnixTimestampMillis().ToString());
                        msgFail.elNo = elNo;
                        string msgFailure = JsonConvert.SerializeObject(msgFail);

                        if (client != null)
                        {
                            if (client.Connected)
                            {
                                client.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deserializedMessage)));
                                addLogComment("Wyslalem message do " + client.RemoteEndPoint.ToString());
                            }
                            else
                            {
                                MessageBox.Show("Klient odłączony !");
                                clientList.RemoveAll(i => i.id == clientID);
                            }
                            if (handler.Connected)
                            {
                                handler.Send(Encoding.ASCII.GetBytes(msgAck));
                                addLogComment("Wyslalem message-ack do " + handler.RemoteEndPoint.ToString());
                            }
                        }
                        else
                        {
                            handler.Send(Encoding.ASCII.GetBytes(msgFailure));
                            addLogComment("Wyslalem message-failure do " + handler.RemoteEndPoint.ToString());
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
                        addLogComment("Klient : Odebrany connect od " + handler.RemoteEndPoint.ToString());
                        ClientConnect deserializedClient = JsonConvert.DeserializeObject<ClientConnect>(result);
                        //
                        if (deserializedClient.clients != null)
                        {
                            foreach (var item in deserializedClient.clients)
                            {
                                clientList.Add(item);
                                clientsConnected.Add(new ClientWorker(item.id, handler));
                            }
                        }
                        clientConnect = new ClientConnect("clients-list", clientList);

                        sendText = JsonConvert.SerializeObject(clientConnect);
                        msg = Encoding.ASCII.GetBytes(sendText);
                        bytesSend = handler.Send(msg);
                        addLogComment("Klient : Wysłany client-list do " + handler.RemoteEndPoint.ToString());

                        if (serverIP != null && serverIP !=myIpAddress)
                        {
                            ClientConnect clientsConnect = new ClientConnect("node-update", clientList.Where(x=>x.node==myIpAddress).ToList());
                            clientsConnect.elNo = elNo;
                            nodesConnected.Where(i => i.ip == serverIP).Select(x=>x.socket).FirstOrDefault().Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(clientsConnect)));
                            addLogComment("Wysłałem clientsConnected do serwera do " + handler.RemoteEndPoint.ToString());
                        }
                        result = null;
                    }
                    break;
                case "get-clients":
                    {
                        addLogComment("Klient : Odebrany get-clients od " + handler.RemoteEndPoint.ToString());
                        var allClients = new List<Client>();
                        if (myIpAddress == serverIP)
                        {
                            foreach (var item in serverClients)
                            {
                                foreach (var item2 in item.listClients)
                                {
                                    allClients.Add(item2);
                                }
                            }
                            allClients.Distinct();
                            clientList = allClients;
                        }
                        else
                        {
                            allClients = clientList;
                        }
                        clientConnect = new ClientConnect("clients-list", allClients.DistinctBy(x=>x.name).ToList());
                        sendText = JsonConvert.SerializeObject(clientConnect);
                        msg = Encoding.ASCII.GetBytes(sendText);
                        bytesSend = handler.Send(msg);
                        addLogComment("Klient : Wysłany client-list do " + handler.RemoteEndPoint.ToString());
                        result = null;
                    }
                    break;
                case "message":
                    {
                        addLogComment("Klient : odebrany message od " + handler.RemoteEndPoint.ToString());
                        Message deserializedMessage = JsonConvert.DeserializeObject<Message>(result);
                        //deserializedMessage.
                        var messageACK = new Message("message-ack", deserializedMessage.clientTo, deserializedMessage.clientFrom, null, Helpers.GetCurrentUnixTimestampMillis().ToString());
                        messageACK.elNo = elNo;
                        string msgAck = JsonConvert.SerializeObject(messageACK);
                        var messageFailed = new Message("message-fail", deserializedMessage.clientTo, deserializedMessage.clientFrom, null, Helpers.GetCurrentUnixTimestampMillis().ToString());
                        messageFailed.elNo = elNo;
                        string msgFailure = JsonConvert.SerializeObject(messageFailed);
                        var client = clientsConnected.Where(i => i.id == deserializedMessage.clientTo.id).Select(i => i.socket).FirstOrDefault();
                        var clientID = clientsConnected.Where(i => i.id == deserializedMessage.clientTo.id).Select(i => i.id).FirstOrDefault();
                        if (client != null)
                        {
                            if (client.Connected)
                            {

                                client.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deserializedMessage)));
                                addLogComment("Klient : wyslany message do " + client.RemoteEndPoint.ToString());
                            }

                            else
                            {
                                MessageBox.Show("Klient odłączony !");
                                clientList.RemoveAll(i => i.id == clientID);
                            }
                            if (client.Connected)
                            {
                                handler.Send(Encoding.ASCII.GetBytes(msgAck));
                                addLogComment("Klient : wyslany msgAck do " + handler.RemoteEndPoint.ToString());
                            }

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
                                addLogComment("Klient : wyslany message do " + clientAll.RemoteEndPoint.ToString());
                            }

                            //handler.Send(Encoding.ASCII.GetBytes(msgFailure));
                        }
                        result = null;

                    }
                    break;
                case "message-ack":
                    {
                        addLogComment("Klient : dostalem message-ack od " + handler.RemoteEndPoint.ToString());
                        Message deserializedMessageACK = JsonConvert.DeserializeObject<Message>(result);
                        var client = clientsConnected.Where(i => i.id == deserializedMessageACK.clientTo.id).Select(i => i.socket).FirstOrDefault();
                        client.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deserializedMessageACK)));
                        addLogComment("Klient : wyslalem deserializedMessageACK do " + client.RemoteEndPoint.ToString());
                        result = null;
                    }
                    break;
                case "message-fail":
                    {
                        addLogComment("Klient : dostalem message-fail od " + handler.RemoteEndPoint.ToString());
                        Message deserializedMessageFailure = JsonConvert.DeserializeObject<Message>(result);
                        var client = clientsConnected.Where(i => i.id == deserializedMessageFailure.clientTo.id).Select(i => i.socket).FirstOrDefault();
                        client.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deserializedMessageFailure)));
                        addLogComment("Klient : wyslalem deserializedMessageFailure do " + client.RemoteEndPoint.ToString());
                        result = null;
                    }
                    break;
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
                {
                    tbMsgReceived.Clear();

                });

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

                foreach (var item in nodesConnected.Select(i => i.socket))
                {
                    item.Close();
                }
                m_nodeSocketListener.Close();
                m_nodeSocketConnectorSingle.Close();
                Close_Button.IsEnabled = false;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            if (serverIP == null)
            {
                addLogComment("Nie zanm serwera");
            }
            else
            {
                nodesConnected.Where(i => i.ip == serverIP).Select(i => i.socket).FirstOrDefault().Send(Encoding.ASCII.GetBytes(Helpers.typeJson("type", "get-clients").ToString()));
            }
        }
        void socket_SocketClosed(Socket socket)
        {

            int a = 0;
            if (myIpAddress == serverIP)
            {
                var clientsNew = clientList.Where(i => i.node != socket.RemoteEndPoint.ToString().Split(':')[0]).ToList();
                clientList = clientsNew;
            }
            // do what you want
        }

    }
}
