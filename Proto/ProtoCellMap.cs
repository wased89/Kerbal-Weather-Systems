using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Database;
using GeodesicGrid;
using ProtoBuf;
using KerbalWeatherSystems;

namespace Proto
{
    [ProtoContract]
    public class ProtoCellMap
    {
        [ProtoMember(1)]
        public KWSCellMap<WeatherCell> map;
        
    }
}
