using GeodesicGrid;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Overlay
{
    public class TerrainData
    {   
        private static readonly Dictionary<string, TerrainData> bodies = new Dictionary<string, TerrainData>();

        public static void Clear()
        {
            bodies.Clear();
        }

        public static TerrainData ForBody(CelestialBody body, int gridLevel)
        {
            if (body == null) { throw new ArgumentException("Body may not be null"); }
            if (!bodies.ContainsKey(body.name))
            {
                bodies[body.name] = new TerrainData(body, gridLevel);
            }
            return bodies[body.name];

        }

        private readonly CellMap<double> heightRatios;

        private TerrainData(CelestialBody body, int gridLevel)
        {
            if (body.pqsController == null) { throw new ArgumentException("Body doesn't have a PQS controller"); }
            heightRatios = new CellMap<double>(gridLevel, c => (body.pqsController.GetSurfaceHeight(c.Position) / body.pqsController.radius));
        }

        public double GetHeightRatio(Cell cell)
        {
            return heightRatios[cell];
        }
    }
}
