// // Copyright (c) mgyrider. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;
using System.Diagnostics;
using System.Windows.Navigation;

namespace FreeScreenShot;


/// <summary>
/// Interaction logic for StartWindow.xaml
/// </summary>

public partial class StartWindow : Window
{
    public bool isAutoCopyToClipboard
    {
        get
        {
            return checkBoxAutoCopy?.IsChecked ?? false;
        }
    }
    public StartWindow()
    {
        InitializeComponent();

        this.startBtn.Click += (o, v) =>
        {
            var screenShot = new ScreenShot(this);

            // スクリーンショットの結果にこのウィンドウを含めないように、スクリーンショットの前に最小化します。
            this.WindowState = System.Windows.WindowState.Minimized;

            screenShot.CaptureFullScreen();

            var screenCatchWindow = new ScreenShotWorkWindow(screenShot);
            screenCatchWindow.Owner = this;
            screenCatchWindow.ShowDialog();

            this.WindowState = System.Windows.WindowState.Normal;
        };
    }
    void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}