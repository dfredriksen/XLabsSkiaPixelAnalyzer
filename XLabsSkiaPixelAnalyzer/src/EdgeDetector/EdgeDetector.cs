using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XLabs.Platform.Services.Media;
using SkiaSharp;

namespace CYINT.XlabsSkiaPixelAnalyzer
{
    public class EdgeDetector : PixelAnalyzer
    {   
        private List<int> 
            _outlineMask
            ,_edgeMask
        ;
        private int _deviations;

        public EdgeDetector(MediaFile mediaFile, int deviations) : base(mediaFile)
        {       
            SetDeviations(deviations);          
        }

        public void DetectOutline()
        {                    
            SetOutlineMask(new List<int>(GetEdgeMask()));           

            ScanMask(
                (int x, int y, int index) =>
                {                
                    if(_outlineMask[index] == 1)
                        _outlineMask[index] = DetectOutlinePixel(x,y,index);         
                }
            );
        }


        private int DetectOutlinePixel(int x, int y, int index)
        {
            int [] adjacentMask = new int [4] {
                (x > 0) ? _outlineMask[convertCoordsToIndex(x-1,y)] : 0,
                (x < _width-1) ? _outlineMask[convertCoordsToIndex(x+1, y)] : 0,
                (y > 0) ? _outlineMask[convertCoordsToIndex(x,y-1)] : 0,
                (y < _height-1) ? _outlineMask[convertCoordsToIndex(x,y+1)] : 0
            };
             
            foreach( int value in adjacentMask )
            {
                if(value == 0)                
                    return 1;
            }

            return 0;
        }



        public void DetectEdges()
        {
            SKColor selectedColor;
            TotalValue AverageDelta = GetTotalByKey("AverageDelta");
            double intensity, threshold;

            threshold = Math.Floor(AverageDelta.value + (AverageDelta.standardDeviation * GetDeviations()));
            SetEdgeMask(new List<int>());

            ScanImagePixels(
                (int x, int y) => 
                {                 
                    SKColor?[] colors = new SKColor?[8] {
                        (x > 0) ? (SKColor?)_sourceImage.GetPixel(x - 1, y) : null,
                        (x < _width - 1)  ? (SKColor?)_sourceImage.GetPixel(x + 1, y) : null,
                        (y > 0) ? (SKColor?)_sourceImage.GetPixel(x, y - 1) : null,
                        (y < _height - 1) ? (SKColor?)_sourceImage.GetPixel(x, y + 1) : null,
                        (x > 0 && y > 0) ? (SKColor?)_sourceImage.GetPixel(x - 1, y - 1) : null,
                        (x < _width -1 && y > 0) ? (SKColor?)_sourceImage.GetPixel(x + 1, y - 1) : null,
                        (x > 0 && y < _height -1) ?(SKColor?) _sourceImage.GetPixel(x - 1, y + 1) : null,
                        (x < _width - 1 && y < _height - 1) ? (SKColor?)_sourceImage.GetPixel(x + 1, y + 1) : null
                    };                   
                    
                    selectedColor = _sourceImage.GetPixel(x, y);
                    intensity = (selectedColor.Red + selectedColor.Green + selectedColor.Blue) / 3;

                    foreach( SKColor? adjacentColor in colors)
                    {
                        double adjacentIntensity, delta;
                        if(adjacentColor != null)
                        {
                            adjacentIntensity = (((SKColor)adjacentColor).Red + ((SKColor)adjacentColor).Green + ((SKColor)adjacentColor).Blue) / 3;
                            delta = Math.Abs(intensity - adjacentIntensity);
                            if( delta >  threshold )
                            {
                                _edgeMask.Add(1);
                                return;
                            }
                        }
                    }                         
                    
                    _edgeMask.Add(0);
                }            
            );           
        }

        public void SetOutlineMask(List<int> outlineMask)
        {
            _outlineMask = outlineMask;
        }

        public void SetEdgeMask(List<int> edgeMask)
        {
            _edgeMask = edgeMask;
        }

        public List<int> GetOutlineMask()
        {            
            if(_outlineMask == null)
            {
                if(_edgeMask == null)
                    DetectEdges();

                DetectOutline();
            }

            return _outlineMask;
        }

        public List<int> GetEdgeMask()
        {
            if(_edgeMask == null)
                DetectEdges();

            return _edgeMask;
        }

        public new void SetTotalPixels(int totalPixels)
        {
            base.SetTotalPixels(totalPixels);
            SetOutlineMask(null);
            SetEdgeMask(null);
        }

        public void SetDeviations(int deviations)
        {
            _deviations = deviations;
            SetOutlineMask(null);
            SetEdgeMask(null);
        }

        public int GetDeviations()
        {
            return _deviations;
        }
    }
}
