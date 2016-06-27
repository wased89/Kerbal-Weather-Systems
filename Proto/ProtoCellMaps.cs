using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Database;
using ProtoBuf;
using KerbalWeatherSystems;

namespace Proto
{
    [ProtoContract]
    public class ProtoCellMaps
    {
        [ProtoMember(1)]
        public KWSCellMap<WeatherCell> liveMaps;
        [ProtoMember(2)]
        public KWSCellMap<WeatherCell> bufferMaps;
    }
}
