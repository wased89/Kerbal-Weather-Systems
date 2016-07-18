
﻿using System.Collections.Generic;

namespace Database
{
    public class PlanetData
    {
        public int index; //index assigned to you by KWS for accessing the Planet Data from the database
        public int gridLevel;
        public float meanTropoHeight; //mean tropopause altitutde
        public float irradiance; //the value of incoming SW radiaton, ie. Kerbin = 1360.8
        //public float SHF; //scale height factor constant
        public float SH_correction; // ScaleHeight correction (to best approach KSP pressure curves)
        public double updateTime; //time since last update
        public int layers;
        public int stratoLayers;
        public CelestialBody body;
        public AtmoData atmoData;
        public DewData dewData;
        public Dictionary<float, float> TropAlts = new Dictionary<float, float>(); //one time init for the tropopause altitudes, Key = latitude, Value = altitude
        public KWSCellMap<string> biomes = new KWSCellMap<string>(5);
        public Dictionary<string, BiomeData> biomeDatas = new Dictionary<string, BiomeData>(); //biome data for all the biomes

        public List<KWSCellMap<WeatherCell>> LiveMap = new List<KWSCellMap<WeatherCell>>();
        public KWSCellMap<SoilCell> LiveSoilMap = new KWSCellMap<SoilCell>(1); //1 as default value because we re-assign it anyways
        public List<KWSCellMap<WeatherCell>> LiveStratoMap = new List<KWSCellMap<WeatherCell>>();

        public List<KWSCellMap<WeatherCell>> BufferMap = new List<KWSCellMap<WeatherCell>>();
        public KWSCellMap<SoilCell> BufferSoilMap = new KWSCellMap<SoilCell>(1);
        public List<KWSCellMap<WeatherCell>> BufferStratoMap = new List<KWSCellMap<WeatherCell>>();
    }
}
