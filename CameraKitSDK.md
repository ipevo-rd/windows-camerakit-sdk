# CameraKit.Core Documentation

CameraKit.Core is the core library for IPEVO camera control, providing a unified camera interface (`IcCamera`) and camera manager (`CamerasManager`).

---

## Table of Contents

- [Getting Started](#getting-started)
- [CamerasManager](#camerasmanager)
- [IcCamera Interface](#iccamera-interface)
  - [Format and Resolution Constraints](#format-and-resolution-constraints)
- [Enum Reference](#enum-reference)
- [Complete Example](#complete-example)
- [Notes](#notes)

---

## Getting Started

```csharp
using CameraKit.Core;

// Get the global shared CamerasManager instance
var manager = CamerasManager.SharedManager;

// Start monitoring camera devices
manager.StartMonitor(
    delayMsForBackground: 500,          // Background work delay (milliseconds)
    defaultAutoPollingForCamera: true,  // Auto-enable polling for new cameras
    scanWifiCamera: true                // Scan for Wi-Fi wireless cameras
);

// Get all connected cameras
List<IcCamera> cameras = manager.Cameras;

// Operate cameras...

// Stop monitoring
manager.StopMonitor();
```

---

## CamerasManager

`CamerasManager` is a singleton class responsible for managing detection, connection, and monitoring of all IPEVO cameras.

### Getting the Instance

```csharp
CamerasManager manager = CamerasManager.SharedManager;
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `SharedManager` | `CamerasManager` | Static property, the globally shared manager instance |
| `Cameras` | `List<IcCamera>` | Gets all currently connected cameras |

### Methods

#### StartMonitor

Starts monitoring for camera devices.

```csharp
public void StartMonitor(
    int delayMsForBackground = 0, 
    bool defaultAutoPollingForCamera = true, 
    bool scanWifiCamera = true
)
```

| Parameter | Description |
|-----------|-------------|
| `delayMsForBackground` | Delay before starting background work (USB watcher, mDNS) in milliseconds |
| `defaultAutoPollingForCamera` | Whether newly detected cameras should automatically enable status polling |
| `scanWifiCamera` | Whether to scan for Wi-Fi wireless cameras (e.g., VZ-X series). Requires firewall permissions |

#### StopMonitor

Stops all background monitoring and releases all camera devices.

```csharp
public void StopMonitor()
```

### Camera Type Detection Methods

```csharp
// Check if it's an IPEVO UVC camera
bool isIpevoUvc = CamerasManager.IsIpevoUvcCamera(camera);

// Check if it's an IPEVO virtual camera
bool isVirtual = CamerasManager.IsIpevoVirtualCamera(camera);

// Check if it's an IPEVO sharing camera
bool isSharing = CamerasManager.IsIpevoSharingCamera(camera);

// Check if it's an IPEVO wireless camera
bool isWireless = CamerasManager.IsIpevoWirelessCamera(camera);

// Check if it's NOT an IPEVO camera
bool isNonIpevo = CamerasManager.IsNonIpevoCamera(camera);
```

### Custom Camera Provider

```csharp
public void SetCustomCameraProvider(ICustomIcCameraProvider provider)
```

Sets a custom camera implementation provider for the manager.

---

## IcCamera Interface

`IcCamera` is the abstract base class for all cameras, defining a unified interface for camera control.

### Basic Properties

| Property | Type | Description |
|----------|------|-------------|
| `CamType` | `IcCameraType` | Camera type identifier |
| `Uuid` | `string` | Unique identifier of the camera |
| `Model` | `string` | Full device display name |
| `ShortModel` | `string` | Short device display name |
| `InstanceName` | `string` | Instance name with index (e.g., "Ziggi-HD #1") |
| `DevicePath` | `string` | Device path |
| `Pid` | `string` | UVC device PID |
| `IsAutoPolling` | `bool` | Whether automatic status polling is enabled |

### Capability Query

Always check capabilities before using specific features:

```csharp
// Check if camera supports specific features
bool hasZoom = camera.HasCapability(Capability.Zoom);
bool hasLight = camera.HasCapability(Capability.Light);
bool hasAutoFocus = camera.HasCapability(Capability.AutoFocus);
bool hasImageAdjustment = camera.HasCapability(Capability.ImageAdjustment);
```

### Resolution and Format

```csharp
// Get supported formats list (informational only)
List<Dictionary<FormatKey, object>> formats = camera.GetSupportedFormats();

// The following methods are NOT supported in the SDK:
// CommandReturnValue result = camera.SetFormat(selectedFormat);
// camera.GetFormat(out Dictionary<FormatKey, object> currentFormat);
```

**FormatKey Fields:**

| Key | Description |
|-----|-------------|
| `Width` | Frame width in pixels |
| `Height` | Frame height in pixels |
| `Fps` | Frames per second |
| `MediaType` | Media type (MJPEG, YUY2, H264, NV12) |
| `Resolution` | Resolution string representation |
| `StreamIndex` | Stream index |
| `FormatIndex` | Format index |

---

### Format and Resolution Constraints

> ⚠️ **Important**: `SetFormat` and `GetFormat` are **not supported** in the CameraKit SDK. These methods exist solely to satisfy IPEVO's internal video pipeline interface contract and will always return `CommandReturnValue.Unsupported` in SDK builds.

#### Background: Why Format Control Is Unavailable

Although IPEVO cameras expose a list of supported formats via `GetSupportedFormats()`, the actual resolution and frame rate negotiation does **not** occur at the camera object level. Instead, it is delegated entirely to the video streaming framework used by the host application.

When a client application opens a camera stream through a framework such as **DirectShow** or **Windows Media Foundation (WMF)**, it is the framework itself—not the camera driver or the SDK—that negotiates and selects the output format during stream initialization. The resolution is determined by the media type filter the framework applies when connecting the capture graph or activating the source reader.

As a result:

- **`SetFormat`** has no effect in the SDK. Resolution selection must be performed at the streaming framework level.
- **`GetFormat`** cannot return a meaningful value because no format has been committed outside of an active streaming session managed by a framework.
- The camera hardware genuinely supports multiple formats; however, the selection of which format to use is a concern of the integration layer, not the SDK.

#### Correct Approach by Framework

**DirectShow** — Configure the capture format by enumerating and setting the media type on the `IAMStreamConfig` interface of the capture filter:

```csharp
// Pseudocode: Selecting a media type in DirectShow
IAMStreamConfig streamConfig = /* obtain from capture pin */;
streamConfig.GetNumberOfCapabilities(out int count, out int size);
for (int i = 0; i < count; i++)
{
    streamConfig.GetStreamCaps(i, out AMMediaType mediaType, out VideoStreamConfigCaps caps);
    // Inspect mediaType and select the desired resolution/fps
    if (/* desired resolution? */)
    {
        streamConfig.SetFormat(mediaType);
        break;
    }
}
```

> 📖 Reference: [IAMStreamConfig interface (DirectShow) — Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/strmif/nn-strmif-iamstreamconfig)

**Windows Media Foundation (WMF)** — Configure the media type on the `IMFSourceReader` before starting the session:

```csharp
// Pseudocode: Selecting a media type in WMF
IMFSourceReader reader = /* create from MFCreateSourceReaderFromMediaSource */;
for (int i = 0; ; i++)
{
    IMFMediaType nativeType;
    reader.GetNativeMediaType(MF_SOURCE_READER_FIRST_VIDEO_STREAM, i, out nativeType);
    // nativeType.GetGUID(MF_MT_SUBTYPE), GetSize(MF_MT_FRAME_SIZE), etc.
    if (/* desired format? */)
    {
        reader.SetCurrentMediaType(MF_SOURCE_READER_FIRST_VIDEO_STREAM, 0, nativeType);
        break;
    }
}
```

> 📖 Reference: [IMFSourceReader::SetCurrentMediaType (WMF) — Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/mfreadwrite/nf-mfreadwrite-imfsourcereader-setcurrentmediatype)
> 📖 Reference: [Configuring a WMF Video Capture Device — Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/medfound/configuring-a-video-capture-device)

#### Summary Table

| Method | Availability in SDK | Notes |
|--------|--------------------|---------|
| `GetSupportedFormats()` | ✅ Supported | Returns the list of formats the hardware can produce |
| `SetFormat()` | ❌ Not supported | Must be done through DirectShow / WMF APIs |
| `GetFormat()` | ❌ Not supported | No committed format outside a framework streaming session |

### Image Adjustment

```csharp
// Brightness
camera.SetBrightness(50);
camera.GetBrightness(out short brightness, PropertyValueType.Current);
camera.GetBrightness(out short min, PropertyValueType.Minimum);
camera.GetBrightness(out short max, PropertyValueType.Maximum);
camera.GetBrightness(out short defaultVal, PropertyValueType.Default);

// Contrast
camera.SetContrast(50);
camera.GetContrast(out short contrast, PropertyValueType.Current);

// Saturation
camera.SetSaturation(50);
camera.GetSaturation(out short saturation, PropertyValueType.Current);

// Sharpness
camera.SetSharpness(50);
camera.GetSharpness(out short sharpness, PropertyValueType.Current);

// Gamma
camera.SetGamma(50);
camera.GetGamma(out short gamma, PropertyValueType.Current);

// Hue
camera.SetHue(50);
camera.GetHue(out short hue, PropertyValueType.Current);

// Gain
camera.SetGain(50);
camera.GetGain(out short gain, PropertyValueType.Current);

// Backlight Compensation
camera.SetBacklightCompensation(1);
camera.GetBacklightCompensation(out short blc, PropertyValueType.Current);
```

### Auto Focus Control

```csharp
// Enable/disable auto focus
camera.SetAutoFocus(true);
camera.GetAutoFocus(out bool isAutoFocus);

// Manual focus distance
camera.SetFocus(100);
camera.GetFocus(out short focusValue, PropertyValueType.Current);

// Auto focus mode (Single/Continuous)
camera.SetAutoFocusMode(AutoFocusMode.Continuous);
camera.GetAutoFocusMode(out AutoFocusMode mode);

// Trigger single auto focus
camera.StartFocus();
```

### Auto Exposure Control

```csharp
// Enable/disable auto exposure
camera.SetAutoExposure(true);
camera.GetAutoExposure(out bool isAutoExposure);

// Manual exposure value
camera.SetExposure(8);
camera.GetExposure(out short exposure, PropertyValueType.Current);
```

### White Balance Control

```csharp
// Enable/disable auto white balance
camera.SetAutoWhiteBalance(true);
camera.GetAutoWhiteBalance(out bool isAutoWB);

// Manual white balance value
camera.SetWhiteBalance(5000);
camera.GetWhiteBalance(out short wb, PropertyValueType.Current);
```

### LED Light Control

```csharp
// Turn light on/off
camera.SetLight(true);
camera.GetLight(out bool isLightOn);

// Light intensity level
camera.SetLightLevel(3);
camera.GetLightLevel(out short level, PropertyValueType.Current);

// Internal LED flash signal
camera.SetInterLedFlash(true);
camera.GetInterLedFlash(out bool isFlashOn);
```

### Zoom Control

```csharp
camera.SetZoomLevel(2);
camera.GetZoomLevel(out short zoomLevel, PropertyValueType.Current);
camera.GetZoomLevel(out short maxZoom, PropertyValueType.Maximum);
```

### Rotate Control

```csharp
// Rotate image 180 degrees
camera.SetRotate(true);
camera.GetRotate(out bool isRotated);
```

### Filter Control

```csharp
// Get supported filters
List<FilterType> filters = camera.SupportedFilters;

// Set filter
camera.SetFilter(FilterType.Grayscale);
camera.GetFilter(out FilterType currentFilter);
```

### Power Line Frequency (Anti-Flicker)

```csharp
camera.SetFrequency(PowerlineFrequency.Hz50);
camera.GetFrequency(out PowerlineFrequency freq, PropertyValueType.Current);
```

### Firmware Version

```csharp
camera.GetFirmwareVersion(out string version);
```

### High Resolution Image Capture

```csharp
CommandReturnValue result = camera.GetHighResolutionImage(out byte[] imageData);
if (result == CommandReturnValue.Succeeded)
{
    // imageData contains bitmap data
}
```

### AI Features

```csharp
// AI Voice (Noise Reduction)
camera.SetAiVoice(true);
camera.GetAiVoice(out bool isAiVoiceOn);

// AI Stage Mode
List<AiStageMode> modes = camera.SupportedAiStageModes;
camera.SetAiStage(AiStageMode.Speaker);
camera.GetAiStage(out AiStageMode currentMode);
```

### Button Lock

```csharp
camera.SetButtonLock(ButtonLockMode.All, true);
camera.GetButtonLock(ButtonLockMode.All, out bool isLocked);
```

### Inner Camera (Multi-Camera Devices)

```csharp
// Get supported inner cameras
List<InnerCamera> innerCameras = camera.SupportedInnerCamera();

// Set active inner camera
camera.SetInnerCamera(position: 1);
camera.GetInnerCamera(out int currentPosition);
```

---

## Enum Reference

### CommandReturnValue

| Value | Description |
|-------|-------------|
| `Unsupported` | Feature not supported |
| `Succeeded` | Operation succeeded |
| `Failed` | Operation failed |

### PropertyValueType

| Value | Description |
|-------|-------------|
| `Current` | Current value |
| `Minimum` | Minimum value |
| `Maximum` | Maximum value |
| `Default` | Default value |
| `Delta` | Step increment value |

### Capability

| Value | Description |
|-------|-------------|
| `Resolution` | Resolution adjustment |
| `Filter` | Image filter |
| `Exposure` | Exposure value |
| `AutoExposure` | Auto exposure toggle |
| `WhiteBalance` | White balance value |
| `AutoWhiteBalance` | Auto white balance toggle |
| `Focus` | Focus distance |
| `AutoFocus` | Auto focus toggle |
| `AutoFocusMode` | Auto focus mode selection |
| `ImageAdjustment` | Image adjustment (brightness, contrast, etc.) |
| `Zoom` | Digital/optical zoom |
| `Rotate` | Image rotation |
| `Light` | Light on/off |
| `LightIntensity` | Light brightness level |
| `InternalLedFlash` | Internal LED signal |
| `PowerlineFrequency` | Power line frequency |
| `InnerCamera` | Inner camera selection |
| `AiVoice` | AI voice noise reduction |
| `AiStage` | AI stage mode |
| `HighResolutionImage` | High resolution image capture |

### IcCameraType

| Value | Description |
|-------|-------------|
| `Uvc` | Generic UVC camera |
| `DoCam` | DO-CAM |
| `V4k` | V4K |
| `V4kPro` | V4K PRO |
| `V4kPro120` | V4K PRO 120 |
| `V4ks` | V4K S |
| `V4kUltra` | V4K Ultra |
| `Vzr` | VZ-R |
| `VzrUltra` | VZ-R Ultra |
| `Vzx` | VZ-X |
| `VzxUltra` | VZ-X Ultra |
| `Vz1` | VZ-1 |
| `P2v` | Point 2 View |
| `P2vUltra` | Point 2 View Ultra |
| `Totem120` | TOTEM 120 |
| `Totem180` | TOTEM 180 |
| `Totem360` | TOTEM 360 |
| `ZiggiHd` | Ziggi-HD |
| `ZiggiHdPlus` | Ziggi-HD Plus |

### AutoFocusMode

| Value | Description |
|-------|-------------|
| `Unknown` | Unknown mode |
| `Single` | Single auto focus (AF-S) |
| `Continuous` | Continuous auto focus (AF-C) |

### PowerlineFrequency

| Value | Description |
|-------|-------------|
| `None` | Disabled |
| `Hz50` | 50 Hz |
| `Hz60` | 60 Hz |

### MediaType

| Value | Description |
|-------|-------------|
| `Mjpeg` | Motion JPEG |
| `Yuy2` | YUY2 uncompressed |
| `H264` | H.264 compressed |
| `Nv12` | NV12 format |
| `Rgb24` | RGB24 format |

---

## Complete Example

```csharp
using CameraKit.Core;
using System;
using System.Linq;
using System.Threading;

class Program
{
    static void Main()
    {
        var manager = CamerasManager.SharedManager;
        
        // Start monitoring
        manager.StartMonitor(delayMsForBackground: 500);
        
        // Wait for camera detection
        Thread.Sleep(2000);
        
        // Get first available camera
        var camera = manager.Cameras.FirstOrDefault();
        if (camera == null)
        {
            Console.WriteLine("No camera found.");
            return;
        }
        
        // Display camera information
        Console.WriteLine($"Camera Name: {camera.InstanceName}");
        Console.WriteLine($"Camera Type: {camera.CamType}");
        Console.WriteLine($"Device Path: {camera.DevicePath}");
        
        // Get firmware version
        if (camera.GetFirmwareVersion(out string version) == CommandReturnValue.Succeeded)
        {
            Console.WriteLine($"Firmware: {version}");
        }
        
        // Check and use auto focus
        if (camera.HasCapability(Capability.AutoFocus))
        {
            camera.SetAutoFocus(true);
            Console.WriteLine("Auto focus enabled.");
        }
        
        // Check and control light
        if (camera.HasCapability(Capability.Light))
        {
            camera.SetLight(true);
            Console.WriteLine("Light turned on.");
        }
        
        // Get and display supported formats
        var formats = camera.GetSupportedFormats();
        if (formats != null)
        {
            Console.WriteLine($"Supported formats: {formats.Count}");
            foreach (var format in formats.Take(5))
            {
                var width = format[FormatKey.Width];
                var height = format[FormatKey.Height];
                var fps = format[FormatKey.Fps];
                Console.WriteLine($"  {width}x{height} @ {fps}fps");
            }
        }
        
        // Adjust image settings if supported
        if (camera.HasCapability(Capability.ImageAdjustment))
        {
            camera.GetBrightness(out short minBright, PropertyValueType.Minimum);
            camera.GetBrightness(out short maxBright, PropertyValueType.Maximum);
            camera.GetBrightness(out short currentBright, PropertyValueType.Current);
            Console.WriteLine($"Brightness: {currentBright} (range: {minBright}-{maxBright})");
        }
        
        // Clean up
        Console.WriteLine("\nPress any key to stop...");
        Console.ReadKey();
        
        manager.StopMonitor();
        Console.WriteLine("Monitor stopped.");
    }
}
```

---

## Notes

1. **Singleton Pattern**: `CamerasManager.SharedManager` is the only instance available globally.

2. **Capability Check**: Always use `HasCapability()` before calling feature-specific methods.

3. **Return Values**: All control methods return `CommandReturnValue`. Always check the result:
   ```csharp
   var result = camera.SetBrightness(50);
   if (result != CommandReturnValue.Succeeded)
   {
       // Handle error or unsupported feature
   }
   ```

4. **Wi-Fi Cameras**: Scanning wireless cameras (VZ-X series) requires firewall permissions for mDNS.

5. **Format Dictionary**: When working with formats, use `FormatKey` enum as dictionary keys:
   ```csharp
   var width = (int)format[FormatKey.Width];
   var height = (int)format[FormatKey.Height];
   ```

6. **Resource Cleanup**: Always call `StopMonitor()` when your application exits to properly release camera resources.

7. **Format Control**: `SetFormat` and `GetFormat` are **not supported** in this SDK. Resolution and format negotiation must be performed through the video streaming framework (DirectShow or WMF) at stream-open time, not through the camera interface. See [Format and Resolution Constraints](#format-and-resolution-constraints) for detailed guidance.

---

## License

Copyright © IPEVO Inc. All rights reserved.
