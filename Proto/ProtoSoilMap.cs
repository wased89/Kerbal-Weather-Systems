using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Database;
using ProtoBuf;

namespace Proto
{
    [ProtoContract]
    public class ProtoSoilMap
    {
        [ProtoMember(1)]
        public KWSCellMap<SoilCell> map;
    }
}
