using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
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
        private const int DefaultUIDelayTimeMs = 1;
        private const int MessageCount = 16;
        private const int MessageLength = 11;
        private const int RxBufferLength = MessageLength<<1;

        private const double GFactor = 2.0;
        private const double MFactor = 100.0;
        private const double AFactor = 200.0;
        private const double RFactor = 180.0;
        private const double PFactor = 360.0;

        private string PortName = string.Empty;

        private SerialPort Port = null;

        private delegate void UpdateData(byte[] byteData);
        private delegate void UpdateDataList(List<byte[]> byteDatas);

        private byte[] RxBuffer = new byte[RxBufferLength];

        private int usRxLength = 0;

        private bool closing = false;
        private bool closed = false;
        private bool paused = false;

        private UpdateData updateData = null;
        private UpdateDataList updateDataList = null;

        private string DefaultTitle = string.Empty;

        private MenuItem PauseMenuItem = null;

        public MainWindow()
        {
            InitializeComponent();
            this.updateData = new UpdateData(this.DecodeDataAndUpdate);
            this.updateDataList = new UpdateDataList(this.DecodeDataAndUpdateList);
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            
            this.Title = this.DefaultTitle = "九轴传感器";
            this.GravityDisplay.AngleUnit
                = this.MagnetDisplay.AngleUnit
                = this.AngleSpeedDisplay.AngleUnit
                = this.AngleValueDisplay.AngleUnit
                = "°";

            this.GravityDisplay.Title = "重力场";
            this.GravityDisplay.AText = "重力场强度";
            this.GravityDisplay.TText = "水平方向角";
            this.GravityDisplay.DText = "垂直方向角";
            this.GravityDisplay.ValueUnit = "g";
            this.GravityDisplay.ScaleFactor = 1.0;
            this.GravityDisplay.InputMode = AxisDisplayerControl.Modes.Vector;

            this.MagnetDisplay.AText = "磁场强度  ";
            this.MagnetDisplay.TText = "水平方向角";
            this.MagnetDisplay.DText = "垂直方向角";
            this.MagnetDisplay.Title = "磁场";
            this.MagnetDisplay.ValueUnit = "uT";
            this.MagnetDisplay.InputMode = AxisDisplayerControl.Modes.Vector;

            this.AngleValueDisplay.Title = "方位角";
            this.AngleValueDisplay.ValueUnit = "°";
            this.AngleValueDisplay.AText = "滚转角";
            this.AngleValueDisplay.TText = "俯仰角";
            this.AngleValueDisplay.DText = "偏航角";
            this.AngleValueDisplay.AValueText.Visibility = Visibility.Hidden;
            this.AngleValueDisplay.TValueText.Visibility = Visibility.Hidden;
            this.AngleValueDisplay.DValueText.Visibility = Visibility.Hidden;
            this.AngleValueDisplay.InputMode = AxisDisplayerControl.Modes.Rotate;

            this.AngleSpeedDisplay.AText = "角速度    ";
            this.AngleSpeedDisplay.TText = "水平方向角";
            this.AngleSpeedDisplay.DText = "垂直方向角";
            this.AngleSpeedDisplay.Title = "角速度";
            this.AngleSpeedDisplay.ValueUnit = "°/s";
            this.AngleSpeedDisplay.InputMode = AxisDisplayerControl.Modes.Vector;

            this.RebuildMainMenu();

            Thread.CurrentThread.Priority = ThreadPriority.Highest;
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
                    this.Port = new SerialPort(this.PortName = m.Header.ToString(),
                        DefaultBaudRate);
                    this.Port.ReceivedBytesThreshold = RxBufferLength;
                    this.Port.DataReceived += Port_DataReceived;
                    this.Port.Open();

                    if (m != null)
                    {
                        m.IsChecked = true;
                    }
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
        private List<byte[]> RmBuffers = new List<byte[]>();
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!this.closing)
            {
                byte[] RmBuffer = new byte[RxBufferLength];

                try
                {
                    int DeltaLength = RxBufferLength - usRxLength;
                    int ReadLength = this.Port.BytesToRead > DeltaLength ? DeltaLength : this.Port.BytesToRead;

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
                            byte[] RmBufferCopy = new byte[RmBuffer.Length];
                            Array.Copy(RmBuffer, RmBufferCopy, RmBuffer.Length);
                            RmBuffers.Add(RmBufferCopy);

                            if(RmBuffers.Count == MessageCount)
                            {
                                Dispatcher.Invoke(
                                    this.updateDataList,
                                    TimeSpan.FromMilliseconds(
                                        DefaultUIDelayTimeMs
                                        ),
                                    RmBuffers
                                    );
                                RmBuffers.Clear();
                            }
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


        private void DecodeDataAndUpdateList(List<byte[]> buffers)
        {
            foreach(var buffer in buffers)
            {
                this.DecodeDataAndUpdate(buffer);
            }
            this.GravityDisplay.Update();
            this.MagnetDisplay.Update();
            this.AngleValueDisplay.Update();
            this.AngleSpeedDisplay.Update();
        }
        private void DecodeDataAndUpdate(byte[] buffer)
        {
            if (!this.paused)
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

        }


        private void CloseComPort()
        {
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
            this.MainMenu.Header = "文件";

            var ExitMenuItem = new MenuItem() { Header = "退出" };
            ExitMenuItem.Click += ExitMenuItem_Click;
            var RefreshMenuItem = new MenuItem() { Header = "刷新" };
            RefreshMenuItem.Click += RefreshMenuItem_Click;
            var CloseMenuItem = new MenuItem() { Header = "关闭" };
            CloseMenuItem.Click += CloseMenuItem_Click;
            this.MainMenu.Items.Clear();

            this.PauseMenuItem = new MenuItem() { Header = "暂停" };
            PauseMenuItem.Click += PauseMenuItem_Click;
                
            var Names = new List<string>(SerialPort.GetPortNames());

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
            this.MainMenu.Items.Add(PauseMenuItem);
            this.MainMenu.Items.Add(new Separator());
            this.MainMenu.Items.Add(ExitMenuItem);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if(e.Key == Key.Space)
            {
                this.PauseMenuItem_Click(this.PauseMenuItem, new RoutedEventArgs());
            }
        }

        private void PauseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;

            mi.IsChecked = (this.paused = !this.paused);

            this.Title = mi.IsChecked ? this.DefaultTitle + " (暂停)" : this.DefaultTitle;
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
