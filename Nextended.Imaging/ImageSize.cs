using System;
using System.Drawing;
using System.Linq;
using Nextended.Core.Extensions;

namespace Nextended.Imaging
{
    internal class ImageSize
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public Size ToSize()
        {
            return new Size(Width, Height);
        }

        public static Size ParseSize(object size)
        {
            if (size is Size size1)
                return size1;
            string newSize = size.ToString().Replace("px", "");
            var result = new Size();
            if (Int32.TryParse(newSize, out var i))
            {
                result.Width = result.Height = i;
                return result;
            }
            if (newSize.Contains("{") && SerializationHelper.TryJsonDeserialize(newSize, out ImageSize tmpSize))
                return tmpSize.ToSize();
            result = ParseWithSplitter(newSize, 'x', ':', ',');
            if (result.Width != 0 || result.Height != 0)
                return result;
            return size.MapTo<Size>();
        }


        private static Size ParseWithSplitter(string newSize, params char[] possibleSplitters)
        {
            int w = 0, h = 0;
            return Enumerable.Any(Enumerable.Select(Enumerable.Where(possibleSplitters, c => newSize.Contains(c)), splitter => newSize.Split(splitter)), split => Int32.TryParse(split[0], out w) && Int32.TryParse(split[1], out h)) ? new Size(w, h) : new Size(w, h);
        }
    }
}