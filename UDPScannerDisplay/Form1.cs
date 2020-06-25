using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace UDPScannerDisplay
{
    public partial class fghj : Form
    {
        SolidBrush myBrush = new SolidBrush(Color.Black);
        Graphics g = null;


        public delegate void AddListItem();
        public AddListItem myDelegate;
        public void AddListItemMethod()
        {
            panel1.Refresh();
        }

        public fghj()
        {
            InitializeComponent();
            myDelegate = new AddListItem(AddListItemMethod);
            MyUDPServer.Start(this);
        }

        private void Panel1_Paint(object sender, PaintEventArgs e)
        {
            g = panel1.CreateGraphics();
            drawLine();
        }

        private void Panel1_MouseClick(object sender, MouseEventArgs e)
        {
            Text = "UDPScannerDisplay (x: "+e.X+" y: "+e.Y+")";
        }

        private void Fghj_FormClosing(object sender, FormClosingEventArgs e)
        {
            MyUDPServer.Stop();
        }

        private void drawLine()
        {
            int scan_data_size = MyUDPServer.scan_data.Count;
            foreach (Point p in MyUDPServer.scan_data)
            {
                g.FillRectangle(myBrush,  p.X,  p.Y, 5, 5); // 5x5 just so it is easier to see
            }
            MyUDPServer.scan_data.RemoveRange(0, scan_data_size);
        }
    }

    public class MyUDPServer
    {
        static string msg;
        static byte[] data;
        static UdpClient client;
        static UdpClient server;
        static int port = 12322;
        public static List<Point> scan_data;
        static bool bRunning = true;
        public static void Stop()
        {
            bRunning = false;
        }
        public static void DoWork()
        {
            Thread t = Thread.CurrentThread; server = new UdpClient(port);
            Console.WriteLine("UDP Server");
            System.Drawing.Size sd = myform.Size;
            byte[] data = new byte[4096];
            while (bRunning)
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, port);

                data = server.Receive(ref sender);
                ushort magic = (ushort)((data[0] << 8) | data[1]);           // 16
                ushort packet_type = (ushort)((data[2] << 8) | data[3]);     // 16
                uint packet_size = (ushort)((data[4] << 24) | (data[5] << 16) | (data[6] << 8) | data[7]);     // 32
                ushort header_size;     // 16
                byte[] header_padding;  // 16 - 2 bytes

                int x = 0;
                int y = 0;
                double cosOfAng = 0;
                double sinOfAng = 0;

                float distance;

                int scannum;
                int scannumbackup;

                // Payload Data
                for (int i = 12; i - 12 < packet_size - 12 - 4; i += 6)
                {

                    distance = System.BitConverter.ToSingle(data, i);
                    
                    scannum = ((i - 12) / 6);
                    scannumbackup = scannum;

                    if      (scannum <= 90)                   {}
                    else if (scannum > 90 && scannum <= 180)  scannum = 180 - scannum;
                    else if (scannum > 180 && scannum <= 270) scannum = scannum - 180;
                    else                                      scannum = 360 - scannum;

                    cosOfAng = (Math.Cos(scannum * (Math.PI / 180.0)) * distance) * 43.75;
                    sinOfAng = (Math.Sin(scannum * (Math.PI / 180.0)) * distance) * 43.75;

                    if (scannumbackup < 90)
                    {
                        x = (int)((sd.Width / 2) + cosOfAng);
                        y = (int)((sd.Width / 2) - sinOfAng);
                    }
                    else if (scannumbackup >= 90 && scannumbackup < 180)
                    {
                        x = (int)((sd.Width / 2) - cosOfAng);
                        y = (int)((sd.Width / 2) - sinOfAng);
                    }
                    else if (scannumbackup >= 180 && scannumbackup < 270)
                    {
                        x = (int)((sd.Width / 2) - cosOfAng);
                        y = (int)((sd.Width / 2) + sinOfAng);
                    }
                    else
                    {
                        x = (int)((sd.Width / 2) + cosOfAng);
                        y = (int)((sd.Width / 2) + sinOfAng);
                    }

                    scan_data.Add(new Point(x, y));

                }
                try
                {
                    myform.Invoke(myform.myDelegate);
                }
                catch
                {

                }

            }
        }
        static fghj myform;
        public static void Start(fghj myfrm)
        {
            scan_data = new List<Point>();
            myform = myfrm;
            Thread thread1 = new Thread(DoWork);
            thread1.Start();
        }
    }
}
