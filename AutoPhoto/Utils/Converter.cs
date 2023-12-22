using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput.Native;

namespace AutoPhoto.Utils
{
    public static class Converter
    {
        public static Bitmap ConvertToFormat(this System.Drawing.Image image, PixelFormat format)
        {
            Bitmap copy = new Bitmap(image.Width, image.Height, format);
            using (Graphics gr = Graphics.FromImage(copy))
            {
                gr.DrawImage(image, new Rectangle(0, 0, copy.Width, copy.Height));
            }
            return copy;
        }

        public static Bitmap ConvertToFormatAndCut(this System.Drawing.Image image, PixelFormat format)
        {
            Bitmap copy = new Bitmap(image.Width / 4, image.Height / 8, format);
            using (Graphics gr = Graphics.FromImage(copy))
            {
                gr.DrawImage(image, new Rectangle(0, 0, copy.Width, copy.Height), new Rectangle(3 * copy.Width, 0, copy.Width, copy.Height), GraphicsUnit.Pixel);
            }
            return copy;
        }

        public static VirtualKeyCode ToVirtualKeyCode(this string key)
        {
            switch (key)
            {
                case "F": return VirtualKeyCode.VK_F;
                case "1": return VirtualKeyCode.VK_1;
                case "2": return VirtualKeyCode.VK_2;
                case "3": return VirtualKeyCode.VK_3;
                case "4": return VirtualKeyCode.VK_4;
                case "5": return VirtualKeyCode.VK_5;
                case "6": return VirtualKeyCode.VK_6;
                case "7": return VirtualKeyCode.VK_7;
                case "8": return VirtualKeyCode.VK_8;
                case "9": return VirtualKeyCode.VK_9;
                case "0": return VirtualKeyCode.VK_0;
                default: return VirtualKeyCode.VK_1;
            }
        }
    }
}
