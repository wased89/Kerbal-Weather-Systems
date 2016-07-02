using Database;
using ProtoBuf;

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
