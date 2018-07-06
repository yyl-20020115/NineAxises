using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace NineAxises
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int DefaultBaudRate = 115200;
        private const int DefaultUIDelayTimeMs = 25;
        private const int MessageLength = 11;
        private const int RxBufferLength = MessageLength<<1;

        private string PortName = string.Empty;

        private SerialPort Port = null;

        private delegate void UpdateData(byte[] byteData);

        private byte[] RxBuffer = new byte[RxBufferLength];

        private int usRxLength = 0;

        private bool closing = false;
        private bool closed = false;

        private DispatcherTimer timer = null;
        private UpdateData updateData = null;

        private long TotalReadLength = 0L;
        private long LastTotalReadLength = 0L;

        private string DefaultTitle = string.Empty;
        public MainWindow()
        {
            InitializeComponent();
            this.timer = new DispatcherTimer(TimeSpan.FromSeconds(1.0),DispatcherPriority.Normal, new EventHandler(this.OnTimer),this.Dispatcher);
            this.updateData = new UpdateData(this.DecodeDataAndUpdate);

        }

        //440B/s
        private void OnTimer(object sender,EventArgs e)
        {
            
            long DeltaReadLength = this.TotalReadLength - this.LastTotalReadLength;

            this.Title = this.DefaultTitle + string.Format(" Speed:{0} B/s", DeltaReadLength);

            this.LastTotalReadLength = this.TotalReadLength;

        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            
            this.Title = this.DefaultTitle = "九轴传感器";
            this.GravityDisplay.AText = "重力场强度";
            this.GravityDisplay.TText = "水平方向角";
            this.GravityDisplay.DText = "垂直方向角";
            this.MagnetDisplay.AText = "磁场强度  ";
            this.MagnetDisplay.TText = "水平方向角";
            this.MagnetDisplay.DText = "垂直方向角";

            this.AngleSpeedDisplay.AText = "角速度    ";
            this.AngleSpeedDisplay.TText = "水平方向角";
            this.AngleSpeedDisplay.DText = "垂直方向角";

            this.GravityDisplay.Title = "重力场";
            this.MagnetDisplay.Title = "磁场";
            this.AngleSpeedDisplay.Title = "角速度";
            this.AngleValueDisplay.Title = "方位角";

            this.GravityDisplay.ScaleFactor = 1.0;
            this.MagnetDisplay.ScaleFactor = 1.0 / 1000.0;
            this.AngleSpeedDisplay.ScaleFactor = double.NaN;

            this.GravityDisplay.UnitAngle
                = this.MagnetDisplay.UnitAngle
                = this.AngleSpeedDisplay.UnitAngle
                = this.AngleValueDisplay.UnitAngle
                = "°";

            this.GravityDisplay.UnitValue = "g";
            this.MagnetDisplay.UnitValue = "uT";
            this.AngleSpeedDisplay.UnitValue = "°/s";
            this.AngleValueDisplay.UnitValue = "°";
            this.AngleValueDisplay.AText = "滚转角";
            this.AngleValueDisplay.TText = "俯仰角";
            this.AngleValueDisplay.DText = "偏航角";

            this.AngleValueDisplay.AValueText.Visibility = Visibility.Hidden;
            this.AngleValueDisplay.TValueText.Visibility = Visibility.Hidden;
            this.AngleValueDisplay.DValueText.Visibility = Visibility.Hidden;

            this.GravityDisplay.InputMode = AxisDisplayerControl.Modes.Vector;
            this.MagnetDisplay.InputMode = AxisDisplayerControl.Modes.Vector;
            this.AngleSpeedDisplay.InputMode = AxisDisplayerControl.Modes.Vector;
            this.AngleValueDisplay.InputMode = AxisDisplayerControl.Modes.Rotate;

            this.GravityDisplay.AddValue(new Vector3D());
            this.MagnetDisplay.AddValue(new Vector3D());
            this.AngleSpeedDisplay.AddValue(new Vector3D());
            this.AngleValueDisplay.AddValue(new Vector3D());

            this.RebuildMainMenu();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.CloseComPort();
            base.OnClosing(e);
        }

        private void SelectComPort(MenuItem m)
        {
            if (m != null && m.Header.ToString() != this.PortName)
            {
                this.CloseComPort();
                try
                {
                    this.timer.Stop();
                    this.Port = new SerialPort(this.PortName = m.Header.ToString(),
                        DefaultBaudRate);
                    this.Port.ReceivedBytesThreshold = RxBufferLength;
                    this.Port.DataReceived += Port_DataReceived;
                    this.Port.Open();

                    if (m != null)
                    {
                        m.IsChecked = true;
                    }
                    this.timer.Start();
                }
                catch(Exception ex)
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
            if (!this.closing)
            {
                byte[] RmBuffer = new byte[RxBufferLength];

                try
                {
                    int DeltaLength = RxBufferLength - usRxLength;
                    int ReadLength = this.Port.BytesToRead > DeltaLength ? DeltaLength : this.Port.BytesToRead;
                    this.TotalReadLength+= ReadLength;

                    int usLength = this.Port.Read(RxBuffer, usRxLength, ReadLength);

                    usRxLength += usLength;

                    while (usRxLength >= MessageLength)
                    {
                    
                        RxBuffer.CopyTo(RmBuffer, 0);

                        if (!((RmBuffer[0] == 0x55) & ((RmBuffer[1] & 0x50) == 0x50)))
                        {
                            for (int i = 1; i < usRxLength; i++)
                            {
                                RxBuffer[i - 1] = RxBuffer[i];
                            }
                            usRxLength--;
                            continue;
                        }
                        //11Bytes:
                        //440Bytes/s / 11Bytes/sample = 40 samples/s
                        if (((RmBuffer[0] + RmBuffer[1] + RmBuffer[2] + RmBuffer[3] + RmBuffer[4] + RmBuffer[5] + RmBuffer[6] + RmBuffer[7] + RmBuffer[8] + RmBuffer[9]) & 0xff)
                            == RmBuffer[10])
                        {
                            Dispatcher.Invoke(
                                this.updateData,
                                TimeSpan.FromMilliseconds(
                                    DefaultUIDelayTimeMs
                                    ),
                                RmBuffer
                                );
                        }
                        for (int i = MessageLength; i < usRxLength; i++)
                        {
                            RxBuffer[i - MessageLength] = RxBuffer[i];
                        }
                        usRxLength -= MessageLength;
                    }
                }
                catch(Exception ex)
                {
                    string t = ex.Message;
                }
            }
            else
            {
                this.closed = true;
            }
        }


        private void DecodeDataAndUpdate(byte[] buffer)
        {
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
                    this.GravityDisplay.AddValue(
                        new Vector3D(
                            Data[0] / 32768.0 * 16.0,
                            Data[1] / 32768.0 * 16.0,
                            Data[2] / 32768.0 * 16.0
                            )
                        );
                    break;
                case 0x52:
                    //AngleSpeed
                    this.AngleSpeedDisplay.AddValue(
                        new Vector3D(
                            Data[0] / 32768.0 * 2000.0,
                            Data[1] / 32768.0 * 2000.0,
                            Data[2] / 32768.0 * 2000.0
                            )
                        );
                    break;
                case 0x53:
                    //AngleValue
                    this.AngleValueDisplay.AddValue(
                        new Vector3D(
                            Data[0] / 32768.0 * 180.0,
                            Data[1] / 32768.0 * 180.0,
                            Data[2] / 32768.0 * 180.0
                            )
                        );
                    break;
                case 0x54:
                    //Magnet
                    this.MagnetDisplay.AddValue(
                        new Vector3D(
                            Data[0] / 32768.0 * 1200.0 * 2.0,
                            Data[1] / 32768.0 * 1200.0 * 2.0,
                            Data[2] / 32768.0 * 1200.0 * 2.0
                            )
                        );
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
        }


        private void CloseComPort()
        {
            this.timer.Stop();

            if (this.Port != null)
            {
                this.closing = true;

                for(int i = 0;i<100 && (!this.closed);i++)
                {
                    Thread.Sleep(10);
                }
                try
                {
                    this.Port.Dispose();
                }
                finally
                {
                    this.Port = null;
                    this.closing = false;
                }
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

        private class ComNameComparer : StringComparer
        {
            public override int Compare(string x, string y)
            {
                return this.TryParseNumber(x) - this.TryParseNumber(y);
            }

            public override bool Equals(string x, string y)
            {
                return this.TryParseNumber(x) == this.TryParseNumber(y);
            }

            public override int GetHashCode(string obj)
            {
                return obj != null ? this.TryParseNumber(obj) : 0;
            }

            private int TryParseNumber(string name)
            {
                int n = -1;
                if(!string.IsNullOrEmpty(name)&& name.Length > 3)
                {
                    if(int.TryParse(name.Substring(3),out n))
                    {
                        
                    }
                }
                return n;
            }
        }
        private void RebuildMainMenu()
        {
            var ExitMenuItem = new MenuItem() { Header = "E_xit" };
            ExitMenuItem.Click += ExitMenuItem_Click;
            var RefreshMenuItem = new MenuItem() { Header = "_Refresh" };
            RefreshMenuItem.Click += RefreshMenuItem_Click;
            var CloseMenuItem = new MenuItem() { Header = "_Close" };
            CloseMenuItem.Click += CloseMenuItem_Click;
            this.MainMenu.Items.Clear();
            List<string> Names = new List<string>(SerialPort.GetPortNames());

            Names.Sort(new ComNameComparer());

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
            if (sender is MenuItem m)
            {
                if (m.Header.ToString() != this.PortName)
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
