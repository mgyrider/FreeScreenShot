// // Copyright (c) mgyrider. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace FreeScreenShot;

public class ScreenShot
{
    System.Windows.Window resolutionMesureWindow;
    double ownerWindowPosX;
    double ownerWindowPosY;
    public System.Drawing.Rectangle fullScreenRect { get; private set; }

    public System.Windows.Media.Imaging.WriteableBitmap? fullScreenShotImage { get; private set; }
    private Color[,]? fullScrennShotPixels;

    public ScreenShot(System.Windows.Window ownerWindow)
    {
        var window = new System.Windows.Window();
        window.Topmost = true;
        window.Owner = ownerWindow;
        window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        window.WindowState = System.Windows.WindowState.Maximized;
        window.WindowStyle = System.Windows.WindowStyle.None;

        var rect = new System.Windows.Shapes.Rectangle();
        rect.Fill = System.Windows.Media.Brushes.Black;
        rect.Stroke = System.Windows.Media.Brushes.LimeGreen;
        rect.StrokeThickness = 1;
        rect.Loaded += LoadedEventHandler;
        window.Content = rect;

        window.Loaded += (o, e) =>
        {
            window.Activate();
        };

        resolutionMesureWindow = window;
        this.ownerWindowPosX = ownerWindow.Left;
        this.ownerWindowPosY = ownerWindow.Top;
    }

    async void LoadedEventHandler(object sender, System.Windows.RoutedEventArgs e)
    {
        await System.Threading.Tasks.Task.Delay(200);
        const int MAX_LEN = 10000;

        var black = System.Drawing.Color.Black;
        int left = 0, right = 0, top = 0, bottom = 0;

        // WPFのAPI（例えば、System.Windows.Forms.Screen.AllScreens）には、マルチスクリーン環境でbugがあるようです。
        // 正しい結果が得られないため、下記のように自分でスクリーンのサイズを測定します。

        // スクリーンの幅を測定する
        Bitmap pixelsOfX = new Bitmap(MAX_LEN + MAX_LEN, 1, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(pixelsOfX))
        {
            graphics.CopyFromScreen((int)ownerWindowPosX - MAX_LEN, (int)ownerWindowPosY, 0, 0, pixelsOfX.Size);
            int index = MAX_LEN;
            for (index = MAX_LEN; index < pixelsOfX.Width; index++)
            {
                var color = pixelsOfX.GetPixel(index, 0);
                if (color.A != black.A || color.R != black.R || color.G != black.G || color.B != black.B)
                {
                    right = (int)ownerWindowPosX + (index - MAX_LEN);
                    break;
                }
            }
            for (index = MAX_LEN; index >= 0; index--)
            {
                var color = pixelsOfX.GetPixel(index, 0);
                if (color.A != black.A || color.R != black.R || color.G != black.G || color.B != black.B)
                {
                    left = (int)ownerWindowPosX - (MAX_LEN - index);
                    break;
                }
            }
        }

        // スクリーンの高さを測定する
        Bitmap pixelsOfY = new Bitmap(1, MAX_LEN + MAX_LEN, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(pixelsOfY))
        {
            graphics.CopyFromScreen((int)ownerWindowPosX, (int)ownerWindowPosY - MAX_LEN, 0, 0, pixelsOfY.Size);
            int index = MAX_LEN;
            for (index = MAX_LEN; index < pixelsOfY.Height; index++)
            {
                var color = pixelsOfY.GetPixel(0, index);
                if (color.A != black.A || color.R != black.R || color.G != black.G || color.B != black.B)
                {
                    bottom = (int)ownerWindowPosY + (index - MAX_LEN);
                    break;
                }
            }
            for (index = MAX_LEN; index >= 0; index--)
            {
                var color = pixelsOfY.GetPixel(0, index);
                if (color.A != black.A || color.R != black.R || color.G != black.G || color.B != black.B)
                {
                    top = (int)ownerWindowPosY - (MAX_LEN - index);
                    break;
                }
            }
        }

        this.fullScreenRect = new System.Drawing.Rectangle(left, top, right - left + 1, bottom - top + 1);
        System.Console.WriteLine($"Screen size is {this.fullScreenRect}");

        resolutionMesureWindow.Close();
        return;
    }

    public byte[]? CreateScreenShotPixelBytes(ScreenShotParam param, Action<int>? reportProgress = null)
    {
        return CreateScreenShotPixelBytes(param.startX, param.startY, param.width, param.height, param.rotatedDegree, reportProgress);
    }

    byte[]? CreateScreenShotPixelBytes(int startX, int startY, int width, int height, double rotatedDegree = 0.0, Action<int>? reportProgress = null)
    {
        if (fullScrennShotPixels == null)
        {
            return null;
        }
        DateTime t1 = DateTime.Now;
        const int minPixelNumWhenMultiTask = 10000;
        const int reportTime = 10000;

        (int x, int y)[] positions = new (int, int)[height * width];
        int cnt = 0;
        for (int y = startY; y < startY + height; y++)
        {
            for (int x = startX; x < startX + width; x++)
            {
                positions[cnt++] = (x, y);
            }
        }

        int taskNum = (positions.Length <= minPixelNumWhenMultiTask ? 1 : 8);
        int stride = positions.Length / taskNum;
        int[] index = new int[taskNum];
        for (int i = 1; i < taskNum; i++)
        {
            index[i] = index[i - 1] + stride;
        }

        Func<int, int, Color> getPixel = (i, j) => fullScrennShotPixels[i, j];
        if (!rotatedDegree.Equals(0.0))
        {
            getPixel = (i, j) =>
            {
                var newPoint = ImageProcess.GetRotatedPos(startX, startY, i, j, rotatedDegree);
                return ImageProcess.GetPixelByBicubicInterpolation(fullScrennShotPixels, newPoint.X, newPoint.Y);
            };
        }

        int reportCnt = 0;
        double total = height * width;
        byte[] pixels = new byte[height * width * 4];
        System.Threading.Tasks.Parallel.For(0, taskNum, (arg) =>
        {
            int start = index[arg];
            int end = Math.Min(start + stride, positions.Length);
            for (int i = start; i < end; i++)
            {
                Color color = getPixel(positions[i].x, positions[i].y);
                int j = i * 4;
                pixels[j++] = color.B;
                pixels[j++] = color.G;
                pixels[j++] = color.R;
                pixels[j++] = color.A;
                if (reportProgress == null)
                {
                    continue;
                }
                System.Threading.Interlocked.Add(ref reportCnt, 1);
                if (reportCnt % reportTime == 0)
                {
                    lock (reportProgress)
                    {
                        reportProgress((int)(reportCnt / total * 100));
                    }
                }
            }
        });

        DateTime t2 = DateTime.Now;
        var d = t2 - t1;
        System.Console.WriteLine($"time cost {d.TotalMilliseconds}");

        return pixels;
    }

    public static System.Windows.Media.Imaging.WriteableBitmap CreateImageSourceFromPixels(byte[] pixels, int width, int height)
    {
        var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32, null);
        bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixels, width * 4, 0);
        return bitmap;
    }

    public void CaptureFullScreen()
    {
        // スクリーンの解像度を取得する
        resolutionMesureWindow.ShowDialog();

        var screenShotBitmap = new Bitmap((int)(fullScreenRect.Width), (int)(fullScreenRect.Height), PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(screenShotBitmap))
        {
            // WPFのAPIを使って、あらかじめ全画面のスクリーンショットを撮ります
            graphics.CopyFromScreen(fullScreenRect.X, fullScreenRect.Y, 0, 0, screenShotBitmap.Size, CopyPixelOperation.SourceCopy);
        }

        fullScrennShotPixels = new Color[screenShotBitmap.Width, screenShotBitmap.Height];
        for (int i = 0; i < screenShotBitmap.Width; i++)
        {
            for (int j = 0; j < screenShotBitmap.Height; j++)
            {
                fullScrennShotPixels[i, j] = screenShotBitmap.GetPixel(i, j);
            }
        }

        byte[]? pixels = CreateScreenShotPixelBytes(0, 0, screenShotBitmap.Width, screenShotBitmap.Height);
        if (pixels == null)
        {
            return;
        }

        this.fullScreenShotImage = CreateImageSourceFromPixels(pixels, screenShotBitmap.Width, screenShotBitmap.Height);
    }
}

public class ScreenShotParam
{
    public int startX;
    public int startY;
    public int width;
    public int height;
    public double rotatedDegree;
    public ScreenShotParam(int x, int y, int w, int h, double r)
    {
        startX = x;
        startY = y;
        width = w;
        height = h;
        rotatedDegree = r;
    }
}

class ImageProcess
{

    static Color GetPixelWithCheck(Color[,] pixels, int x, int y)
    {
        if ((pixels == null) || (x < 0) || (x >= pixels.GetLength(0)) || (y < 0) || (y >= pixels.GetLength(1)))
        {
            return Color.Black;
        }
        return pixels[x, y];
    }

    // (centerX, centerY) を中心に時計回りに回転させた後の座標 (x, y) を求める
    public static System.Windows.Point GetRotatedPos(double centerX, double centerY, double x, double y, double angleDegree)
    {
        double radians = Double.DegreesToRadians(angleDegree);
        double newX = (x - centerX) * Math.Cos(radians) - (y - centerY) * Math.Sin(radians) + centerX;
        double newY = (x - centerX) * Math.Sin(radians) + (y - centerY) * Math.Cos(radians) + centerY;

        return new System.Windows.Point(newX, newY);
    }

    // アルゴリズムの詳しい説明：https://en.wikipedia.org/wiki/Bicubic_interpolation
    public static Color GetPixelByBicubicInterpolation(Color[,] pixels, double x, double y)
    {
        int ox = (int)Math.Floor(x);
        int oy = (int)Math.Floor(y);
        double u = x - ox;
        double v = y - oy;

        double[] weightX = new double[] { 1 + u, u, 1 - u, 2 - u };
        double[] weightY = new double[] { 1 + v, v, 1 - v, 2 - v };

        static double BicubicWeightFunc(double x)
        {
            double abs = Math.Abs(x);
            if (abs < 1.0)
            {
                return 1 - 2 * abs * abs + abs * abs * abs;
            }
            else if (abs > 1.0 && abs < 2.0)
            {
                return 4 - 8 * abs + 5 * abs * abs - abs * abs * abs;
            }
            else
            {
                return 0;
            }
        }

        for (int i = 0; i < 4; i++)
        {
            weightX[i] = BicubicWeightFunc(weightX[i]);
            weightY[i] = BicubicWeightFunc(weightY[i]);
        }

        int[] offsetY = new int[] { -1, 0, 1, 2 };
        int[] offsetX = new int[] { -1, 0, 1, 2 };

        double[] argb = new double[4];

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                var temp = GetPixelWithCheck(pixels, ox + offsetX[i], oy + offsetY[j]);
                var w = weightX[i] * weightY[j];
                argb[0] += temp.A * w;
                argb[1] += temp.R * w;
                argb[2] += temp.G * w;
                argb[3] += temp.B * w;
            }
        }
        for (int i = 0; i < argb.Length; i++)
        {
            if (argb[i] > byte.MaxValue)
            {
                argb[i] = byte.MaxValue;
            }
            else if (argb[i] < 0)
            {
                argb[i] = 0;
            }
        }
        return Color.FromArgb((int)argb[0], (int)argb[1], (int)argb[2], (int)argb[3]);
    }
}