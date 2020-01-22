using System;

namespace Itinero.Transit.IO.VectorTiles
{
    public class BBox
    {
        private double _minLat = double.MaxValue;
        private double _minLon = double.MaxValue;
        private double _maxLat = double.MinValue;
        private double _maxLon = double.MinValue;


        public void AddCoordinate((double Longitude, double Latitude) c)
        {
            _minLat = Math.Min(c.Latitude, _minLat);
            _minLon = Math.Min(c.Longitude, _minLon);
            _maxLat = Math.Max(c.Latitude, _maxLat);
            _maxLon = Math.Max(c.Longitude, _maxLon);
        }

        public void AddBBox(BBox bbox)
        {
            AddCoordinate((bbox._minLon, bbox._minLat));
            AddCoordinate((bbox._maxLon, bbox._maxLat));
        }


        public string ToJson()
        {
            return $"[{_minLon}, {_minLat}, {_maxLon}, {_maxLat}]";
        }
    }
}