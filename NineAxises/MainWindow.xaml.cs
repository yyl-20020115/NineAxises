using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Threading;
using System.Windows.Media.Media3D;

namespace NineAxises
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int DefaultBaudRate = 115200;

        private string PortName = string.Empty;

        private SerialPort Port = null;

        private delegate void UpdateData(byte[] byteData);

        private byte[] RxBuffer = new byte[4096];

        private int usRxLength = 0;

        private ManualResetEvent MRE = new ManualResetEvent(false);

        private enum PacketType
        {
            Unknown = 0,
            Gravity = 1,
            Magnet = 2,
            AngleSpeed = 3,
            AngleVale = 4,
        }
        private class Packet
        {
            public PacketType Type = PacketType.Unknown;
            public double X = 0.0;
            public double Y = 0.0;
            public double Z = 0.0;

            public double Roll
            {
                get { return this.X; }
                set { this.X = value; }
            }
            public double Pitch
            {
                get { return this.Y; }
                set { this.Y = value; }
            }
            public double Yaw
            {
                get { return this.Z; }
                set { this.Z = value; }
            }

            public double D => Math.Sqrt(X * X + Y * Y + Z * Z);

            public Packet(PacketType Type = PacketType.Unknown, double X = 0.0,double Y = 0.0, double Z = 0.0)
            {
                this.Type = Type;
                this.X = X;
                this.Y = Y;
                this.Z = Z;
            }

            public Vector3D ToVector()
            {
                return new Vector3D(this.X, this.Y, this.Z);
            }
        }

        private Packet GravityPacket = new Packet();
        private Packet MagnetPacket = new Packet();
        private Packet AngleSpeedPacket = new Packet();
        private Packet AngleValuePacket = new Packet();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            this.GravityDisplay.TitleText.Text = "Gravity Field";
            this.MagnetDisplay.TitleText.Text = "Magnet Field";
            this.AngleSpeedDisplay.TitleText.Text = "Angle Speed";
            this.AngleValueDisplay.TitleText.Text = "Angle Value";

            this.GravityDisplay.UnitAngle
                = this.MagnetDisplay.UnitAngle
                = this.AngleSpeedDisplay.UnitAngle
                = this.AngleValueDisplay.UnitAngle
                = "°";

            this.GravityDisplay.UnitValue = "g";
            this.MagnetDisplay.UnitValue = "mG";
            this.AngleSpeedDisplay.UnitValue = "°/s";
            this.AngleValueDisplay.UnitValue = "°";
            this.AngleValueDisplay.DValueText.Visibility = Visibility.Hidden;
            this.AngleValueDisplay.AValueText.Visibility = Visibility.Hidden;
            this.AngleValueDisplay.PValueText.Visibility = Visibility.Hidden;
            this.GravityDisplay.RedirectPointerTo(new Vector3D());
            this.MagnetDisplay.RedirectPointerTo(new Vector3D());
            this.AngleSpeedDisplay.RedirectPointerTo(new Vector3D());
            this.AngleValueDisplay.RedirectPointerTo(new Vector3D());

            this.RebuildMainMenu();
        }

        protected override void OnClosed(EventArgs e)
        {
            this.CloseComPort();
            base.OnClosed(e);
        }

        private void SelectComPort(MenuItem m)
        {
            if (m!=null && m.Header.ToString() != this.PortName)
            {
                this.CloseComPort();
                try
                {
                    this.Port = new SerialPort(this.PortName = m.Header.ToString(), 
                        DefaultBaudRate);
                    this.Port.DataReceived += Port_DataReceived;
                    this.Port.Open();

                    if (m != null)
                    {
                        m.IsChecked = true;
                    }
                }
                catch
                {
                    if (this.Port != null)
                    {
                        this.Port.Dispose();
                    }
                    this.Port = null;

                    if (m != null)
                    {
                        m.IsChecked = false;
                    }
                     
                }
            }
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buffer = new byte[4096];

            try
            {
                int usLength = this.Port.Read(RxBuffer, usRxLength, buffer.Length);

                usRxLength += usLength;

                while (usRxLength >= 11)
                {
                    UpdateData Update = new UpdateData(DecodeDataAndUpdate);

                    RxBuffer.CopyTo(buffer, 0);

                    if (!((buffer[0] == 0x55) & ((buffer[1] & 0x50) == 0x50)))
                    {
                        for (int i = 1; i < usRxLength; i++)
                        {
                            RxBuffer[i - 1] = RxBuffer[i];
                        }
                        usRxLength--;
                        continue;
                    }
                    if (((buffer[0] + buffer[1] + buffer[2] + buffer[3] + buffer[4] + buffer[5] + buffer[6] + buffer[7] + buffer[8] + buffer[9]) & 0xff)
                        == buffer[10])
                    {
                        Dispatcher.Invoke(Update, buffer);
                    }
                    for (int i = 11; i < usRxLength; i++)
                    {
                        RxBuffer[i - 11] = RxBuffer[i];
                    }
                    usRxLength -= 11;
                }
            }
            finally
            {
                this.MRE.Set();
            }
        }

        private void UpdateDisplays()
        {
            this.GravityDisplay.RedirectPointerTo(this.GravityPacket.ToVector());
            this.MagnetDisplay.RedirectPointerTo(this.MagnetPacket.ToVector());
            this.AngleSpeedDisplay.RedirectPointerTo(this.AngleSpeedPacket.ToVector());
            this.AngleValueDisplay.RotatePointerTo(this.AngleSpeedPacket.ToVector());
        }

        private void DecodeDataAndUpdate(byte[] buffer)
        {
            PacketType PT = PacketType.Unknown;
            double[] Data = new double[4];

            Data[0] = BitConverter.ToInt16(buffer, 2);
            Data[1] = BitConverter.ToInt16(buffer, 4);
            Data[2] = BitConverter.ToInt16(buffer, 6);
            Data[3] = BitConverter.ToInt16(buffer, 8);
           
            switch (buffer[1])
            {
                case 0x50:
                    //ChipTime
                    break;
                case 0x51:
                    //Gravity
                    this.GravityPacket.X = Data[0] / 32768.0 * 16.0;
                    this.GravityPacket.Y = Data[1] / 32768.0 * 16.0;
                    this.GravityPacket.Z = Data[2] / 32768.0 * 16.0;
                    PT = PacketType.Gravity;
                    break;
                case 0x52:
                    //AngleSpeed
                    this.AngleSpeedPacket.X = Data[0] / 32768.0 * 2000.0;
                    this.AngleSpeedPacket.Y = Data[1] / 32768.0 * 2000.0;
                    this.AngleSpeedPacket.Z = Data[2] / 32768.0 * 2000.0;
                    PT = PacketType.AngleSpeed;
                    break;
                case 0x53:
                    //AngleValue
                    this.AngleValuePacket.Roll = Data[0] / 32768.0 * 180.0;
                    this.AngleValuePacket.Pitch = Data[1] / 32768.0 * 180.0;
                    this.AngleValuePacket.Yaw = Data[2] / 32768.0 * 180.0;
                    PT = PacketType.AngleVale;

                    break;
                case 0x54:
                    //Magnet
                    this.MagnetPacket.X = Data[0];
                    this.MagnetPacket.Y = Data[1];
                    this.MagnetPacket.Z = Data[2];
                    PT = PacketType.Magnet;
                    break;
                case 0x55:
                    //PortVoltage
                    //PortVoltage[0] = Data[0];
                    //PortVoltage[1] = Data[1];
                    //PortVoltage[2] = Data[2];
                    //PortVoltage[3] = Data[3];
                    break;
                case 0x56:
                    //Pressure = BitConverter.ToInt32(byteTemp, 2);
                    //Altitude = (double)BitConverter.ToInt32(byteTemp, 6) / 100.0;
                    break;
                case 0x57:
                    //Longitude = BitConverter.ToInt32(byteTemp, 2);
                    //Latitude = BitConverter.ToInt32(byteTemp, 6);
                    break;
                case 0x58:
                    //GPSHeight = (double)BitConverter.ToInt16(byteTemp, 2) / 10.0;
                    //GPSYaw = (double)BitConverter.ToInt16(byteTemp, 4) / 10.0;
                    //GroundVelocity = BitConverter.ToInt16(byteTemp, 6) / 1e3;
                    break;
                default:
                    break;
            }

            if(PT!= PacketType.Unknown)
            {
                this.UpdateDisplays();
            }
        }


        private void CloseComPort()
        {
            if (this.Port != null)
            {
                if (this.Port.IsOpen)
                {
                    this.Port.Close();
                }

                this.MRE.WaitOne();

                this.Port.Dispose();
                this.Port = null;
            }
            this.PortName = string.Empty;
        }

        private void ClearMainMenuItemChecks()
        {
            foreach (object n in this.MainMenu.Items)
            {
                if (n is MenuItem nm)
                {
                    nm.IsChecked = false;
                }
            }
        }
        private void RebuildMainMenu()
        {
            var ExitMenuItem = new MenuItem() { Header = "Exit" };
            ExitMenuItem.Click += ExitMenuItem_Click;
            var RefreshMenuItem = new MenuItem() { Header = "Refresh" };
            RefreshMenuItem.Click += RefreshMenuItem_Click;
            var CloseMenuItem = new MenuItem() { Header = "Close" };
            CloseMenuItem.Click += CloseMenuItem_Click;
            this.MainMenu.Items.Clear();
            List<string> Names = new List<string>(SerialPort.GetPortNames());

            Names.Sort();

            foreach (string PN in Names)
            {
                var PortMenuItem = new MenuItem() { Header = PN };

                PortMenuItem.Click += PortMenuItem_Click;

                if (PN == this.PortName)
                {
                    PortMenuItem.IsChecked = true;
                }

                this.MainMenu.Items.Add(PortMenuItem);
            }

            this.MainMenu.Items.Add(new Separator());
            this.MainMenu.Items.Add(RefreshMenuItem);
            this.MainMenu.Items.Add(CloseMenuItem);
            this.MainMenu.Items.Add(new Separator());
            this.MainMenu.Items.Add(ExitMenuItem);
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.CloseComPort();
            this.ClearMainMenuItemChecks();
        }

        private void PortMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem m)
            {
                if(m.Header.ToString() != this.PortName)
                {
                    //change port
                    this.SelectComPort(m);

                    this.ClearMainMenuItemChecks();

                    m.IsChecked = true;
                }
            }
        }

        private void RefreshMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.RebuildMainMenu();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
