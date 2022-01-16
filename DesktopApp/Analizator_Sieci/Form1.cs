using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Analizator_V1_2;
using MathWorks.MATLAB.NET.Arrays;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Net;
using cellexamp;
using zapadTest;
namespace Analizator_Sieci
{ 

    public partial class Form1 : Form
    {

         ManualResetEvent connectDone = new ManualResetEvent(false);
        ManualResetEvent sendDone = new ManualResetEvent(false);
        ManualResetEvent receiveDone = new ManualResetEvent(false);
        public AutoResetEvent functionCallDone = new AutoResetEvent(true);
        StringBuilder ramki = new StringBuilder();
        static Socket client;
        string dataStart;
        bool firstCall = true;
        BlockingCollection<string> data = new BlockingCollection<string>();

          int dlugoscSygnalu = 3878000;

        public class Zapady
        {
            public string DeltaU { get; set; }
            public string CzasTrwania { get; set; }
            public string Godzina { get; set; }

        }
        public enum Funkcja {None,VrmsT,VrmsHist,HarProc,HarV,FreqT,FreqHist,Oscylogram,VrmsDelta,FreqDelta,ThdT,Flicker }
        public List<Zapady> ListZapadow()
        {
            var list = new List<Zapady>();
            list.Add(new Zapady() { DeltaU = "20 V", CzasTrwania = "0.02", Godzina="14:41:21" });
            return list;
        }
        public class StateObject
        {
            // Client socket.  
            public Socket workSocket = null;
            // Size of receive buffer.  
            public const int BufferSize = 32768;
            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];
            // Received data string.  
            public StringBuilder sb = new StringBuilder();
        

        }

        public string odpowiedz = String.Empty;

        public void StartClient(int port, string ip)
        {

            functionCallDone.Set();
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


            Receive(client);

     



        }

        public void Disconnect()
        {
            try
            {
                ramki.Clear();
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                
            }
            catch
            {

            }
                functionCallDone.Set();
        }

        public void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                
                Socket client = (Socket)ar.AsyncState;

               
                client.EndConnect(ar);


                client.RemoteEndPoint.ToString();

              
                connectDone.Set();
                MethodInvoker invoker = new MethodInvoker(delegate
                {
                    status_połaczenia.Text = "Połączony po przez TCP/IP";
                    status_połaczenia.ForeColor = System.Drawing.Color.Green;
         
                });
                this.Invoke(invoker);
                }
            catch (Exception e)
            {
                connectDone.Set();
                MessageBox.Show(e.ToString());
            }

        }

        public void Receive(Socket client)
        {

            try
            {
             
                StateObject state = new StateObject();
                state.workSocket = client;

          
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }


        }


        public void ReceiveCallback(IAsyncResult ar)
        {
            try
            { 
            functionCallDone.WaitOne();

      
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;

            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0)
            {

                ramki.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                if (ramki.Length > dlugoscSygnalu)
                {
                    Matlab_Plot_Function();

                }
                else
                {
                    functionCallDone.Set();
                }

                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                          new AsyncCallback(ReceiveCallback), state);

            }
            else
            {

                receiveDone.Set();
              
                    MethodInvoker invoker = new MethodInvoker(delegate
                    {
                        status_połaczenia.Text = "Brak połączenia";
                        status_połaczenia.ForeColor = System.Drawing.Color.Red;
                     
                        button1.Text = "Połącz";
                        if (backgroundWorker2.IsBusy == true)
                        {
                            backgroundWorker2.CancelAsync();

                        }
                        dataStart = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss.ffffff", System.Globalization.CultureInfo.InvariantCulture);

                        if (backgroundWorker2.IsBusy)
                        {
                            backgroundWorker2.CancelAsync();
                        }
                    });
                    this.Invoke(invoker);
                  
                }
            }
            catch(Exception ex)
            {
                receiveDone.Set();

                MethodInvoker invoker = new MethodInvoker(delegate
                {
                    status_połaczenia.Text = "Brak połączenia";
                    status_połaczenia.ForeColor = System.Drawing.Color.Red;

                    button1.Text = "Połącz";
                    if (backgroundWorker2.IsBusy == true)
                    {
                        backgroundWorker2.CancelAsync();

                    }
                    dataStart = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss.ffffff", System.Globalization.CultureInfo.InvariantCulture);

                    if (backgroundWorker2.IsBusy)
                    {
                        backgroundWorker2.CancelAsync();
                    }
                });
                this.Invoke(invoker);

            }
        }

        public void Send(Socket client, String data)
        {


            if (client.Connected)
            {
                // Convertowanie ramki do bytów 
                byte[] byteData = Encoding.ASCII.GetBytes(data);

           
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

       
            int bytesSent = client.EndSend(ar);


            // Sygnał,że wszystkie byte zostaly wysłane 

            sendDone.Set();


        }



        ManualResetEvent functionSetDone = new ManualResetEvent(true);
        AutoResetEvent autoResetEvent = new AutoResetEvent(true);
  
        int status;
        DateTime startDate;
       public Funkcja funkcja;
        double timeDiv;
       
        private Button lastButton = null;
        MWArray bufor = "";
        MWArray memoryX = MWNumericArray.FloatingPointAccuracy, memoryY = MWNumericArray.FloatingPointAccuracy, wykres = MWNumericArray.FloatingPointAccuracy;
        string _Odpowiedz_Serwera = String.Empty;

        public class MemoryVariables
        {
            public string FreqMaxTime;
            public List<double> freq = new List<double>();
            public List<double> vrms = new List<double>();
            public List<string> time = new List<string>();
            public List<string> timeFlicker = new List<string>();
            public MWArray Data;
            public List<double> thd = new List<double>();
            public List<double> deltaU = new List<double>();
            public string ostatniPrzebieg = String.Empty;
            public List<MWCellArray> time2 = new List<MWCellArray>();
            public List<MWNumericArray> freq2 = new List<MWNumericArray>();
           public MWNumericArray peaksFlicker = MWNumericArray.Empty;
            public List<double> deltaHz = new List<double>();
            public List<double> pst = new List<double>();
            public MWNumericArray harmVect = MWNumericArray.Empty;

        }




        public Form1()
        {
            Analizator analizator = new Analizator();
            InitializeComponent();
            Panel_TCP.Visible = true;
            analizator.makePlot(0, 0, 0, 0, 0, 0);
            Thread.Sleep(500);
            foundWindow = FindWindow("SunAwtFrame", "Figure 1");
            SetParent(foundWindow, Panel1.Handle);
            Panel1.SizeChanged += new EventHandler(Panel1_Resize);
            Panel1_Resize(new Object(), new EventArgs());
            panel6.Visible = false;
   


        }
        double vMax, vRms, freq, thd,flicker;
        double licznik_display = 0.00;
        double VrmsMax = 0, VrmsMin = 1000, VrmsAvg = 0, VrmsSum = 0;
        double freqMax = 0, freqMin = 1000, freqAvg = 0, FreqSum = 0;
        double vPeakMax = 0, vPeakMin = 1000, vPeakAvg = 0, VPeakSum = 0;
        double thdMax = 0, thdMin = 1000, thdAvg = 0, thdSum = 0;
        double flickerMax = 0, flickerMin = 1000, flickerAvg = 0,flickerSum=0;
        double sample_rate;
        MWArray Aktualne_x = MWNumericArray.Empty, Aktualne_y = MWNumericArray.Empty;

    

        MemoryVariables pamiec = new MemoryVariables();

        private readonly object balanceLock = new object();


        DateTime firstCallTime;
        int counter = 0;

        public delegate void InvokeDelegate();
        public MWArray[] InvokeMethod()
        {
            Analizator analizator = new Analizator();
            MWArray[] aktualne_pomiary;
            pamiec.ostatniPrzebieg = String.Concat(ramki);
  
            if (funkcja == Funkcja.Oscylogram)
            {
                analizator.LiveTimeVoltage(0, ramki.ToString(), timeDiv,0);

            }
            if (funkcja == Funkcja.HarProc)
            {
                analizator.Harmoniczne(0, ramki.ToString(), 1,0,0);
            }
            if (funkcja == Funkcja.HarV)
            {
                analizator.Harmoniczne(0, ramki.ToString(), 0,0,0);
            }


            if (firstCall)
            {
                aktualne_pomiary =analizator.DisplayMeasurments(12,String.Concat(ramki), dataStart,pamiec.peaksFlicker,pamiec.harmVect);
                ramki.Clear();
            }
            else
            {
                aktualne_pomiary =analizator.DisplayMeasurments(12, String.Concat(ramki), pamiec.time[pamiec.time.Count - 1], pamiec.peaksFlicker,pamiec.harmVect);
                ramki.Clear();
            }
            pamiec.peaksFlicker = (MWNumericArray)aktualne_pomiary[9];
            pamiec.harmVect = (MWNumericArray)aktualne_pomiary[11];
            firstCall = false;
            return aktualne_pomiary;
        }

        public async void Matlab_Plot_Function()
        {
            switch(pamiec.time.Count)
            {
                case 10:
                    dlugoscSygnalu = dlugoscSygnalu + 233059;
                    break;
                case 100:
                    dlugoscSygnalu = dlugoscSygnalu + 233059;
                    break;
                case 1000:
                    dlugoscSygnalu = dlugoscSygnalu + 233059;
                    break;
                case 10000:
                    dlugoscSygnalu = dlugoscSygnalu + 233059;
                    break;

                case 100000:
                    dlugoscSygnalu = dlugoscSygnalu + 233059;
                    break;

            }

            Task<MWArray[]> task = new Task<MWArray[]>(InvokeMethod);
            task.Start();
            MWArray[] aktualne_pomiary = await task;
            Analizator analizator = new Analizator();
            Aktualne_x = MWNumericArray.Empty;
                Aktualne_y = MWNumericArray.Empty;


                vMax = Convert.ToDouble(aktualne_pomiary[0].ToString());
                vRms = Convert.ToDouble(aktualne_pomiary[1].ToString());
                freq = Convert.ToDouble(aktualne_pomiary[2].ToString());

            MWArray zapadValue = aktualne_pomiary[7];
            MWArray zapadTime = aktualne_pomiary[6];


     


            if (aktualne_pomiary[10].ToString()!="NaN")
            {
                pamiec.peaksFlicker = MWNumericArray.Empty;
                flicker = Convert.ToDouble(aktualne_pomiary[10].ToString());
                pamiec.timeFlicker.Add(aktualne_pomiary[3].ToString());
                pamiec.pst.Add(Convert.ToDouble(aktualne_pomiary[10].ToString()));
                flickerSum = flickerSum + flicker;

   
            }
    

                thd = Convert.ToDouble(aktualne_pomiary[4].ToString());

                pamiec.deltaU.Add(Convert.ToDouble(aktualne_pomiary[5].ToString()));
            pamiec.deltaHz.Add(Convert.ToDouble(aktualne_pomiary[8].ToString()));

            VrmsSum = VrmsSum + vRms;
                FreqSum = FreqSum + freq;
                VPeakSum = VPeakSum + vMax;
                thdSum = thdSum + thd;

            pamiec.freq2.Add((MWNumericArray)aktualne_pomiary[2]);
                pamiec.freq.Add(freq);
                pamiec.time.Add(aktualne_pomiary[3].ToString());
                pamiec.vrms.Add(vRms);
                pamiec.thd.Add(thd);
            MethodInvoker invoker = new MethodInvoker(delegate
            {
                for (int i = 1; i <= zapadValue.NumberOfElements; i++)
                {
                    if (zapadValue[i].ToString() != "NaN")
                    {
                        try
                        {
                            var row = new string[] { zapadValue[i].ToString(), zapadTime[i].ToString(), aktualne_pomiary[3].ToString() };
                            listView1.Items.Add(new ListViewItem(row));
                        }
                        catch
                        {

                        }
                        }
                    }

                if (aktualne_pomiary[10].ToString()!="NaN")
                {
                    if (flickerMax < flicker)
                    {

                        toolTip1.SetToolTip(panel45, aktualne_pomiary[3].ToString());
                        toolTip1.SetToolTip(label74, aktualne_pomiary[3].ToString());
                        toolTip1.SetToolTip(label72, aktualne_pomiary[3].ToString());
                        toolTip1.SetToolTip(labelPstMax, aktualne_pomiary[3].ToString());
                        flickerMax = flicker;
                    }
                    if (flickerMin > flicker)
                    {

                        toolTip1.SetToolTip(panel43, aktualne_pomiary[3].ToString());
                        toolTip1.SetToolTip(label68, aktualne_pomiary[3].ToString());
                        toolTip1.SetToolTip(label63, aktualne_pomiary[3].ToString());
                        toolTip1.SetToolTip(labelPstMin, aktualne_pomiary[3].ToString());
                        flickerMin = flicker;
                    }
                    flickerAvg = flickerSum / pamiec.timeFlicker.Count;

                    labelPstMax.Text = String.Format("{0:0.00}", flickerMax);
                    labelPstMin.Text = String.Format("{0:0.00}", flickerMin);
                    labelPstNow.Text = String.Format("{0:0.00}", flicker);
                    labelPstAvg.Text = String.Format("{0:0.00}", flickerAvg);
                }

                if (thd > thdMax)
                {
                    thdMax = thd;

                    toolTip1.SetToolTip(panel38, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label49, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label39, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(labelThdMax, aktualne_pomiary[3].ToString());





                }
                if (thd < thdMin)
                {
                    thdMin = thd;


                    toolTip1.SetToolTip(panel40, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label23, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label33, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(labelThdMin, aktualne_pomiary[3].ToString());


                }




                if (Convert.ToDouble(aktualne_pomiary[1].ToString()) > VrmsMax)
                {

                    VrmsMax = Convert.ToDouble(aktualne_pomiary[1].ToString());

                    toolTip1.SetToolTip(panel12, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label20, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label21, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(labelVrmsMax, aktualne_pomiary[3].ToString());



                }
                if (Convert.ToDouble(aktualne_pomiary[1].ToString()) < VrmsMin)
                {
                    VrmsMin = Convert.ToDouble(aktualne_pomiary[1].ToString());

                    toolTip1.SetToolTip(panel10, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label13, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label12, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(labelVrmsMin, aktualne_pomiary[3].ToString());

                }

                if (Convert.ToDouble(aktualne_pomiary[2].ToString()) < freqMin)

                {
                    freqMin = Convert.ToDouble(aktualne_pomiary[2].ToString());

                    toolTip1.SetToolTip(panel18, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label11, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label25, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(labelFreqMin, aktualne_pomiary[3].ToString());
                    ;
                }
                if (Convert.ToDouble(aktualne_pomiary[2].ToString()) > freqMax)

                {
                    freqMax = Convert.ToDouble(aktualne_pomiary[2].ToString());

                    toolTip1.SetToolTip(panel20, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label18, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label24, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(labelFreqMax, aktualne_pomiary[3].ToString());

                }

                if (Convert.ToDouble(aktualne_pomiary[0].ToString()) < vPeakMin)

                {
                    vPeakMin = Convert.ToDouble(aktualne_pomiary[0].ToString());


                    toolTip1.SetToolTip(panel21, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label19, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label35, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(labelVPeakMin, aktualne_pomiary[3].ToString());

                }
                if (Convert.ToDouble(aktualne_pomiary[0].ToString()) > vPeakMax)

                {
                    vPeakMax = Convert.ToDouble(aktualne_pomiary[0].ToString());


                    toolTip1.SetToolTip(panel23, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label34, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(label16, aktualne_pomiary[3].ToString());
                    toolTip1.SetToolTip(labelVPeakMax, aktualne_pomiary[3].ToString());

                }
                licznik_display = licznik_display + 1.00;
                freqAvg = FreqSum / Convert.ToDouble(licznik_display);
                VrmsAvg = VrmsSum / Convert.ToDouble(licznik_display);
                vPeakAvg = VPeakSum / Convert.ToDouble(licznik_display);
                thdAvg = thdSum / Convert.ToDouble(licznik_display);

                labelThdAvg.Text = String.Format("{0:0.00}", thdAvg) + "%";
                labelThdMax.Text = String.Format("{0:0.00}", thdMax) + "%";
                labelThdMin.Text = String.Format("{0:0.00}", thdMin) + "%";
                labelThdNow.Text = String.Format("{0:0.00}", thd) + "%";
                vMaxLabel.Text = String.Format("{0:0.00}", vMax) + " V";
                labelVrms.Text = String.Format("{0:0.00}", vRms) + " V";
                freqLabel.Text = String.Format("{0:0.00}", freq) + " Hz";
                labelVrmsMax.Text = String.Format("{0:0.00}", VrmsMax) + " V";
                labelVrmsMin.Text = String.Format("{0:0.00}", VrmsMin) + " V";
                labelFreqMax.Text = String.Format("{0:0.00}", freqMax) + " Hz";
                labelFreqMin.Text = String.Format("{0:0.00}", freqMin) + " Hz";
                labelFreqAvg.Text = String.Format("{0:0.00}", freqAvg) + " Hz";
                labelVrmsAvg.Text = String.Format("{0:0.00}", VrmsAvg) + " V";
                labelVPeakAvg.Text = String.Format("{0:0.00}", vPeakAvg) + " V";
                labelVPeakMax.Text = String.Format("{0:0.00}", vPeakMax) + " V";
                labelVPeakMin.Text = String.Format("{0:0.00}", vPeakMin) + " V";
                progressBar2.Value = 0;

                if(pamiec.pst.Count>1 && !button14.Enabled)
                {
                    button14.Enabled = true;
                }

                if(pamiec.vrms.Count>1 && !button4.Enabled)
                {
                    button4.Enabled = true;
                    button9.Enabled = true;
                    button3.Enabled = true;
                    button11.Enabled = true;
                    button2.Enabled = true;
                    button5.Enabled = true;
                    button10.Enabled = true;
                    button13.Enabled = true;
                    button12.Enabled = true;
                }

                if (funkcja == Funkcja.FreqT)
                {
                    MWStringArray pom1 = pamiec.time.ToArray();
                    MWNumericArray pom2 = pamiec.freq.ToArray();

                    MWArray[] recive = analizator.makePlot(1, pom1, pom2, 1, 0, 0);
                    wykres = recive[0];
                }

                if (funkcja == Funkcja.VrmsT)
                {
                    MWStringArray pom1 = pamiec.time.ToArray();
                    MWNumericArray pom2 = pamiec.vrms.ToArray();

                    MWArray[] recive = analizator.makePlot(1, pom1, pom2, 2, 0, 0);
                    wykres = recive[0];
                }

                if (funkcja == Funkcja.FreqHist)
                {

                    MWNumericArray pom2 = pamiec.freq.ToArray();
                    analizator.makePlot(0, 0, pom2, 3, 0, 0);

                }

                if (funkcja ==Funkcja.VrmsHist)
                {
                    MWNumericArray pom = pamiec.vrms.ToArray();
                    analizator.makePlot(0, 0, pom, 4, 0, 0);
                }
                if (funkcja == Funkcja.VrmsDelta)
                {
                    MWNumericArray pom = pamiec.deltaU.ToArray();
                    MWStringArray pom1 = pamiec.time.ToArray();
                    analizator.makePlot(0, pom1, pom, 6, 0, 0);
                }
                if (funkcja == Funkcja.ThdT)
                {
                    MWNumericArray pom = pamiec.thd.ToArray();
                    MWStringArray pom1 = pamiec.time.ToArray();
                    analizator.makePlot(0, pom1, pom, 5, 0, 0);
                }
                if(funkcja==Funkcja.Flicker)
                {
                    MWNumericArray pom = pamiec.pst.ToArray();
                    MWStringArray pom1 = pamiec.timeFlicker.ToArray();
                    analizator.makePlot(0, pom1, pom, 8, 0, 0);
                }
            });
           this.BeginInvoke(invoker);
          
            functionCallDone.Set();
        }
        





    

        public void Update_Interfejs_Status_Polaczenia(int status)
        {
            if(status==1)
            {
                status_połaczenia.ForeColor = System.Drawing.Color.Green;
                status_połaczenia.Text = "Połączony z urządzeniem po przez TCP/IP";
            }
            if(status==2)
            {
                status_połaczenia.ForeColor = System.Drawing.Color.Green;
                status_połaczenia.Text = "Połączony z urządzeniem po przez RS232";
            }
            if(status==0)
            {
                status_połaczenia.ForeColor = System.Drawing.Color.Red;
                status_połaczenia.Text = "Brak połączenia";
            }
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);


        bool clicked = true;
       
        static CancellationTokenSource cts = new CancellationTokenSource();
        public void TimerPomiaru()
        {
            DateTime czasStartu = DateTime.Now;
            Task taskRead =  Task.Run(() =>
            {


                while (true)
                {
                    labelCzasPomiaru.Text = Convert.ToString(czasStartu - DateTime.Now);
                    Thread.Sleep(1000);

                }
            }, cts.Token);

        }
        private void button1_Click(object sender, EventArgs e)
        {
            firstCallTime = DateTime.Now;
            if (clicked)
            {
                connectDone.Reset();
                sendDone.Reset();
                receiveDone.Reset();
                
               
                StartClient(Convert.ToInt32(textBox_port.Text), textBox_ip.Text);
      
                button1.Text = "Rozłącz";
             
                dataStart = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss.ffffff", System.Globalization.CultureInfo.InvariantCulture);
                //timer1.Enabled = true;\
                if (!backgroundWorker2.IsBusy)
                {
                    backgroundWorker2.RunWorkerAsync();
                    firstCallTime = DateTime.Now;
                }

            }
            else
            {

                status_połaczenia.Text = "Brak połączenia";
                status_połaczenia.ForeColor = System.Drawing.Color.Red;
                Disconnect();
                button1.Text = "Połącz";
                if (backgroundWorker2.IsBusy == true)
                {
                    backgroundWorker2.CancelAsync();

                }
                dataStart = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

                if (backgroundWorker2.IsBusy)
                {
                    backgroundWorker2.CancelAsync();
                }

            }



            clicked = !clicked;
          
            //timer1.Enabled = true;


        }
        IntPtr foundWindow;
        private async void button2_Click(object sender, EventArgs e)
        {
            if (lastButton != null)
            {

                lastButton.BackColor = System.Drawing.Color.Yellow;
            }
            button2.BackColor = System.Drawing.Color.Green;
            lastButton = (Button)sender;
            Task task = new Task(()=>AsynchronicznyPlot(Funkcja.FreqT));
            task.Start();
            await task;


        }
        public void AsynchronicznyPlot(Funkcja rodzaj_funkcji)
        {
            Analizator analizator = new Analizator();

            MethodInvoker invoker = new MethodInvoker(delegate
            {
                if (rodzaj_funkcji == Funkcja.Oscylogram)
                {
                    panel6.Visible = true;
                }
                else
                {
                    panel6.Visible = false;
                }
            });
          this.Invoke(invoker);
    
            switch (rodzaj_funkcji)
            {
                case Funkcja.FreqT:
                    if (pamiec.freq.Count < 1)
                    {
                        

                    }
                    else
                    {
                        MWNumericArray freq = pamiec.freq.ToArray();
                        MWStringArray czas = pamiec.time.ToArray();
                        var answer = analizator.makePlot(1, czas, freq, 1, 0, 0); ;
                        wykres = answer[0];

                    }

                    funkcja = Funkcja.FreqT;
                    break;

                case Funkcja.Flicker:
                    if (pamiec.pst.Count < 1)
                    {
                      

                    }
                    else
                    {
                        MWNumericArray pst = pamiec.pst.ToArray();
                        MWStringArray czas = pamiec.timeFlicker.ToArray();
                        var answer = analizator.makePlot(1, czas, pst, 8, 0, 0); ;
                        wykres = answer[0];

                    }

                    funkcja = Funkcja.Flicker;

                    break;

                case Funkcja.FreqDelta:
                    if (pamiec.deltaHz.Count < 1)
                    {
                      

                    }
                    else
                    {
                        MWNumericArray pom1 = pamiec.deltaHz.ToArray();
                        MWStringArray pom2 = pamiec.time.ToArray();
                        var answer = analizator.makePlot(1, pom2, pom1, 7, 0, 0); ;
                        wykres = answer[0];

                    }

                    funkcja = Funkcja.FreqDelta;

                    break;

                case Funkcja.FreqHist:
                    if (pamiec.freq.Count < 1)
                    {
                       

                    }
                    else
                    {
                        MWNumericArray pom1 = pamiec.freq.ToArray();
                   
                        var answer = analizator.makePlot(1, 0, pom1, 3, 0, 0); ;
                        wykres = answer[0];

                    }

                    funkcja = Funkcja.FreqHist;

                    break;



                case Funkcja.ThdT:
                    if (pamiec.thd.Count < 1)
                    {
                      

                    }
                    else
                    {
                        MWNumericArray pom1 = pamiec.thd.ToArray();
                        MWStringArray pom2 = pamiec.time.ToArray();

                        var answer = analizator.makePlot(1, pom2, pom1, 5, 0, 0); ;
                        wykres = answer[0];

                    }

                    funkcja = Funkcja.ThdT;

                    break;



                case Funkcja.VrmsDelta:
                    if (pamiec.deltaU.Count < 1)
                    {
                     

                    }
                    else
                    {
                        MWNumericArray pom1 = pamiec.deltaU.ToArray();
                        MWStringArray pom2 = pamiec.time.ToArray();

                        var answer = analizator.makePlot(1, pom2, pom1, 6, 0, 0); ;
                        wykres = answer[0];

                    }

                    funkcja = Funkcja.VrmsDelta;

                    break;


                case Funkcja.VrmsHist:
                    if (pamiec.thd.Count < 1)
                    {
                   

                    }
                    else
                    {
                        MWNumericArray pom1 = pamiec.vrms.ToArray();
                

                        var answer = analizator.makePlot(1, 0, pom1, 4, 0, 0); ;
                        wykres = answer[0];

                    }

                    funkcja = Funkcja.VrmsHist;

                    break;


                case Funkcja.VrmsT:
                    if (pamiec.vrms.Count < 1)
                    {
                  

                    }
                    else
                    {
                        MWNumericArray pom1 = pamiec.vrms.ToArray();
                        MWStringArray pom2 = pamiec.time.ToArray();

                        var answer = analizator.makePlot(1, pom2, pom1, 2, 0, 0); ;
                        wykres = answer[0];

                    }

                    funkcja = Funkcja.VrmsT;

                    break;
                case Funkcja.Oscylogram:

                    if(pamiec.ostatniPrzebieg.Length>0)
                    {

                        analizator.LiveTimeVoltage(0, pamiec.ostatniPrzebieg, timeDiv,0);
                    }

                    break;




            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            panel6.Visible = false;
     
        }


        private void Form1_Resize(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (lastButton != null)
            {

                lastButton.BackColor = System.Drawing.Color.Yellow;
            }
            button3.BackColor = System.Drawing.Color.Green;
            lastButton = (Button)sender;
            funkcja = Funkcja.HarProc;
        }


        private async void button5_Click_1(object sender, EventArgs e)
        {
            Analizator analizator = new Analizator();

   

            if (lastButton != null)
            {

                lastButton.BackColor = System.Drawing.Color.Yellow;
            }
            button5.BackColor = System.Drawing.Color.Green;
            lastButton = (Button)sender;
            Task task = new Task(() => AsynchronicznyPlot(Funkcja.FreqHist));
            task.Start();
            await task;


        }

        

        private void button8_Click(object sender, EventArgs e)
        {
            Analizator analizator = new Analizator();

         
            MWStringArray pom1 = pamiec.time.ToArray();
            MWNumericArray pom2 = pamiec.freq.ToArray();
            MWNumericArray pom3 = pamiec.vrms.ToArray();
            MWNumericArray pom4 = pamiec.thd.ToArray();
            MWNumericArray pom5 = pamiec.pst.ToArray();
            MWStringArray pom6 = pamiec.timeFlicker.ToArray();
            MWNumericArray pom7 = pamiec.deltaU.ToArray();
            MWNumericArray pom8 = pamiec.deltaHz.ToArray();
            string filename;
            try
            {
                analizator.makePlot(0, pom1, pom2, 1, 0, 1);
                analizator.makePlot(0, pom1, pom3, 2, 0, 1);
                analizator.makePlot(0, 0, pom2, 3, 0, 1);
                analizator.makePlot(0, 0, pom3, 4, 0, 1);
                analizator.makePlot(0, pom1, pom4, 5, 0, 1);
                analizator.makePlot(0, pom1, pom7, 6, 0, 1);
                analizator.makePlot(0, pom1, pom8, 7, 0, 1);
                analizator.makePlot(0, pom6, pom5, 8, 0, 1);
                analizator.LiveTimeVoltage(0, pamiec.ostatniPrzebieg, 0.04, 1);
                analizator.Harmoniczne(0,0,0,pamiec.harmVect,pamiec.time.Count);
                analizator.Harmoniczne(0, 0, 1, pamiec.harmVect, pamiec.time.Count);

            }
            catch(Exception ex)
            {
                Console.WriteLine("Wystąpił błąd z zapisywaniem pliku, spróbuj ponownie.");
            }
            DateTime data = DateTime.Now;
            string freqTime = "freqTime.pdf";
            string freqHist = "freqHist.pdf";
            string vrmsHist = "vrmsHist.pdf";
            string vrmsTime = "vrmsTime.pdf";
            string thdTime = "thdTime.pdf";
            string deltaU = "deltaUTime.pdf";
            string deltaHz = "deltaHzTime.pdf";
            string flickerTime = "flickerTime.pdf";
            string oscylogram = "oscylogram.pdf";
            string harmProc = "harmProc.pdf";
            string harmVolt = "harmVolt.pdf";

            XFont font = new XFont("Verdana", 20, XFontStyle.BoldItalic);
            XFont font1 = new XFont("Verdana", 10, XFontStyle.BoldItalic);
            XFont font3 = new XFont("Verdana", 8, XFontStyle.Bold);
            XFont font4 = new XFont("Verdana", 5.5);
            XFont font5 = new XFont("Verdana", 4);
            DateTime today = DateTime.Now;
            if (textBox1.Text == String.Empty)
            {
                 filename = "Raport_" + today.Year.ToString() + "_" + today.Month.ToString() + "_" + today.Day.ToString() + ".pdf";       
            }
            else
            {
                filename = textBox1.Text + ".pdf";
            }
            XPen pen = new XPen(XColors.Black, 1);
            PdfDocument document = new PdfDocument();

            // Pusta strona
            PdfPage page = document.AddPage();

            XGraphics gfx = XGraphics.FromPdfPage(page);


            gfx.DrawString("Raport z pomiaru napięcia sieciowego", font, XBrushes.Black, 350, 0, XStringFormats.TopCenter);
            XRect rect = new XRect(20, 50, 250, 220);
            try
            {
                gfx.DrawString("Okres analizy: " + pamiec.time[0] + "-" + pamiec.time[pamiec.time.Count - 1], font1, XBrushes.Black, rect, XStringFormats.TopLeft);
            }
            catch
            {
                gfx.DrawString("Okres analizy: pomiar nie rozpoczęty", font1, XBrushes.Black, rect, XStringFormats.TopLeft);
            }
            rect = new XRect(20, 75, 250, 220);
            gfx.DrawString("1. Spis parametrów ", font, XBrushes.Black, rect, XStringFormats.TopLeft);

            //Opis Tabeli
            rect = new XRect(25, 142, 250, 220);
            gfx.DrawString("Max ", font3, XBrushes.Black, rect, XStringFormats.TopLeft);
            rect = new XRect(25, 162, 250, 220);
            gfx.DrawString("Min ", font3, XBrushes.Black, rect, XStringFormats.TopLeft);
            rect = new XRect(25, 182, 250, 220);

            gfx.DrawString("Avg ", font3, XBrushes.Black, rect, XStringFormats.TopLeft);

            rect = new XRect(60, 122, 250, 220);
            gfx.DrawString("Vrms ", font3, XBrushes.Black, rect, XStringFormats.TopLeft);
            rect = new XRect(120, 122, 250, 220);
            gfx.DrawString("Freq ", font3, XBrushes.Black, rect, XStringFormats.TopLeft);
            rect = new XRect(180, 122, 75, 75);
            gfx.DrawString("THD+T ", font3, XBrushes.Black, rect, XStringFormats.TopLeft);
            rect = new XRect(240, 122, 75, 75);
            gfx.DrawString("Vpeak ", font3, XBrushes.Black, rect, XStringFormats.TopLeft);
            rect = new XRect(300, 122, 75, 75);
            gfx.DrawString("Flicker ", font3, XBrushes.Black, rect, XStringFormats.TopLeft);
           
            //Linie Pionowe
            gfx.DrawLine(pen, 20, 120, 20, 200);
            gfx.DrawLine(pen, 50, 120, 50, 200);
            gfx.DrawLine(pen, 110, 120, 110, 200);
            gfx.DrawLine(pen, 170, 120, 170, 200);
            gfx.DrawLine(pen, 230, 120, 230, 200);
            gfx.DrawLine(pen, 290, 120, 290, 200);
            gfx.DrawLine(pen, 350, 120, 350, 200);

            //Linie Poziome
            gfx.DrawLine(pen, 20, 120, 350, 120);
            gfx.DrawLine(pen, 20, 140, 350, 140);
            gfx.DrawLine(pen, 20, 160, 350, 160);
            gfx.DrawLine(pen, 20, 180, 350, 180);
            gfx.DrawLine(pen, 20, 200, 350, 200);

            try
            {
                //Vmax
                rect = new XRect(240, 142, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", vPeakMax) + " V", font4, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(232, 152, 75, 75);
                gfx.DrawString(toolTip1.GetToolTip(panel23), font5, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(240, 162, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", vPeakMin) + " V", font4, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(232, 172, 75, 75);
                gfx.DrawString(toolTip1.GetToolTip(panel21), font5, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(240, 182, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", vPeakAvg) + " V", font4, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(232, 192, 75, 75);
                gfx.DrawString(toolTip1.GetToolTip(panel22), font5, XBrushes.Black, rect, XStringFormats.TopLeft);


                //THD+T
                rect = new XRect(180, 142, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", thdMax) + " %", font4, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(172, 152, 75, 75);
                gfx.DrawString(toolTip1.GetToolTip(panel38), font5, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(180, 162, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", thdMin) + " %", font4, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(172, 172, 75, 75);
                gfx.DrawString(toolTip1.GetToolTip(panel40), font5, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(180, 182, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", thdAvg) + "%", font4, XBrushes.Black, rect, XStringFormats.TopLeft);


                //Freq
                rect = new XRect(120, 142, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", freqMax) + " Hz", font4, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(112, 152, 75, 75);
                gfx.DrawString(toolTip1.GetToolTip(panel20), font5, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(120, 162, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", freqMin) + " Hz", font4, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(112, 172, 75, 75);
                gfx.DrawString(toolTip1.GetToolTip(panel18), font5, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(120, 182, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", freqAvg) + " Hz", font4, XBrushes.Black, rect, XStringFormats.TopLeft);


                //Vrms
                rect = new XRect(60, 142, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", VrmsMax) + " V", font4, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(52, 152, 75, 75);
                gfx.DrawString(toolTip1.GetToolTip(panel12), font5, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(60, 162, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", VrmsMin) + " V", font4, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(52, 172, 75, 75);
                gfx.DrawString(toolTip1.GetToolTip(panel10), font5, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(60, 182, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", VrmsAvg) + " V", font4, XBrushes.Black, rect, XStringFormats.TopLeft);

                //Flicker
                rect = new XRect(300, 142, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", flickerMax), font4, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(292, 152, 75, 75);
                gfx.DrawString(toolTip1.GetToolTip(panel45), font5, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(300, 162, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", flickerMin), font4, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(292, 172, 75, 75);
                gfx.DrawString(toolTip1.GetToolTip(panel43), font5, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(300, 182, 75, 75);
                gfx.DrawString(String.Format("{0:0.00}", flickerAvg), font4, XBrushes.Black, rect, XStringFormats.TopLeft);

                rect = new XRect(20, 272, 250, 220);
                gfx.DrawString("2. Charakterystki", font, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(40, 312, 250, 220);
                gfx.DrawString("2.1 Wahania częstotliwości", font1, XBrushes.Black, rect, XStringFormats.TopLeft);
                DrawImage(gfx, Path.Combine(Environment.CurrentDirectory, freqTime), 20, 350, 600, 900);

                //2 Strona
                PdfPage page1 = document.AddPage();
                gfx = XGraphics.FromPdfPage(page1);
                rect = new XRect(40, 50, 250, 220);
                gfx.DrawString("2.2 Histogram częstotliwości", font1, XBrushes.Black, rect, XStringFormats.TopLeft);
                DrawImage(gfx, Path.Combine(Environment.CurrentDirectory, freqHist), 10, 70, 600, 900);
                rect = new XRect(40, 450, 250, 220);
                gfx.DrawString("2.3 Odchylenia częstotliwości od wartości znamionowej", font1, XBrushes.Black, rect, XStringFormats.TopLeft);
                DrawImage(gfx, Path.Combine(Environment.CurrentDirectory, deltaHz), 10, 470, 600, 900);

                //3 Strona
                PdfPage page2 = document.AddPage();
                gfx = XGraphics.FromPdfPage(page2);
                rect = new XRect(40, 50, 250, 220);
                gfx.DrawString("2.4 Wahania wartości skutecznej napięcia", font1, XBrushes.Black, rect, XStringFormats.TopLeft);
                DrawImage(gfx, Path.Combine(Environment.CurrentDirectory, vrmsTime), 10, 70, 600, 900);
                rect = new XRect(40, 450, 250, 220);
                gfx.DrawString("2.5 Histogram wartości skutecznej napięcia", font1, XBrushes.Black, rect, XStringFormats.TopLeft);
                DrawImage(gfx, Path.Combine(Environment.CurrentDirectory, vrmsHist), 10, 470, 600, 900);

                //4 Strona 
                PdfPage page3 = document.AddPage();
                gfx = XGraphics.FromPdfPage(page3);

                rect = new XRect(40, 50, 250, 220);
                gfx.DrawString("2.6 Odchylenia wartości skutecznej napięcia od wartości znamionowej", font1, XBrushes.Black, rect, XStringFormats.TopLeft);
                DrawImage(gfx, Path.Combine(Environment.CurrentDirectory, deltaU), 10, 70, 600, 900);

                rect = new XRect(40, 450, 250, 220);
                gfx.DrawString("2.7 Zmiany THD w czasie", font1, XBrushes.Black, rect, XStringFormats.TopLeft);
                DrawImage(gfx, Path.Combine(Environment.CurrentDirectory, thdTime), 10, 470, 600, 900);

                //5 Strona 
                PdfPage page4 = document.AddPage();
                gfx = XGraphics.FromPdfPage(page4);

                rect = new XRect(40, 50, 250, 220);
                gfx.DrawString("2.8 Harmoniczne napięcia (wartości średnie z całego okresu pomiaru)", font1, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(60, 70, 250, 220);
                gfx.DrawString("2.81 Wartości w procentach", font3, XBrushes.Black, rect, XStringFormats.TopLeft);
                DrawImage(gfx, Path.Combine(Environment.CurrentDirectory, harmProc), 10, 85, 600 / 2, 900 / 2);
                rect = new XRect(360, 70, 250, 220);
                gfx.DrawString("2.82 Względna wartość napięcia", font3, XBrushes.Black, rect, XStringFormats.TopLeft);
                DrawImage(gfx, Path.Combine(Environment.CurrentDirectory, harmVolt), 310, 85, 600 / 2, 900 / 2);

                rect = new XRect(40, 300, 250, 220);
                gfx.DrawString("2.9 Zmiany wspołczynnika migotania światła", font1, XBrushes.Black, rect, XStringFormats.TopLeft);
                DrawImage(gfx, Path.Combine(Environment.CurrentDirectory, flickerTime), 10, 315, 600, 900);

                //6 Strona 
                PdfPage page5 = document.AddPage();
                gfx = XGraphics.FromPdfPage(page5);

                rect = new XRect(40, 50, 250, 220);
                gfx.DrawString("2.10 Chwilowy oscylogram napięcia", font1, XBrushes.Black, rect, XStringFormats.TopLeft);
                DrawImage(gfx, Path.Combine(Environment.CurrentDirectory, oscylogram), 10, 85, 600, 900);

                rect = new XRect(40, 450, 250, 220);
                gfx.DrawString("3. Wykryte zapady zasilania", font, XBrushes.Black, rect, XStringFormats.TopLeft);
                if (listView1.Items.Count < 1)
                {
                    rect = new XRect(50, 490, 250, 220);
                    gfx.DrawString("- Brak", font3, XBrushes.Black, rect, XStringFormats.TopLeft);
                }
                else
                {
                    for(int i=0;i<listView1.Items.Count;i++)
                    {
                        rect = new XRect(50, 490+(i*20), 250, 220);
                        string tekst = "- ΔU:" + listView1.Items[i].SubItems[0].Text+" V, "+ " t: "+ listView1.Items[i].SubItems[1].Text+ " s, "+ " Data wystąpienia: " +listView1.Items[i].SubItems[2].Text;
                        gfx.DrawString(tekst, font3, XBrushes.Black, rect, XStringFormats.TopLeft);
                    }
                }
                }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }



            try
            {
             
                if(File.Exists(Path.Combine(Environment.CurrentDirectory,filename)))
                {
                    for (int i=1;i<100;i++)
                    {
                        if((!File.Exists(Path.Combine(Environment.CurrentDirectory, "Raport_" + today.Year.ToString() + "_" + today.Month.ToString() + "_" + today.Day.ToString() + "(" + Convert.ToString(i) + ")" + ".pdf"))))
                        {
                            filename = "Raport_" + today.Year.ToString() + "_" + today.Month.ToString() + "_" + today.Day.ToString() +"("+Convert.ToString(i)+")" +".pdf";
                            break;
                        }
                    }
}
                document.Save(filename);
                Process.Start(filename);
                if (pamiec.time.Count < 2)
                {
                    MessageBox.Show("Dokonano operacji generowania raportu dla pustych tablic. Wygenerowane przebiegi pochodzą z pamięci ostatniego pomiaru.",Application.ProductName,MessageBoxButtons.OK,MessageBoxIcon.Information);
                }
            }
            catch
            {
                MessageBox.Show("Dokument jest już używany przez inny proces.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);

            }
        }
        void DrawImage(XGraphics gfx, string jpegSamplePath, int x, int y, int width, int height)
        {
            XImage image = XImage.FromFile(jpegSamplePath);
            gfx.DrawImage(image, x, y, width, height);
        }



        private async void button9_Click(object sender, EventArgs e)
        {
            Analizator analizator = new Analizator();
            if (lastButton != null)
            {

                lastButton.BackColor = System.Drawing.Color.Yellow;
            }
            button9.BackColor = System.Drawing.Color.Green;
            lastButton = (Button)sender;
            Task task = new Task(() => AsynchronicznyPlot(Funkcja.VrmsHist));
            task.Start();
            await task;
        }

        private void panel11_MouseDown(object sender, MouseEventArgs e)
        {
     
        }

        private void panel9_MouseEnter(object sender, EventArgs e)
        {
         
        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        private void panel9_MouseDown(object sender, MouseEventArgs e)
        {
           
        }



        private async void button10_Click(object sender, EventArgs e)
        {
            Analizator analizator = new Analizator();
            if (lastButton != null)
            {

                lastButton.BackColor = System.Drawing.Color.Yellow;
            }
            button10.BackColor = System.Drawing.Color.Green;
            lastButton = (Button)sender;
          

            panel6.Visible = true;

            Task task = new Task(() => AsynchronicznyPlot(Funkcja.VrmsDelta));
            task.Start();
            await task;




        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
        

        }

        private async void button12_Click(object sender, EventArgs e)
        {
            Analizator analizator = new Analizator();
            if (lastButton != null)
            {

                lastButton.BackColor = System.Drawing.Color.Yellow;
            }
            button12.BackColor = System.Drawing.Color.Green;
            lastButton = (Button)sender;
            Task task = new Task(() => AsynchronicznyPlot(Funkcja.ThdT));
            task.Start();
            await task;

        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (lastButton != null)
            {

                lastButton.BackColor = System.Drawing.Color.Yellow;
            }
            button11.BackColor = System.Drawing.Color.Green;
            lastButton = (Button)sender;
            funkcja = Funkcja.HarV;


       
        }

        private async void trackTimeDiv_MouseUp(object sender, MouseEventArgs e)
        {
            Task task = new Task(() => AsynchronicznyPlot(Funkcja.Oscylogram));
            task.Start();
            await task;
        }

        private void label56_Click(object sender, EventArgs e)
        {

        }

        private void label59_Click(object sender, EventArgs e)
        {

        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            double progress;
            while (!worker.CancellationPending)
            {
                progress = (Convert.ToDouble(ramki.Length)/ Convert.ToDouble(dlugoscSygnalu)) * 100.0;

               
                    worker.ReportProgress(Convert.ToInt32(progress));


                Thread.Sleep(1000);
            }
        }
        int licznikPrzetwarzanie=0;

        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                TimeSpan timeSpan = DateTime.Now - firstCallTime;
                labelCzasPomiaru.Text = timeSpan.ToString(@"hh\:mm\:ss");
                progressBar2.Value = e.ProgressPercentage;
                if (e.ProgressPercentage == 100)
                {


                    label36.Visible = true;
                    switch (licznikPrzetwarzanie)
                    {
                        case 0:
                        label36.Text = "Trwa przetwarzanie danych.";
                            break;
                        case 1:
                            label36.Text = "Trwa przetwarzanie danych..";
                            break;
                        case 2:
                            label36.Text = "Trwa przetwarzanie danych...";
                            break;

                    }
                    licznikPrzetwarzanie++;
                    if (licznikPrzetwarzanie==3)
                    {
                        licznikPrzetwarzanie = 0;
                    }
                 
                }
                else
                {
                    licznikPrzetwarzanie = 0;
                    label36.Visible = false;
                }
             
            }
        catch
            {
                
            }

       }

        private void button7_Click_1(object sender, EventArgs e)
        {
      
            MWArray[] cellout = null;

            var zapad1 = ListZapadow();
            Zapad zapad = new Zapad();
            cellout = zapad.zapadTest(2);

            MWArray zapadValue = cellout[0];
            MWArray zapadTime = cellout[1];

            for (int i = 1; i <= zapadValue.Dimensions[1]; i++)
            {

                var row = new string[] { zapadValue[i].ToString(), zapadTime[i].ToString(), zapad1[0].Godzina };
                listView1.Items.Add(new ListViewItem(row));
            }
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            textBox1.BackColor=System.Drawing.SystemColors.Window;
        }

        private void AnalizatorOff_Click(object sender, EventArgs e)
        {
            try
            {
                Send(client, "SHUTDOWN/n");
            }
            catch
            {

            }
            }

        private void button15_Click(object sender, EventArgs e)
        {
            listView1.Clear();
            pamiec.deltaHz.Clear();
            pamiec.deltaU.Clear();
            pamiec.freq.Clear();
            pamiec.freq2.Clear();
            pamiec.harmVect = MWNumericArray.Empty;
            pamiec.peaksFlicker = MWNumericArray.Empty;
            pamiec.pst.Clear();
            pamiec.thd.Clear();
            pamiec.time.Clear();
            pamiec.time2.Clear();
            pamiec.timeFlicker.Clear();
            pamiec.vrms.Clear();
            ramki.Clear();
        }

        private async void button13_Click(object sender, EventArgs e)
        {
            Analizator analizator = new Analizator();
            if (lastButton != null)
            {

                lastButton.BackColor = System.Drawing.Color.Yellow;
            }
            button13.BackColor = System.Drawing.Color.Green;
            lastButton = (Button)sender;


            panel6.Visible = true;

            Task task = new Task(() => AsynchronicznyPlot(Funkcja.FreqDelta));
            task.Start();
            await task;


        }

        private void backgroundWorker1_DoWork_1(object sender, DoWorkEventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            Analizator analizator = new Analizator();
   
            MatlabTesty testy = new MatlabTesty();

          var answ = testy.cellexamp(2);

            MWCellArray czasy = (MWCellArray)answ[0];
            MWNumericArray czestotliwosc = (MWNumericArray)answ[1];
            //MessageBox.Show(czasy.ToString());
            MessageBox.Show(czestotliwosc.ToString());

            MessageBox.Show(Convert.ToString(czasy));
           
            //analizator.makePlot(0, cellout[1], cellout[2], 1, 0,0);
        }

        private async void button14_Click(object sender, EventArgs e)
        {
            Analizator analizator = new Analizator();
            if (lastButton != null)
            {

                lastButton.BackColor = System.Drawing.Color.Yellow;
            }
            button14.BackColor = System.Drawing.Color.Green;
            lastButton = (Button)sender;

            Task task = new Task(() => AsynchronicznyPlot(Funkcja.Flicker));
            task.Start();
            await task;

        }

        private void Panel1_Resize(object sender, EventArgs e)
        {
       
                MoveWindow(foundWindow, -9, -55, Panel1.Width+52, Panel1.Height+65, true);


      

            
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void Panel_TCP_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label50_Click(object sender, EventArgs e)
        {

        }

        private void trackBarRefreshPanel_ValueChanged(object sender, EventArgs e)
        {


          

        }

        private async void button4_Click(object sender, EventArgs e)
        {
            Analizator analizator = new Analizator();
            if (lastButton!=null)
            {

                lastButton.BackColor = System.Drawing.Color.Yellow;
            }
            button4.BackColor = System.Drawing.Color.Green;
            lastButton = (Button)sender;
            Task task = new Task(() => AsynchronicznyPlot(Funkcja.VrmsT));
            task.Start();
            await task;



        }

        private void label26_Click(object sender, EventArgs e)
        {

        }

  

        private void label19_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private async void button6_Click(object sender, EventArgs e)
        {
            timeDiv = trackTimeDiv.Value / 10000.0;



            if (lastButton != null)
            {

                lastButton.BackColor = System.Drawing.Color.Yellow;
            }
            button6.BackColor = System.Drawing.Color.Green;
            lastButton = (Button)sender;
      
            panel6.Visible = true;
            funkcja = Funkcja.Oscylogram;
            Task task = new Task(() => AsynchronicznyPlot(Funkcja.Oscylogram));
            task.Start();
            await task;

        }

        private void panel16_Click(object sender, EventArgs e)
        {
            //analizator.empty_plot();
            //Thread.Sleep(200);
            //funkcja = "a";
        }

        private void trackTimeDiv_ValueChanged(object sender, EventArgs e)
        {
          
            timeDiv = trackTimeDiv.Value / 10000.0;
            label4.Text = timeDiv + "[s]";

        }

        private void trackTimeDiv_Scroll(object sender, EventArgs e)
        {

        }

        private void status_połaczenia_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Klient_TCP_IP klient = new Klient_TCP_IP();
      
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            if (progressBar2.Value < 100)
            {
                progressBar2.Value = progressBar2.Value + 10;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
           
        }
    }
}
