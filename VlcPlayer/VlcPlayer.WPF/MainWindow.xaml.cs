using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using Vlc.DotNet.Core;
using Vlc.DotNet.Forms;
using Windows.Storage;

namespace VlcPlayer.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        VlcControl player;

        public MainWindow()
        {
            InitializeComponent();
            player = new VlcControl();
            WindowsFormsHost.Child = player;

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

        private void OnOpenDiskButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PlayMediaByMrl($"dvd:///{dialog.SelectedPath}");
            }
        }

        private void OnOpenFileButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PlayMediaByMrl(GetMrl(dialog.FileName));
            }
        }

        private void PlayMediaByMrl(string mrl)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                player.Play(mrl);
            });
        }

        private static string GetMrl(string source)
        {
            var uriMrlMedia = new UriBuilder(source);
            return uriMrlMedia.Uri.ToString();
        }

        private void OnRootLoaded(object sender, RoutedEventArgs e)
        {
            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            var libDirectory = new DirectoryInfo(Path.Combine(currentDirectory, "..", "libvlc", "win-x64"));
            player.BeginInit();
            player.VlcLibDirectory = libDirectory;
            player.Log += OnPlayerLog;
            player.EndInit();
        }

        private void OnPlayerLog(object sender, VlcMediaPlayerLogEventArgs e)
        {
            Log.Debug(e.Message);
        }
    }
}