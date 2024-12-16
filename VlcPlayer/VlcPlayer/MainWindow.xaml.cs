using LibVLCSharp.Platforms.Windows;
using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using Serilog;
using System;
using System.IO;
using System.Threading;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace VlcPlayer;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private LibVLC libvlc;
    private MediaPlayer mp;

    public MainWindow()
    {
        this.InitializeComponent();
        VideoView.Initialized += OnVideoViewInitialized;
        Closed += OnMainWindowClosed;

        const long fileSizeLimit = 100 * 1000 * 1000; //100 MB
        const int countOfFiles = 1;

        var configuration = new LoggerConfiguration();

        configuration = configuration.MinimumLevel.Debug();
        configuration = configuration.WriteTo.File(
            Path.Combine(ApplicationData.Current.LocalFolder.Path, "log.txt"),
            rollingInterval: RollingInterval.Month,
            rollOnFileSizeLimit: true,
            fileSizeLimitBytes: fileSizeLimit,
            retainedFileCountLimit: countOfFiles);

        Log.Logger = configuration.CreateLogger();
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        mp.Stopped += (s, e) =>
        {
            grid.Children.Remove(VideoView);
        };
        mp.Stop();
    }

    private void OnVideoViewInitialized(object sender, InitializedEventArgs e)
    {
        libvlc = new LibVLC(enableDebugLogs: true, e.SwapChainOptions);
        mp = new MediaPlayer(libvlc);
        libvlc.Log += OnLibvlcLog;
    }

    private void OnLibvlcLog(object sender, LogEventArgs e)
    {
        Log.Debug(e.Message);
    }

    private async void OnOpenFileButtonClick(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hwnd);
        picker.FileTypeFilter.Add("*");
        var file = await picker.PickSingleFileAsync();
        PlayMediaByMrl(GetMrl(file.Path));
    }

    private void PlayMediaByMrl(string mrl)
    {
        var media = new Media(libvlc, mrl, FromType.FromLocation);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            this.mp.Play(media);
        });
    }

    private async void OnOpenDiskButtonClick(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hwnd);
        picker.FileTypeFilter.Add("*");
        var folder = await picker.PickSingleFolderAsync();
        PlayMediaByMrl($"dvd:///{folder.Path}");
    }

    private string GetMrl(string source)
    {
        var uriMrlMedia = new UriBuilder(source);
        return uriMrlMedia.Uri.ToString();
    }
}