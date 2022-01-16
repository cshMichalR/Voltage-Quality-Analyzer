using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms; 
using System.Xml;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
namespace Analizator_Sieci
{
     public  class Klient_TCP_IP
    {

        ManualResetEvent connectDone = new ManualResetEvent(false);
        ManualResetEvent sendDone = new ManualResetEvent(false);
         ManualResetEvent receiveDone = new ManualResetEvent(false);
        public  AutoResetEvent functionCallDone = new AutoResetEvent(true);
        StringBuilder Buffor_Data = new StringBuilder();
        string Buffor_Data_String = String.Empty;

        static Socket client;
        string response;



        public class StateObject
        {
            // Client socket.  
            public Socket workSocket = null;
            // Size of receive buffer.  
            public const int BufferSize = 50000;
            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];
            //public BlockingCollection<byte> buffer = new BlockingCollection<byte>();
            // Received data string.  
          
            public StringBuilder sb = new StringBuilder();
           public string local_answ = String.Empty;
            
        
            public BlockingCollection<string>emptyColection = new BlockingCollection<string>();

        }
        
   
        public void StartClient(int port, string ip)
        {
      
           

            //Inicjializacja polaczenia  

            IPAddress ipAddress = IPAddress.Parse(ip);

            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // Tworzenie gniazda 
            client = new Socket(ipAddress.AddressFamily,
                 SocketType.Stream, ProtocolType.Tcp);

            // Polaczenie to zdalnego punktu
            client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), client);

            connectDone.WaitOne();

            // Wysyłanie wiadomości testowej  

            Send(client, "Hello");

            sendDone.WaitOne();

            // Nasluchiwanie odpowiedzi od serwera.  

            if (client != null)
            {
                Receive(client);
            }
            // receiveDone.WaitOne();



        }

        public void ConnectCallback(IAsyncResult ar)
        {

            Socket client = (Socket)ar.AsyncState;
            // Konczenie połączenia 
            try
            {
                // Sygnał ,że połączenie zostało nawiązane  
                client.EndConnect(ar);
                connectDone.Set();


            }
            catch (Exception ex)
            {
                connectDone.Set();
                receiveDone.Set();
                sendDone.Set();

                DialogResult answer =MessageBox.Show("Urządzenie nie jest połączone z siecią, należy wskazać sieć z która urządzenie powinno nawiązać połączenie. Czy chcessz skonfigurować łączność po przez TCP/IP?",Application.ProductName,MessageBoxButtons.YesNo,MessageBoxIcon.Question);


          
                if (answer == DialogResult.Yes)
                {
                    Form2 Form2 = new Form2();
                    Form2.ShowDialog();
                }
      

            }


        }

        public void Receive(Socket client)
        {

            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state); ;
            }
            catch 
            {
               
            }


        }


        public void ReceiveCallback(IAsyncResult ar)
        {
          
            StateObject state = (StateObject)ar.AsyncState;
       

                // Retrieve the state object and the client socket
                // from the asynchronous state object.  

                 client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                functionCallDone.WaitOne();
                response = Encoding.ASCII.GetString(state.buffer);
                //Calling long time operation function
              
                Update_Interfejsu(response);
              


                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
                }
                else
                {

                    receiveDone.Set();
                }
         


        }








        public void Send(Socket client, String data)
        {
         
          
            if (client.Connected)
            {
                // Convertowanie ramki do bytów 
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Rozpoczecie wysyłania wiadomości do RTI'a
                try
                {
                    client.BeginSend(byteData, 0, byteData.Length, 0,
                        new AsyncCallback(SendCallback), client);
                }
                catch
                {
                   
                 MessageBox.Show("Polączenie zostało zerwane!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);


                }

            }
        }

        public void SendCallback(IAsyncResult ar)
        {
        
       

            Socket client = (Socket)ar.AsyncState;

            // Koniec wysyłania do Rti'a
            int bytesSent = client.EndSend(ar);


            // Sygnał,że wszystkie byte zostaly wysłane 
  
            sendDone.Set();

         
        }

        public  void Update_Interfejsu(string msg)
        {
            response = String.Empty;

         

        }


    }
}
