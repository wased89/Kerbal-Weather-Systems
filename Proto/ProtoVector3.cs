using ProtoBuf;
using UnityEngine;

namespace Proto
{
    [ProtoContract]
    public struct ProtoVector3
    {
        // Vector3 definitions
        [ProtoMember(1)]
        public float x;
        [ProtoMember(2)]
        public float y;
        [ProtoMember(3)]
        public float z;

        // Convert Vector3d to ProtoVector3d
        public static implicit operator ProtoVector3(Vector3 input)
        {
            return new ProtoVector3
            {
                x = input.x,
                y = input.y,
                z = input.z
            };
        }

        // Convert ProtoVector3d to Vector3d
        public static implicit operator Vector3(ProtoVector3 input)
        {
            return new Vector3
            {
                x = input.x,
                y = input.y,
                z = input.z
            };
        }
        
        public override string ToString()
        {
            return "("+x+","+y+","+z+")";
        }
    }
}
