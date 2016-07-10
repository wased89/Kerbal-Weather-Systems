using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using GeodesicGrid;
using KerbalWeatherSystems;

using System.IO;
using System.Threading;
using Proto;
using ProtoBuf;
using Simulation;

namespace Database
{
    public class WeatherDatabase
    {
        internal static List<PlanetData> PlanetaryData = new List<PlanetData>();
        public static int AddPlantaryData(PlanetData PD)
        {
            PlanetaryData.Add(PD);
            return PlanetaryData.Count - 1;
        }
        public static PlanetData GetPlanetData(CelestialBody cb)
        {
            return PlanetaryData.Find(pd => pd.body == cb);
        }
        internal static void SaveConfigSimData(ConfigNode node, uint cellIndex)
        {
            Debug.Log("Saving ConfigNode...");
            foreach(PlanetData PD in PlanetaryData)
            {
                //general KWS data
                //KWS Sim Data
                if (!node.HasNode("KWSSimData" + PD.body.bodyName))
                {
                    node.AddNode("KWSSimData" + PD.body.bodyName);
                }
                node.AddValue("KWSSimData" + PD.body.bodyName, cellIndex);
                node.AddValue("KWSSimData" + PD.body.bodyName, CellUpdater.run);
                node.AddValue("KWSSimData" + PD.body.bodyName, PD.updateTime);
                node.AddValue("KWSSimData" + PD.body.bodyName, PD.layers);
                node.AddValue("KWSSimData" + PD.body.bodyName, PD.stratoLayers);
                node.AddValue("KWSSimData" + PD.body.bodyName, PD.gridLevel);
                node.AddValue("KWSSimData" + PD.body.bodyName, PD.index);
                //Soil Data
                using (MemoryStream s = new MemoryStream())
                {
                    if(!node.HasNode("KWSSoilData"+PD.body.bodyName))
                    {
                        node.AddNode("KWSSoilData"+PD.body.bodyName);
                    }
                    ProtoSoilMaps pcm = new ProtoSoilMaps();
                    pcm.liveMap = PD.LiveSoilMap;
                    pcm.bufferMap = PD.BufferSoilMap;
                    Serializer.Serialize(s, pcm);
                    node.AddValue("KWSSoilData"+PD.body.bodyName, ToBase64String(s.ToArray()));
                    //Debug.Log("Length at save: " + ToBase64String(s.GetBuffer()).Length);
                }
                //Tropo Data
                for(int i = 0; i < PD.layers; i++)
                {
                    using (MemoryStream s = new MemoryStream())
                    {
                        if (!node.HasNode("KWSTropoData" + i+ PD.body.bodyName))
                        {
                            node.AddNode("KWSTropoData" +i + PD.body.bodyName);
                        }
                        ProtoCellMaps pcm = new ProtoCellMaps();
                        pcm.liveMaps = PD.LiveMap[i];
                        pcm.bufferMaps = PD.BufferMap[i];
                        Serializer.Serialize(s, pcm);
                        node.AddValue("KWSTropoData" + i + PD.body.bodyName, ToBase64String(s.ToArray()));
                    }
                }
                
                //Strato Data
                for(int i = 0; i < PD.stratoLayers; i++)
                {
                    using (MemoryStream s = new MemoryStream())
                    {
                        if (!node.HasNode("KWSStratoData" + i+ PD.body.bodyName))
                        {
                            node.AddNode("KWSStratoData" +i+ PD.body.bodyName);
                        }
                        ProtoCellMaps pcm = new ProtoCellMaps();
                        pcm.liveMaps = PD.LiveStratoMap[i];
                        pcm.bufferMaps = PD.BufferStratoMap[i];
                        Serializer.Serialize(s, pcm);
                        node.AddValue("KWSStratoData" + i + PD.body.bodyName, ToBase64String(s.ToArray()));
                    }
                }
                
            }
            Debug.Log("Saved ConfigNode");

        }
        internal static void LoadConfigSimData(ConfigNode node)
        {
            Debug.Log("Loading ConfigNode...");
            foreach(PlanetData PD in PlanetaryData)
            {
                //KWS Sim settings
                if(PD == null)
                {
                    Logger("Found null PD");
                    continue;
                }
                if(!node.HasNode("KWSSimData"+PD.body.bodyName))
                {
                    Debug.Log("No planetary data found, creating new..");
                    PD.updateTime = 10;
                    CellUpdater.run = 0;
                    WeatherSimulator.InitPlanetData(PD);
                    continue;
                }
                if(PD.TropAlts.Count == 0)
                {
                    WeatherSimulator.InitTropopauseAlts(PD);
                }
                string[] strings = node.GetValues("KWSSimData"+PD.body.bodyName);
                WeatherSimulator.cellindex = uint.Parse(strings[0]);
                CellUpdater.run = long.Parse(strings[1]);
                PD.updateTime = double.Parse(strings[2]);
                PD.index = int.Parse(strings[6]);
                PD.gridLevel = int.Parse(strings[5]);
                if(int.Parse(strings[3]) != PD.layers || int.Parse(strings[4]) != PD.stratoLayers) //check for editing or tampering or change
                {
                    Debug.Log("Change in layers detected, remaking maps");
                    PD.layers = int.Parse(strings[3]);
                    PD.stratoLayers = int.Parse(strings[4]);
                    CellUpdater.run = 0;
                    WeatherSimulator.InitPlanetData(PD); //remake the maps because the layering has changed
                    continue;
                }
                PD.layers = int.Parse(strings[3]);
                PD.stratoLayers = int.Parse(strings[4]);
                //else continue as normal
                //Soil map load
                if(!node.HasNode("KWSSoilData"+PD.body.bodyName))
                {
                    Debug.Log("Tampered/Missing Soil Data, creating new..");
                    CellUpdater.run = 0;
                    WeatherSimulator.InitPlanetData(PD);
                    continue;
                }
                //Debug.Log("Node char count: " + node.GetValue("KWSSoilData" + PD.body.bodyName).Length);
                //Debug.Log("Length at load: " + FromBase64String(node.GetValue("KWSSoilData" + PD.body.bodyName)).Length);
                using (MemoryStream s = new MemoryStream(FromBase64String(node.GetValue("KWSSoilData" + PD.body.bodyName))))
                {
                    ProtoSoilMaps pcm = Serializer.Deserialize<ProtoSoilMaps>(s);
                    PD.LiveSoilMap = pcm.liveMap;
                    PD.BufferSoilMap = pcm.bufferMap;
                }
                
                for(int i = 0; i < PD.layers; i++)
                {
                    if (!node.HasNode("KWSTropoData" +i+ PD.body.bodyName))
                    {
                        Debug.Log("Tampered/Missing Tropo Data, creating new...");
                        CellUpdater.run = 0;
                        WeatherSimulator.InitPlanetData(PD);
                        continue;
                    }
                    using (MemoryStream s = new MemoryStream(FromBase64String(node.GetValue("KWSTropoData"+ i + PD.body.bodyName))))
                    {
                        ProtoCellMaps pcm = Serializer.Deserialize<ProtoCellMaps>(s);
                        if(PD.LiveMap.Count == i)
                        {
                            PD.LiveMap.Add(pcm.liveMaps);
                            PD.BufferMap.Add(pcm.bufferMaps);
                        }
                        else
                        {
                            PD.LiveMap[i] = pcm.liveMaps;
                            PD.BufferMap[i] = pcm.bufferMaps;
                        }
                    }
                }
                
                for(int i = 0; i < PD.stratoLayers; i++)
                {
                    if(!node.HasNode("KWSStratoData" +i+ PD.body.bodyName))
                    {
                        Debug.Log("Tampered/Missing Strato Data, creating new..");
                        CellUpdater.run = 0;
                        WeatherSimulator.InitPlanetData(PD);
                        continue;
                    }
                    using (MemoryStream s = new MemoryStream(FromBase64String(node.GetValue("KWSStratoData" + i + PD.body.bodyName))))
                    {
                        ProtoCellMaps pcm = Serializer.Deserialize<ProtoCellMaps>(s);
                        if (PD.LiveMap.Count == i)
                        {
                            PD.LiveStratoMap.Add(pcm.liveMaps);
                            PD.BufferStratoMap.Add(pcm.bufferMaps);
                        }
                        else
                        {
                            PD.LiveStratoMap[i] = pcm.liveMaps;
                            PD.BufferStratoMap[i] = pcm.bufferMaps;
                        }
                    }
                }
                PlanetaryData[PD.index] = PD;
            }
            
            Debug.Log("Loaded ConfigNode");
        }
        internal static void SaveInitFlightState(uint cellIndex, String saveFolder)
        {
            Logger("Saving initial flight state..");
            SaveInitFlightStateData(cellIndex, saveFolder);
            Logger("Saved initial flight state");
        }
        private static void SaveInitFlightStateData(uint cellIndex, String saveFolder)
        {
            using (StreamWriter file = new StreamWriter(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/SimSettings/Settings.cfg", false))
            {
                file.WriteLine("currentIndex= " + cellIndex);
                file.WriteLine("run= " + CellUpdater.run);
            }
            foreach (PlanetData PD in PlanetaryData)
            {
                using (StreamWriter file = new StreamWriter(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/info.cfg", false))
                {
                    file.WriteLine("updateTime= " + PD.updateTime);
                    file.WriteLine("layers= " + PD.layers);
                    file.WriteLine("stratoLayers= " + PD.stratoLayers);
                }
                #region Saving
                using (var file = File.OpenWrite(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/WeatherData/LiveSoil/" + "soil" + ".bin"))
                {
                    ProtoSoilMap pcm = new ProtoSoilMap();
                    pcm.map = PD.LiveSoilMap;
                    Serializer.Serialize(file, pcm);
                    file.SetLength(file.Position);
                }
                using (var file = File.OpenWrite(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/WeatherData/BufferSoil/" + "soil" + ".bin"))
                {
                    ProtoSoilMap pcm = new ProtoSoilMap();
                    pcm.map = PD.BufferSoilMap;
                    Serializer.Serialize(file, pcm);
                    file.SetLength(file.Position);
                }
                for(int i = 0; i < PD.layers; i++)
                {
                    using (var file = File.OpenWrite(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/WeatherData/Live/" + i + ".bin"))
                    {
                        ProtoCellMaps pcm = new ProtoCellMaps();
                        pcm.liveMaps = PD.LiveMap[i];
                        Serializer.Serialize(file, pcm);
                        file.SetLength(file.Position);
                    }
                    using (var file = File.OpenWrite(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/WeatherData/Buffer/" + i + ".bin"))
                    {
                        ProtoCellMaps pcm = new ProtoCellMaps();
                        pcm.bufferMaps = PD.BufferMap[i];
                        Serializer.Serialize(file, pcm);
                        file.SetLength(file.Position);
                    }
                }
                for(int i = 0; i < PD.stratoLayers; i++)
                {
                    using (var file = File.OpenWrite(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/WeatherData/LiveStrato/" + i + ".bin"))
                    {
                        ProtoCellMaps pcm = new ProtoCellMaps();
                        pcm.liveMaps = PD.LiveStratoMap[i];
                        Serializer.Serialize(file, pcm);
                        file.SetLength(file.Position);
                    }
                    using (var file = File.OpenWrite(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/WeatherData/BufferStrato/" + i + ".bin"))
                    {
                        ProtoCellMaps pcm = new ProtoCellMaps();
                        pcm.liveMaps = PD.BufferStratoMap[i];
                        Serializer.Serialize(file, pcm);
                        file.SetLength(file.Position);
                    }
                }
                
                #endregion
            }
        }
        internal static void LoadInitFlightState(String saveFolder)
        {
            Logger("Loading initial flight state...");
            LoadInitFlightStateData(saveFolder);
            Logger("Loaded initial flight state");
        }
        private static void LoadInitFlightStateData(String saveFolder)
        {
            if (File.Exists(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/Kerbin/WeatherData/Buffer/0.bin")) //assume full save integrity
            {
                Logger("Save exists, loading...");
                foreach(PlanetData PD in PlanetaryData)
                {
                    if (PD.biomes[new Cell(0)] == null)
                    {
                        WeatherSimulator.InitBiomeMap(PD);
                    }
                    if (!File.Exists(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/info.cfg"))
                    {
                        File.Create(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/info.cfg");
                        using (var file = new StreamWriter(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/info.cfg"))
                        {
                            file.WriteLine("updateTime= 10");
                            file.WriteLine("layers= 6");
                            file.WriteLine("stratoLayers= 1");
                        }
                    }
                    using (var file = new StreamReader(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/info.cfg"))
                    {
                        string line = null;
                        while ((line = file.ReadLine()) != null)
                        {
                            if (line.StartsWith("layers"))
                            {
                                PD.layers = int.Parse(line.Substring(line.IndexOf('=') + 1).Trim());
                            }
                            if (line.StartsWith("stratoLayers"))
                            {
                                PD.stratoLayers = int.Parse(line.Substring(line.IndexOf('=') + 1).Trim());
                            }
                        }
                    }
                    if (PD.TropAlts.Count == 0)
                    {
                        WeatherSimulator.InitTropopauseAlts(PD);
                    }
                    using (var file = File.OpenRead(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/WeatherData/LiveSoil/" + "soil" + ".bin"))
                    {
                        ProtoSoilMap pcm = new ProtoSoilMap();
                        pcm = Serializer.Deserialize<ProtoSoilMap>(file);
                        KWSCellMap<SoilCell> KWSCellMap = new KWSCellMap<SoilCell>(PD.gridLevel);
                        KWSCellMap = pcm.map;
                        PD.LiveSoilMap = KWSCellMap;
                    }
                    using (var file = File.OpenRead(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/WeatherData/BufferSoil/" + "soil" + ".bin"))
                    {
                        ProtoSoilMap pcm = new ProtoSoilMap();
                        pcm = Serializer.Deserialize<ProtoSoilMap>(file);
                        KWSCellMap<SoilCell> KWSCellMap = new KWSCellMap<SoilCell>(PD.gridLevel);
                        KWSCellMap = pcm.map;
                        PD.BufferSoilMap = KWSCellMap;
                    }

                    for (int i = 0; i < PD.layers; i++)
                    {
                        using (var file = File.OpenRead(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/WeatherData/Live/" + i + ".bin"))
                        {
                            ProtoCellMap pcm = new ProtoCellMap();
                            pcm = Serializer.Deserialize<ProtoCellMap>(file);
                            KWSCellMap<WeatherCell> KWSCellMap = new KWSCellMap<WeatherCell>(PD.gridLevel);
                            KWSCellMap = pcm.map;
                            PD.LiveMap.Add(KWSCellMap);
                        }
                        using (var file = File.OpenRead(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/WeatherData/Buffer/" + i + ".bin"))
                        {
                            ProtoCellMap pcm = new ProtoCellMap();
                            pcm = Serializer.Deserialize<ProtoCellMap>(file);
                            KWSCellMap<WeatherCell> KWSCellMap = new KWSCellMap<WeatherCell>(PD.gridLevel);
                            KWSCellMap = pcm.map;
                            PD.BufferMap.Add(KWSCellMap);
                        }
                    }
                    for (int i = 0; i < PD.stratoLayers; i++)
                    {
                        using (var file = File.OpenRead(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/WeatherData/LiveStrato/" + i + ".bin"))
                        {
                            ProtoCellMap pcm = new ProtoCellMap();
                            pcm = Serializer.Deserialize<ProtoCellMap>(file);
                            KWSCellMap<WeatherCell> KWSCellMap = new KWSCellMap<WeatherCell>(PD.gridLevel);
                            KWSCellMap = pcm.map;
                            PD.LiveStratoMap.Add(KWSCellMap);
                        }
                        using (var file = File.OpenRead(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + PD.body.bodyName + "/WeatherData/BufferStrato/" + i + ".bin"))
                        {
                            ProtoCellMap pcm = new ProtoCellMap();
                            pcm = Serializer.Deserialize<ProtoCellMap>(file);
                            KWSCellMap<WeatherCell> KWSCellMap = new KWSCellMap<WeatherCell>(PD.gridLevel);
                            KWSCellMap = pcm.map;
                            PD.BufferStratoMap.Add(KWSCellMap);
                        }
                    }
                    Logger("Getting stored time...");

                    string line1 = null;
                    if (File.Exists(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/info.cfg"))
                    {
                        using (StreamReader file = new StreamReader(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/info.cfg"))
                        {
                            while ((line1 = file.ReadLine()) != null)
                            {
                                if (line1.StartsWith("updateTime"))
                                {
                                    PD.updateTime = double.Parse(line1.Substring(line1.IndexOf('=') + 1).Trim());
                                }
                            }
                        }
                    }
                    CellUpdater.run = getRunCount(saveFolder);
                    PlanetaryData[PD.index] = PD;
                }
                
                Logger("Saved sim loaded!");
            }
        }
        internal static void SavePlanetaryData(uint cellIndex, String saveFolder)
        {
            Logger("Saving to: " + saveFolder);
            SaveSimData(cellIndex, saveFolder);
            Logger("Saved data to: " + saveFolder);
        }
        private static void SaveSimData(uint cellIndex, String saveFolder)
        {
            Debug.Log("Saving to: " + saveFolder);
            using (StreamWriter file = new StreamWriter(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/SimSettings/Settings.cfg",false))
            {
                file.WriteLine("currentIndex= " + cellIndex);
                file.WriteLine("run= " + CellUpdater.run);
            }

            foreach (PlanetData PD in PlanetaryData)
            {
                using (StreamWriter file = new StreamWriter(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/"+ PD.body.bodyName + "/info.cfg", false))
                {
                    file.WriteLine("updateTime= " + PD.updateTime);
                    file.WriteLine("layers= "+PD.layers);
                    file.WriteLine("stratoLayers= "+PD.stratoLayers);
                }
                #region Copying
                DirectoryCopy(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName,
                KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Old/" + PD.body.bodyName, true);
                #endregion
                #region Saving
                using (var file = File.OpenWrite(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/WeatherData/LiveSoil/" + "soil" + ".bin"))
                {
                    ProtoSoilMap pcm = new ProtoSoilMap();

                    pcm.map = PD.LiveSoilMap;
                    Serializer.Serialize(file, pcm);
                    file.SetLength(file.Position);
                }
                using (var file = File.OpenWrite(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/WeatherData/BufferSoil/" + "soil" + ".bin"))
                {
                    ProtoSoilMap pcm = new ProtoSoilMap();
                    pcm.map = PD.BufferSoilMap;
                    Serializer.Serialize(file, pcm);
                    file.SetLength(file.Position);
                }
                for(int i = 0; i < PD.LiveMap.Count; i++)
                {
                    using (var file = File.OpenWrite(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/WeatherData/Live/" + i + ".bin"))
                    {
                        ProtoCellMaps pcm = new ProtoCellMaps();

                        pcm.liveMaps = PD.LiveMap[i];
                        Serializer.Serialize(file, pcm);
                        file.SetLength(file.Position);
                    }
                    using (var file = File.OpenWrite(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/WeatherData/Buffer/" + i + ".bin"))
                    {
                        ProtoCellMaps pcm = new ProtoCellMaps();
                        pcm.bufferMaps = PD.BufferMap[i];
                        Serializer.Serialize(file, pcm);
                        file.SetLength(file.Position);
                    }
                }
                for(int i = 0; i < PD.LiveStratoMap.Count; i++)
                {
                    using (var file = File.OpenWrite(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/WeatherData/LiveStrato/" + i + ".bin"))
                    {
                        ProtoCellMaps pcm = new ProtoCellMaps();
                        pcm.liveMaps = PD.LiveStratoMap[i];
                        Serializer.Serialize(file, pcm);
                        file.SetLength(file.Position);
                    }
                    using (var file = File.OpenWrite(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/WeatherData/BufferStrato/" + i + ".bin"))
                    {
                        ProtoCellMaps pcm = new ProtoCellMaps();
                        pcm.bufferMaps = PD.BufferStratoMap[i];
                        Serializer.Serialize(file, pcm);
                        file.SetLength(file.Position);
                    }
                }
                
                #endregion
            }
        }
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
        internal static void CheckCreateForFiles(String saveFolder)
        {
            if (saveFolder.Equals("")) { return; }
            if(!Directory.Exists(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder+"/KWS"))
            {
                Logger("New Save detected/Save not Found, (re)creating save folder...");
                Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder+"/KWS");
                Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New");
                Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Old");
                Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight");
                Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/SimSettings");

                using (StreamWriter file = new StreamWriter(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/SimSettings/Settings.cfg", false))
                {
                    file.WriteLine("currentIndex= 0");
                    file.WriteLine("run= 0");
                }

                foreach (PlanetData PD in PlanetaryData)
                {
                    String bodyName = PD.body.bodyName;
                    if (!Directory.Exists(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + bodyName))
                    {
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + bodyName);
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + bodyName+"/WeatherData");
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + bodyName + "/WeatherData/Buffer");
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + bodyName + "/WeatherData/BufferSoil");
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + bodyName + "/WeatherData/BufferStrato");
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + bodyName + "/WeatherData/Live");
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + bodyName + "/WeatherData/LiveSoil");
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + bodyName + "/WeatherData/LiveStrato");
                        using (var file = new StreamWriter(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + bodyName + "/info.cfg"))
                        {
                            file.WriteLine("updateTime= 10");
                            file.WriteLine("layers= 6");
                            file.WriteLine("stratoLayers= 1");
                        }
                    }
                    if(!Directory.Exists(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Old/" + bodyName))
                    {
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Old/" + bodyName);
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Old/" + bodyName+"/WeatherData");
                        using (var file = new StreamWriter(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Old/" + bodyName + "/info.cfg"))
                        {
                            file.WriteLine("updateTime= 10");
                            file.WriteLine("layers= 6");
                            file.WriteLine("stratoLayers= 1");
                            file.Close();
                        }
                    }
                    if (!Directory.Exists(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + bodyName))
                    {
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + bodyName);
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + bodyName + "/WeatherData");
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + bodyName + "/WeatherData/Buffer");
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + bodyName + "/WeatherData/BufferSoil");
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + bodyName + "/WeatherData/BufferStrato");
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + bodyName + "/WeatherData/Live");
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + bodyName + "/WeatherData/LiveSoil");
                        Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + bodyName + "/WeatherData/LiveStrato");
                        using (var file = new StreamWriter(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/Flight/" + bodyName + "/info.cfg"))
                        {
                            file.WriteLine("updateTime= 10");
                            file.WriteLine("layers= 6");
                            file.WriteLine("stratoLayers= 1");
                        }
                    }
                }
                Logger("Save Folder created!");
            }
        }
        internal static void LoadInitPlanetaryData()
        {
            String line = null;
            StreamReader file = new StreamReader(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/PlanetaryData.cfg");
            PlanetData PD = new PlanetData();
            PD.dewData = new DewData();
            PD.atmoData = new AtmoData();
            #region filereading
            while ((line = file.ReadLine()) != null)
            {

                if (line.StartsWith("name"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.body = FlightGlobals.Bodies.Where(x => x.bodyName == line).ToList()[0];
                    continue;
                }
                if (line.StartsWith("index"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.index = int.Parse(line);
                    continue;
                }
                if (line.StartsWith("gridLevel"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.gridLevel = int.Parse(line);
                    continue;
                }
                if(line.StartsWith("layers"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.layers = int.Parse(line);
                    continue;
                }
                if(line.StartsWith("stratoLayers"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.stratoLayers = int.Parse(line);
                    continue;
                }
                if (line.StartsWith("meanTropHeight"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.meanTropoHeight = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("irradiance"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.irradiance = float.Parse(line);
                    continue;
                }


                if (line.StartsWith("SWA"))
                {
                    line = line.Substring(line.IndexOf("=") + 1);
                    PD.atmoData.SWA = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("IRA"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.atmoData.IRA = float.Parse(line);
                    continue;
                }
                /*
                if (line.StartsWith("SHF"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.SHF = float.Parse(line);
                    continue;
                }
                */
                if (line.StartsWith("SH_correction"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.SH_correction = float.Parse(line);
                    continue;
                }
            }


            file = new StreamReader(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/AtmoData.cfg");
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("cg"))
                {
                    line = line.Substring(line.IndexOf("=") + 1);
                    PD.atmoData.specificHeatGas = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("M"))
                {
                    line = line.Substring(line.IndexOf("=") + 1);
                    PD.atmoData.M = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("ks"))
                {
                    line = line.Substring(line.IndexOf("=") + 1);
                    PD.atmoData.ks = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("n1"))
                {
                    line = line.Substring(line.IndexOf("=") + 1);
                    PD.atmoData.n1 = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("a_VdW"))
                {
                    line = line.Substring(line.IndexOf("=") + 1);
                    PD.atmoData.a_VdW = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("b_VdW"))
                {
                    line = line.Substring(line.IndexOf("=") + 1);
                    PD.atmoData.b_VdW = float.Parse(line);
                    continue;
                }
            }
            file = new StreamReader(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/DewData.cfg");
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("name"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.name = line;
                    continue;
                }
                if (line.StartsWith("formula"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.formula = line;
                    continue;
                }
                if (line.StartsWith("A1"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.A1 = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("A2"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.A2 = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("B1"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.B1 = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("B2"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.B2 = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("C1"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.C1 = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("C2"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.C2 = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("M"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.M = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("cg"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.cg = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("cl"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.cl = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("cs"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.cs = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("he"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.he = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("hm"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.hm = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("hs"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.hs = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("dl"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.Dl = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("ds"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.Ds = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("ks"))
                {
                    line = line.Substring(line.IndexOf("=") + 1);
                    PD.dewData.ks = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("T_fr"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.T_fr = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("T_m"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.T_m = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("T_b"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    PD.dewData.T_b = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("n1"))
                {
                    line = line.Substring(line.IndexOf("=") + 1);
                    PD.dewData.n1 = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("a_VdW"))
                {
                    line = line.Substring(line.IndexOf("=") + 1);
                    PD.dewData.a_VdW = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("b_VdW"))
                {
                    line = line.Substring(line.IndexOf("=") + 1);
                    PD.dewData.b_VdW = float.Parse(line);
                    continue;
                }
            }
            //Load the biome data
            //Ice caps and Tundra dont have values calculated
            //tundra set at arbitrary 4.2e+07
            //ice caps set to 3.9e+07
            Logger("Loading Biome Data");
            foreach (String s in Directory.GetFiles(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/Biomes"))
            {
                BiomeData BD = new BiomeData();
                String line1 = null;
                StreamReader file1 = new StreamReader(s);
                while ((line1 = file1.ReadLine()) != null)
                {
                    if (line1.StartsWith("name"))
                    {
                        BD.name = line1.Substring(line1.IndexOf("=") + 2);
                        Logger("Loading: " + BD.name);
                        continue;
                    }
                    if (line1.StartsWith("albedo"))
                    {
                        BD.Albedo = float.Parse(line1.Substring(line1.IndexOf("=") + 2));
                    }
                    if (line1.StartsWith("STC"))
                    {
                        BD.SoilThermalCap = float.Parse(line1.Substring(line1.IndexOf("=") + 2));
                    }
                    if (line1.StartsWith("FLC"))
                    {
                        BD.FLC = float.Parse(line1.Substring(line1.IndexOf("=") + 2));
                    }
                }
                PD.biomeDatas.Add(BD.name, BD);
            }
            #endregion
            Logger("Loaded Biome Data");

            
            PD.LiveMap = new List<KWSCellMap<WeatherCell>>(7);
            PD.BufferMap = new List<KWSCellMap<WeatherCell>>(7);
            PlanetaryData.Add(PD);
            Logger("Finished Loading Initial Planet Data");
        }
        internal static void LoadPlanetaryData(String saveFolder)
        {
            Logger("Loading data...");
            LoadSimData(saveFolder);
            Logger("Data loaded");
        }
        private static void LoadSimData(String saveFolder)
        {
            Logger("Checking for sim Data...");
            foreach(PlanetData PD in PlanetaryData)
            {
                if(PD.biomes[new Cell(0)] == null)
                {
                    WeatherSimulator.InitBiomeMap(PD);
                }
                if (File.Exists(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/WeatherData/Buffer/0.bin")) //assume full save integrity
                {
                    Logger("Save exists, loading...");
                    if (!File.Exists(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/info.cfg"))
                    {
                        File.Create(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/info.cfg");
                        using (var file = new StreamWriter(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/info.cfg"))
                        {
                            file.WriteLine("updateTime= 10");
                            file.WriteLine("layers= 6");
                            file.WriteLine("stratoLayers= 1");
                        }
                    }
                    using (var file = new StreamReader(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/info.cfg"))
                    {
                        string line = null;
                        while ((line = file.ReadLine()) != null)
                        {
                            if (line.StartsWith("layers"))
                            {
                                PD.layers = int.Parse(line.Substring(line.IndexOf('=') + 1).Trim());
                            }
                            if (line.StartsWith("stratoLayers"))
                            {
                                PD.stratoLayers = int.Parse(line.Substring(line.IndexOf('=') + 1).Trim());
                            }
                        }
                    }
                    if (PD.TropAlts.Count == 0)
                    {
                        WeatherSimulator.InitTropopauseAlts(PD);
                    }
                    using (var file = File.OpenRead(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/WeatherData/LiveSoil/" + "soil" + ".bin"))
                    {
                        ProtoSoilMap pcm = new ProtoSoilMap();
                        pcm = Serializer.Deserialize<ProtoSoilMap>(file);
                        KWSCellMap<SoilCell> KWSCellMap = new KWSCellMap<SoilCell>(PD.gridLevel);
                        KWSCellMap = pcm.map;
                        PD.LiveSoilMap = KWSCellMap;
                    }
                    using (var file = File.OpenRead(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/WeatherData/BufferSoil/" + "soil" + ".bin"))
                    {
                        ProtoSoilMap pcm = new ProtoSoilMap();
                        pcm = Serializer.Deserialize<ProtoSoilMap>(file);
                        KWSCellMap<SoilCell> KWSCellMap = new KWSCellMap<SoilCell>(PD.gridLevel);
                        KWSCellMap = pcm.map;
                        PD.BufferSoilMap = KWSCellMap;
                    }
                    for(int i = 0; i < PD.layers; i++)
                    {
                        using (var file = File.OpenRead(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/WeatherData/Live/" + i + ".bin"))
                        {
                            ProtoCellMaps pcm = new ProtoCellMaps();
                            pcm = Serializer.Deserialize<ProtoCellMaps>(file);
                            if(PD.LiveMap.Count == i)
                            {
                                PD.LiveMap.Add(pcm.liveMaps);
                            }
                            else
                            {
                                PD.LiveMap[i] = pcm.liveMaps;
                            }
                        }
                        using (var file = File.OpenRead(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/WeatherData/Buffer/" + i + ".bin"))
                        {
                            ProtoCellMaps pcm = new ProtoCellMaps();
                            pcm = Serializer.Deserialize<ProtoCellMaps>(file);
                            if(PD.BufferMap.Count == i)
                            {
                                PD.BufferMap.Add(pcm.bufferMaps);
                            }
                            else
                            {
                                PD.BufferMap[i] = pcm.bufferMaps;
                            }
                        }
                    }
                    for(int i = 0; i < PD.stratoLayers; i++)
                    {
                        using (var file = File.OpenRead(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/WeatherData/LiveStrato/" + i + ".bin"))
                        {
                            ProtoCellMaps pcm = new ProtoCellMaps();
                            pcm = Serializer.Deserialize<ProtoCellMaps>(file);
                            if(PD.LiveStratoMap.Count == i)
                            {
                                PD.LiveStratoMap.Add(pcm.liveMaps);
                            }
                            else
                            {
                                PD.LiveStratoMap[i] = pcm.liveMaps;
                            }
                            

                        }
                        using (var file = File.OpenRead(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + PD.body.bodyName + "/WeatherData/BufferStrato/" + i + ".bin"))
                        {
                            ProtoCellMaps pcm = new ProtoCellMaps();
                            pcm = Serializer.Deserialize<ProtoCellMaps>(file);
                            if(PD.BufferStratoMap.Count == i)
                            {
                                PD.BufferStratoMap.Add(pcm.bufferMaps);
                            }
                            else
                            {
                                PD.BufferStratoMap[i] = pcm.bufferMaps;
                            }
                        }
                    }

                    Logger("Getting stored time...");
                    PD.updateTime = getStoredUpdateTime(saveFolder, PD.body.bodyName);
                    CellUpdater.run = getRunCount(saveFolder);
                    PlanetaryData[PD.index] = PD;
                    Logger("Saved sim loaded!");
                }
                else
                {
                    Logger("No Save Data found, starting from scratch..");
                    PD.layers = 6;
                    PD.stratoLayers = 1;
                    CheckCreateForFiles(saveFolder);
                    WeatherSimulator.InitPlanetData(PD);
                    PD.updateTime = 10;
                    
                    PlanetaryData[PD.index] = PD;
                    Logger("New data inited");
                }
            }
        }
        internal static void LoadPersData(String saveFolder, String persistence)
        {
            
        }
        internal static uint getStoredCellIndex(String saveFolder)
        {
            string line = null;
            if(File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/Saves/" + saveFolder + "/SimSettings/Settings.cfg"))
            {
                using (StreamReader file = new StreamReader(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/Saves/" + saveFolder + "/SimSettings/Settings.cfg"))
                {
                    while ((line = file.ReadLine()) != null)
                    {
                        if (line.StartsWith("currentIndex"))
                        {
                            return uint.Parse(line.Substring(line.IndexOf('=') + 1).Trim());
                        }
                    }
                }
            }
            else
            {
                using(StreamWriter file = new StreamWriter(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/Saves/" + saveFolder + "/SimSettings/Settings.cfg", false))
                {
                    file.Write("currentIndex= 0");
                    file.Write("run= 0");
                }
                return 0;
            }
            return 0;
        }

        internal static double getStoredUpdateTime(String saveFolder, String bodyFolder)
        {
            string line = null;
            if(File.Exists(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + bodyFolder + "/info.cfg"))
            {
                using (StreamReader file = new StreamReader(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/New/" + bodyFolder + "/info.cfg"))
                {
                    while ((line = file.ReadLine()) != null)
                    {
                        if (line.StartsWith("updateTime"))
                        {
                            return double.Parse(line.Substring(line.IndexOf('=') + 1).Trim());
                        }
                    }
                }
            }
            return 10;
        }
        private static long getRunCount(String saveFolder)
        {
            using (StreamReader file = new StreamReader(KSPUtil.ApplicationRootPath + "/saves/" + saveFolder + "/KWS/SimSettings/Settings.cfg"))
            {
                string line = null;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith("run"))
                    {
                        return long.Parse(line.Substring(line.IndexOf('=') + 1).Trim());
                    }
                }
            }
            return 0;
        }
        internal static bool BasicSanityCheck()
        {
            return false;
        }
        internal static bool BasicFileIntegrityCheck()
        {
            #region Directory Checks
            if (!Directory.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData"))
            {
                Logger("Body Data Dir missing!");
                return false;
            }
            if (!Directory.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin"))
            {
                Logger("Kerbin Body Data Dir missing");
                return false;
            }
            if (!Directory.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/Biomes"))
            {
                Logger("Kerbin Biome Data Dir missing");
                return false;
            }
            #endregion

            #region File Checks
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/CellData.cfg"))
            {
                Logger("Kerbin Cell Data Missing");
                return false;
            }
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/DewData.cfg"))
            {
                Logger("Kerbin Dew Data Missing");
                return false;
            }
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/PlanetaryData.cfg"))
            {
                Logger("Kerbin PlanetaryData Missing");
                return false;
            }
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/AtmoData.cfg"))
            {
                Logger("Kerbin AtmoData Missing");
                return false;
            }
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/DewData.cfg"))
            {
                Logger("Kerbin DewData Missing");
                return false;
            }
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/Biomes/Badlands.cfg"))
            {
                Logger(KSPUtil.ApplicationRootPath + "Kerbin Biome Badlands Missing");
                return false;
            }
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/Biomes/Deserts.cfg"))
            {
                Logger("Kerbin Biome Deserts Missing");
                return false;
            }
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/Biomes/Grasslands.cfg"))
            {
                Logger("Kerbin Biome Grasslands Missing");
                return false;
            }
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/Biomes/Highlands.cfg"))
            {
                Logger("Kerbin Biome Highlands Missing");
                return false;
            }
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/Biomes/IceCaps.cfg"))
            {
                Logger("Kerbin Biome IceCaps Missing");
                return false;
            }
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/Biomes/Mountains.cfg"))
            {
                Logger("Kerbin Biome Mountains Missing");
                return false;
            }
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/Biomes/Shores.cfg"))
            {
                Logger("Kerbin Biome Shores Missing");
                return false;
            }
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/Biomes/Tundra.cfg"))
            {
                Logger("Kerbin Biome Tundra Missing");
                return false;
            }
            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/BodyData/Kerbin/Biomes/Water.cfg"))
            {
                Logger("Kerbin Biome Water Missing");
                return false;
            }
            #endregion
            return true;
        }

        //This code generously donated by NULL
        public static string ToBase64String(byte[] data)
        {
            return Convert.ToBase64String(data).Replace('/', '.').Replace('=', '%');
        }
        public static byte[] FromBase64String(string encoded)
        {
            return Convert.FromBase64String(encoded.Replace('.', '/').Replace('%', '='));
        }
        //

        private static void Logger(String s)
        {
            WeatherLogger.Log("[WD]" + s);
        }
    }
}
