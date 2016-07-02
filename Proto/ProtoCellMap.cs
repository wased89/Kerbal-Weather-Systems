using Database;
using ProtoBuf;

namespace Proto
{
    [ProtoContract]
    public class ProtoCellMap
    {
        [ProtoMember(1)]
        public KWSCellMap<WeatherCell> map;
        
    }
}
