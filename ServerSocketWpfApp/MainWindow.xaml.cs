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

namespace ServerSocketWpfApp
{
    public partial class MainWindow : Window
    {

        SocketPermission permission;
        public List<Socket> sListener = new List<Socket>();
        public List<IPEndPoint> ipEndPoint = new List<IPEndPoint>();
        Socket handler;
        public List<Client> clientList = new List<Client>();
        public List<int> portstClient = new List<int>();

        private TextBox tbAux = new TextBox();

        public MainWindow()
        {
            InitializeComponent();
            tbAux.SelectionChanged += tbAux_SelectionChanged;

            Start_Button.IsEnabled = true;
            StartListen_Button.IsEnabled = false;
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
                // Creates one SocketPermission object for access restrictions
                permission = new SocketPermission(
                NetworkAccess.Accept,     // Allowed to accept connections 
                TransportType.Tcp,        // Defines transport types 
                "",                       // The IP addresses of local host 
                SocketPermission.AllPorts // Specifies all ports 
                );

                // Ensures the code to have permission to access a Socket 
                permission.Demand();

                // Resolves a host name to an IPHostEntry instance 
                IPHostEntry ipHost = Dns.GetHostEntry("");

                // Gets first IP address associated with a localhost 
                IPAddress ipAddr = ipHost.AddressList[0];

                // Creates a network endpoint 
                foreach (var item in portstClient)
                {
                    var listener = new Socket(
                        ipAddr.AddressFamily,
                        SocketType.Stream,
                        ProtocolType.Tcp
                        );
                    var portip = new IPEndPoint(ipAddr, item);
                    
                    ipEndPoint.Add(portip);
                    sListener.Add(listener);
                    listener.Bind(portip);
                }
                // Associates a Socket with a local endpoint 
             
                

                tbStatus.Text = "Server started.";

                Start_Button.IsEnabled = false;
                StartListen_Button.IsEnabled = true;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        private void Listen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Places a Socket in a listening state and specifies the maximum 
                // Length of the pending connections queue 
                
                sListener.ForEach(i => i.Listen(10));

                // Begins an asynchronous operation to accept an attempt 
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                
                sListener.ForEach(i=>i.BeginAccept(aCallback, i));

                StartListen_Button.IsEnabled = false;
                Send_Button.IsEnabled = true;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = null;

            // A new Socket to handle remote host communication 
            Socket handler = null;
            try
            {
                // Receiving byte array 
                byte[] buffer = new byte[1024];
                // Get Listening Socket object 
                listener = (Socket)ar.AsyncState;
                // Create a new socket 
                handler = listener.EndAccept(ar);

                // Using the Nagle algorithm 
                handler.NoDelay = false;

                // Creates one object array for passing data 
                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = handler;

                // Begins to asynchronously receive data 
                handler.BeginReceive(
                    buffer,        // An array of type Byt for received data 
                    0,             // The zero-based position in the buffer  
                    buffer.Length, // The number of bytes to receive 
                    SocketFlags.None,// Specifies send and receive behaviors 
                    new AsyncCallback(ReceiveCallback),//An AsyncCallback delegate 
                    obj            // Specifies infomation for receive operation 
                    );

                // Begins an asynchronous operation to accept an attempt 
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                listener.BeginAccept(aCallback, listener);
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public void ReceiveCallback(IAsyncResult ar)
        {
            JObject json = null;
            try
            {
                // Fetch a user-defined object that contains information 
                object[] obj = new object[2];
                obj = (object[])ar.AsyncState;

                // Received byte array 
                byte[] buffer = (byte[])obj[0];

                // A Socket to handle remote host communication. 
                handler = (Socket)obj[1];

                // Received message 
                string content = string.Empty;


                // The number of bytes received. 
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    content += Encoding.Unicode.GetString(buffer, 0,
                        bytesRead);

                    // If message contains "<Client Quit>", finish receiving
                    try
                    {
                         json = JObject.Parse(content);

                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
                        {
                            tbAux.Text = "Read " + json["type"];
                        }
                       );
                    }
                    catch
                    {
                        // Continues to asynchronously receive data
                        byte[] buffernew = new byte[1024];
                        obj[0] = buffernew;
                        obj[1] = handler;
                        handler.BeginReceive(buffernew, 0, buffernew.Length,
                            SocketFlags.None,
                            new AsyncCallback(ReceiveCallback), obj);
                    }

                    Logic(json, content);
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
                    {
                        tbAux.Text = content;
                    }
                    );
                }
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        private void Logic(JObject json, string content)
        {
            switch ((string)json["type"])
            {
                case "connect":
                    ClientConnect deserializedClient = JsonConvert.DeserializeObject<ClientConnect>(content);
                    foreach (var item in deserializedClient.list)
                    {
                        clientList.Add(item);
                    }
                    
                    ClientConnect clientConnect = new ClientConnect("clients-list", clientList);

                    string result = JsonConvert.SerializeObject(clientConnect);
                    byte[] msg = Encoding.Unicode.GetBytes(result);
                    int bytesSend = handler.Send(msg);
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
                    Encoding.Unicode.GetBytes(str);

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
                if (sListener.Any(i=>i.Connected))
                {
                    sListener.ForEach(i=>i.Shutdown(SocketShutdown.Receive));
                    sListener.ForEach(i=>i.Close());
                }

                Close_Button.IsEnabled = false;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
    }
}
