using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeodesicGrid;
using ProtoBuf;

namespace Database
{
    [ProtoContract(IgnoreListHandling = true)]
    public class KWSCellMap<T> : IEnumerable<KeyValuePair<Cell, T>>
    {
        [ProtoMember(1)]
        private readonly T[] values;
        public KWSCellMap()
        {
            
        }
        public KWSCellMap(int level)
        {
            values = new T[Cell.CountAtLevel(level)];
        }
        public KWSCellMap(T[] vals)
        {
            values = vals;
        }
        public KWSCellMap(int level, Func<Cell, T> selector)
        {
            values = Cell.AtLevel(level).Select(selector).ToArray();
        }
        public KWSCellMap(KWSCellMap<T> other)
        {
            values = other.values.ToArray();
        }
        public int Level
        {
            get { return new Cell((uint)values.Length - 1).Level; }
        }

        public T this[Cell cell]
        {
            get
            {
                if (cell.Index >= values.Length) { throw new ArgumentException(); }
                return values[cell.Index];
            }
            set
            {
                if (cell.Index >= values.Length) { throw new ArgumentException(); }
                values[cell.Index] = value;
            }
        }

        public IEnumerator<KeyValuePair<Cell, T>> GetEnumerator()
        {
            for (uint i = 0; i < values.Length; i++)
            {
                yield return new KeyValuePair<Cell, T>(new Cell(i), values[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}
