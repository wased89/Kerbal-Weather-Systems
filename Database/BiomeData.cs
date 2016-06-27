using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Database
{
    public class BiomeData
    {
        public string name;
        public float Albedo; //albedo of the biome
        public float SoilThermalCap; //The thermal capacity of the soil in the biome
        public float FLC; //Free liquid content in biome soil, ie. Percentage of liquid content in soil, from 0-1
    }
}
