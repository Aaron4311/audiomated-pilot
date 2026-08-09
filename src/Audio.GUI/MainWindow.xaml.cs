using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Audio.Core;
using Microsoft.Win32;

namespace Audio.GUI;

public partial class MainWindow : Window
{
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const int HotkeyIdGaming = 1;
    private const int HotkeyIdStreaming = 2;
    private const int HotkeyIdMeeting = 3;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly AudioManager _manager = new();
    private readonly List<RadioButton> _playbackButtons = [];
    private readonly List<RadioButton> _recordingButtons = [];
    private readonly List<RadioButton> _communicationButtons = [];
    private HwndSource? _hwndSource;

    public MainWindow()
    {
        InitializeComponent();
        RefreshDevices();
        RefreshProfiles();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(WndProc);

        // CTRL+SHIFT+1/2/3 -> Gaming/Streaming/Meeting profiles. Registered
        // globally (works regardless of window focus) for as long as this
        // process is running; silently no-ops per key if already taken by
        // another app, since RegisterHotKey's bool result isn't otherwise
        // actionable from here.
        RegisterHotKey(handle, HotkeyIdGaming, MOD_CONTROL | MOD_SHIFT, 0x31);
        RegisterHotKey(handle, HotkeyIdStreaming, MOD_CONTROL | MOD_SHIFT, 0x32);
        RegisterHotKey(handle, HotkeyIdMeeting, MOD_CONTROL | MOD_SHIFT, 0x33);
    }

    protected override void OnClosed(EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(handle, HotkeyIdGaming);
        UnregisterHotKey(handle, HotkeyIdStreaming);
        UnregisterHotKey(handle, HotkeyIdMeeting);
        _hwndSource?.RemoveHook(WndProc);

        base.OnClosed(e);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_HOTKEY)
            return IntPtr.Zero;

        string? profileName = wParam.ToInt32() switch
        {
            HotkeyIdGaming => "Gaming",
            HotkeyIdStreaming => "Streaming",
            HotkeyIdMeeting => "Meeting",
            _ => null,
        };

        if (profileName is not null)
        {
            try
            {
                _manager.ApplyProfile(profileName);
                RefreshDevices();
                StatusText.Text = $"Hotkey: applied profile '{profileName}'.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Hotkey error: {ex.Message}";
            }
        }

        handled = true;
        return IntPtr.Zero;
    }

    private void RefreshDevices()
    {
        PlaybackPanel.Children.Clear();
        RecordingPanel.Children.Clear();
        CommunicationPanel.Children.Clear();
        _playbackButtons.Clear();
        _recordingButtons.Clear();
        _communicationButtons.Clear();

        try
        {
            var playbackDevices = _manager.GetPlaybackDevices();
            var recordingDevices = _manager.GetRecordingDevices();

            var defaultPlayback = _manager.GetDefaultDevice(DeviceKind.Playback, DeviceRole.Default);
            var defaultPlaybackComm = _manager.GetDefaultDevice(DeviceKind.Playback, DeviceRole.Communications);
            var defaultRecording = _manager.GetDefaultDevice(DeviceKind.Recording, DeviceRole.Default);

            foreach (var device in playbackDevices)
            {
                var rb = new RadioButton { Content = device.FriendlyName, GroupName = "Playback", Tag = device, Margin = new Thickness(0, 4, 0, 4) };
                rb.IsChecked = device.Id == defaultPlayback?.Id;
                PlaybackPanel.Children.Add(rb);
                _playbackButtons.Add(rb);

                var commRb = new RadioButton { Content = device.FriendlyName, GroupName = "Communication", Tag = device, Margin = new Thickness(0, 4, 0, 4) };
                commRb.IsChecked = device.Id == defaultPlaybackComm?.Id;
                CommunicationPanel.Children.Add(commRb);
                _communicationButtons.Add(commRb);
            }

            foreach (var device in recordingDevices)
            {
                var rb = new RadioButton { Content = device.FriendlyName, GroupName = "Recording", Tag = device, Margin = new Thickness(0, 4, 0, 4) };
                rb.IsChecked = device.Id == defaultRecording?.Id;
                RecordingPanel.Children.Add(rb);
                _recordingButtons.Add(rb);
            }

            StatusText.Text = "Devices refreshed.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error refreshing devices: {ex.Message}";
        }
    }

    private void RefreshProfiles()
    {
        ProfileComboBox.Items.Clear();
        foreach (var name in _manager.GetProfiles().Keys)
            ProfileComboBox.Items.Add(name);
    }

    private static AudioDevice? SelectedDevice(List<RadioButton> buttons) =>
        buttons.FirstOrDefault(b => b.IsChecked == true)?.Tag as AudioDevice;

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var playback = SelectedDevice(_playbackButtons);
            var recording = SelectedDevice(_recordingButtons);
            var communication = SelectedDevice(_communicationButtons);

            if (playback is not null)
                _manager.SetPlaybackDevice(playback.FriendlyName);
            if (recording is not null)
                _manager.SetRecordingDevice(recording.FriendlyName);
            if (communication is not null)
                _manager.SetCommunicationsDevice(communication.FriendlyName, DeviceKind.Playback);

            _manager.SaveCurrentConfig();

            RefreshDevices();
            StatusText.Text = "Applied.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshDevices();

    private void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        string name = ProfileNameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            StatusText.Text = "Enter a profile name first.";
            return;
        }

        try
        {
            _manager.SaveProfile(name);
            RefreshProfiles();
            StatusText.Text = $"Saved profile '{name}'.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void LoadProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileComboBox.SelectedItem is not string name)
        {
            StatusText.Text = "Select a profile first.";
            return;
        }

        try
        {
            _manager.ApplyProfile(name);
            RefreshDevices();
            StatusText.Text = $"Applied profile '{name}'.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileComboBox.SelectedItem is not string name)
        {
            StatusText.Text = "Select a profile first.";
            return;
        }

        try
        {
            _manager.DeleteProfile(name);
            RefreshProfiles();
            StatusText.Text = $"Deleted profile '{name}'.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = "profiles.json" };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            string source = _manager.ProfilesFilePath;
            if (!File.Exists(source))
            {
                StatusText.Text = "No profiles.json to export yet.";
                return;
            }

            File.Copy(source, dialog.FileName, overwrite: true);
            StatusText.Text = $"Exported to {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "JSON files (*.json)|*.json" };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            File.Copy(dialog.FileName, _manager.ProfilesFilePath, overwrite: true);
            RefreshProfiles();
            StatusText.Text = $"Imported from {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }
}
