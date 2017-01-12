using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using XLabs.Platform.Services.Media;

namespace CYINT.XlabsSkiaPixelAnalyzer
{
    public class PixelAnalyzer
    {
        protected SKBitmap _sourceImage;   
        protected TotalData? _Totals;
        protected delegate void ScanImageCallBackDelegate(int x, int y);
        protected delegate void ScanMaskCallBackDelegate(int x, int y, int index);
        protected List<int> _zeroedMask;
        protected int
            _width
            ,_height
            ,_totalPixels
        ;

        public struct TotalData
        {
            public Dictionary<string, TotalValue> values;
        }

        public struct TotalValue
        {
            public double value;
            public double variance;
            public double standardDeviation;
        }

        public PixelAnalyzer(MediaFile mediaFile)
        {
            SKManagedStream stream;
            _width = 0;
            _height = 0;
            SetTotals(null);
            stream = new SKManagedStream(mediaFile.Source);
            SetSourceImage(SKBitmap.Decode(stream));            
        }

        protected void ScanImagePixels(ScanImageCallBackDelegate Callback)
        {
            for (int y = 0; y < _height; y++)             
                for (int x = 0; x < _width; x++)
                    Callback(x,y);
        }

        protected void ScanMask(ScanMaskCallBackDelegate Callback)
        {
            for (int index = 0; index < _totalPixels; index++)                                      
                Callback(getIndexX(index),getIndexY(index),index);               
        }

        public void SetZeroedMask()
        {
            int totalPixels = GetTotalPixels();
            _zeroedMask = new List<int>();

            for(int index = 0; index < totalPixels; index++)            
                _zeroedMask.Add(0);            
        }

        public void SetWidth(int width)
        {
            _width = width;
            if(GetHeight() != 0)
                SetTotalPixels(_width * _height);
        }

        public void SetHeight(int height)
        {
            _height = height;
            if(GetWidth() != 0)
                SetTotalPixels(_width * _height);
        }

        public void SetTotalPixels(int totalPixels)
        {
            _totalPixels = totalPixels;
            SetZeroedMask();
            SetTotals(null);
        }

        public void SetSourceImage(SKBitmap sourceImage)
        {
            _sourceImage = sourceImage;
           SetWidth(_sourceImage.Width);
           SetHeight(_sourceImage.Height);
        }

        private void CalculateTotals()
        {    
            double globalAverageDelta;
            List<double> localAverageDeltas = new List<double>();         
            TotalData Totals = new TotalData();
            TotalValue AverageDelta;

            globalAverageDelta = 0;
            ScanImagePixels( 
                (int x, int y) =>
                {
                    SKColor pixelColor;  
                    double intensity, localAverageDelta = 0d;
                    int localDeltaCount = 0;
                    SKColor? [] colors = new SKColor?[4] {
                        (x > 0 ) ? (SKColor?)_sourceImage.GetPixel(x - 1, y) : null,
                        (x < _width - 1) ? (SKColor?)_sourceImage.GetPixel(x + 1, y) : null,
                        (y > 0) ? (SKColor?)_sourceImage.GetPixel(x, y - 1) : null,
                        (y < _height - 1) ? (SKColor?)_sourceImage.GetPixel(x, y + 1) : null
                    };

                    pixelColor = _sourceImage.GetPixel(x, y);
                    intensity = (pixelColor.Red + pixelColor.Green + pixelColor.Blue) / 3;

                    foreach(SKColor? selectedColor in colors)
                    {
                        if(selectedColor != null)
                        {
                            localAverageDelta += calculateDelta(intensity, (SKColor)selectedColor);
                            localDeltaCount++;
                        }
                    }
                                        
                    if (localDeltaCount > 0)
                    {
                        localAverageDelta = localAverageDelta / localDeltaCount;
                        globalAverageDelta += localAverageDelta;
                        localAverageDeltas.Add(localAverageDelta);
                    }
                }                
            );

            AverageDelta = calculateAverageDeltas(localAverageDeltas, globalAverageDelta);

            if(Totals.values.ContainsKey("AverageDelta"))
                Totals.values["AverageDelta"] = AverageDelta;
            else
                Totals.values.Add("AverageDelta", AverageDelta);
            
            SetTotals(Totals);
        }


        private double calculateDelta(double intensity, SKColor pixelColor)
        {
            double adjacentIntensity, delta;
            adjacentIntensity = (pixelColor.Red + pixelColor.Green + pixelColor.Blue) / 3;
            delta = Math.Abs(intensity - adjacentIntensity);
            return delta;
        }




        private TotalValue calculateAverageDeltas(List<double> localAverages, double globalAverage)
        {
            TotalValue AverageDelta = new TotalValue();
            double sum = 0;

            AverageDelta.value = 0;
            AverageDelta.standardDeviation = 0;
            AverageDelta.variance = 0;

            if (localAverages.Count > 0)
            {
                globalAverage = globalAverage / localAverages.Count;
                AverageDelta.value = globalAverage;
                sum = 0;
                foreach( double localAverage in localAverages )
                {
                    sum += Math.Pow(localAverage - AverageDelta.value,2d);
                }
                AverageDelta.variance = sum / localAverages.Count;
                AverageDelta.standardDeviation = Math.Sqrt(AverageDelta.variance);
            }

            return AverageDelta;
        }

        public void SetTotals(TotalData? Totals)
        {
            _Totals = Totals;
        }


        public TotalData GetTotals()
        {
            if(_Totals == null)
                CalculateTotals();

            return (TotalData)_Totals;
        }

        public TotalValue GetTotalByKey(string key)
        {
            TotalData Totals = GetTotals();
            if(Totals.values.ContainsKey(key))
                return Totals.values[key];

            throw new PixelAnalyzerException("Total for key " +  key + " not found.");
        }


        protected bool validIndex(int index)
        {
            return (index > -1 && index < _totalPixels);                
        }

        protected int getIndexY(int index)
        {
            return index / _width;
        }

        protected int getIndexX(int index)
        {
            return index % _width;
        }

        protected int convertCoordsToIndex(int x, int y)
        {
            return (y * _width) + x;
        }

        public int GetWidth()
        {
            return _width;
        }

        public int GetHeight()
        {
            return _height;
        }

        public int GetTotalPixels()
        {
            return _totalPixels;
        }

        public List<int> GetZeroedMask()
        {
            return _zeroedMask;
        }

        public SKBitmap GetSourceImage()
        {
            return _sourceImage;
        }

    }

    class PixelAnalyzerException : Exception
    {
        public PixelAnalyzerException(string message) :  base(message) { }
    }
}