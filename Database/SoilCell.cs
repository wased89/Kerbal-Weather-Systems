using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Database
{
    [ProtoContract]
    public struct SoilCell
    {
        [ProtoMember(1)]
        public float temperature { get; internal set; }
        public byte GetCCN()
        {
            return 1;
        }
    }
}