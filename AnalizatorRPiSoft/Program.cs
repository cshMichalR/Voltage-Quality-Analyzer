using System;
using System.Device.Spi;
using System.Text;
using Iot.Device.Adc;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Diagnostics;
using System.Device.I2c;
using Iot.Device.CharacterLcd;
using Iot.Device.Pcx857x;
using System.Device.Gpio;

namespace AnalizatorRPiSoft
{

    class Program
    {
        static CancellationTokenSource cts = new CancellationTokenSource();
        static Task taskWrite;
        static Task taskRead;
        static string IpAdress;

        public class StateObject
        { 
            public const int BufferSize = 1024; 
            public byte[] buffer = new byte[BufferSize];
            public StringBuilder sb = new StringBuilder();
            public Socket workSocket = null;
        }


        public static ManualResetEvent allDone = new ManualResetEvent(false);
        static Socket listener;
        static Socket gniazdo;

        public static void StartListening()
        {
            using I2cDevice i2c = I2cDevice.Create(new I2cConnectionSettings(1, 0x27));
            using var driver = new Pcf8574(i2c);
            using var lcd = new Lcd2004(registerSelectPin: 0,
                                    enablePin: 2,
                                    dataPins: new int[] { 4, 5, 6, 7 },
                                    backlightPin: 3,
                                    backlightBrightness: 0.1f,
                                    readWritePin: 1,
                                    controller: new GpioController(PinNumberingScheme.Logical, driver));
            int currentLine = 0;


            lcd.Clear();
            lcd.SetCursorPosition(0, currentLine);
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            Console.WriteLine("Informacje sieciowe analizatora:");
            for (int i = 0; i < ipHostInfo.AddressList.Length; i++)
            {
                Console.WriteLine(ipHostInfo.AddressList[i].ToString());
            }
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 4210);
            IpAdress = ipHostInfo.AddressList[1].ToString();
            try
            {
                Lcd_Wyswietl("IP: " + ipHostInfo.AddressList[1].ToString() + "/n" + "Brak Polaczenia", 0, 0);
            }
            catch
            {
                Lcd_Wyswietl("Wi-Fi Off", 0, 0);

            }
            listener = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);

            try
            {

                listener.Bind(localEndPoint);
                Wait_For_Connections();



            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }


        public static void Wait_For_Connections()
        {
            Program program = new Program();


            listener.Listen(100);
            allDone.Reset();


            Lcd_Wyswietl("IP: " + IpAdress + "/n" + "Brak Polaczenia", 0, 0);
            Console.WriteLine("Oczekiwanie na połączenie...");
            listener.BeginAccept(
                new AsyncCallback(AcceptCallback),
                listener);

            allDone.WaitOne();
            Console.WriteLine("Klient połączył się z serwerem.");
            Console.WriteLine("Startuje taski ponownie");
            OdczytWysylanieADc();



        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            Program program = new Program();
            allDone.Set();


            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            StateObject state = new StateObject();
            state.workSocket = handler;
            gniazdo = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);


        }

        public static void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            try
            {
                String content = String.Empty;
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {

                    state.sb.Append(Encoding.ASCII.GetString(
                        state.buffer, 0, bytesRead));

                    content = state.sb.ToString();
                    if (content.IndexOf("/n") > -1)
                    {
                        Console.WriteLine(content);
                        if (content == "SHUTDOWN") ;
                        {
                            for (int shutdowncountdown = 0; shutdowncountdown < 6; shutdowncountdown++)
                            {
                                Lcd_Wyswietl("Wylaczanie za " + Convert.ToString(5 - shutdowncountdown) + "s", 0, 0);
                                Thread.Sleep(1000);
                            }
                            Lcd_Wyswietl("System wylaczony", 0, 0);
                            Thread.Sleep(500);
                            Process.Start(new ProcessStartInfo() { FileName = "sudo", Arguments = "shutdown now" });
                        }
                        Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                            content.Length, content);
                        allDone.Set();
                    }
                    else
                    {
                        allDone.Set();
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                    }


                }
            }
            catch
            {
                CancelTasks();
            }
        }

        public static void Send(Socket handler, String data)
        {
            try
            {
                byte[] byteData = Encoding.ASCII.GetBytes(data);
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), handler);
            }
            catch (Exception ex)
            {

                Console.WriteLine("Klient się rozłączył");

                CancelTasks();


            }
        }
        static void CancelTasks()
        {

            cts.Cancel();


        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);
            }
            catch (Exception ex)
            {

                CancelTasks();
            }
        }


        public static void Main(string[] args)
        {
            StartListening();
            Console.WriteLine("Wcisnij dowolny klawisz aby zakończyć");
            Console.Read();
        }


        private static void OdczytWysylanieADc()
        {
            double currentTime;
            cts = new CancellationTokenSource();
            var data = new BlockingCollection<string>();
            var hardwareSpiSettings = new SpiConnectionSettings(0, 0);
            hardwareSpiSettings.ClockFrequency = 1000000;
            hardwareSpiSettings.Mode = SpiMode.Mode0;

            bool HighVoltage = false;
            using SpiDevice spi1 = SpiDevice.Create(hardwareSpiSettings);
            Mcp3201 mcp = new Mcp3201(spi1);

            taskRead = Task.Factory.StartNew(() =>
            {

                long dataStart = Stopwatch.GetTimestamp();

                var sensorReading = String.Empty;
                int timeLicznik = 0;
                string Tekst = "No signal";

                    // Delay ze względu na zastosowanie kondensatorów w układzie
                    while ((Stopwatch.GetTimestamp() - dataStart) / Stopwatch.Frequency < 5)

                {
                    if ((Stopwatch.GetTimestamp() - dataStart) / Stopwatch.Frequency > timeLicznik)
                    {
                        timeLicznik++;
                        Lcd_Wyswietl("Inicjalizacja..." + "/n" + "Pozostalo:" + (5 - timeLicznik).ToString() + " s", 0, 0);
                    }
                    if (mcp.Read() > 2600 && !HighVoltage)
                    {

                        HighVoltage = true;
                    }

                }

                if (HighVoltage)
                {
                    Tekst = "Signal detected";
                }
                Lcd_Wyswietl("Polaczony" + "/n" + Tekst, 3, 0);
                while (true)
                {
                    if (cts.Token.IsCancellationRequested)
                        break;
                    currentTime = (Stopwatch.GetTimestamp() - dataStart);
                    sensorReading = Convert.ToString(mcp.Read());

                    data.Add(sensorReading + " " + currentTime);
                }
                Console.WriteLine("Odczyt wstrzymany");



            }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);


            taskWrite = Task.Factory.StartNew(() =>
            {

                var buffer = new List<string>();

                while (true)
                {
                    if (cts.Token.IsCancellationRequested)
                        break;
                    if (buffer.Count > 500)
                    {
                        Send(gniazdo, String.Join('/', buffer) + '/');
                        buffer.Clear();
                    }
                    else
                    {

                        buffer.Add(data.Take());


                    }

                }
                Console.WriteLine("Przesylanie wstrzymane");



            }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Task[] tasks = { taskRead, taskWrite };


            Task.WaitAll(tasks);

            taskRead.Dispose();
            taskWrite.Dispose();
            Wait_For_Connections();
        }


        static void Lcd_Wyswietl(string text, int pos, int line)
        {
            using I2cDevice i2c = I2cDevice.Create(new I2cConnectionSettings(1, 0x27));
            using var driver = new Pcf8574(i2c);
            using var lcd = new Lcd2004(registerSelectPin: 0,
                                    enablePin: 2,
                                    dataPins: new int[] { 4, 5, 6, 7 },
                                    backlightPin: 3,
                                    backlightBrightness: 0.1f,
                                    readWritePin: 1,
                                    controller: new GpioController(PinNumberingScheme.Logical, driver));


            lcd.Clear();
            lcd.SetCursorPosition(pos, line);

            string[] lines = text.Split("/n");
            if (lines.Length > 1)
            {
                lcd.SetCursorPosition(0, 0);
                lcd.Write(lines[0]);
                lcd.SetCursorPosition(0, 1);
                lcd.Write(lines[1]);


            }
            else
            {
                lcd.SetCursorPosition(pos, line);
                lcd.Write(text);
            }

        }

    }
}
