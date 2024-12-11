using CameraKit.Core;
using CameraKit.Core.CameraNet;
using CameraKit.Core.ToolKit;
using OpenCvSharp;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace IpevoSdkDemo
{
    /*
     * This project demonstrates how to use the IPEVO Camera SDK ( CameraKit.Core ) to control IPEVO products.
     *
     * The SDK is integrated via NuGet and is located in the `.\LocalPackages` folder of the project.
     *
     * It is imported into this project through the `nuget.config` file.
     *
     * Since it depends on other third-party public packages available on nuget.org,
     * please ensure that the development environment has access to nuget.org during the build process.
     *
     * This SDK is windows only!!
     * You can use it in WinForm, WPF and WinUI windows application development.
     *
     */

    public partial class MainForm:Form
    {
        public MainForm()
        {
            InitializeComponent();

            Load += OnLoad;
            Closing += OnClosing;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            //Set SynchronizationContext to SDK NotificationCenter, this is "WinForm only"
            NotificationCenter.SynchronizationContext = SynchronizationContext.Current;

#if DEBUG
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
#endif

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

        private void OnClosing(object sender, CancelEventArgs e)
        {
            //stop working capture if exist
            vlc?.Dispose();
            vlc = null;

            //release SDK when application off
            CamerasManager.SharedManager.StopMonitor();
            DealNotifyEventOfCameraManager(false);
            DealNotifyEventOfWhiteBalance(false);
            DealNotifyEventOfFocus(false);
            DealNotifyEventOfDeviceButton(false);
        }

        private void CameraComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //stop working capture if exist
            vlc?.Dispose();
            vlc = null;

            var camera = ActCamera;
            if (camera == null) return;

            //if this camera is a IcNetCamera, for example, VZ-X
            //you should try to log in it at first
            //admin/admin is a default account and password
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

            //obtain the supported formats from camera
            //you can access more detail of format via *FormatKey* index
            ResolutionComboBox.Items.Clear();
            foreach (var value in camera.GetSupportedFormats())
            {
                ResolutionComboBox.Items.Add(value[FormatKey.FormatInfo]);
            }
            ResolutionComboBox.Text = string.Empty;

            //obtain the latest status of camera
            InitWhiteBalance();
            InitFocus();
        }

        private void ResolutionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //stop working streaming
            vlc?.Dispose();
            vlc = null;

            var camera = ActCamera;
            if (camera == null) return;

            //find target format object
            var tarFormat = camera
                .GetSupportedFormats()
                .FirstOrDefault(f => f[FormatKey.FormatInfo].ToString() == ResolutionComboBox.SelectedItem.ToString());
            if (tarFormat is null) throw new InvalidOperationException("null format");

            //parse format args for capture
            var w = Convert.ToInt32(tarFormat[FormatKey.Width]);
            var h = Convert.ToInt32(tarFormat[FormatKey.Height]);
            var fps = Convert.ToInt32(tarFormat[FormatKey.Fps]);
            var mType = (MediaType)Enum.Parse(typeof(MediaType), tarFormat[FormatKey.MediaType].ToString());

            // IPEVO WiFi series camera
            if (camera is IcNetCamera netCam)
            {
                //asking remote camera to use this format and start streaming
                AssertCamOperationResult(netCam.SetFormat(tarFormat));

                //start capture
                vlc = new VlcHelper(
                    netCam.DevicePath,
                    w,
                    h,
                    RenderImage);
            }
            //IPEVO usb camera
            else
            {
                //start capture
                vlc = new VlcHelper(camera.DevicePath, w, h, fps, mType, RenderImage);
            }
        }

        #region Capture Image Render

        /*
         * **Note:** This is for demonstration purposes only and is not part of the SDK functionality.
         *
         * Here is a demonstration of how to easily display an image stored in a Mat object on the screen.
         * This method is specifically designed for outputting with VLC or OpenCV.
         * If you are using a different image framework, you will need to implement the required output functionality for your image object accordingly.
         *
         */

        /// <summary>
        /// Here we use VLC library to fetch image from camera
        /// </summary>
        private VlcHelper vlc;

        /// <summary>
        /// Bitmap object for UI layout
        /// </summary>
        private Bitmap captureImage;

        /// <summary>
        /// Render Mat image to UI layout
        /// </summary>
        /// <param name="image"></param>
        private void RenderImage(Mat image)
        {
            unsafe
            {
                // copy image data into buffer
                var width = image.Width;
                var height = image.Height;
                var dataLen = image.Width * image.Height * image.Channels();

                // render buffer data in ui thread
                BeginInvoke(() =>
                {
                    try
                    {
                        RbgDataToBitmap();
                        image.Dispose();
                        resLabel.Text = @$"{width} * {height}";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                });

                return;

                //Write RGB data into Bitmap object and refresh UI layout
                void RbgDataToBitmap()
                {
                    if (captureImage == null || captureImage.Width != width || captureImage.Height != height)
                    {
                        captureImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                        pictureBox.Image = captureImage;
                    }

                    var bmData = captureImage.LockBits(
                        new Rectangle(0, 0, captureImage.Width, captureImage.Height),
                        ImageLockMode.ReadWrite,
                        captureImage.PixelFormat);

                    var scan0 = bmData.Scan0;
                    var srcPtr = (IntPtr)image.DataPointer;
                    srcPtr.CopyTo(scan0, dataLen);
                    captureImage.UnlockBits(bmData);
                    pictureBox.Invalidate();
                }
            }
        }

        #endregion

        #region Helper

        /// <summary>
        /// when using SDK api, always remember to check result of camera api
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

        #endregion

        #region SDK Example of CameraManager

        /*
         * CamerasManager.SharedManager is a global manager that provides users to
         * access interface of IPEVO cameras currently connected to the local system.
         * Through these interfaces, users can further control these cameras.
         *
         * By listening to its events, you can receive clear notifications when these cameras are added to or removed from the system.
         *
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
         * The IPEVO camera operations are primarily based on the **IcCamera** interface,
         * which provides methods for accessing or setting all available camera properties.         * 
         *
         * Additionally, by subscribing to relevant events from **NotificationCenter.SharedCenter**,
         * you can receive notifications when the camera undergoes automatic changes such as adjustments to the auto white balance value.         *
         * These change events are triggered automatically and provide you with the current white balance value.
         *
         * From the list defined in **CamerasManager.Notification**, you can identify which notifications can be accessed by this way.
         *
         * All IcCamera methods are designed with consistency, allowing you to derive the handling of other properties from the following example.
         *
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

        #region SDK Example of Focus

        /*
         * In the part of the camera's focus function, you can see an astonishing similarity to the way white balance is used,
         * except it includes some additional controllable attributes.
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
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.FocusChanged, WhitebalanceChanged);
                NotificationCenter.SharedCenter.UnRegisterObserver(CamerasManager.Notification.AutoFocusChanged, AutoWhitebalanceChanged);
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
         * When an event is triggered, you can decide how to handle it.
         * Additionally, physical events are monitored periodically for changes.
         * Rapid button presses may result in changes going undetected, in which case the event will not be triggered.
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
