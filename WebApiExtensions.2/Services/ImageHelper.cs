using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
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

        private static readonly Color[] colorSet = {
            Color.FromArgb(145, 60, 205),
            Color.FromArgb(241, 95, 116),
            Color.FromArgb(247, 109, 60),
            Color.FromArgb(247, 216, 66),
            Color.FromArgb(44, 168, 194),
            Color.FromArgb(152, 203, 74),
            Color.FromArgb(131, 144, 152),
            Color.FromArgb(84, 129, 230),
        };
        public static void ProfileFromName(Stream dest, int size, string name)
        {
            using (var bitmap = new Bitmap(size, size))
            using (var img = new ImageFactory())
            {
                var background = colorSet[Math.Abs(name.GetHashCode()) % 8];
                var text = getValidChar(name).ToString();
                img.Load(bitmap)
                    .BackgroundColor(background)
                    .Watermark(new TextLayer
                    {
                        FontColor = Color.White,
                        FontSize = size * 7 / 10,
                        Style = FontStyle.Bold,
                        Text = text
                    })
                    .Format(jpeg)
                    .Save(dest);
                dest.Position = 0;
            }
        }
        private static readonly HashSet<char> validChars = "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรลวศษสหฬอฮฤฦ".ToHashSet();
        private static char getValidChar(string name)
        {
            return name.Take(2).Where(validChars.Contains).Select(c => (char?)c).FirstOrDefault() ?? name.ToUpper()[0];
        }

    }
}