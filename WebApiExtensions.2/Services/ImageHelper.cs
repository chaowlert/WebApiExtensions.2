using System.Drawing;
using System.IO;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;

namespace WebApiExtensions.Services
{
    public static class ImageHelper
    {
        private static readonly ISupportedImageFormat jpeg = new JpegFormat { Quality = 80 };
        public static void Resize(Stream src, Stream dest, int width, int height, ResizeMode mode = ResizeMode.Max)
        {
            using (var img = new ImageFactory())
            {
                img.Load(src)
                    .AutoRotate()
                    .BackgroundColor(Color.White)
                    .Resize(new ResizeLayer(new Size(width, height), mode))
                    //.GaussianSharpen(gauss3)
                    .Format(jpeg)
                    .Save(dest);
                dest.Position = 0;
            }
        }
    }
}