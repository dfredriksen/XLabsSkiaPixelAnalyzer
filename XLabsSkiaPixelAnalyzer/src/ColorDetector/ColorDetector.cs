using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using XLabs.Platform.Services.Media;

namespace CYINT.XlabsSkiaPixelAnalyzer
{
    public class ColorDetector : PixelAnalyzer
    {
        protected Dictionary<SKColor, List<int>> _colors;

        public ColorDetector(MediaFile mediaFile) : base(mediaFile)
        {
            _colors = null;
        }

        public void DetectColors()
        {   
            int index = -1;
            SKColor currentColor;
            SetColors(new Dictionary<SKColor, List<int>>());                    

            ScanImagePixels(
                (int x, int y) =>
                {                                 
                    index = convertCoordsToIndex(x,y);
                    currentColor = _sourceImage.GetPixel(x,y);
                    if(!_colors.ContainsKey(currentColor))                                                              
                        _colors.Add(currentColor, new List<int>());   
                                        
                    _colors[currentColor].Add(index);
                }
            );
        }


        public void SetColors(Dictionary<SKColor, List<int>> colors)
        {
            _colors = colors;
        }


        public Dictionary<SKColor, List<int>> GetColors()
        {
            if(_colors == null)
                DetectColors();

            return _colors;
        }
    }
}
