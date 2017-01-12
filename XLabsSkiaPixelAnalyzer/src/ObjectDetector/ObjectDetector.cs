using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XLabs.Platform.Services.Media;

namespace CYINT.XlabsSkiaPixelAnalyzer
{
    public class ObjectDetector : PixelAnalyzer
    {
        protected Dictionary<int, List<int>> _objects;
        protected IEnumerable<KeyValuePair<int, int>> _objectsSorted;
        protected List<int> 
            _objectMask
            ,_pixelFlags
        ;

        public ObjectDetector(MediaFile mediaFile, List<int> pixelFlags) : base(mediaFile)
        {
            SetPixelFlags(pixelFlags);
        }


        public void DetectObjects()
        {   
            SetObjectMask(new List<int>(GetZeroedMask()));     
            SetObjects(new Dictionary<int, List<int>>());        
            
            int label = 1;
            int lastY = 0;
            bool onBackground = true;

            ScanImagePixels(
                (int x, int y) =>
                {                
                    int index = convertCoordsToIndex(x,y);
                    int value = 0;

                    if(_pixelFlags[index] == 1)
                    {                        
                        if(onBackground || y != lastY)
                        {
                            onBackground = false;
                            label++;
                        }

                        value = DetermineObjectLabelValue(index, x, y, label);
                        _objectMask[index] = value;
                        if(!_objects.ContainsKey(value))
                            _objects.Add(value, new List<int>());

                        _objects[value].Add(index);
                    }
                    else
                    {
                        if(!onBackground)                        
                            onBackground = true;                        
                    }
                }
            );

            DetermineLargestObject();
            sortObjectsByProximity();
        }


       
        protected int DetermineObjectLabelValue(int index, int x, int y, int label)
        {
            int value = label;                     

            int?[] adjacentPixels = new int?[4] {
                (x-1 > -1 && y-1 > -1) ? ((int?)_objectMask[convertCoordsToIndex(x-1,y-1)]) : null,
                (y-1 > -1) ? ((int?)_objectMask[convertCoordsToIndex(x,y-1)]) : null,
                (x+1 < _width && y-1 > -1) ? ((int?)_objectMask[convertCoordsToIndex(x+1,y-1)]) : null,
                (x-1 > -1) ? ((int?)_objectMask[convertCoordsToIndex(x-1,y)]) : null                       
            };

            foreach( int? adjacentLabel in adjacentPixels)
            {
                if(adjacentLabel != null && adjacentLabel != 0 && adjacentLabel < value)            
                    value = (int)adjacentLabel;                                                                       
            }

            ProcessObjectEquivalence(value, adjacentPixels);

            return value;
        }



        protected void ProcessObjectEquivalence(int value, int?[] adjacentPixels)
        {
            List<int> labels = new List<int>();
            foreach( int? label in adjacentPixels )
            {
                if(label != null && value != label && labels.IndexOf((int)label) == -1)
                    labels.Add((int)label);
            }

            if(labels.Count > 0)
            {
                foreach( int label in labels )
                {
                    if(label != 0)
                        ConvertObjectLabels(label, value);
                }
            }
        }


        protected void ConvertObjectLabels(int label, int value)
        {
            if(_objects.ContainsKey(label))
            {
                List<int> targetIndexes = new List<int>(_objects[label]);   
                _objects.Remove(label);
                if(!_objects.ContainsKey(value))
                {
                    _objects.Add(value, targetIndexes);
                }
                else
                {
                    _objects[value].AddRange(targetIndexes);
                }

                foreach (int index in targetIndexes)
                {
                    _objectMask[index] = value;
                }
            }
        }


        private void DetermineLargestObject()
        {
            int largestObjectId = 0;
            int largestObjectCount = 0;

            foreach( KeyValuePair<int, List<int>> objectData in _objects )
            {
                if(largestObjectCount < objectData.Value.Count)
                {
                    largestObjectId = objectData.Key;
                    largestObjectCount = objectData.Value.Count;
                }               
            }

            TotalData Totals = GetTotals();
            TotalValue LargestObject = new TotalValue();
            TotalValue LargestObjectCount = new TotalValue();
            LargestObject.value = largestObjectId;
            LargestObjectCount.value = largestObjectCount;
            
            Totals.values.Add("LargestObject", LargestObject);
            Totals.values.Add("LargestObjectSize", LargestObjectCount);
            SetTotals(Totals);
        }


        public void sortObjectsByProximity()
        {
            TotalData Totals; 
            List<int> largestObjectsPixels;
            Dictionary<int, int> objectDistances; 
            int[] centerCoords;
            int centerX, centerY, largestObjectId;
            Totals = GetTotals();
            largestObjectId = (int)Totals.values["LargestObject"].value;
            largestObjectsPixels = _objects[largestObjectId];
            objectDistances = new Dictionary<int, int>();
            centerCoords = averageCoords(largestObjectsPixels);
            centerX = centerCoords[0];
            centerY = centerCoords[1];
            foreach(KeyValuePair<int, List<int>> indexes in _objects)
            {
                int averageX, averageY;
                int[] objectCoords = averageCoords(indexes.Value);
                int distance;
                averageX = objectCoords[0];
                averageY = objectCoords[1];
                distance = (int)Math.Sqrt((double)(averageX^2) + (double)(averageY^2));
                objectDistances.Add(indexes.Key, distance);
            }

            _objectsSorted = objectDistances.OrderBy(pair => pair.Value);

        }



        public int[] averageCoords(List<int> indexes)
        {
            int totalX, totalY, x, y, count, averageX, averageY;
            totalX = totalY = count = 0;
         
            foreach(int index in indexes)
            {
                x = getIndexX(index);
                y = getIndexY(index);
                totalX += x;
                totalY += y;
                count++;
            }
            
            averageX = totalX / count;
            averageY = totalY / count;
            return new int[2] { averageX, averageY };
        }

        public void SetPixelFlags(List<int> pixelFlags)
        {
            _pixelFlags = pixelFlags;
            SetObjects(null);
            SetObjectMask(null);
        }

        public List<int> GetPixelFlags()
        {
            return _pixelFlags;
        }

        public void SetObjectMask(List<int> objectMask)
        {
            _objectMask = objectMask;
        }

        public List<int> GetObjectMask()
        {
            if(_objectMask == null)
                DetectObjects();         
            return _objectMask;
        }


        public void SetObjects(Dictionary<int, List<int>> objects)
        {
            _objects = objects;
        }

        public IEnumerable<KeyValuePair<int, int>> GetSortedObjects()
        {
            return _objectsSorted;
        }

        public Dictionary<int, List<int>> GetObjects()
        {
            if(_objects == null)
                DetectObjects();

            return _objects;
        }
    }
}
