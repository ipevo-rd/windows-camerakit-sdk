using CameraKit.Core;
using CameraKit.Core.CameraNet;
using CameraKit.Core.ToolKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace IpevoSdkDemo
{
    /*
     * This project demonstrates how to use the IPEVO Camera SDK (CameraKit.Core) to control IPEVO products.
     *
     * Start with README.md — it explains how to build, how to use this demo,
     * and the one access pattern shared by every camera property.
     * For SDK details such as the format/resolution constraints, see CameraKitSDK.md.
     */

    /// <summary>
    /// Demo form that exercises most of the IcCamera property APIs through runtime generated UI rows.
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// Initializes the form and hooks the lifecycle events used to start/stop the SDK.
        /// </summary>
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
            DealNotifyEventOfKitLog(true);

            //build all camera property rows; each row registers its own change notifications
            BuildPropertyRows();

            //subscribe device button event
            DealNotifyEventOfDeviceButton(true);

            //invoke SDK when application on
            DealNotifyEventOfCameraManager(true);
            CamerasManager.SharedManager.StartMonitor();
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
            DealNotifyEventOfDeviceButton(false);
            DealNotifyEventOfKitLog(false);

            //unsubscribe every notification registered by the property rows
            foreach (var observer in rowObservers)
            {
                NotificationCenter.SharedCenter.UnRegisterObserver(observer.Key, observer.Value);
            }
        }

        /// <summary>
        /// select camera from combo box, then you can control it through SDK api.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var camera = ActCamera;

            //The SDK supports both USB cameras (IcCamera) and network cameras (IcNetCamera).
            //For network cameras (e.g., VZ-X in Wi-Fi mode), you must log in before use.
            //admin/admin is the default account and password;
            //if you have changed them on the device, replace the values below with your own.
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

            //obtain the latest status of the selected camera for every UI row
            UpdateDeviceInfo();
            RefreshAllRows();
        }

        /// <summary>
        /// open system camera application for checking the result of SDK api operation.
        /// Most camera properties only take effect while a video stream is active,
        /// so this demo forces you to open a capture application before selecting a camera.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenCamButton_Click(object sender, EventArgs e)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "microsoft.windows.camera:",
                UseShellExecute = true
            };
            Process.Start(startInfo);

            //the camera can be selected only after a stream is (assumed to be) opened
            CameraComboBox.Enabled = true;
        }

        /// <summary>
        /// When using SDK APIs, always check the return value.
        /// Common CommandReturnValue values include:
        ///   - Succeeded:       The operation completed successfully
        ///   - Unsupported:     The camera does not support this feature
        ///   - Failed:          The operation failed
        /// </summary>
        /// <param name="crv"></param>
        private static void AssertCamOperationResult(CommandReturnValue crv)
        {
            if (crv != CommandReturnValue.Succeeded) Debug.WriteLine($"Command Return Value = {crv}");
        }

        #region SDK Example of inner log

        /*
         * CameraKit.Core emits its inner log through the NotificationCenter.LoggerName notification.
         * Observing it is optional, but very helpful when debugging SDK behaviors.
         * This demo prints the messages into the read-only message box at the bottom of the window.
         */

        /// <summary>
        /// Registers or unregisters the inner log notification of CameraKit.Core.
        /// </summary>
        /// <param name="isReg"></param>
        private void DealNotifyEventOfKitLog(bool isReg)
        {
            if (isReg)
            {
                NotificationCenter.SharedCenter.RegisterObserver(NotificationCenter.LoggerName, NotifyKitLog);
            }
            else
            {
                NotificationCenter.SharedCenter.UnRegisterObserver(NotificationCenter.LoggerName, NotifyKitLog);
            }
        }

        /// <summary>
        /// Receives the inner log of CameraKit.Core and shows it in the log message box.
        /// </summary>
        /// <param name="notificationName"></param>
        /// <param name="sender"></param>
        /// <param name="userInfo"></param>
        private void NotifyKitLog(string notificationName, object sender, object userInfo)
        {
            if (userInfo is not Hashtable data) return;

            foreach (var dataValue in data.Values)
            {
                AppendLog(dataValue?.ToString());
            }
        }

        /// <summary>
        /// Appends one line to the log message box; safe to call from any thread.
        /// </summary>
        /// <param name="message"></param>
        private void AppendLog(string message)
        {
            if (string.IsNullOrEmpty(message) || IsDisposed) return;

            //log notifications may arrive from a background thread, marshal them to the UI thread first
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => AppendLog(message)));
                return;
            }

            logTextBox.AppendText(message + Environment.NewLine);
        }

        #endregion

        #region SDK Example of CameraManager

        /*
         * CamerasManager.SharedManager is a global manager that provides access to
         * the interfaces of IPEVO cameras currently connected to the local system.
         * Through these interfaces, you can further control the cameras.
         *
         * By subscribing to its events, you can receive notifications
         * when cameras are added to or removed from the system.
         */

        /// <summary>
        /// The camera currently selected in the combo box, or null when nothing is selected.
        /// </summary>
        private IcCamera ActCamera
        {
            get
            {
                //search target camera from SharedManager.Cameras
                return CamerasManager.SharedManager.Cameras.FirstOrDefault(
                    value => value.InstanceName == CameraComboBox.SelectedItem?.ToString());
            }
        }

        /// <summary>
        /// Registers or unregisters the camera attach/detach notifications.
        /// </summary>
        /// <param name="isReg"></param>
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

        /// <summary>
        /// Keeps the camera combo box in sync with the cameras currently connected to the system.
        /// </summary>
        /// <param name="notificationName"></param>
        /// <param name="sender"></param>
        /// <param name="userInfo"></param>
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
                    var wasSelected = CameraComboBox.SelectedItem?.ToString() == cam.InstanceName;
                    if (CameraComboBox.Items.Contains(cam.InstanceName)) CameraComboBox.Items.Remove(cam.InstanceName);

                    //clear the selection when the active camera is unplugged, the rows go back to the neutral state
                    if (wasSelected) CameraComboBox.SelectedItem = null;
                }
                break;
            }
        }

        /// <summary>
        /// Shows model, PID and firmware version of the selected camera.
        /// The firmware version is read through IcCamera.GetFirmwareVersion().
        /// </summary>
        private void UpdateDeviceInfo()
        {
            var cam = ActCamera;
            if (cam == null)
            {
                deviceInfoLabel.Text = @"No camera selected";
                return;
            }

            var firmware = cam.GetFirmwareVersion(out var version) == CommandReturnValue.Succeeded ? version : "unknown";
            deviceInfoLabel.Text = $@"Model: {cam.Model}   PID: {cam.Pid}   Firmware: {firmware}";
        }

        #endregion

        #region Generic property row helpers

        /*
         * Every IcCamera property API follows the same access pattern:
         *
         *   1. HasCapability(Capability.Xxx)                 -> is the feature supported at all
         *   2. GetXxx(out value, PropertyValueType.Minimum)  -> value range (also Maximum / Delta / Default)
         *   3. GetXxx(out value, PropertyValueType.Current)  -> current value
         *   4. SetXxx(value)                                 -> apply a new value
         *   5. CamerasManager.Notification.XxxChanged        -> the camera changed the value by itself
         *
         * The helpers below implement this pattern once, and BuildPropertyRows() maps each
         * camera property to a UI row by passing the matching IcCamera method pair as delegates.
         *
         * When the selected camera does not support a property, the row title turns RED
         * and the row controls are disabled.
         */

        /// <summary>
        /// Reads a short-typed camera property, e.g. (c, out v, t) => c.GetBrightness(out v, t).
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        private delegate CommandReturnValue ShortPropertyGetter(IcCamera camera, out short value, PropertyValueType type);

        /// <summary>
        /// Reads a bool-typed camera property, e.g. (c, out v) => c.GetLight(out v).
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="value"></param>
        private delegate CommandReturnValue BoolPropertyGetter(IcCamera camera, out bool value);

        /// <summary>
        /// Reads an enum-typed camera property, e.g. (c, out v) => c.GetFilter(out v).
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="value"></param>
        private delegate CommandReturnValue EnumPropertyGetter<T>(IcCamera camera, out T value);

        /// <summary>
        /// Fixed width of a generated property row.
        /// </summary>
        private const int RowWidth = 485;

        /// <summary>
        /// Fixed height of a generated property row.
        /// </summary>
        private const int RowHeight = 28;

        /// <summary>
        /// Width of the row title label.
        /// </summary>
        private const int TitleWidth = 110;

        /// <summary>
        /// Left position of the optional "Auto" checkbox / secondary label.
        /// </summary>
        private const int AutoLeft = 112;

        /// <summary>
        /// Left position of the main row control (slider, checkbox, combo box or button).
        /// </summary>
        private const int ControlLeft = 175;

        /// <summary>
        /// Width of the slider control.
        /// </summary>
        private const int SliderWidth = 240;

        /// <summary>
        /// Left position of the current-value label at the end of a slider row.
        /// </summary>
        private const int ValueLeft = 425;

        /// <summary>
        /// Actions that re-read camera status of every row; executed when the selected camera changes.
        /// </summary>
        private readonly List<Action> rowRefreshActions = new List<Action>();

        /// <summary>
        /// Actions that reset a property to its PropertyValueType.Default value; executed by the "Reset to Default" button.
        /// </summary>
        private readonly List<Action> rowResetActions = new List<Action>();

        /// <summary>
        /// All notifications registered by the property rows, kept for unregistering on close.
        /// </summary>
        private readonly List<KeyValuePair<string, NotificationCenter.NotificationObserver>> rowObservers = new List<KeyValuePair<string, NotificationCenter.NotificationObserver>>();

        /// <summary>
        /// The tab page that newly created rows are placed into; assigned by StartSection().
        /// </summary>
        private TabPage currentSection;

        /// <summary>
        /// Vertical position where the next row of the current section will be placed.
        /// </summary>
        private int currentSectionY;

        /// <summary>
        /// Re-reads the capability and status of the selected camera for every generated row.
        /// </summary>
        private void RefreshAllRows()
        {
            foreach (var refresh in rowRefreshActions) refresh();
        }

        /// <summary>
        /// Registers a notification observer and remembers it so it can be unregistered when the form closes.
        /// </summary>
        /// <param name="notificationName"></param>
        /// <param name="observer"></param>
        private void AddRowObserver(string notificationName, NotificationCenter.NotificationObserver observer)
        {
            NotificationCenter.SharedCenter.RegisterObserver(notificationName, observer);
            rowObservers.Add(new KeyValuePair<string, NotificationCenter.NotificationObserver>(notificationName, observer));
        }

        /// <summary>
        /// Extracts the payload of a camera notification.
        /// Every notification carries a Hashtable; the values are read via the CamerasManager.IndexString keys,
        /// and the notification is ignored when it belongs to a camera other than the selected one.
        /// </summary>
        /// <param name="userInfo"></param>
        /// <param name="valueKey"></param>
        /// <param name="value"></param>
        private bool TryGetNotificationValue(object userInfo, string valueKey, out object value)
        {
            value = null;
            if (userInfo is not Hashtable data) return false;

            //notifications are global, filter out those which are not from the selected camera
            var camera = data[CamerasManager.IndexString.Camera] as IcCamera;
            if (camera == null || ActCamera?.Uuid != camera.Uuid) return false;

            value = data[valueKey];
            return value != null;
        }

        /// <summary>
        /// Selects the tab page that the rows created afterwards are placed into.
        /// </summary>
        /// <param name="page"></param>
        private void StartSection(TabPage page)
        {
            currentSection = page;
            currentSectionY = 8;
        }

        /// <summary>
        /// Inserts an extra vertical gap before the next created row of the current section.
        /// </summary>
        private void AddGap()
        {
            currentSectionY += 14;
        }

        /// <summary>
        /// Creates an empty row inside the current section with a title label on the left.
        /// The title label is returned so the row builders can turn it red when the feature is unsupported.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="titleLabel"></param>
        private Panel CreateRow(string title, out Label titleLabel)
        {
            var row = new Panel { Left = 8, Top = currentSectionY, Width = RowWidth, Height = RowHeight };

            titleLabel = new Label { Left = 0, Top = 5, Width = TitleWidth, Height = 18, Text = title, TextAlign = ContentAlignment.MiddleLeft };
            row.Controls.Add(titleLabel);

            currentSection.Controls.Add(row);
            currentSectionY += RowHeight + 2;

            return row;
        }

        /// <summary>
        /// Builds a row for a short-typed camera property: [title] [optional Auto checkbox] [slider] [current value].
        /// The slider range comes from PropertyValueType Minimum/Maximum/Delta, and the row also
        /// contributes a reset action which writes the PropertyValueType.Default value back to the camera.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="capability"></param>
        /// <param name="getter"></param>
        /// <param name="setter"></param>
        /// <param name="valueNotification"></param>
        /// <param name="valueKey"></param>
        /// <param name="autoCapability"></param>
        /// <param name="autoGetter"></param>
        /// <param name="autoSetter"></param>
        /// <param name="autoNotification"></param>
        /// <param name="autoKey"></param>
        private void AddSliderRow(string title, Capability capability, ShortPropertyGetter getter, Func<IcCamera, short, CommandReturnValue> setter, string valueNotification, string valueKey,
            Capability? autoCapability = null, BoolPropertyGetter autoGetter = null, Func<IcCamera, bool, CommandReturnValue> autoSetter = null, string autoNotification = null, string autoKey = null)
        {
            var row = CreateRow(title, out var titleLabel);

            CheckBox autoCheckBox = null;
            if (autoGetter != null)
            {
                autoCheckBox = new CheckBox { Left = AutoLeft, Top = 4, Width = 58, Text = @"Auto", Enabled = false };
                row.Controls.Add(autoCheckBox);

                autoCheckBox.Click += (s, e) =>
                {
                    var cam = ActCamera;
                    if (cam == null) return;

                    //switch the auto mode of this property, e.g. SetAutoExposure()
                    AssertCamOperationResult(autoSetter(cam, autoCheckBox.Checked));
                };
            }

            var trackBar = new TrackBar { Left = ControlLeft, Top = 1, Width = SliderWidth, Height = 26, AutoSize = false, TickStyle = TickStyle.None, Enabled = false };
            var valueLabel = new Label { Left = ValueLeft, Top = 5, Width = 55, Height = 18, Text = @"-" };
            row.Controls.Add(trackBar);
            row.Controls.Add(valueLabel);

            trackBar.Scroll += (s, e) =>
            {
                var cam = ActCamera;
                if (cam == null) return;

                //apply the value dragged by the user, e.g. SetBrightness()
                AssertCamOperationResult(setter(cam, (short)trackBar.Value));
                valueLabel.Text = trackBar.Value.ToString();
            };

            //re-read capability, range and current value when the selected camera changes
            void Refresh()
            {
                var cam = ActCamera;

                //RED title only when the camera reports no such capability; no camera selected -> neutral title.
                //a failed Get is not treated as unsupported, it may just be temporary (e.g. no active stream yet)
                var hasAbility = cam != null && cam.HasCapability(capability);
                titleLabel.ForeColor = cam == null || hasAbility ? SystemColors.ControlText : Color.Red;

                var current = (short)0;
                var supported = hasAbility && getter(cam, out current, PropertyValueType.Current) == CommandReturnValue.Succeeded;

                trackBar.Enabled = supported;
                valueLabel.Text = supported ? current.ToString() : @"-";

                if (supported)
                {
                    getter(cam, out var min, PropertyValueType.Minimum);
                    getter(cam, out var max, PropertyValueType.Maximum);
                    getter(cam, out var step, PropertyValueType.Delta);

                    trackBar.Minimum = min;
                    trackBar.Maximum = Math.Max(min, max);
                    trackBar.SmallChange = Math.Max(1, (int)step);
                    trackBar.LargeChange = Math.Max(1, (int)step);
                    trackBar.Value = Math.Min(Math.Max(current, trackBar.Minimum), trackBar.Maximum);
                }

                if (autoCheckBox != null)
                {
                    var isAuto = false;
                    var autoSupported = cam != null && autoCapability.HasValue && cam.HasCapability(autoCapability.Value) && autoGetter(cam, out isAuto) == CommandReturnValue.Succeeded;

                    autoCheckBox.Enabled = autoSupported;
                    autoCheckBox.Checked = autoSupported && isAuto;
                }
            }

            rowRefreshActions.Add(Refresh);

            //reset this property to its default value, demonstrates PropertyValueType.Default
            rowResetActions.Add(() =>
            {
                var cam = ActCamera;
                if (cam == null || !cam.HasCapability(capability)) return;
                if (getter(cam, out var defaultValue, PropertyValueType.Default) != CommandReturnValue.Succeeded) return;

                AssertCamOperationResult(setter(cam, defaultValue));
                Refresh();
            });

            //follow the value adjusted by the camera itself, e.g. while auto mode is working
            if (valueNotification != null)
            {
                AddRowObserver(valueNotification, (name, sender, userInfo) =>
                {
                    if (!TryGetNotificationValue(userInfo, valueKey, out var raw)) return;

                    var newValue = Convert.ToInt16(raw);
                    if (newValue >= trackBar.Minimum && newValue <= trackBar.Maximum) trackBar.Value = newValue;
                    valueLabel.Text = newValue.ToString();
                });
            }

            //follow the auto mode switched from somewhere else, e.g. another application
            if (autoNotification != null && autoCheckBox != null)
            {
                AddRowObserver(autoNotification, (name, sender, userInfo) =>
                {
                    if (!TryGetNotificationValue(userInfo, autoKey, out var raw)) return;
                    autoCheckBox.Checked = Convert.ToBoolean(raw);
                });
            }
        }

        /// <summary>
        /// Builds a row for a bool-typed camera property: [title] [On checkbox].
        /// </summary>
        /// <param name="title"></param>
        /// <param name="capability"></param>
        /// <param name="getter"></param>
        /// <param name="setter"></param>
        /// <param name="valueNotification"></param>
        /// <param name="valueKey"></param>
        private void AddToggleRow(string title, Capability capability, BoolPropertyGetter getter, Func<IcCamera, bool, CommandReturnValue> setter, string valueNotification, string valueKey)
        {
            var row = CreateRow(title, out var titleLabel);

            var checkBox = new CheckBox { Left = ControlLeft, Top = 4, Width = 58, Text = @"On", Enabled = false };
            row.Controls.Add(checkBox);

            checkBox.Click += (s, e) =>
            {
                var cam = ActCamera;
                if (cam == null) return;

                //switch the property on/off, e.g. SetLight()
                AssertCamOperationResult(setter(cam, checkBox.Checked));
            };

            rowRefreshActions.Add(() =>
            {
                var cam = ActCamera;

                //RED title only when the camera reports no such capability, a failed Get does not turn it red
                var hasAbility = cam != null && cam.HasCapability(capability);
                titleLabel.ForeColor = cam == null || hasAbility ? SystemColors.ControlText : Color.Red;

                var current = false;
                var supported = hasAbility && getter(cam, out current) == CommandReturnValue.Succeeded;

                checkBox.Enabled = hasAbility;
                checkBox.Checked = supported && current;
            });

            //follow the status switched by the camera itself or another application
            if (valueNotification != null)
            {
                AddRowObserver(valueNotification, (name, sender, userInfo) =>
                {
                    if (!TryGetNotificationValue(userInfo, valueKey, out var raw)) return;
                    checkBox.Checked = Convert.ToBoolean(raw);
                });
            }
        }

        /// <summary>
        /// Builds a row for an enum-typed camera property: [title] [combo box].
        /// The selectable items are provided per camera (e.g. IcCamera.SupportedFilters).
        /// </summary>
        /// <param name="title"></param>
        /// <param name="capability"></param>
        /// <param name="itemsProvider"></param>
        /// <param name="getter"></param>
        /// <param name="setter"></param>
        /// <param name="valueNotification"></param>
        /// <param name="valueKey"></param>
        private void AddComboRow<T>(string title, Capability capability, Func<IcCamera, IList<T>> itemsProvider, EnumPropertyGetter<T> getter, Func<IcCamera, T, CommandReturnValue> setter,
            string valueNotification, string valueKey)
        {
            var row = CreateRow(title, out var titleLabel);

            var combo = new ComboBox { Left = ControlLeft, Top = 2, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            row.Controls.Add(combo);

            //SelectionChangeCommitted only fires on user operations, so refreshing the list below does not write back to the camera
            combo.SelectionChangeCommitted += (s, e) =>
            {
                var cam = ActCamera;
                if (cam == null || combo.SelectedItem == null) return;

                //apply the item picked by the user, e.g. SetFilter()
                AssertCamOperationResult(setter(cam, (T)combo.SelectedItem));
            };

            rowRefreshActions.Add(() =>
            {
                var cam = ActCamera;

                combo.Items.Clear();
                combo.Enabled = false;

                if (cam == null)
                {
                    titleLabel.ForeColor = SystemColors.ControlText;
                    return;
                }

                //RED title only when the camera reports no such capability; the capability covers setting the value,
                //the current value may still be readable (e.g. the VZ-X power line is fixed by hardware but can be read),
                //so the current status is displayed anyway and only changing it is blocked
                var hasAbility = cam.HasCapability(capability);
                titleLabel.ForeColor = hasAbility ? SystemColors.ControlText : Color.Red;

                //the item list may be queried from the device and can come back empty without an active stream;
                //in that case the row stays disabled but is not marked as unsupported
                var items = itemsProvider(cam);
                if (items == null || items.Count == 0) return;

                foreach (var item in items) combo.Items.Add(item);
                if (getter(cam, out var current) == CommandReturnValue.Succeeded) combo.SelectedItem = current;

                combo.Enabled = hasAbility;
            });

            //follow the item switched by the camera itself, e.g. via a physical button
            if (valueNotification != null)
            {
                AddRowObserver(valueNotification, (name, sender, userInfo) =>
                {
                    if (!TryGetNotificationValue(userInfo, valueKey, out var raw)) return;
                    if (raw is T typed) combo.SelectedItem = typed;
                });
            }
        }

        #endregion

        #region Property rows definition

        /*
         * This is where every camera property is mapped to a UI row.
         * Each call passes the matching IcCamera method pair plus the related
         * capability and change notification, nothing else is required.
         */

        /// <summary>
        /// Creates all property rows of this demo.
        /// The rows for exposure / white balance / focus include an extra "Auto" checkbox
        /// which maps to the SetAutoXxx/GetAutoXxx methods of the same property.
        /// </summary>
        private void BuildPropertyRows()
        {
            //3A properties: a value slider plus an auto switch
            StartSection(tabPage3A);

            AddSliderRow("Exposure", Capability.Exposure, (IcCamera c, out short v, PropertyValueType t) => c.GetExposure(out v, t), (c, v) => c.SetExposure(v),
                CamerasManager.Notification.ExposureChanged, CamerasManager.IndexString.ExposureValue,
                Capability.AutoExposure, (IcCamera c, out bool v) => c.GetAutoExposure(out v), (c, v) => c.SetAutoExposure(v),
                CamerasManager.Notification.AutoExposureChanged, CamerasManager.IndexString.AutoExposureStatus);

            AddSliderRow("White Balance", Capability.WhiteBalance, (IcCamera c, out short v, PropertyValueType t) => c.GetWhiteBalance(out v, t), (c, v) => c.SetWhiteBalance(v),
                CamerasManager.Notification.WhitebalanceChanged, CamerasManager.IndexString.WhitebalanceValue,
                Capability.AutoWhiteBalance, (IcCamera c, out bool v) => c.GetAutoWhiteBalance(out v), (c, v) => c.SetAutoWhiteBalance(v),
                CamerasManager.Notification.AutoWhitebalanceChanged, CamerasManager.IndexString.AutoWhitebalanceStatus);

            AddSliderRow("Focus", Capability.Focus, (IcCamera c, out short v, PropertyValueType t) => c.GetFocus(out v, t), (c, v) => c.SetFocus(v),
                CamerasManager.Notification.FocusChanged, CamerasManager.IndexString.FocusValue,
                Capability.AutoFocus, (IcCamera c, out bool v) => c.GetAutoFocus(out v), (c, v) => c.SetAutoFocus(v),
                CamerasManager.Notification.AutoFocusChanged, CamerasManager.IndexString.AutoFocusStatus);

            //focus extras: AF-S/AF-C mode switching and one-shot focus triggering
            AddAutoFocusModeRow();
            AddFocusTriggerRow();

            //image adjustment properties, they all share Capability.ImageAdjustment
            StartSection(tabPageImage);

            AddSliderRow("Brightness", Capability.ImageAdjustment, (IcCamera c, out short v, PropertyValueType t) => c.GetBrightness(out v, t), (c, v) => c.SetBrightness(v),
                CamerasManager.Notification.BrightnessChanged, CamerasManager.IndexString.BrightnessValue);

            AddSliderRow("Contrast", Capability.ImageAdjustment, (IcCamera c, out short v, PropertyValueType t) => c.GetContrast(out v, t), (c, v) => c.SetContrast(v),
                CamerasManager.Notification.ContrastChanged, CamerasManager.IndexString.ContrastValue);

            AddSliderRow("Gamma", Capability.ImageAdjustment, (IcCamera c, out short v, PropertyValueType t) => c.GetGamma(out v, t), (c, v) => c.SetGamma(v),
                CamerasManager.Notification.GammaChanged, CamerasManager.IndexString.GammaValue);

            AddSliderRow("Hue", Capability.ImageAdjustment, (IcCamera c, out short v, PropertyValueType t) => c.GetHue(out v, t), (c, v) => c.SetHue(v),
                CamerasManager.Notification.HueChanged, CamerasManager.IndexString.HueValue);

            AddSliderRow("Saturation", Capability.ImageAdjustment, (IcCamera c, out short v, PropertyValueType t) => c.GetSaturation(out v, t), (c, v) => c.SetSaturation(v),
                CamerasManager.Notification.SaturationChanged, CamerasManager.IndexString.SaturationValue);

            AddSliderRow("Sharpness", Capability.ImageAdjustment, (IcCamera c, out short v, PropertyValueType t) => c.GetSharpness(out v, t), (c, v) => c.SetSharpness(v),
                CamerasManager.Notification.SharpnessChanged, CamerasManager.IndexString.SharpnessValue);

            AddSliderRow("Gain", Capability.ImageAdjustment, (IcCamera c, out short v, PropertyValueType t) => c.GetGain(out v, t), (c, v) => c.SetGain(v),
                CamerasManager.Notification.GainChanged, CamerasManager.IndexString.GainValue);

            //keep a visual gap between the sliders and the reset button
            AddGap();
            AddResetRow();

            //device level functions
            StartSection(tabPageFunctions);

            //the selectable filters differ per camera model, so they come from IcCamera.SupportedFilters
            AddComboRow("Filter", Capability.Filter, c => c.SupportedFilters, (IcCamera c, out FilterType v) => c.GetFilter(out v), (c, v) => c.SetFilter(v),
                CamerasManager.Notification.FilterChanged, CamerasManager.IndexString.FilterType);

            AddSliderRow("Zoom", Capability.Zoom, (IcCamera c, out short v, PropertyValueType t) => c.GetZoomLevel(out v, t), (c, v) => c.SetZoomLevel(v),
                CamerasManager.Notification.ZoomChanged, CamerasManager.IndexString.ZoomLevel);

            AddToggleRow("Rotate 180", Capability.Rotate, (IcCamera c, out bool v) => c.GetRotate(out v), (c, v) => c.SetRotate(v),
                CamerasManager.Notification.RotateChanged, CamerasManager.IndexString.RotateStatus);

            AddToggleRow("Light", Capability.Light, (IcCamera c, out bool v) => c.GetLight(out v), (c, v) => c.SetLight(v),
                CamerasManager.Notification.LightStatusChanged, CamerasManager.IndexString.LightStatus);

            //anti-flicker of the power line frequency; note that GetFrequency() also takes a PropertyValueType
            AddComboRow("Power Line", Capability.PowerlineFrequency, c => new List<PowerlineFrequency> { PowerlineFrequency.None, PowerlineFrequency._50Hz, PowerlineFrequency._60Hz },
                (IcCamera c, out PowerlineFrequency v) => c.GetFrequency(out v, PropertyValueType.Current), (c, v) => c.SetFrequency(v),
                CamerasManager.Notification.PowerlineFrequencyChanged, CamerasManager.IndexString.PowerlineFrequencyMode);
        }

        /// <summary>
        /// Converts an AutoFocusMode to the short text commonly printed on cameras.
        /// </summary>
        /// <param name="mode"></param>
        private static string AfModeText(AutoFocusMode mode)
        {
            switch (mode)
            {
                case AutoFocusMode.Single: return "AF-S";
                case AutoFocusMode.Continuous: return "AF-C";
                default: return "?";
            }
        }

        /// <summary>
        /// Builds the row that switches the autofocus mode between single (AF-S) and continuous (AF-C),
        /// using GetAutoFocusMode()/SetAutoFocusMode() and the AutoFocusModeChanged notification.
        /// </summary>
        private void AddAutoFocusModeRow()
        {
            var row = CreateRow("AF Mode", out var titleLabel);

            var modeLabel = new Label { Left = AutoLeft, Top = 5, Width = 58, Height = 18, Text = @"-" };
            var switchButton = new Button { Left = ControlLeft, Top = 2, Width = 100, Height = 24, Text = @"Switch", Enabled = false };
            row.Controls.Add(modeLabel);
            row.Controls.Add(switchButton);

            switchButton.Click += (s, e) =>
            {
                var cam = ActCamera;
                if (cam == null) return;
                if (cam.GetAutoFocusMode(out var mode) != CommandReturnValue.Succeeded) return;

                //toggle between single(AF-S) and continuous(AF-C)
                AssertCamOperationResult(cam.SetAutoFocusMode(mode == AutoFocusMode.Single ? AutoFocusMode.Continuous : AutoFocusMode.Single));
            };

            rowRefreshActions.Add(() =>
            {
                var cam = ActCamera;

                //RED title only when the camera reports no such capability, a failed Get does not turn it red
                var hasAbility = cam != null && cam.HasCapability(Capability.AutoFocusMode);
                titleLabel.ForeColor = cam == null || hasAbility ? SystemColors.ControlText : Color.Red;
                switchButton.Enabled = hasAbility;

                var mode = AutoFocusMode.Unknown;
                var supported = hasAbility && cam.GetAutoFocusMode(out mode) == CommandReturnValue.Succeeded;
                modeLabel.Text = supported ? AfModeText(mode) : @"-";
            });

            AddRowObserver(CamerasManager.Notification.AutoFocusModeChanged, (name, sender, userInfo) =>
            {
                if (!TryGetNotificationValue(userInfo, CamerasManager.IndexString.AutoFocusMode, out var raw)) return;
                if (raw is AutoFocusMode mode) modeLabel.Text = AfModeText(mode);
            });
        }

        /// <summary>
        /// Builds the row that triggers a one-shot autofocus through IcCamera.StartFocus().
        /// </summary>
        private void AddFocusTriggerRow()
        {
            var row = CreateRow("Focus Trigger", out var titleLabel);

            var focusButton = new Button { Left = ControlLeft, Top = 2, Width = 100, Height = 24, Text = @"Focus Once", Enabled = false };
            row.Controls.Add(focusButton);

            focusButton.Click += (s, e) =>
            {
                var cam = ActCamera;
                if (cam == null) return;

                //invoke camera auto focusing once
                AssertCamOperationResult(cam.StartFocus());
            };

            rowRefreshActions.Add(() =>
            {
                var cam = ActCamera;
                var supported = cam != null && cam.HasCapability(Capability.Focus);

                titleLabel.ForeColor = cam == null || supported ? SystemColors.ControlText : Color.Red;
                focusButton.Enabled = supported;
            });
        }

        /// <summary>
        /// Builds the row that resets every slider property back to its PropertyValueType.Default value.
        /// </summary>
        private void AddResetRow()
        {
            var row = CreateRow("Defaults", out _);

            var resetButton = new Button { Left = ControlLeft, Top = 2, Width = 130, Height = 24, Text = @"Reset to Default" };
            row.Controls.Add(resetButton);

            resetButton.Click += (s, e) =>
            {
                //each slider row registered its own reset action, unsupported properties are skipped inside
                foreach (var reset in rowResetActions) reset();
            };
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

        /// <summary>
        /// Registers or unregisters the physical button notifications of the cameras.
        /// </summary>
        /// <param name="isReg"></param>
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

        /// <summary>
        /// Shows which physical button of which camera was pressed.
        /// </summary>
        /// <param name="notificationName"></param>
        /// <param name="sender"></param>
        /// <param name="userInfo"></param>
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
