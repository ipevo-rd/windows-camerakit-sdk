using CameraKit.Core;
using CameraKit.Core.CameraNet;
using CameraKit.Core.ToolKit;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace IpevoSdkDemo
{
    /*
     * This project demonstrates how to use the IPEVO Camera SDK (CameraKit.Core) to control IPEVO products.
     *
     * The SDK is integrated via NuGet and is located in the `.\LocalPackages` folder of the project.
     * It is imported into this project through the `nuget.config` file.
     *
     * The following packages are included in `.\LocalPackages` and are required for using the SDK:
     *   - CameraKit.Core.SDK: Core SDK for controlling IPEVO camera properties.
     *   - CameraKit.Hid: HID support for certain camera models (e.g., P2V).
     *
     * Since the SDK depends on third-party packages available on nuget.org,
     * please ensure that the development environment has access to nuget.org during the build process.
     *
     * Note: This SDK is Windows-only and targets .NET Standard 2.0.
     * It can be used with .NET Framework 4.6.2 or later, or .NET 8 or later.
     * Supported application types include WinForms, WPF, and WinUI.
     *
     * ---     
     *
     * Camera operations are primarily based on the IcCamera interface,
     * which provides methods for accessing and setting all available camera properties.
     *
     * By subscribing to relevant events from NotificationCenter.SharedCenter,
     * you can receive notifications when the camera undergoes automatic changes,
     * such as adjustments to the auto white balance or focus values.
     *
     * Refer to the list defined in CamerasManager.Notification to identify
     * which notifications are available for subscription.
     *
     * All IcCamera methods are designed with consistency,
     * allowing you to apply the same patterns across different camera properties.
     *
     * For better readability, this demo only implements representative methods of IcCamera.
     * Other methods not demonstrated here can be used in a similar manner.
     *
     * ---
     *
     * IMPORTANT - Format / Resolution Control:
     *   IcCamera.SetFormat() and IcCamera.GetFormat() are NOT supported in this SDK.
     *   These methods exist solely to satisfy IPEVO's internal video pipeline interface
     *   and will always return CommandReturnValue.Unsupported.
     *
     *   Resolution and format selection must be performed at the streaming framework level
     *   (e.g., IAMStreamConfig for DirectShow, IMFSourceReader for WMF) when the stream
     *   session is being opened — not via the camera object.
     *
     *   IcCamera.GetSupportedFormats() is supported and can be used to enumerate the
     *   formats that the camera hardware is capable of producing.
     *
     *   For full details and code examples, refer to:
     *   CameraKitSDK.md > "Format and Resolution Constraints"
     *
     * --- 
     *
     * This SDK provides control over IPEVO camera properties,
     * but 'does not' include implementation for capturing video frames from the camera.
     * 
     * For capturing video on Windows, refer to various video API frameworks:
     *   - VLC (LibVLCSharp): https://github.com/videolan/libvlcsharp
     *   - OpenCV (OpenCvSharp): https://github.com/shimat/opencvsharp
     *   - Windows Media Foundation (WMF): https://learn.microsoft.com/en-us/windows/win32/medfound/microsoft-media-foundation-sdk
     *   - DirectShow: https://learn.microsoft.com/en-us/windows/win32/directshow/directshow
     */

    public partial class MainForm:Form
    {
        public MainForm()
        {
            InitializeComponent();

            Load += OnLoad;
            Closing += OnClosing;
        }
                
        /// <summary>
        /// Perform one-time initialization required for using the SDK.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLoad(object sender, EventArgs e)
        {
            //Set SynchronizationContext to ensure SDK event callbacks are executed on the UI thread.
            //This is required for WinForms applications only.
            NotificationCenter.SynchronizationContext = SynchronizationContext.Current;

            //register inner log from CameraKit.Core, this is helpful when debugging
            NotificationCenter.SharedCenter.RegisterObserver(NotificationCenter.LoggerName, (name, o, info) =>
            {
                //inner log of CameraKit.Core
                if (info is not Hashtable data) return;
                foreach (var dataValue in data.Values)
                {
                    Debug.WriteLine(dataValue);
                }
            });

            //invoke SDK when application on
            DealNotifyEventOfCameraManager(true);
            CamerasManager.SharedManager.StartMonitor();

            //subscribe white balance events
            DealNotifyEventOfWhiteBalance(true);

            //subscribe focus events
            DealNotifyEventOfFocus(true);

            //subscribe device button event
            DealNotifyEventOfDeviceButton(true);
        }

        /// <summary>
        /// Perform cleanup and release resources required by the SDK.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClosing(object sender, CancelEventArgs e)
        {
            //release SDK when application off
            CamerasManager.SharedManager.StopMonitor();

            //unsubscribe events
            DealNotifyEventOfCameraManager(false);
            DealNotifyEventOfWhiteBalance(false);
            DealNotifyEventOfFocus(false);
            DealNotifyEventOfDeviceButton(false);
        }

        /// <summary>
        /// select camera from combo box, then you can control it through SDK api.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var camera = ActCamera;
            if (camera == null) return;

            //The SDK supports both USB cameras (IcCamera) and network cameras (IcNetCamera).
            //For network cameras (e.g., VZ-X in Wi-Fi mode), you must log in before use.
            //admin/admin is the default account and password.
            if (camera is IcNetCamera netCamera)
            {
                netCamera.NetDeviceAccount = "admin";
                netCamera.NetDevicePassword = "admin";

                if (!netCamera.IsLogin)
                {
                    MessageBox.Show(@"Failed to login");
                    CameraComboBox.SelectedItem = null;
                    return;
                }
            }            

            //obtain the latest status of camera
            InitWhiteBalance();
            InitFocus();
        }

        /// <summary>
        /// open system camera application, this is not necessary for using SDK,
        /// just a demo for checking the result of SDK api operation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OpenCamButton_Click(object sender, EventArgs e)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "microsoft.windows.camera:",
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }

        /// <summary>
        /// When using SDK APIs, always check the return value.
        /// Common CommandReturnValue values include:
        ///   - Succeeded:       The operation completed successfully
        ///   - NotSupported:    The camera does not support this feature
        ///   - Failed:          The operation failed
        /// </summary>
        /// <param name="crv"></param>
        private static void AssertCamOperationResult(CommandReturnValue crv)
        {
            if (crv != CommandReturnValue.Succeeded) Debug.WriteLine($"Command Return Value = {crv}");
        }

        /// <summary>
        /// show the common autofocus mode text
        /// </summary>
        /// <param name="afMode"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void UpdateAfModeText(AutoFocusMode afMode)
        {
            switch (afMode)
            {
                case AutoFocusMode.Unknown:
                    afModeLabel.Text = "?";
                    break;
                case AutoFocusMode.Single:
                    afModeLabel.Text = "AF-S";
                    break;
                case AutoFocusMode.Continuous:
                    afModeLabel.Text = "AF-C";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(afMode), afMode, null);
            }
        }        

        #region SDK Example of CameraManager

        /*
         * CamerasManager.SharedManager is a global manager that provides access to
         * the interfaces of IPEVO cameras currently connected to the local system.
         * Through these interfaces, you can further control the cameras.
         *
         * By subscribing to its events, you can receive notifications
         * when cameras are added to or removed from the system.
         */

        private IcCamera ActCamera
        {
            get
            {
                //search target camera from SharedManager.Cameras
                return CamerasManager.SharedManager.Cameras.FirstOrDefault(
                    value => value.InstanceName == CameraComboBox.SelectedItem?.ToString());
            }
        }

        private void DealNotifyEventOfCameraManager(bool isReg)
        {
            if (isReg)
            {
                //register the event for new camera found
                NotificationCenter.SharedCenter.RegisterObserver(CamerasManager.Notification.DeviceAttached, UpdateDevice);
                NotificationCenter.SharedCenter.RegisterObserver(CamerasManager.Notification.DeviceDetached, UpdateDevice);
            }
            else
            {
                //register the event for camera not exist
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.DeviceAttached, UpdateDevice);
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.DeviceDetached, UpdateDevice);
            }
        }

        private void UpdateDevice(string notificationName, object sender, object userInfo)
        {
            if (userInfo is not IcCamera cam) return;

            switch (notificationName)
            {
                case CamerasManager.Notification.DeviceAttached:
                {
                    if (!CameraComboBox.Items.Contains(cam.InstanceName)) CameraComboBox.Items.Add(cam.InstanceName);
                }
                break;
                case CamerasManager.Notification.DeviceDetached:
                {
                    if (CameraComboBox.Items.Contains(cam.InstanceName)) CameraComboBox.Items.Remove(cam.InstanceName);
                }
                break;
            }
        }

        #endregion

        #region SDK Example of Camera WhiteBalance operation

        /*
         * This section demonstrates how to control the white balance property of a camera.
         * The same pattern can be applied to other camera properties.
         *
         * Before calling any property method, use HasCapability() to verify
         * that the camera supports the specific feature.
         *
         * When getting property values, specify the PropertyValueType:
         *   - Current:  The current value of the property
         *   - Maximum:  The maximum allowed value
         *   - Minimum:  The minimum allowed value
         *   - Delta:    The step size for value adjustments
         */
        private void InitWhiteBalance()
        {
            var tarCam = ActCamera;
            if (tarCam is null) return;

            //init camera latest value for UI
            if (tarCam.GetAutoWhiteBalance(out var isAuto) == CommandReturnValue.Succeeded) checkBoxWB.Checked = isAuto;
            if (tarCam.GetWhiteBalance(out var wbValue, PropertyValueType.Current) == CommandReturnValue.Succeeded) wbLabel.Text = wbValue.ToString();
        }

        private void DealNotifyEventOfWhiteBalance(bool isReg)
        {
            //register or unregister events of white balance
            if (isReg)
            {
                NotificationCenter.SharedCenter.RegisterObserver(CamerasManager.Notification.WhitebalanceChanged, WhitebalanceChanged);
                NotificationCenter.SharedCenter.RegisterObserver(CamerasManager.Notification.AutoWhitebalanceChanged, AutoWhitebalanceChanged);
            }
            else
            {
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.WhitebalanceChanged, WhitebalanceChanged);
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.AutoWhitebalanceChanged, AutoWhitebalanceChanged);
            }
        }

        private void AutoWhitebalanceChanged(string notificationName, object sender, object userInfo)
        {
            if (userInfo is not Hashtable data) return;
            var camera = data[CamerasManager.IndexString.Camera] as IcCamera;
            if (ActCamera?.Uuid != camera.Uuid) return;
            var isAutoWhiteBalance = Convert.ToBoolean(data[CamerasManager.IndexString.AutoWhitebalanceStatus]);
            if (checkBoxWB.Checked != isAutoWhiteBalance) checkBoxWB.Checked = isAutoWhiteBalance;
        }

        private void WhitebalanceChanged(string notificationName, object sender, object userInfo)
        {
            if (userInfo is not Hashtable data) return;
            var camera = data[CamerasManager.IndexString.Camera] as IcCamera;
            if (ActCamera?.Uuid != camera.Uuid) return;
            var whiteBalanceValue = Convert.ToInt16(data[CamerasManager.IndexString.WhitebalanceValue]);
            wbLabel.Text = whiteBalanceValue.ToString();
        }

        private void checkBoxWB_Click(object sender, EventArgs e)
        {
            var tarCam = ActCamera;
            if (tarCam is null) return;

            //check capability
            if (!tarCam.HasCapability(Capability.WhiteBalance)) return;

            //set auto white balance
            AssertCamOperationResult(tarCam.SetAutoWhiteBalance(checkBoxWB.Checked));
        }

        private void buttonWbAdd_Click(object sender, EventArgs e)
        {
            var tarCam = ActCamera;
            if (tarCam is null) return;

            //check capability
            if (!tarCam.HasCapability(Capability.WhiteBalance)) return;

            //get current value and maximum value of white balance
            if (tarCam.GetWhiteBalance(out var currValue, PropertyValueType.Current) == CommandReturnValue.Succeeded
                &&
                tarCam.GetWhiteBalance(out var maxValue, PropertyValueType.Maximum) == CommandReturnValue.Succeeded
                &&
                tarCam.GetWhiteBalance(out var step, PropertyValueType.Delta) == CommandReturnValue.Succeeded)
            {
                var tarValue = (short)(currValue + step * 100);

                //set white balance value
                if (tarValue <= maxValue) AssertCamOperationResult(ActCamera.SetWhiteBalance(tarValue));
            }
        }

        private void buttonWbDec_Click(object sender, EventArgs e)
        {
            var tarCam = ActCamera;
            if (tarCam is null) return;

            //check capability
            if (!tarCam.HasCapability(Capability.WhiteBalance)) return;

            //get current value and minimum value of white balance
            if (tarCam.GetWhiteBalance(out var currValue, PropertyValueType.Current) == CommandReturnValue.Succeeded
                &&
                tarCam.GetWhiteBalance(out var minValue, PropertyValueType.Minimum) == CommandReturnValue.Succeeded
                &&
                tarCam.GetWhiteBalance(out var step, PropertyValueType.Delta) == CommandReturnValue.Succeeded)
            {
                var tarValue = (short)(currValue - step * 100);

                //set white balance value
                if (tarValue >= minValue) AssertCamOperationResult(ActCamera.SetWhiteBalance(tarValue));
            }
        }

        #endregion

        #region SDK Example of Camera Focus operation

        /*
         * The camera focus functionality follows a similar pattern to white balance,
         * with some additional controllable attributes.
         */

        private void InitFocus()
        {
            var tarCam = ActCamera;
            if (tarCam is null) return;

            //init camera latest value for UI
            if (tarCam.GetAutoFocus(out var isAuto) == CommandReturnValue.Succeeded) checkBoxAF.Checked = isAuto;
            if (tarCam.GetFocus(out var focusValue, PropertyValueType.Current) == CommandReturnValue.Succeeded) focusLabel.Text = focusValue.ToString();
            if (tarCam.GetAutoFocusMode(out var afMode) == CommandReturnValue.Succeeded) UpdateAfModeText(afMode);
        }

        private void DealNotifyEventOfFocus(bool isReg)
        {
            //register or unregister events of focus
            if (isReg)
            {
                NotificationCenter.SharedCenter.RegisterObserver(CamerasManager.Notification.FocusChanged, FocusChanged);
                NotificationCenter.SharedCenter.RegisterObserver(CamerasManager.Notification.AutoFocusChanged, AutoFocusChanged);
                NotificationCenter.SharedCenter.RegisterObserver(CamerasManager.Notification.AutoFocusModeChanged, AutoFocusModeChanged);
            }
            else
            {
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.FocusChanged, FocusChanged);
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.AutoFocusChanged, AutoFocusChanged);
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.AutoFocusModeChanged, AutoFocusModeChanged);
            }
        }

        private void AutoFocusChanged(string notificationName, object sender, object userInfo)
        {
            if (userInfo is not Hashtable data) return;
            var camera = data[CamerasManager.IndexString.Camera] as IcCamera;
            if (ActCamera?.Uuid != camera.Uuid) return;

            var isAutoFocus = Convert.ToBoolean(data[CamerasManager.IndexString.AutoFocusStatus]);
            if (checkBoxAF.Checked != isAutoFocus) checkBoxAF.Checked = isAutoFocus;
        }

        private void FocusChanged(string notificationName, object sender, object userInfo)
        {
            if (userInfo is not Hashtable data) return;
            var camera = data[CamerasManager.IndexString.Camera] as IcCamera;
            if (ActCamera?.Uuid != camera.Uuid) return;
            var focusValue = Convert.ToInt16(data[CamerasManager.IndexString.FocusValue]);
            focusLabel.Text = focusValue.ToString();
        }

        private void AutoFocusModeChanged(string notificationName, object sender, object userInfo)
        {
            if (userInfo is not Hashtable data) return;
            var camera = data[CamerasManager.IndexString.Camera] as IcCamera;
            if (ActCamera?.Uuid != camera.Uuid) return;
            var afMode = (AutoFocusMode)data[CamerasManager.IndexString.AutoFocusMode];
            UpdateAfModeText(afMode);
        }

        private void checkBoxAF_Click(object sender, EventArgs e)
        {
            var tarCam = ActCamera;
            if (tarCam is null) return;

            //check capability
            if (!tarCam.HasCapability(Capability.AutoFocus)) return;

            //set autofocus
            AssertCamOperationResult(tarCam.SetAutoFocus(checkBoxAF.Checked));
        }

        private void buttonFocusAdd_Click(object sender, EventArgs e)
        {
            var tarCam = ActCamera;
            if (tarCam is null) return;

            //check capability
            if (!tarCam.HasCapability(Capability.Focus)) return;

            //get current value and maximum value of focus
            if (tarCam.GetFocus(out var currValue, PropertyValueType.Current) == CommandReturnValue.Succeeded
                &&
                tarCam.GetFocus(out var maxValue, PropertyValueType.Maximum) == CommandReturnValue.Succeeded
                &&
                tarCam.GetFocus(out var step, PropertyValueType.Delta) == CommandReturnValue.Succeeded)
            {
                var tarValue = (short)(currValue + step * 10);

                //set focus value
                if (tarValue <= maxValue) AssertCamOperationResult(ActCamera.SetFocus(tarValue));
            }
        }

        private void buttonFocusDec_Click(object sender, EventArgs e)
        {
            var tarCam = ActCamera;
            if (tarCam is null) return;

            //check capability
            if (!tarCam.HasCapability(Capability.Focus)) return;

            //get current value and minimum value of focus
            if (tarCam.GetFocus(out var currValue, PropertyValueType.Current) == CommandReturnValue.Succeeded
                &&
                tarCam.GetFocus(out var minValue, PropertyValueType.Minimum) == CommandReturnValue.Succeeded
                &&
                tarCam.GetFocus(out var step, PropertyValueType.Delta) == CommandReturnValue.Succeeded)
            {
                var tarValue = (short)(currValue - step * 10);

                //set focus
                if (tarValue >= minValue) AssertCamOperationResult(ActCamera.SetFocus(tarValue));
            }
        }

        private void buttonAfMode_Click(object sender, EventArgs e)
        {
            var tarCam = ActCamera;
            if (tarCam is null) return;

            //check capability
            if (!tarCam.HasCapability(Capability.AutoFocusMode)) return;

            //get current value of autofocus mode
            if (tarCam.GetAutoFocusMode(out var afMode) == CommandReturnValue.Succeeded)
            {
                var tarValue = afMode == AutoFocusMode.Single ? AutoFocusMode.Continuous : AutoFocusMode.Single;

                //set autofocus mode
                AssertCamOperationResult(ActCamera.SetAutoFocusMode(tarValue));
            }
        }

        private void focusButton_Click(object sender, EventArgs e)
        {
            var tarCam = ActCamera;
            if (tarCam is null) return;
            if (!tarCam.HasCapability(Capability.Focus)) return;

            //invoke camera auto focusing once
            AssertCamOperationResult(tarCam.StartFocus());
        }

        #endregion

        #region SDK Example of device button event

        /*
         * This demonstrates how to listen for physical button press events from a camera.
         * When an event is triggered, you can determine how to handle it.
         *
         * Note: Physical button states are monitored periodically.
         * Rapid button presses may not be detected, and in such cases, the event will not be triggered.
         */

        private void DealNotifyEventOfDeviceButton(bool isReg)
        {
            if (isReg)
            {
                NotificationCenter.SharedCenter.RegisterObserver(CamerasManager.Notification.DeviceFilterClick, NotifyDeviceButtonClick);
                NotificationCenter.SharedCenter.RegisterObserver(CamerasManager.Notification.DeviceFocusClick, NotifyDeviceButtonClick);
                NotificationCenter.SharedCenter.RegisterObserver(CamerasManager.Notification.DeviceRotateClick, NotifyDeviceButtonClick);
                NotificationCenter.SharedCenter.RegisterObserver(CamerasManager.Notification.DeviceSnapshotClick, NotifyDeviceButtonClick);
                NotificationCenter.SharedCenter.RegisterObserver(CamerasManager.Notification.DeviceZoomInClick, NotifyDeviceButtonClick);
                NotificationCenter.SharedCenter.RegisterObserver(CamerasManager.Notification.DeviceZoomOutClick, NotifyDeviceButtonClick);
            }
            else
            {
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.DeviceFilterClick, NotifyDeviceButtonClick);
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.DeviceFocusClick, NotifyDeviceButtonClick);
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.DeviceRotateClick, NotifyDeviceButtonClick);
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.DeviceSnapshotClick, NotifyDeviceButtonClick);
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.DeviceZoomInClick, NotifyDeviceButtonClick);
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.DeviceZoomOutClick, NotifyDeviceButtonClick);
            }
        }

        private void NotifyDeviceButtonClick(string notificationName, object sender, object userInfo)
        {
            if (!(userInfo is Hashtable data)) return;
            var camera = data[CamerasManager.IndexString.Camera] as IcCamera;
            if (camera == null) return;

            MessageBox.Show($@"{camera.Model} => {notificationName}");
        }

        #endregion
    }
}
