using System;
using System.Windows;
using System.Runtime.InteropServices;
using Microsoft.FlightSimulator.SimConnect;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Interop;

namespace SimConnectClientExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IntPtr handle;

        private SimConnect? simConnect;
        private const int WM_USER_SIMCONNECT = 0x402;

        public MainWindow()
        {
            InitializeComponent();
            SetUi(false);
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            handle = new WindowInteropHelper(this).Handle;

            HwndSource handleSource = HwndSource.FromHwnd(handle);
            handleSource.AddHook(HandleSimConnectEvents);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }

        private void Connect()
        {
            if (simConnect == null)
            {
                try
                {
                    simConnect = new SimConnect("Managed Data Request", handle, WM_USER_SIMCONNECT, null, 0);

                    simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(OnRecvOpen);
                    simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(OnRecvQuit);
                    simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(OnRecvException);
                    simConnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(OnRecvSimobjectDataBytype);

                    simConnect.AddToDataDefinition(DEFINITIONS.RequestedData, "Title", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    simConnect.AddToDataDefinition(DEFINITIONS.RequestedData, "Plane Latitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    simConnect.AddToDataDefinition(DEFINITIONS.RequestedData, "Plane Longitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    simConnect.AddToDataDefinition(DEFINITIONS.RequestedData, "Plane Heading Degrees True", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    simConnect.AddToDataDefinition(DEFINITIONS.RequestedData, "Ground Altitude", "meters", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    simConnect.RegisterDataDefineStruct<RequestedData>(DEFINITIONS.RequestedData);
                }
                catch (Exception exception)
                {
                    AppendToErrorLog(exception.Message);
                }
            }
        }

        private void Disconnect()
        {
            if (simConnect != null)
            {
                simConnect.Dispose();
                simConnect = null;
            }
            SetUi(false);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (simConnect != null)
            {
                try
                {
                    simConnect.RequestDataOnSimObjectType(DATA_REQUESTS.REQUEST_1, DEFINITIONS.RequestedData, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                }
                catch (Exception exception)
                {
                    AppendToErrorLog(exception.Message);
                    Disconnect();
                }
            }
        }

        private IntPtr HandleSimConnectEvents(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam, ref bool isHandled)
        {
            isHandled = false;

            if (message == WM_USER_SIMCONNECT)
            {
                if (simConnect != null)
                {
                    simConnect.ReceiveMessage();
                    isHandled = true;
                }
            }

            return IntPtr.Zero;
        }

        void Window_Closing(object sender, CancelEventArgs e)
        {
            Disconnect();
        }

        private void SetUi(bool connected)
        {
            lblStatus.Content = connected ? "Connected to sim" : "Not connected to sim";
            btnDisconnect.IsEnabled = connected;
            btnConnect.IsEnabled = !connected;
            if (!connected)
            {
                txtPlane.Text = string.Empty;
                txtLatitude.Text = string.Empty;
                txtLongitude.Text = string.Empty;
                txtTrueHeading.Text = string.Empty;
                txtGroundAltitude.Text = string.Empty;
            }
        }

        private void AppendToErrorLog(string message)
        {
            if (string.IsNullOrEmpty(txtErrorLog.Text))
            {
                txtErrorLog.Text = message;
            }
            else
            {
                txtErrorLog.Text = txtErrorLog.Text + "\n" + message;
            }
        }

        #region Handler

        private void OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            if (data.dwRequestID == 0)
            {
                RequestedData requestedData = (RequestedData)data.dwData[0];

                txtPlane.Text = requestedData.title;
                txtLatitude.Text = requestedData.latitude.ToString();
                txtLongitude.Text = requestedData.longitude.ToString();
                txtTrueHeading.Text = requestedData.trueheading.ToString();
                txtGroundAltitude.Text = requestedData.groundaltitude.ToString();
            }
            else
            {
                AppendToErrorLog("Unknown request ID: " + data.dwRequestID);
            }
        }

        private void OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            AppendToErrorLog("Exception received: " + data.dwException);
        }

        private void OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            SetUi(true);
        }

        private void OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            AppendToErrorLog("Sim has exited");
            Disconnect();
        }

        #endregion

        #region structs and enums

        private enum DEFINITIONS
        {
            RequestedData
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RequestedData
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x100)]
            public string title;
            public double latitude;
            public double longitude;
            public double trueheading;
            public double groundaltitude;
        }

        private enum DATA_REQUESTS
        {
            REQUEST_1
        }

        private enum EVENTS
        {
            KEY_CLOCK_HOURS_INC,
            KEY_CLOCK_HOURS_DEC,
        }

        private enum NOTIFICATION_GROUPS
        {
            GROUP0,
        }

        #endregion

    }
}
