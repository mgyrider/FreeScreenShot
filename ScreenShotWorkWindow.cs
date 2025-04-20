// // Copyright (c) mgyrider. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System;
using static FreeScreenShot.ScreenShotWorkState;
namespace FreeScreenShot;

/// <summary>
/// Interaction logic for ScreenShotWorkWindow.xaml
/// </summary>

enum ScreenShotWorkState
{
    Ready,
    Pick,
    Rotate,
    AdjustWH,
    ShowPreviewResult
}

public partial class ScreenShotWorkWindow : Window
{
    Point startPoint;
    Point endPoint;
    ScreenShot screenShot;
    Double rotatedDegree = 0;

    ScreenShotWorkState innerWorkState = Ready;
    ScreenShotWorkState workState
    {
        get
        {
            return innerWorkState;
        }
        set
        {
            if (value == Ready)
            {
                previewRect.Width = 0;
                previewRect.Height = 0;
                polygonBackground.Points = new PointCollection([backgroundLeftTop, backgroundRightTop, backgroundRightDown, backgroundLeftDown]);
            }

            Visibility vb = (value == AdjustWH ? Visibility.Visible : Visibility.Collapsed);
            adjustVisualLineX.Visibility = vb;
            adjustVisualLineY.Visibility = vb;
            if (value == AdjustWH)
            {
                adjustVisualLineX.RenderTransform = new RotateTransform(rotatedDegree, startPoint.X, startPoint.Y);
                adjustVisualLineY.RenderTransform = new RotateTransform(rotatedDegree, startPoint.X, startPoint.Y);
            }

            if (value != ShowPreviewResult)
            {
                Canvas.SetLeft(toolPanel, -toolPanel.MaxWidth);
                Canvas.SetTop(toolPanel, -toolPanel.MaxHeight);
            }
            innerWorkState = value;
        }
    }

    Point backgroundLeftTop;
    Point backgroundRightTop;
    Point backgroundLeftDown;
    Point backgroundRightDown;

    public ScreenShotWorkWindow(ScreenShot screenShot)
    {
        InitializeComponent();

        this.screenShot = screenShot;
        this.desktopImage.Source = screenShot.fullScreenShotImage;
        this.Loaded += (o, e) =>
        {
            this.Activate();
            //マルチスクリーンで、windowのポジションが確定された後で、サーズが最大にする
            this.WindowState = System.Windows.WindowState.Maximized;
        };

        //　myCanvas.LoadedはwindowのLoadedイベントの後で呼び出される
        myCanvas.Loaded += (o, e) =>
        {
            Console.WriteLine($"window loaded {this.Left} {this.Top} {this.Width} {this.Height}, canvas {myCanvas.ActualHeight} {myCanvas.ActualWidth}");

            backgroundLeftTop = new Point(-myCanvas.ActualWidth, -myCanvas.ActualHeight);
            backgroundRightTop = new Point(myCanvas.ActualWidth * 2, -myCanvas.ActualHeight);
            backgroundLeftDown = new Point(-myCanvas.ActualWidth, myCanvas.ActualHeight * 2);
            backgroundRightDown = new Point(myCanvas.ActualWidth * 2, myCanvas.ActualHeight * 2);
            polygonBackground.Points = new PointCollection([backgroundLeftTop, backgroundRightTop, backgroundRightDown, backgroundLeftDown]);
        };

        myCanvas.MouseDown += MouseDownHandle;
        myCanvas.PreviewMouseMove += MousePreviewMoveHandle;
        myCanvas.MouseMove += MouseMoveHandle;
        myCanvas.MouseLeftButtonUp += MouseUpHandle;

        this.KeyDown += (o, e) =>
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        };

        toolPanelAdjustBtn.Click += (o, v) =>
        {
            workState = AdjustWH;
        };
        toolPanelRotateBtn.Click += (o, v) =>
        {
            workState = Rotate;
        };
        toolPanelRedoBtn.Click += (o, v) =>
        {
            workState = Ready;
        };
        toolPanelOkBtn.Click += (o, v) =>
        {
            double scale = (double)screenShot.fullScreenRect.Width / desktopImage.Width;
            int height = Math.Max((int)((endPoint.Y - startPoint.Y) * scale), 1);
            int width = Math.Max((int)((endPoint.X - startPoint.X) * scale), 1);
            int startX = (int)(startPoint.X * scale);
            int startY = (int)(startPoint.Y * scale);

            var window = new ResultWindow(screenShot,
                new ScreenShotParam(startX, startY, width, height, rotatedDegree), scale);
            window.Owner = this.Owner;

            this.Close();
            window.Show();
        };
    }

    void MouseDownHandle(object sender, MouseButtonEventArgs e)
    {
        if (e.RightButton == MouseButtonState.Pressed)
        {
            e.Handled = true;
            this.Close();
            return;
        }

        if (!((workState == ShowPreviewResult) || (workState == Ready && e.LeftButton == MouseButtonState.Pressed)))
        {
            e.Handled = true;
            return;
        }

        workState = Pick;

        var mouse = e.MouseDevice;
        startPoint = mouse.GetPosition(myCanvas);

        if (previewRect.RenderTransform is RotateTransform rotate && rotate != null)
        {
            rotatedDegree = 0;
            rotate.Angle = 0;
        }

        Canvas.SetTop(previewRect, startPoint.Y);
        Canvas.SetLeft(previewRect, startPoint.X);

        adjustVisualLineX.X1 = this.startPoint.X;
        adjustVisualLineX.Y1 = this.startPoint.Y;
        adjustVisualLineX.X2 = myCanvas.ActualWidth * 2;
        adjustVisualLineX.Y2 = this.startPoint.Y;

        adjustVisualLineY.X1 = this.startPoint.X;
        adjustVisualLineY.Y1 = this.startPoint.Y;
        adjustVisualLineY.X2 = this.startPoint.X;
        adjustVisualLineY.Y2 = myCanvas.ActualHeight * 2;
    }

    void MouseMoveHandle(object sender, MouseEventArgs e)
    {
        var mouse = e.MouseDevice;
        var previewEndPos = mouse.GetPosition(myCanvas);

        if (workState == Pick)
        {
            if (previewEndPos.Y <= startPoint.Y || previewEndPos.X <= startPoint.X)
            {
                previewRect.Height = 0;
                previewRect.Width = 0;
                return;
            }
            previewRect.Height = Math.Abs(previewEndPos.Y - startPoint.Y);
            previewRect.Width = Math.Abs(previewEndPos.X - startPoint.X);
        }
        else if (workState == Rotate)
        {
            var v1 = new Vector(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y);
            var v2 = new Vector(previewEndPos.X - startPoint.X, previewEndPos.Y - startPoint.Y);
            rotatedDegree = Vector.AngleBetween(v1, v2);
            if (previewRect.RenderTransform is RotateTransform rotate && rotate != null)
            {
                rotate.Angle = rotatedDegree;
            }
        }
        else if (workState == AdjustWH)
        {
            var rotatedEndPoint = ImageProcess.GetRotatedPos(startPoint.X, startPoint.Y, startPoint.X, endPoint.Y, rotatedDegree);
            var v1 = new Vector(rotatedEndPoint.X - startPoint.X, rotatedEndPoint.Y - startPoint.Y);
            var v2 = new Vector(previewEndPos.X - startPoint.X, previewEndPos.Y - startPoint.Y);
            var degree = Vector.AngleBetween(v2, v1);
            var radians = Double.DegreesToRadians(degree);
            var height = Math.Cos(radians) * v2.Length;
            var width = Math.Sin(radians) * v2.Length;
            if (height <= 0.0 || width <= 0.0)
            {
                return;
            }
            previewRect.Height = height;
            previewRect.Width = width;
        }

        var previewRightTop = ImageProcess.GetRotatedPos(startPoint.X, startPoint.Y, startPoint.X + previewRect.Width, startPoint.Y, rotatedDegree);
        var previewRightDown = ImageProcess.GetRotatedPos(startPoint.X, startPoint.Y, startPoint.X + previewRect.Width, startPoint.Y + previewRect.Height, rotatedDegree);
        var previewLeftDown = ImageProcess.GetRotatedPos(startPoint.X, startPoint.Y, startPoint.X, startPoint.Y + previewRect.Height, rotatedDegree);

        var points = polygonBackground.Points;
        points.Clear();
        points.Add(backgroundLeftTop);

        points.Add(startPoint);
        points.Add(previewRightTop);
        points.Add(previewRightDown);
        points.Add(previewLeftDown);
        points.Add(startPoint);

        points.Add(backgroundLeftTop);
        points.Add(backgroundLeftDown);
        points.Add(backgroundRightDown);
        points.Add(backgroundRightTop);
    }

    void MousePreviewMoveHandle(object sender, MouseEventArgs e)
    {
        if (workState == Rotate || workState == AdjustWH)
        {
            return;
        }
        if (workState == Pick && e.LeftButton == MouseButtonState.Pressed)
        {
            return;
        }
        e.Handled = true;
    }

    void MouseUpHandle(object sender, MouseButtonEventArgs e)
    {
        var mouse = e.MouseDevice;
        var point = mouse.GetPosition(myCanvas);

        if (workState == Pick || workState == AdjustWH)
        {
            if (previewRect.Width <= 0.0 || previewRect.Height <= 0.0)
            {
                workState = Ready;
                return;
            }
            this.endPoint = new Point(startPoint.X + previewRect.Width, startPoint.Y + previewRect.Height);
        }

        workState = ShowPreviewResult;

        var posY = point.Y;
        var posX = point.X;
        if (posY + toolPanel.ActualHeight > myCanvas.ActualHeight)
        {
            posY = myCanvas.ActualHeight - toolPanel.ActualHeight;
        }
        if (posX + toolPanel.ActualWidth > myCanvas.ActualWidth)
        {
            posX = myCanvas.ActualWidth - toolPanel.ActualWidth;
        }
        Canvas.SetTop(toolPanel, posY);
        Canvas.SetLeft(toolPanel, posX);
    }
}