// // Copyright (c) mgyrider. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Media.Imaging;
using System;
using System.IO;
using System.ComponentModel;

namespace FreeScreenShot;


/// <summary>
/// Interaction logic for ResultWindow.xaml
/// </summary>

public partial class ResultWindow : Window
{
    DateTime savedTimeStamp;
    BackgroundWorker bgWorker;

    public ResultWindow(ScreenShot screenShot, ScreenShotParam screenShotParam, double ImageSizeScale)
    {
        InitializeComponent();
        bgWorker = new BackgroundWorker();

        savedTimeStamp = DateTime.Now;

        this.copyBtn.Click += (o, arg) =>
        {
            Clipboard.SetImage(this.shotImage.Source as BitmapSource);
        };

        int width = screenShotParam.width;
        int height = screenShotParam.height;
        this.sizeText.Text = $"Size: {width} x {height}";
        this.Title = "SnapShot at " + savedTimeStamp;
        this.saveBtn.Click += SaveScreenShotImageAsFile;

        byte[]? pixels = null;
        bgWorker.WorkerReportsProgress = true;
        bgWorker.DoWork += (o, arg) =>
        {
            pixels = screenShot.CreateScreenShotPixelBytes(screenShotParam, bgWorker.ReportProgress);
        };
        bgWorker.ProgressChanged += (o, arg) =>
        {
            this.progressBar.Value = arg.ProgressPercentage;
        };
        bgWorker.RunWorkerCompleted += (o, arg) =>
        {
            if (pixels == null)
            {
                return;
            }
            this.saveBtn.IsEnabled = true;
            this.copyBtn.IsEnabled = true;
            this.shotImage.Source = ScreenShot.CreateImageSourceFromPixels(pixels, width, height);
            this.shotImage.MaxWidth = (int)(width / ImageSizeScale);
            this.shotImage.MaxHeight = (int)(height / ImageSizeScale);
            this.progressBar.Visibility = Visibility.Collapsed;
            var startWindow = this.Owner as StartWindow;
            if (startWindow?.isAutoCopyToClipboard ?? false)
            {
                Clipboard.SetImage(this.shotImage.Source as BitmapSource);
            }
            this.Activate();
        };
        this.Loaded += (o, arg) =>
        {
            bgWorker.RunWorkerAsync();
        };
    }

    void SaveScreenShotImageAsFile(object? sender, EventArgs arg)
    {
        Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
        dlg.FileName = "Snapshot_" + savedTimeStamp.ToString("yyyyMMdd_HH_mm_ssfff");
        dlg.DefaultExt = ".png";
        dlg.Filter = "Text documents (.png)|*.png";

        bool? result = dlg.ShowDialog();
        if (!result ?? false)
        {
            return;
        }

        string filename = dlg.FileName;
        Console.WriteLine($"save {filename}");
        FileStream stream = new FileStream(filename, FileMode.Create);
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        try
        {
            encoder.Frames.Add(BitmapFrame.Create(this.shotImage.Source as BitmapSource));
            encoder.Save(stream);
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e}");
        }
    }
}