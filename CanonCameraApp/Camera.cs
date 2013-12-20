using EDSDKLib;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace CanonCameraApp
{
    public class Camera
    {
        public const int SLEEP_PERIOD = 100;
        public const String PROPERTY_UNAVAILABLE = "Property Unavailable";
        public const int VIDEO_FORMAT = 45317;

        private IntPtr cameraDevice;
        private String name;
        private String portName;
        private String serialNumber;
        private String firmwareVersion;
        private bool isSessionOpened = false;

        private CapturedItem capturedItem = new CapturedItem();
        private String pictureSaveDirectory;

        private CameraController controller;

        private EDSDK.EdsStateEventHandler edsStateEventHandler;
        private EDSDK.EdsPropertyEventHandler edsPropertyEventHandler;
        private EDSDK.EdsObjectEventHandler edsObjectEventHandler;
        private EDSDK.EdsProgressCallback edsProgressCallbackHandler;

        public Camera(IntPtr cameraDevice, CameraController controller)
        {
            this.cameraDevice = cameraDevice;
            this.controller = controller;
        }

        public CameraController Controller
        {
            get { return this.controller; }
        }
        public IntPtr CameraDevice
        {
            get { return this.cameraDevice; }
            set { this.cameraDevice = value; }
        }

        public String Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public String PortName
        {
            get { return this.portName; }
            set { this.portName = value; }
        }

        public String SerialNumber
        {
            get { return this.serialNumber; }
            set { this.serialNumber = value; }
        }

        public String FirmwareVersion
        {
            get { return this.firmwareVersion; }
            set { this.firmwareVersion = value; }
        }

        public bool SessionOpened
        {
            get { return this.isSessionOpened; }
            set { this.isSessionOpened = value; }
        }

        public CapturedItem CapturedItem
        {
            get { return this.capturedItem; }
            set { this.capturedItem = value; }
        }

        public String PictureSaveDirectory
        {
            get { return this.pictureSaveDirectory; }
            set { this.pictureSaveDirectory = value; }
        }

        public EDSDK.EdsStateEventHandler EdsStateEventHandler
        {
            get { return this.edsStateEventHandler; }
            set { this.edsStateEventHandler = value; }
        }

        public EDSDK.EdsPropertyEventHandler EdsPropertyEventHandler
        {
            get { return this.edsPropertyEventHandler; }
            set { this.edsPropertyEventHandler = value; }
        }

        public EDSDK.EdsObjectEventHandler EdsObjectEventHandler
        {
            get { return this.edsObjectEventHandler; }
            set { this.edsObjectEventHandler = value; }
        }

        public EDSDK.EdsProgressCallback EdsProgressCallbackHandler
        {
            get { return this.edsProgressCallbackHandler; }
            set { this.edsProgressCallbackHandler = value; }
        }

        public void RegisterEventHandlers()
        {
            edsStateEventHandler = new EDSDK.EdsStateEventHandler(StateEventHandler);
            uint error = EDSDK.EdsSetCameraStateEventHandler(cameraDevice, EDSDK.StateEvent_All, edsStateEventHandler, new IntPtr(0));
            if (error != EDSDK.EDS_ERR_OK)
            {
                Console.WriteLine("Error registering state event handler");
            }

            edsPropertyEventHandler = new EDSDK.EdsPropertyEventHandler(PropertyEventHandler);
            error = EDSDK.EdsSetPropertyEventHandler(cameraDevice, EDSDK.PropertyEvent_All, edsPropertyEventHandler, cameraDevice);
            if (EDSDK.EDS_ERR_OK != error)
            {
                throw new Exception(String.Format("Unable to register property events with the camera: {0}", error));
            }

            edsObjectEventHandler = new EDSDK.EdsObjectEventHandler(ObjectEventHandler);
            error = EDSDK.EdsSetObjectEventHandler(cameraDevice, EDSDK.ObjectEvent_All, edsObjectEventHandler, IntPtr.Zero);
            if (EDSDK.EDS_ERR_OK != error)
            {
                throw new Exception(String.Format("Unable to register object events with the camera: {0}", error));
            }

            //edsProgressCallbackHandler = new EDSDK.EdsProgressCallback(ProgressEventHandler);
            //error = EDSDK.EdsSetProgressCallback(cameraDevice, edsProgressCallbackHandler, EDSDK.EdsProgressOption.Periodically, IntPtr.Zero);
            //if (EDSDK.EDS_ERR_OK != error)
            //{
            //    throw new Exception(String.Format("Unable to register progress callback events with the camera: {0}", error));
            //}
        }

        public uint ProgressEventHandler(uint inPercent, IntPtr inContext, ref bool outCancel)
        {
            // Not implemented
            return 0x0;
        }

        public uint StateEventHandler(uint inEvent, uint inParameter, IntPtr inContext)
        {
            switch (inEvent)
            {
                case EDSDK.StateEvent_JobStatusChanged:
                    Console.WriteLine(String.Format("There are objects waiting to be transferred.  Job status {0}", inParameter));
                    break;

                case EDSDK.StateEvent_ShutDownTimerUpdate:
                    if (inParameter != 0)
                        Console.WriteLine(String.Format("shutdown timer update: {0}", inParameter));
                    break;

                default:
                    Console.WriteLine(String.Format("StateEventHandler: event {0}, parameter {1}", inEvent, inParameter));
                    break;
            }
            return 0;
        }

        public uint PropertyEventHandler(uint inEvent, uint inPropertyID, uint inParameter, IntPtr inContext)
        {
            switch (inEvent)
            {
                default:
                    Console.WriteLine(String.Format("PropertyEventHandler: event {0}, property {1}, parameter {2}, ",
                        inEvent.ToString("X"), inPropertyID.ToString("X"), inParameter.ToString("X")));
                    break;
            }

            return 0x0;
        }

        public uint ObjectEventHandler(uint inEvent, IntPtr inRef, IntPtr inContext)
        {
            switch (inEvent)
            {
                case EDSDK.ObjectEvent_DirItemCreated:
                    Console.WriteLine("Directory Item Created");
                    GetCapturedItem(inRef);
                    SaveItem();
                    break;
                case EDSDK.ObjectEvent_DirItemRequestTransfer:
                    Console.WriteLine("Directory Item Requested Transfer");
                    break;
                default:
                    Console.WriteLine(String.Format("ObjectEventHandler: event {0}, ref {1}", inEvent.ToString("X"), inRef.ToString()));
                    break;
            }

            return 0x0;
        }

        public void TakePhotograph()
        {
            uint error = EDSDK.EdsSendCommand(cameraDevice, EDSDK.CameraCommand_TakePicture, 0);

            switch (error)
            {
                case EDSDK.EDS_ERR_OK:
                    Console.WriteLine("Took photograph..." + DateTime.UtcNow.ToString("HH:mm:ss.fff"));
                    break;
                case EDSDK.EDS_ERR_DEVICE_BUSY:
                    Console.WriteLine("Device busy, sleeping for " + Camera.SLEEP_PERIOD + "ms...");
                    System.Threading.Thread.Sleep(Camera.SLEEP_PERIOD);
                    TakePhotograph();
                    break;
                default:
                    throw new Exception(String.Format("Unable to take photograph: {0}", error.ToString()));
            }
        }

        public void GetCapturedItem(IntPtr directoryItem)
        {
            uint error = EDSDK.EDS_ERR_OK;
            IntPtr stream = IntPtr.Zero;

            EDSDK.EdsDirectoryItemInfo dirItemInfo;

            error = EDSDK.EdsGetDirectoryItemInfo(directoryItem, out dirItemInfo);

            if (error != EDSDK.EDS_ERR_OK)
            {
                throw new Exception(String.Format("Unable to get captured item info: {0}", error));
            }

            if (error == EDSDK.EDS_ERR_OK)
            {
                error = EDSDK.EdsCreateMemoryStream((uint)dirItemInfo.Size, out stream);
            }

            if (error == EDSDK.EDS_ERR_OK)
            {
                error = EDSDK.EdsDownload(directoryItem, (uint)dirItemInfo.Size, stream);
            }

            error = getImageInfo(stream);

            if (error == EDSDK.EDS_ERR_OK)
            {
                byte[] buffer = new byte[(int)dirItemInfo.Size];

                GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                IntPtr address = gcHandle.AddrOfPinnedObject();

                IntPtr streamPtr = IntPtr.Zero;

                error = EDSDK.EdsGetPointer(stream, out streamPtr);

                if (error != EDSDK.EDS_ERR_OK)
                {
                    throw new Exception(String.Format("Unable to get resultant image: {0}", error));
                }

                try
                {
                    Marshal.Copy(streamPtr, buffer, 0, (int)dirItemInfo.Size);

                    CapturedItem.Item = buffer;
                    CapturedItem.Name = dirItemInfo.szFileName;
                    CapturedItem.Size = (long)dirItemInfo.Size;
                    CapturedItem.IsFolder = Convert.ToBoolean(dirItemInfo.isFolder);
                }
                catch (AccessViolationException ave)
                {
                    throw new Exception(String.Format("Error copying unmanaged stream to managed byte[]: {0}", ave));
                }
                finally
                {
                    gcHandle.Free();
                    EDSDK.EdsRelease(stream);
                    EDSDK.EdsRelease(streamPtr);
                }
            }
            else
            {
                throw new Exception(String.Format("Unable to get resultant image: {0}", error));
            }
        }

        private uint getImageInfo(IntPtr stream)
        {
            IntPtr imageRef = IntPtr.Zero;
            uint error = EDSDK.EdsCreateImageRef(stream, out imageRef);

            if (error == EDSDK.EDS_ERR_OK)
            {
                EDSDK.EdsImageInfo info;
                error = EDSDK.EdsGetImageInfo(imageRef, EDSDK.EdsImageSource.FullView, out info);

                if (error == EDSDK.EDS_ERR_OK)
                {
                    CapturedItem.Height = (int)info.Height;
                    CapturedItem.Width = (int)info.Width;

                    EDSDK.EdsRelease(imageRef);
                }
            }

            return error;
        }

        public void SaveItem()
        {
            MemoryStream ms = new MemoryStream(CapturedItem.Item);
            String name = pictureSaveDirectory + "\\"+ Name + "-" + CapturedItem.Name;

            using (FileStream file = new FileStream(name, FileMode.Create, System.IO.FileAccess.Write))
            {
                byte[] bytes = new byte[ms.Length];
                ms.Read(bytes, 0, (int)ms.Length);
                file.Write(bytes, 0, bytes.Length);
                ms.Close();
            }
        } 
    }
}
