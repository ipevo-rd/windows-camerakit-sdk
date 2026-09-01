# IPEVO Camera SDK Demo (Windows)

This is a small Windows app that shows how to control IPEVO cameras (focus, exposure, white balance, filters, and more) from your own software, using the **IPEVO CameraKit.Core SDK**.

You do not need to know the SDK to start. Build the app, play with it, then open `MainForm.cs` — every button and slider in the app maps directly to one SDK call, so the code is the documentation.

## What you need

- Windows 10 or later
- Visual Studio 2019 or later (with .NET desktop development workload)
- Internet access to nuget.org (the SDK depends on a few public packages)
- An IPEVO camera (e.g. VZ-X, V4K, P2V) connected via USB, or a VZ-X in Wi-Fi mode on the same network (no USB needed)

## How to build and run

1. Open `IpevoSdkDemo.sln` in Visual Studio.
2. Wait for NuGet to restore packages (needs nuget.org access).
3. Press F5.

The SDK itself does not come from nuget.org. It is shipped as two package files in the `LocalPackages` folder and wired in by `nuget.config`:

- `CameraKit.Core.SDK` — the SDK that controls IPEVO camera properties
- `CameraKit.Hid` — extra HID support required by some models (e.g. P2V)

To use the SDK in your own project, copy `LocalPackages` and `nuget.config` into it, then install the two packages from the NuGet package manager.

## How to use the demo

1. Click **Open Capture App** first. This opens the Windows Camera app so a video stream is running.
   Most camera properties only take effect (or can only be read) while a stream is active, so the demo does not let you pick a camera before this step. Any capture software works, not just the Windows Camera app.
2. Pick your camera from the drop-down list next to the button.
3. Use the three tabs to control the camera:
   - **3A (AE / AWB / AF)** — exposure, white balance and focus, each with an Auto switch
   - **Image Adjustment** — brightness, contrast, gamma, hue, saturation, sharpness, gain, plus a reset button
   - **Functions** — filter, zoom, rotate, light, power line frequency
4. The white box at the bottom shows the SDK's internal log. It is useful when something does not behave as expected.

Things to know while playing:

- A **red title** means the selected camera does not support that feature. The controls stay visible but are disabled.
- Some values cannot be read until the stream is really running. If sliders stay disabled without a red title, re-select the camera after the stream is up.
- If the camera is a **VZ-X in Wi-Fi mode**, the demo logs in with the default account `admin` / `admin`. If you have changed the account or password on the device, update them in `CameraComboBox_SelectedIndexChanged` in `MainForm.cs`, otherwise the login fails and the camera cannot be selected.
- Pressing a physical button on the camera pops up a message — this shows the SDK can report hardware button events to your app.

## What the demo is NOT

- **It does not show video.** The SDK controls camera properties only; capturing frames is up to you. Common choices on Windows:
  - VLC ([LibVLCSharp](https://github.com/videolan/libvlcsharp))
  - OpenCV ([OpenCvSharp](https://github.com/shimat/opencvsharp))
  - [Windows Media Foundation](https://learn.microsoft.com/en-us/windows/win32/medfound/microsoft-media-foundation-sdk)
  - [DirectShow](https://learn.microsoft.com/en-us/windows/win32/directshow/directshow)
- **It does not switch resolutions.** `IcCamera.SetFormat()` / `GetFormat()` always return `Unsupported` by design. Resolution and format must be selected by your streaming framework when the stream is opened. `GetSupportedFormats()` does work and tells you what the hardware can produce. See `CameraKitSDK.md` > "Format and Resolution Constraints" for details.

## Reading the code

Everything lives in `MainForm.cs`, organized into regions:

| Region | What it shows |
| --- | --- |
| SDK Example of inner log | Receiving the SDK's internal log messages |
| SDK Example of CameraManager | Finding cameras and reacting to plug/unplug |
| Generic property row helpers | The one access pattern shared by every property |
| Property rows definition | One line per camera property, mapping it to the UI |
| SDK Example of device button event | Reacting to physical camera buttons |

The single most important idea: **every property works the same way.**

1. `HasCapability(...)` — does this camera support the feature at all?
2. `GetXxx(out value, PropertyValueType.Minimum / Maximum / Delta / Default / Current)` — the value range and the current value
3. `SetXxx(value)` — apply a new value, and always check the returned `CommandReturnValue`
4. Subscribe to `CamerasManager.Notification.XxxChanged` — the camera changed the value by itself (e.g. auto mode)

Once you can control one property, you can control all of them. Properties not wired into this demo (AiVoice, AiStage, InterLedFlash, ButtonLock, InnerCamera, GetHighResolutionImage, ...) follow exactly the same pattern.

## Requirements of your own app

- The SDK is Windows-only and targets .NET Standard 2.0, so it works with .NET Framework 4.6.2+ or .NET 8+.
- WinForms, WPF and WinUI are all supported.
- On WinForms, set `NotificationCenter.SynchronizationContext` once at startup so SDK events arrive on the UI thread (see `OnLoad` in `MainForm.cs`).
