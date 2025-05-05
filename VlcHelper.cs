using CameraKit.Core.ToolKit;
using DirectShowLib;
using LibVLCSharp.Shared;
using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace IpevoSdkDemo;

/*
 * **Note:** This class is for demonstration purposes only and is not part of the SDK functionality.
 *
 * Please note that the IPEVO SDK "only" provides access to supported features of IPEVO cameras.
 * It does not support advanced functionalities such as image acquisition, image customization, or video recording.
 * You need to decide on the image framework based on your development requirements and implement these features accordingly.
 *
 * Here, we simply use VLC to provide the most basic usage example. *
 * As a popular open-source framework, VLC offers diverse support for camera image acquisition.
 * We use the **LibVLCSharp** and **VideoLAN.LibVLC.Windows** packages to access native VLC functionality.
 * It is a .NET wrapper for VLC, providing an easy way to install and utilize VLC features.
 * https://code.videolan.org/videolan/libvlc-nuget 
 *
 * Remember... VLC is an easy but not perfected camera solution. 
 * If it does not suit your use case, you can consider using other image frameworks supported by Windows as below.
 *
 * DirectShow (C++)
 * https://learn.microsoft.com/windows/win32/directshow/directshow
 *
 * Windows Media Foundation (C++)
 * https://learn.microsoft.com/zh-tw/windows/win32/medfound/microsoft-media-foundation-sdk
 *
 * Windows Media Foundation C# Sample (C#)
 * https://github.com/OfItselfSo/Tanta
 *
 * OpenCV (C#)
 * https://github.com/shimat/opencvsharp
 *
 */

public class VlcHelper
{
    static VlcHelper()
    {
        //init for vlc library
        Core.Initialize();
    }

    private readonly int videoWidth;
    private readonly int videoHeight;
    private readonly int videoPitch;
    private readonly Action<Mat> videoCallback;

    private LibVLC libVlc;
    private MediaPlayer player;
    private Media media;
    private IntPtr buffer;

    /// <summary>
    /// VLC helper for IPEVO USB Camera
    /// </summary>
    /// <param name="wmfPath"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="fps"></param>
    /// <param name="mediaType"></param>
    /// <param name="callback"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public VlcHelper(string wmfPath, int width, int height, int fps, CameraKit.Core.MediaType mediaType, Action<Mat> callback)
    {
        var dsName = GetDsName(wmfPath);
        if (dsName == null) throw new InvalidOperationException("No match path from Direct Show");
        if (mediaType != CameraKit.Core.MediaType.Mjpeg) throw new ArgumentOutOfRangeException("only mjpg supported in VLC demo");

        videoWidth = width;
        videoHeight = height;
        videoPitch = videoWidth * 3;
        videoCallback = callback;

        libVlc = new LibVLC();
        libVlc.Log += (_, e) => Debug.WriteLine($@"[VLC][{e.Level}] {e.Module}: {e.Message}");

        player = new MediaPlayer(libVlc);

        //using direct show via VLC
        //Warning !!! The VLC library currently only supports using the camera name as a parameter for initialization.
        //Therefore, if there are two cameras with the same name, it will only access the first one.
        var deviceName = @"dshow://";
        string[] options =
        [
            $":dshow-vdev={dsName}",
            ":dshow-adev=none",
            $":dshow-size={width}x{height}", 
            $":dshow-fps={fps}"
        ];
        var media = new Media(libVlc, deviceName, FromType.FromLocation, options);
 
        player.SetVideoFormat("RV24", (uint)width, (uint)height, (uint)videoPitch);
        player.SetVideoCallbacks(LockCallback, UnlockCallback, DisplayCallback);
        player.Play(media);
    }

    /// <summary>
    /// VLC helper for IPEVO WiFi Camera
    /// </summary>
    /// <param name="rtspUrl"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="callback"></param>
    public VlcHelper(string rtspUrl, int width, int height, Action<Mat> callback)
    {
        videoWidth = width;
        videoHeight = height;
        videoPitch = videoWidth * 3;
        videoCallback = callback;

        libVlc = new LibVLC();
        libVlc.Log += (_, e) => Debug.WriteLine($@"[VLC][{e.Level}] {e.Module}: {e.Message}");

        player = new MediaPlayer(libVlc);
        media = new Media(libVlc, rtspUrl, FromType.FromLocation);

        player.SetVideoFormat("RV24", (uint)width, (uint)height, (uint)videoPitch);
        player.SetVideoCallbacks(LockCallback, UnlockCallback, DisplayCallback);
        player.Play(media);
    }

    #region VLC Callback

    private IntPtr LockCallback(IntPtr opaque, IntPtr planes)
    {
        if (buffer == IntPtr.Zero) buffer = Marshal.AllocHGlobal(videoPitch * videoHeight);
        Marshal.WriteIntPtr(planes, buffer);
        return IntPtr.Zero;
    }

    private unsafe void UnlockCallback(IntPtr opaque, IntPtr picture, IntPtr planes)
    {
        var frameBuffer = Marshal.ReadIntPtr(planes);
        var image = new Mat(videoHeight, videoWidth, MatType.CV_8UC3);
        frameBuffer.CopyTo((IntPtr)image.DataPointer, videoWidth * videoHeight * 3);

        var bgrImage = new Mat();
        Cv2.CvtColor(image, bgrImage, ColorConversionCodes.RGB2BGR);
        image.Dispose();

        videoCallback?.Invoke(bgrImage);
    }

    private static void DisplayCallback(IntPtr opaque, IntPtr picture)
    {
        //do nothing...
    }

    #endregion

    /// <summary>
    /// Release VLC helper
    /// </summary>
    public void Dispose()
    {
        player.Stop();

        libVlc?.Dispose();
        libVlc = null;
        player?.Dispose();
        player = null;
        media?.Dispose();
        media = null;
        if (buffer != IntPtr.Zero) Marshal.Release(buffer);
    }

    /// <summary>
    /// Due to CameraKit SDK only provide device path by WMF,
    /// here is a workaround for get direct show device name required by VLC
    /// </summary>
    /// <param name="wmfPath"></param>
    /// <returns></returns>
    private static string GetDsName(string wmfPath)
    {
        var keyPath = wmfPath.Split('#').First(s => s.Contains("vid"));
        Debug.WriteLine($"Target WMF Path : {wmfPath}");
        Debug.WriteLine($"Target Key Path : {keyPath}");

        foreach (var device in DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice))
        {
            Debug.WriteLine($"Compare DS {keyPath} => {device.DevicePath}");
            if (device.DevicePath.ToUpper().Contains(keyPath.ToUpper())) return device.Name;
        }
        return null;
    }
}