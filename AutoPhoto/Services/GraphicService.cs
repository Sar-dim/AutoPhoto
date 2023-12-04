using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using AutoPhoto.Models;


namespace AutoPhoto.Services
{
    public static class GraphicService
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Models.Rect rect);
        public static Bitmap CaptureApplication(string procName)
        {
            var proc = Process.GetProcessesByName(procName).First(x => x.MainModule.FileName.Contains("R2 Original 1338"));
            //proc.MainModule.FileName
            var rect = new Rect();
            GetWindowRect(proc.MainWindowHandle, ref rect);

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }
            bmp.Save("test.png", ImageFormat.Png);
            return bmp;
        }

        public static Color GetPixelFromApplication(string procName, Point point)
        {
            var proc = Process.GetProcessesByName(procName).First(x => x.MainModule.FileName.Contains("R2 Original 1338"));
            //proc.MainModule.FileName
            var rect = new Rect();
            GetWindowRect(proc.MainWindowHandle, ref rect);

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;
            var color = new Color();

            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bmp))
                {
                    graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                    color = bmp.GetPixel(point.X, point.Y);
                }
            }
            
            return color;
        }

        public static Dictionary<string, Color> GetPixelsFromApplication(string procName, Dictionary<string, Point> points)
        {
            var proc = Process.GetProcessesByName(procName).First(x => x.MainModule.FileName.Contains("R2 Original 1338"));
            //proc.MainModule.FileName
            var rect = new Rect();
            GetWindowRect(proc.MainWindowHandle, ref rect);
            
            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;
            var colors = new Dictionary<string, Color>();

            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bmp))
                {
                    graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                    
                    foreach (var point in points)
                    {
                        var color = new Color();

                        color = bmp.GetPixel(point.Value.X, point.Value.Y);
                        colors.Add(point.Key, color);
                    }
                    
                }
            }

            return colors;
        }

        public static IntPtr GetProccessPointer(string procName)
        {
            var proc = Process.GetProcessesByName(procName).First(x => x.MainModule.FileName.Contains("R2 Original 1338"));
            return proc.MainWindowHandle;
        }

        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr point);
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowDC(IntPtr window);
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern uint GetPixel(IntPtr dc, int x, int y);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(IntPtr window, IntPtr dc);
        public static Color GetColorAtDesktopScreen(int x, int y)
        {
            IntPtr desk = GetDesktopWindow();
            IntPtr dc = GetWindowDC(desk);
            int a = (int)GetPixel(dc, x, y);
            ReleaseDC(desk, dc);
            return Color.FromArgb(255, (a >> 0) & 0xff, (a >> 8) & 0xff, (a >> 16) & 0xff);
        }
    }
}
