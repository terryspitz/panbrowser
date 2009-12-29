using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
#if !TRYWPF
using System.Drawing;
using System.Drawing.Imaging;
#else
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
#endif

namespace Terry
{
    public class Helpers
    {
#if !TRYWPF
        //public static Dictionary<string, Bitmap> myBitmaps = new Dictionary<string, Bitmap>();
#else
        protected static byte[] myBuffer;
        protected static int myStride;
        protected static int myWidth;
        protected static int myHeight;
        protected static int myBitsPerPixel;
#endif
        [ThreadStaticAttribute]
        private static string myCachedText;
        [ThreadStaticAttribute]
        private static Bitmap myCacheBitmap;

        private static Helpers myLock = new Helpers();

        /// <summary>
        /// returns text in unit square
        /// </summary>
        /// <param name="text"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool TextPoint(string text, double x, double y)
        {
#if !TRYWPF
            if (double.IsNaN(x) || double.IsNaN(y) || x < 0 || y < 0 || y >= 1 || x>=1) 
                return false;
            if (text != myCachedText)
            {
                lock (myLock)
                {
                    if (text != myCachedText && !string.IsNullOrEmpty(text))
                    {
                        myCachedText = text;
                        //if (!myBitmaps.TryGetValue(text, out myCacheBitmap))
                        {
                            myCacheBitmap = new Bitmap(1, 1, PixelFormat.Format24bppRgb);
                            Font font = new Font(FontFamily.GenericSansSerif, 72);
                            Graphics g = Graphics.FromImage(myCacheBitmap);
                            SizeF size = g.MeasureString(text, font);
                            myCacheBitmap = new Bitmap((int)size.Width, (int)size.Height, PixelFormat.Format24bppRgb);
                            g = Graphics.FromImage(myCacheBitmap);
                            g.DrawString(text, font, Brushes.White, 0, 0);
                            //myBitmaps[text] = myCacheBitmap;
                        }
                    }
                }
            }
            //scale to unit width
            int i = (int)(x * (myCacheBitmap.Width - 1));
            int j = Math.Max(0, myCacheBitmap.Height - 1 - (int)(y * (myCacheBitmap.Width - 1)));

            //else scale to unit height
            //int i = Math.Min(myCacheBitmap.Width-1, (int)(x * (myCacheBitmap.Height- 1)));
            //int j = Math.Max(0, (int)(myCacheBitmap.Height - 1 - y * (myCacheBitmap.Height - 1)));
            return myCacheBitmap.GetPixel(i, j).R > 0;
#else
            lock (myLock)
            {
                if (text != myText)
                {
                    myText = text;
                    FormattedText fText = new FormattedText(text, System.Globalization.CultureInfo.InstalledUICulture,
                        FlowDirection.LeftToRight, new Typeface("Verdana"), 32, Brushes.Black);
                    MemoryStream ms = new MemoryStream();
                    DrawingVisual drawing = new DrawingVisual();
                    //drawing.Opacity = _opacity;

                    DrawingContext dc = drawing.RenderOpen();
                    dc.DrawText(fText, new Point(0,fText.Height));
                    dc.Close();

                    RenderTargetBitmap bitmap = new RenderTargetBitmap((int)Math.Ceiling(fText.WidthIncludingTrailingWhitespace), (int)fText.Height, 96, 96, PixelFormats.Default);
                    bitmap.Render(drawing); 
                    myWidth = bitmap.PixelWidth;
                    myHeight = bitmap.PixelHeight;
                    myBitsPerPixel = bitmap.Format.BitsPerPixel;

                    myStride= myWidth * myBitsPerPixel / 8;
                    myBuffer = new byte[myHeight * myStride];
                    bitmap.CopyPixels(myBuffer, myStride, 0);
                }
                System.Diagnostics.Debug.Assert(myBitsPerPixel >= 8, "next line doesn't handle more than one pixel per byte");
                return myBuffer[myBitsPerPixel/8 * ( (int)(x * (myWidth-1)) + myWidth * Math.Min(myHeight - 1, (int)(y * (myWidth/*not height*/ - 1))))] > 0;  
#endif
        }
    }

}
