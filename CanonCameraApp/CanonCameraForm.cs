using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CanonCameraApp
{
    public partial class CanonCameraForm : Form
    {
        private const String START_LIVE_VIEW = "Start Live View";
        private const String TAKE_PHOTOGRAPH = "Take Photograph";
        private const String NO_CAMERAS = "No Cameras Detected";
        private const String CAM_STAT_TITLE = "Camera Status";
        private const String NEW_LINE = "\n";
        private const String CAM_PROP_SEPERATOR = ",";
        private const String CAM_STAT_BULLET = "-";
        private const String CAM_NAME = "Name: ";
        private const String CAM_PORT = "Port: ";

        private CameraController controller = new CameraController();
        private List<String> cameraModes = new List<String>();

        private CancellationTokenSource ctsTakePhoto = null;
        private CancellationTokenSource ctsCloseCamera = null;
        private Boolean liveView = false;

        public CanonCameraForm()
        {
            InitializeComponent();

            UsbNotification.RegisterUsbDeviceNotification(this.Handle);

            controller.InitializeSdk();
            controller.InitializeCameras();

            saveDirectory.Text = controller.PictureSaveDirectory;

            StartLiveview();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == UsbNotification.WmDevicechange)
            {
                switch ((int)m.WParam)
                {
                    case UsbNotification.DbtDeviceremovecomplete:
                        controller.UpdateCameras();
                        StartLiveview();
                        break;
                    case UsbNotification.DbtDevicearrival:
                        controller.UpdateCameras();
                        StartLiveview();
                        break;
                }
            }
        }
        
        private async void StartLiveview()
        {
            if (controller.Cameras.Count >= 1){
                liveView = true;
                button1.Text = TAKE_PHOTOGRAPH;

                ctsTakePhoto = new CancellationTokenSource();
                ctsCloseCamera = new CancellationTokenSource();
                
                var progress = new Progress<Bitmap>(image => pictureBox.Image = image);
                await Task.Factory.StartNew(() => LiveView.DownloadLiveView(progress, ctsTakePhoto.Token, ctsCloseCamera.Token, controller.Cameras.First()));
            }
        }

        public void StopLiveView()
        {
            liveView = false;
            button1.Text = START_LIVE_VIEW;

            ctsTakePhoto.Cancel();
        }
              
        private void BrowseSaveDirectory(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                var pictureSaveDirectory = folderBrowserDialog1.SelectedPath;

                saveDirectory.Text = pictureSaveDirectory;
                controller.UpdateImageSaveDirectory(pictureSaveDirectory);
            }
        }

        private void TakeCameraAction(object sender, EventArgs e)
        {
            if (button1.Text == START_LIVE_VIEW)
            {
                StartLiveview();
            }
            else
            {
                StopLiveView();
            }
        }

        public void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (liveView)
            {
                ctsCloseCamera.Cancel();
            }
            controller.ReleaseCameras();
            controller.TerminateSDK();
        }
    }
}
