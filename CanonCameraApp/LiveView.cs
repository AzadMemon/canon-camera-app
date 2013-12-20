using EDSDKLib;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace CanonCameraApp
{
    public class LiveView
    {
        public static void DownloadLiveView(IProgress<Bitmap> progress, CancellationToken ctTakePhoto, CancellationToken ctCloseCamera, Camera camera)
        {
            uint error = EDSDK.EDS_ERR_OK;

            // 1. Get the EVF Output device property
            uint device;
            error = EDSDK.EdsGetPropertyData(camera.CameraDevice, EDSDK.PropID_Evf_OutputDevice, 0, out device);

            // 2. Enable the PC as an output
            // PC live view starts by setting the PC as the output device for 
            //the live view image. 
            if (error == EDSDK.EDS_ERR_OK)
            {
                // 3. Set the EVF Output device property to the new value
                device = EDSDK.EvfOutputDevice_PC;
                error = EDSDK.EdsSetPropertyData(camera.CameraDevice, EDSDK.PropID_Evf_OutputDevice, 0, Marshal.SizeOf(device), device);
            }

            if (error == EDSDK.EDS_ERR_OK)
            {
                IntPtr stream = new IntPtr();
                IntPtr evfImage = new IntPtr();

                try
                {
                    while (true)
                    {
                        if (ctCloseCamera.IsCancellationRequested)
                        {
                            // Change LiveView device if necessary
                            if (stream != null)
                            {
                                error = EDSDK.EdsRelease(stream);
                                stream = IntPtr.Zero;
                            }
                            ctTakePhoto.ThrowIfCancellationRequested();
                        }
                        if (ctTakePhoto.IsCancellationRequested)
                        {
                            // Change LiveView device if necessary
                            if (stream != null)
                            {
                                error = EDSDK.EdsRelease(stream);
                                stream = IntPtr.Zero;
                            }

                            camera.TakePhotograph();
                            ctTakePhoto.ThrowIfCancellationRequested();
                        }

                        // 4. Create an Eds Memory Stream 
                        error = EDSDK.EdsCreateMemoryStream(0, out stream);

                        // 5. Create an Eds EVF Image ref
                        if (error == EDSDK.EDS_ERR_OK)
                            error = EDSDK.EdsCreateEvfImageRef(stream, out evfImage);

                        // 6. Download the EVF image 
                        if (error == EDSDK.EDS_ERR_OK)
                            error = EDSDK.EdsDownloadEvfImage(camera.CameraDevice, evfImage);

                        // 7. convert the evf image
                        Bitmap image = GetEvfImage(stream);
                            
                        // 8. do what you wish with the bitmap
                        if (image != null)
                        {
                            progress.Report(image);
                        }

                        // 10. Release the Evf Image ref
                        if (evfImage != null)
                        {
                            error = EDSDK.EdsRelease(stream);
                            evfImage = IntPtr.Zero;
                        }
                    }
                }
                catch (OperationCanceledException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public unsafe static Bitmap GetEvfImage(IntPtr evfStream)
        {
            uint error;
            uint length = 0;

            IntPtr jpgPointer;
            Bitmap image = null;

            error = EDSDK.EdsGetPointer(evfStream, out jpgPointer);

            if (error == EDSDK.EDS_ERR_OK)
                error = EDSDK.EdsGetLength(evfStream, out length);

            if (error == EDSDK.EDS_ERR_OK)
            {
                if (length != 0)
                {
                    UnmanagedMemoryStream ums = new UnmanagedMemoryStream
                    ((byte*)jpgPointer.ToPointer(), length, length, FileAccess.Read);
                    image = new Bitmap(ums, true);
                }
            }

            return image;
        }
    }
}
