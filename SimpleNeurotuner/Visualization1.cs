using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;

namespace SimpleNeurotuner
{
    public static class Visualization1
    {
        public static void DrawNormalizedAudio(ref float[] data, System.Windows.Controls.Image pb,
    Color color)
        {
            Bitmap bmp;
            if (pb.Source == null)
            {
                bmp = new Bitmap(pb.Width.ToString(), Convert.ToBoolean(pb.Height));
            }
            else
            {
                bmp = (Bitmap)BitmapImageSource.ImageWpfToGDI(pb.Source); ;
            }

            int BORDER_WIDTH = 5;
            int width = bmp.Width - (2 * BORDER_WIDTH);
            int height = bmp.Height - (2 * BORDER_WIDTH);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                Pen pen = new Pen(color);
                int size = data.Length;
                for (int iPixel = 0; iPixel < width; iPixel++)
                {
                    // determine start and end points within WAV
                    int start = (int)((float)iPixel * ((float)size / (float)width));
                    int end = (int)((float)(iPixel + 1) * ((float)size / (float)width));
                    float min = float.MaxValue;
                    float max = float.MinValue;
                    for (int i = start; i < end; i++)
                    {
                        float val = data[i];
                        min = val < min ? val : min;
                        max = val > max ? val : max;
                    }
                    int yMax = BORDER_WIDTH + height - (int)((max + 1) * .5 * height);
                    int yMin = BORDER_WIDTH + height - (int)((min + 1) * .5 * height);
                    g.DrawLine(pen, iPixel + BORDER_WIDTH, yMax,
                        iPixel + BORDER_WIDTH, yMin);
                }
            }
            pb.Source = BitmapImageSource.ChangeBitmapToImageSource(bmp);
        }
    }
}
