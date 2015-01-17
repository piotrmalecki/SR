﻿﻿﻿// Houssem Dellai    
// houssem.dellai@ieee.org    
// +216 95 325 964    
// Studying Software Engineering    
// in the National Engineering School of Sfax (ENIS)   

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
        
        const int MAX_CLIENTS = 10;
        public List<ClientWorker> clientsConnected = new List<ClientWorker>();
        public AsyncCallback pfnWorkerCallBack;
        public int port = 4511;
        private Socket m_mainSocket;
        private Socket[] m_workerSocket = new Socket[10];
        private int m_clientCount = 0;
        //public List<Socket> sListener = new List<Socket>();
        //public List<IPEndPoint> ipEndPoint = new List<IPEndPoint>();
        Socket handler;
        public List<Client> clientList = new List<Client>();
        public List<int> portstClient = new List<int>();
        public String result = null;

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
                // Resolves a host name to an IPHostEntry instance 
                IPHostEntry ipHost = Dns.GetHostEntry("");

                // Gets first IP address associated with a localhost 
                IPAddress ipAddr = Dns.Resolve("localhost").AddressList[0];

                // Creates a network endpoint 

                m_mainSocket = new Socket(
                       ipAddr.AddressFamily,
                       SocketType.Stream,
                       ProtocolType.Tcp
                       );
                var portip = new IPEndPoint(ipAddr, port);


                // ipEndPoint.Add(portip);
                // sListener.Add(m_mainSocket);
                m_mainSocket.Bind(portip);
                m_mainSocket.Listen(15);
                m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
                // Associates a Socket with a local endpoint 



                tbStatus.Text = "Server started.";

                Start_Button.IsEnabled = false;

            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
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
                WaitForData(m_workerSocket[m_clientCount]);
                // Now increment the client count
                ++m_clientCount;
                // Display this client connection as a status message on the GUI	
                String str = String.Format("Client # {0} connected", m_clientCount);
                //textBoxMsg.Text = str;

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
