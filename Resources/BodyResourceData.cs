using System;
using Database;
using GeodesicGrid;
using UnityEngine;

namespace Resources
{
    public class BodyResourceData
    {
        private readonly CellSet scans;

        public IBodyResources Resources { get; private set; }

        protected BodyResourceData(IBodyResources resources, CellSet scans)
        {
            Resources = resources;
            this.scans = scans;
        }

        public bool IsCellScanned(Cell cell)
        {
            return scans[cell];
        }

        public void ScanCell(Cell cell)
        {
            scans[cell] = true;
        }

        public static BodyResourceData Load(IResourceGenerator generator, PlanetData PD, ConfigNode bodyNode)
        {
            if (bodyNode == null) { bodyNode = new ConfigNode(); }
            var resources = generator.Load(PD.body, bodyNode.GetNode("GeneratorData"));
            var scans = new CellSet(PD.gridLevel);

            var scanMask = bodyNode.GetValue("ScanMask");
            if (scanMask != null)
            {
                try
                {
                    scans = new CellSet(PD.gridLevel, WeatherDatabase.FromBase64String(scanMask));
                }
                catch (FormatException e)
                {
                    Debug.LogError(String.Format("[Kethane] Failed to parse {0} scan string, resetting ({1})", PD.body.name, e.Message));
                }
            }

            return new BodyResourceData(resources, scans);
        }

        public void Save(ConfigNode bodyNode)
        {
            bodyNode.AddValue("ScanMask", WeatherDatabase.ToBase64String(scans.ToByteArray()));

            var node = Resources.Save() ?? new ConfigNode();
            node.name = "GeneratorData";
            bodyNode.AddNode(node);
        }
    }
    public interface IResourceGenerator
    {
        IBodyResources Load(CelestialBody body, ConfigNode node);
    }
    public interface IBodyResources
    {
        ConfigNode Save();
        double MaxQuantity { get; }
        double? GetQuantity(Cell cell);
        double Extract(Cell cell, double amount);
    }
}
