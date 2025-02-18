// // Copyright (c) mgyrider. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;

namespace FreeScreenShot;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>

public partial class App : Application
{
    void Application_Startup(object sender, StartupEventArgs e)
    {
        var window = new StartWindow();
        window.Show();
    }
}