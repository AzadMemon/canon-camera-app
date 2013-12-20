using EDSDKLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CanonCameraApp
{
    /**
     * The CameraController class has the ability to control multiple cameras. Currently the CameraForm only has the ability
     * to support one camera at a time. The code in the CameraForm can be modified to support multiple cameras, using the 
     * code found in this class.
     * */
    public class CameraController
    {
        private List<Camera> cameras = new List<Camera>();
        private bool sdkLoaded = false;
        private String pictureSaveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        private String videoSaveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        public List<Camera> Cameras
        {
            get { return this.cameras; }
            set { this.cameras = value; }
        }

        public String PictureSaveDirectory
        {
            get { return this.pictureSaveDirectory; }
            set { this.pictureSaveDirectory = value; }
        }

        public String VideoSaveDirectory
        {
            get { return this.videoSaveDirectory; }
            set { this.videoSaveDirectory = value; }
        }

        public bool IsSdkLoaded
        {
            get { return this.sdkLoaded; }
            set { this.sdkLoaded = value; }
        }

        public void InitializeSdk()
        {
            if(!IsSdkLoaded)
            {
                uint error = EDSDK.EdsInitializeSDK();
                if (error != 0)
                {
                    throw new Exception(String.Format("Unable to initialize SDK: {0}", error));
                }

                IsSdkLoaded = true;
            }
        }

        public void InitializeCameras()
        {
            try
            {
                Cameras = GetCameras();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public List<Camera> GetCameras()
        {
            List<Camera> cameras = new List<Camera>();

            IntPtr cameraList = new IntPtr();

            uint error = EDSDK.EdsGetCameraList(out cameraList);
            if (EDSDK.EDS_ERR_OK != error)
            {
                Debug.WriteLine(String.Format("Unable to get camera list: {0}", error));
                return Cameras;
            }
            else
            {
                int cameraCount = 0;
                error = EDSDK.EdsGetChildCount(cameraList, out cameraCount);

                if (EDSDK.EDS_ERR_OK != error)
                {
                    Debug.WriteLine(String.Format("Unable to get camera count: {0}", error));
                    return Cameras;
                }
                else
                {
                    if (cameraCount <= 0)
                    {
                        Debug.WriteLine(String.Format("No camera was detected to be connected to the host."));
                        return Cameras;
                    }

                    for (int i = 0; i < cameraCount; i++)
                    {
                        IntPtr cameraDev = new IntPtr(i);

                        error = EDSDK.EdsGetChildAtIndex(cameraList, i, out cameraDev);
                        
                        if (EDSDK.EDS_ERR_OK != error)
                        {
                            Debug.WriteLine(String.Format("Unable to get camera at index ({0}): {1}", i, error));
                            continue;
                        }

                        EDSDK.EdsDeviceInfo deviceInfo;
                        error = EDSDK.EdsGetDeviceInfo(cameraDev, out deviceInfo);

                        if (EDSDK.EDS_ERR_OK != error)
                        {
                            Debug.WriteLine(String.Format("Unable to get device information at index ({0}): {1}", i, error));
                            continue;
                        }

                        error = EDSDK.EdsOpenSession(cameraDev);
                        if (EDSDK.EDS_ERR_OK != error)
                        {
                            if(error == EDSDK.EDS_ERR_DEVICE_NOT_FOUND)
                            {
                                    Debug.WriteLine(String.Format("Unable to open session with camera at index ({0}) [{1}] because it was not found!", i, deviceInfo.szDeviceDescription));
                            }
                            else
                            {
                                    Debug.WriteLine(String.Format("Unable to open session with camera at index ({0}) [{1}] : {2}", i, deviceInfo.szDeviceDescription, error));
                                    //Try closing and reponeing session
                                    EDSDK.EdsCloseSession(cameraDev);
                                    EDSDK.EdsOpenSession(cameraDev);
                            }
                        }

                        Camera camera = new Camera(cameraDev, this);

                        camera.Name = deviceInfo.szDeviceDescription;
                        camera.PortName = deviceInfo.szPortName;
                        camera.RegisterEventHandlers();
                        //  Firmware Version
                        EDSDK.EdsTime firmwareDate;
                        error = EDSDK.EdsGetPropertyData(cameraDev, EDSDK.PropID_FirmwareVersion, 0, out firmwareDate);
                        if (EDSDK.EDS_ERR_OK == error)
                        {
                            camera.FirmwareVersion = firmwareDate.ToString();
                        }
                        else
                        {
                            camera.FirmwareVersion = Camera.PROPERTY_UNAVAILABLE;
                        }

                        // Serial Number
                        uint serialNumber;
                        error = EDSDK.EdsGetPropertyData(cameraDev, EDSDK.PropID_BodyIDEx, 0, out serialNumber);
                        if (EDSDK.EDS_ERR_OK == error)
                        {
                            camera.SerialNumber = serialNumber.ToString();
                        }
                        else
                        {
                            camera.SerialNumber = Camera.PROPERTY_UNAVAILABLE;
                        }
                        camera.SessionOpened = true;
                        camera.PictureSaveDirectory = pictureSaveDirectory;

                        cameras.Add(camera);
                    }
                }
            }

            return cameras;
        }

        public void UpdateImageSaveDirectory(String pictureSaveDirectory)
        {
            foreach (Camera camera in Cameras)
            {
                camera.PictureSaveDirectory = pictureSaveDirectory;
            }
        }

        // Initialize the cameras
        public bool UpdateCameras()
        {
            InitializeSdk();
            InitializeCameras();

            return (cameras.Count > 0);
        }

        public void ReleaseCameras()
        {
            foreach (Camera camera in Cameras)
            {
                if (camera.SessionOpened && camera.CameraDevice != null)
                {
                        uint error = EDSDK.EdsCloseSession(camera.CameraDevice);
                        if (error != EDSDK.EDS_ERR_OK)
                            Debug.WriteLine(String.Format("EdsCloseSession: " + error.ToString()));

                        Console.WriteLine("Successfully closed sesssion");
                        camera.SessionOpened = false;
                }
            }

            Cameras.Clear();
            Cameras = new List<Camera>();
        }

        public void TerminateSDK()
        {
            if (IsSdkLoaded)
            {
                EDSDK.EdsTerminateSDK();
                IsSdkLoaded = false;
            }
        }
    }
}