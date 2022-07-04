using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNeurotuner
{
    internal class BitmapImageSource
    {

        #region Change Bitmap To ImageSource
        [System.Runtime.InteropServices.DllImport("gdi32.dll", SetLastError = true)]

        private static extern bool DeleteObject(IntPtr hObject);

        public static System.Windows.Media.ImageSource ChangeBitmapToImageSource(System.Drawing.Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            System.Windows.Media.ImageSource wpfBitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                System.Windows.Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

            if (!DeleteObject(hBitmap)) // Обратите внимание на освобождение памяти, в противном случае содержимое продолжает увеличиваться, что приводит к нехватке памяти
            {
                throw new System.ComponentModel.Win32Exception();
            }
            return wpfBitmap;

        }

        public static System.Drawing.Image ImageWpfToGDI(System.Windows.Media.ImageSource image)
        {
            MemoryStream ms = new MemoryStream();
            var encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image as System.Windows.Media.Imaging.BitmapSource));
            encoder.Save(ms);
            ms.Flush();
            return System.Drawing.Image.FromStream(ms);
        }
        #endregion 
    }
}
