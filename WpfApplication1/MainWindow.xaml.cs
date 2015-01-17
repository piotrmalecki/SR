﻿﻿﻿// Houssem Dellai    
// houssem.dellai@ieee.org    
// +216 95 325 964    
// Studying Software Engineering    
// in the National Engineering School of Sfax (ENIS)   

using System;
using System.Text;
using System.Windows;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using System.Linq;
using WpfApplication1.Comminication;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading;
using System.Windows.Threading;

namespace WpfApplication1
{
    // State object for receiving data from remote device.

    public partial class MainWindow : Window
    {

        // Receiving byte array  
        byte[] m_dataBuffer = new byte[1024];
        IAsyncResult m_result;
        public AsyncCallback m_pfnCallBack;
        public Socket m_clientSocket;
        Client sendTo = null;
        public string id = null;
        public int port = 4511;
        public String name = "Ala";
        public string message = null;

        private static String response = String.Empty;
        private string result;
        public MainWindow()
        {
            id = Guid.NewGuid().ToString();
            InitializeComponent();

            Send_Button.IsEnabled = false;
            Disconnect_Button.IsEnabled = false;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                Client client = new Client(id, name);
                ClientConnect clientConnect = new ClientConnect("connect", new List<Client>() { client });
                m_clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Resolves a host name to an IPHostEntry instance            
                IPHostEntry ipHost = Dns.GetHostEntry("");
                IPAddress ipAddr = Dns.Resolve("localhost").AddressList[0];
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);

                m_clientSocket.Connect(ipEndPoint);
                if (m_clientSocket.Connected)
                {
                    //Wait for data asynchronously 
                    WaitForData();
                }

                tbStatus.Text = "Socket connected to " + m_clientSocket.RemoteEndPoint.ToString();

                string connect = JsonConvert.SerializeObject(clientConnect);
                Send(m_clientSocket, connect);
                //Receive(senderSock);

                Connect_Button.IsEnabled = false;
                Send_Button.IsEnabled = true;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }

        }

        public void WaitForData()
        {
            try
            {
                if (m_pfnCallBack == null)
                {
                    m_pfnCallBack = new AsyncCallback(OnDataReceived);
                }
                SocketPacket theSocPkt = new SocketPacket();
                theSocPkt.thisSocket = m_clientSocket;
                // Start listening to the data asynchronously
                m_result = m_clientSocket.BeginReceive(theSocPkt.dataBuffer,
                                                        0, theSocPkt.dataBuffer.Length,
                                                        SocketFlags.None,
                                                        m_pfnCallBack,
                                                        theSocPkt);
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }

        }
        public class SocketPacket
        {
            public System.Net.Sockets.Socket thisSocket;
            public byte[] dataBuffer = new byte[2048];
        }

        public void OnDataReceived(IAsyncResult asyn)
        {
            try
            {
                SocketPacket theSockId = (SocketPacket)asyn.AsyncState;
                int iRx = theSockId.thisSocket.EndReceive(asyn);
                char[] chars = new char[iRx + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(theSockId.dataBuffer, 0, iRx, chars, 0);
                System.String szData = new System.String(chars);
                //richTextRxMessage.Text = richTextRxMessage.Text + szData;
                Logic(szData);
                WaitForData();
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

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private void Send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Sending message 
                //<Client Quit> is the sign for end of data 
                message = tbMsg.Text;
                Message toBeSent = new Message("message", new Client(id, name), sendTo, tbMsg.Text, Helpers.GetTimestamp(DateTime.Now));
                string connect = JsonConvert.SerializeObject(toBeSent);
                Send(m_clientSocket, connect);
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
        public void AddMyMessage()
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
            {
                tbReceivedMsg.Text += "Ja :" + message + "\n";
                message = null;
            });
            
        }

        public void AddNoMessage()
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
            {
                tbReceivedMsg.Text += "Delivering Failure \n";
            });
            
        }
        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disables sends and receives on a Socket. 
                //senderSock.Shutdown(SocketShutdown.Both);

                //Closes the Socket connection and releases all resources 
                //senderSock.Close();

                Disconnect_Button.IsEnabled = false;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

            if (ClintsListBox.SelectedItem != null)
            {
                String[] selected = ClintsListBox.SelectedItem.ToString().Split(' ');
                sendTo = new Client(selected[2], selected[0]);
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();
                writer.WritePropertyName("type");
                writer.WriteValue("get-clients");
                writer.WriteEndObject();
            }
            Send(m_clientSocket, sb.ToString());
            //sendDone.WaitOne();
        }
        private void Logic(string content)
        {
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

                case "clients-list":
                    {
                        ClientConnect deserializedClient = JsonConvert.DeserializeObject<ClientConnect>(result);

                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
                        {
                            ClintsListBox.Items.Clear();
                            foreach (var item in deserializedClient.list)
                            {
                                if (item.id != id)
                                ClintsListBox.Items.Add(item.name + "  " + item.id);
                            }
                        });


                        result = null;
                    }
                    break;
                case "message" :
                    {
                        Message deserializedMessage = JsonConvert.DeserializeObject<Message>(result);
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
                        {
                            tbReceivedMsg.Text += deserializedMessage.clientFrom.name+" : " +deserializedMessage.message +"\n" ;
                        });
                        result = null;
                    }
                    break;
                case "message-ack":
                    {
                        AddMyMessage();
                        result = null;
                    }
                    break;
                case "message-fail":
                    {
                        AddNoMessage();
                        result = null;
                    }
                    break;
            }
        }
    }
}
