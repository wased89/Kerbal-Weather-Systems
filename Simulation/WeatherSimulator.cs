using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Text;
using GeodesicGrid;
using UnityEngine;
using Database;
using Overlay;
using KerbalWeatherSystems;
using Debug = UnityEngine.Debug;
/*
    Order of Ops for cell
    --------------------
    SunAngleEffect
    Tropopause Alt



*/


namespace Simulation
{
    //[KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class WeatherSimulator : MonoBehaviour
    {
        internal static TimeSpan StartTime = new TimeSpan();
        public static double AvgCycleTime = 0;
        private static double cycleTime = 0;
        public int cellsPerUpdate = WeatherSettings.SD.cellsPerUpdate;
        internal static uint cellindex = 0;
        internal static double currentTime = 0;
        internal static int layers = 6; //layers in the troposphere
        internal static int stratoLayers = 1; //layers in the stratosphere
        internal static bool debug = true;
        internal static bool isSaving = false;
        internal static bool isPaused = false;
        internal static bool configSaving = false; //KSP integrated(true) or standalone?
        public void OnSceneLoadRequested(GameScenes scene)
        {
            if (scene == GameScenes.MAINMENU)
            {
                isPaused = true;
                //do the saving

            }
            if(scene == GameScenes.SPACECENTER && HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                isPaused = false;
            }
            if (scene == GameScenes.FLIGHT && scene == GameScenes.SPACECENTER)
            {
                isPaused = false;
                if (configSaving)
                {
                    Debug.Log("[KWS] Reverting to space center config state");
                    WeatherDatabase.LoadConfigSimData(HighLogic.CurrentGame.config);
                }
                else
                {
                    Debug.Log("[KWS] Reverting to space center state");
                    WeatherDatabase.LoadInitFlightState(HighLogic.SaveFolder);
                }
            }
            if (scene == GameScenes.EDITOR || scene == GameScenes.FLIGHT)
            {
                if (FlightDriver.StartupBehaviour == FlightDriver.StartupBehaviours.RESUME_SAVED_CACHE || EditorDriver.StartupBehaviour == EditorDriver.StartupBehaviours.LOAD_FROM_CACHE)
                {
                    //load flight launch state
                    //we can assume that reverting to the editor allows us to have flight state because we must first have gone into flight to be able to revert to editor.
                    if (configSaving)
                    {
                        Debug.Log("[KWS] Loading init config flight state");
                        WeatherDatabase.LoadConfigSimData(HighLogic.CurrentGame.config);
                    }
                    else
                    {
                        Debug.Log("[KWS] Loading init flight state");
                        WeatherDatabase.LoadInitFlightState(HighLogic.SaveFolder);
                    }
                }
                if (scene == GameScenes.FLIGHT)
                {
                    if (configSaving)
                    {
                        Debug.Log("[KWS] Saving init config flight state");
                        WeatherDatabase.SaveConfigSimData(HighLogic.CurrentGame.config, cellindex);
                    }
                    else
                    {
                        Debug.Log("[KWS] Saving init flight state");
                        WeatherDatabase.SaveInitFlightState(cellindex, HighLogic.SaveFolder);
                    }
                }
                if (scene == GameScenes.EDITOR)
                { isPaused = true; }
            }
        }
        public void OnLaunch(EventReport report)
        {
            //save initial flight state
            isPaused = false;
            if (configSaving)
            {
                WeatherDatabase.SaveConfigSimData(HighLogic.CurrentGame.config, cellindex);
            }
            else
            {
                WeatherDatabase.SaveInitFlightState(cellindex, HighLogic.SaveFolder);
            }
        }
        public void OnPause()
        {
            Debug.Log("Game paused: " + (isPaused = true));
        }
        public void OnUnPause()
        {
            Debug.Log("Game paused: " + (isPaused = false));
        }
        public void OnGameSaved(Game game)
        {
            Debug.Log("[KWS] Saving game state...");
            if (configSaving)
            {
                WeatherDatabase.SaveConfigSimData(game.config, cellindex);
            }
            else
            {
                WeatherDatabase.CheckCreateForFiles(HighLogic.SaveFolder);
                WeatherDatabase.SavePlanetaryData(cellindex, HighLogic.SaveFolder);
            }
            Debug.Log("[KWS] Game state saved");
        }
        public void OnGamePostLoad(ConfigNode node)
        {
            Debug.Log("[KWS] Loading game state...");
            if (configSaving)
            {
                WeatherDatabase.LoadConfigSimData(node);
            }
            else
            {
                WeatherDatabase.LoadPlanetaryData(HighLogic.SaveFolder);
            }
            Debug.Log("[KWS] Game state loaded");
        }
        public void OnGameCreated(Game game)
        {
            Logger("Checking for files...");
            WeatherDatabase.CheckCreateForFiles(game.linkURL);
        }
        public void Awake()
        {
            //init stuff
            Debug.Log("WS Awoke");
            CellUpdater.run = 0;
            WeatherDatabase.CheckCreateForFiles(HighLogic.SaveFolder);
            if(configSaving)
            {
                WeatherDatabase.LoadConfigSimData(HighLogic.CurrentGame.config);
            }
            else
            {
                WeatherDatabase.LoadPlanetaryData(HighLogic.SaveFolder);
            }
            GameEvents.onGameSceneLoadRequested.Remove(OnSceneLoadRequested);
            GameEvents.onGameSceneLoadRequested.Add(OnSceneLoadRequested);
            GameEvents.onLaunch.Remove(OnLaunch);
            GameEvents.onLaunch.Add(OnLaunch);
            GameEvents.onGamePause.Remove(OnPause);
            GameEvents.onGamePause.Add(OnPause);
            GameEvents.onGameUnpause.Remove(OnUnPause);
            GameEvents.onGameUnpause.Add(OnUnPause);
            GameEvents.onGameStateSaved.Remove(OnGameSaved);
            GameEvents.onGameStateSaved.Add(OnGameSaved);
            GameEvents.onGameStatePostLoad.Remove(OnGamePostLoad);
            GameEvents.onGameStatePostLoad.Add(OnGamePostLoad);
            GameEvents.onGameStateCreated.Remove(OnGameCreated);
            GameEvents.onGameStateCreated.Add(OnGameCreated);
        }
        
        public void FixedUpdate()
        {
            ///*
            if (!isPaused &&!isSaving)
            {
                if (HighLogic.LoadedScene != GameScenes.EDITOR)
                {
                    //Debug.Log("time: " + Planetarium.GetUniversalTime());
                    ///*
                    foreach (PlanetData PD in WeatherDatabase.PlanetaryData)
                    {
                        UpdatePlanetaryData(PD);
                    }
                    currentTime += Time.fixedDeltaTime * TimeWarp.CurrentRate;
                }
            }
            //*/
            WeatherLogger.Update();

        }
        public void threadUpdatePlanetaryData(object obj)
        {
            UpdatePlanetaryData((PlanetData)obj);
        }
        public void threadedUpdate1(object obj)
        {
            PlanetData PD = (PlanetData)obj;
            for(int i = 0; i < Cell.CountAtLevel(PD.gridLevel)/2; i++)
            {
                
            }
        }
        public void threadedUpdate2(object obj)
        {
            PlanetData PD = (PlanetData)obj;
            for(int i = (int)Cell.CountAtLevel(PD.gridLevel)/2; i < Cell.CountAtLevel(PD.gridLevel); i++)
            {

            }
        }
        public void UpdatePlanetaryData(PlanetData PD)
        {
            StartTime = Process.GetCurrentProcess().TotalProcessorTime;
            //update temps for the cell stack
            for (int i = 0; i < cellsPerUpdate; i++, cellindex++)
            {
                if (cellindex == Cell.CountAtLevel(PD.gridLevel))
                {
                    cellindex = 0;
                    BufferFlip(PD);
                    cycleTime += (Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds - StartTime.TotalMilliseconds) * 1000;
                    AvgCycleTime = cycleTime / Cell.CountAtLevel(PD.gridLevel);
                    cycleTime = 0;
                    if (WeatherSettings.SD.statistics) { Statistics.PrintStat(PD); }
                    return;
                }
                Cell cell = new Cell(cellindex);
                //update the cell
                CellUpdater.UpdateCell(PD, cell);
            }
            cycleTime += (Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds - StartTime.TotalMilliseconds) * 1000;
        }
        public void BufferFlip(PlanetData PD)
        {
            Logger("BufferFlip!  cycle = " + CellUpdater.run);
            List<KWSCellMap<WeatherCell>> temp = PD.LiveMap;
            PD.LiveMap = PD.BufferMap;
            PD.BufferMap = temp;

            KWSCellMap<SoilCell> temp1 = PD.LiveSoilMap;
            PD.LiveSoilMap = PD.BufferSoilMap;
            PD.BufferSoilMap = temp1;

            List<KWSCellMap<WeatherCell>> temp2 = PD.LiveStratoMap;
            PD.LiveStratoMap = PD.BufferStratoMap;
            PD.BufferStratoMap = temp2;

            PD.updateTime = currentTime; //update the update time
            WeatherDatabase.PlanetaryData[PD.index] = PD;
            CellUpdater.run++;
            currentTime = 0;
            MapOverlay.refreshCellColours();

            //if(CellUpdater.run % 50 == 0) WeatherDatabase.SavePlanetaryData(cellindex, HighLogic.SaveFolder);
        }
        internal static void InitPlanetData(PlanetData PD)
        {
            InitTropopauseAlts(PD);
            InitStuff(PD);
            if(configSaving)
            {
                WeatherDatabase.SaveConfigSimData(HighLogic.CurrentGame.config, 0);
            }
        }

        private static void InitStuff(PlanetData PD)
        {
            Logger("[PB]: Initializing temps and pressures for: " + PD.body.name);

            PD.LiveSoilMap = InitSoilCalcs(PD);
            PD.LiveMap = InitCalcs(PD);
            PD.LiveStratoMap = InitStratoCalcs(PD);

            PD.BufferSoilMap = InitSoilCalcs(PD);
            PD.BufferMap = InitCalcs(PD);
            PD.BufferStratoMap = InitStratoCalcs(PD);
            PD.updateTime = 10;
            WeatherDatabase.PlanetaryData[PD.index] = PD;
            Logger("[PB]: Successfully initialized temps and pressures for: " + PD.body.name);

        }

        private static KWSCellMap<SoilCell> InitSoilCalcs(PlanetData PD)
        {
            KWSCellMap<SoilCell> tempMap = new KWSCellMap<SoilCell>(PD.gridLevel);
            foreach(Cell cell in Cell.AtLevel(PD.gridLevel))
            {
                SoilCell wCell = tempMap[cell];
                wCell.temperature = GetInitTemperature(PD, 0, cell);
                
                tempMap[cell] = wCell;

            }
            return tempMap;
        }

        private static List<KWSCellMap<WeatherCell>> InitStratoCalcs(PlanetData PD)
        {
            List<KWSCellMap<WeatherCell>> tempMap = new List<KWSCellMap<WeatherCell>>();
            for(int i = 0; i < PD.stratoLayers; i++)
            {
                tempMap.Add(new KWSCellMap<WeatherCell>(PD.gridLevel));
            }
            for(int layer = 0; layer < PD.stratoLayers; layer++)
            {
                foreach (Cell cell in Cell.AtLevel(PD.gridLevel))
                {
                    WeatherCell wCell = new WeatherCell();

                    wCell.CCN = 0;
                    wCell.temperature = GetInitTemperature(PD, layer+layers, cell);
                    if(layer == 0)
                    {
                        wCell.pressure = (float)(PD.LiveMap[layers - 1][cell].pressure 
                            * Math.Exp(-WeatherFunctions.GetDeltaLayerAltitude(PD.index, cell) / (CellUpdater.UGC * PD.SH_correction / PD.atmoData.M / CellUpdater.G(PD.index, layer * WeatherFunctions.GetDeltaLayerAltitude(PD.index, cell)) * PD.LiveMap[layers - 1][cell].temperature)));
                    }
                    else
                    {
                        wCell.pressure = (float)(tempMap[layer - 1][cell].pressure 
                            * Math.Exp(-WeatherFunctions.GetDeltaLayerAltitude(PD.index, cell) / (CellUpdater.UGC * PD.SH_correction / PD.atmoData.M / CellUpdater.G(PD.index, layer * WeatherFunctions.GetDeltaLayerAltitude(PD.index, cell)) * tempMap[layer - 1][cell].temperature)));
                    }
                    

                    wCell.relativeHumidity = 0;
                    
                    wCell.windVector = new Vector3(0f, 0f, 0f);
                    tempMap[layer][cell] = wCell;
                }
            }
            return tempMap;
        }

        private static List<KWSCellMap<WeatherCell>> InitCalcs(PlanetData PD)
        {
            List<KWSCellMap<WeatherCell>> tempMap = new List<KWSCellMap<WeatherCell>>();
            for(int i = 0; i < PD.layers; i++)
            {
                tempMap.Add(new KWSCellMap<WeatherCell>(PD.gridLevel));
            }
            float basePressure = (float)FlightGlobals.getStaticPressure(0, PD.body) * 1000;

            for (int AltLayer = 0; AltLayer < PD.layers; AltLayer++)
            {
                foreach(Cell cell in Cell.AtLevel(PD.gridLevel))
                {
                    WeatherCell wCell = new WeatherCell();
                    
                    wCell.temperature = GetInitTemperature(PD, AltLayer, cell);

                    if (AltLayer == 0)
                    {
                        wCell.CCN = 1;
                        
                        wCell.pressure = basePressure;
                        wCell.relativeHumidity = PD.biomeDatas[WeatherFunctions.GetBiome(PD.index, cell)].FLC * 0.85f;  //* wCell.temperature / 288.15f
                    }
                    else
                    {
                        wCell.CCN = 0;
                        
                        wCell.pressure = (float)(tempMap[AltLayer-1][cell].pressure 
                            * Math.Exp(-WeatherFunctions.GetDeltaLayerAltitude(PD.index, cell) / (CellUpdater.UGC * PD.SH_correction / PD.atmoData.M / CellUpdater.G(PD.index, AltLayer * WeatherFunctions.GetDeltaLayerAltitude(PD.index, cell)) * tempMap[AltLayer - 1][cell].temperature)));
                        wCell.relativeHumidity = (PD.biomeDatas[WeatherFunctions.GetBiome(PD.index, cell)].FLC * wCell.temperature / 288.15f) * 0.4f;
                    }
                    
                    wCell.windVector = new Vector3(0f, 0f, 0f);
                    wCell.flowPChange = 0;
                    tempMap[AltLayer][cell] = wCell;
                }
            }
            

            return tempMap;
        }


        internal static void InitTropopauseAlts(PlanetData PD)
        {
            //kerbin's default mean troposphere height is 8815.22
            CelestialBody body = PD.body;
            if (body == null)
            {
                Logger("[PB]: Celestial Body supplied is null");
                return;
            }
            foreach (Cell cell in Cell.AtLevel(PD.gridLevel))
            {
                double latitude = WeatherFunctions.GetCellLatitude(cell);
                if (!PD.TropAlts.ContainsKey((float)latitude))
                {
                    double bodyRad = body.Radius;

                    //changes: added brackets around bodyRad nad meanTropoHeight

                    double tropHeight = Math.Sqrt(((bodyRad + PD.meanTropoHeight * 0.63) * Math.Sin(latitude * Mathf.Deg2Rad)) * ((bodyRad + PD.meanTropoHeight * 0.63) * Math.Sin(latitude * Mathf.Deg2Rad)) +
                        ((bodyRad + PD.meanTropoHeight * 1.2) * Math.Cos(latitude * Mathf.Deg2Rad)) * ((bodyRad + PD.meanTropoHeight * 1.2) * Math.Cos(latitude * Mathf.Deg2Rad))) - bodyRad;
                    PD.TropAlts.Add((float)latitude, (float)tropHeight);
                }
            }
            Logger("[PB]: Planet Tropopause Altitudes initialized for " + PD.body.name + "!");
        }
        internal static void InitBiomeMap(PlanetData PD)
        {
            foreach(Cell cell in Cell.AtLevel(PD.gridLevel))
            {
                PD.biomes[cell] = ScienceUtil.GetExperimentBiome(PD.body, WeatherFunctions.GetCellLatitude(cell), WeatherFunctions.GetCellLongitude(cell));
            }
        }
        internal static float GetInitTemperature(PlanetData PD, int AltLayer, Cell cell)
        {
            float Altitude = WeatherFunctions.GetCellAltitude(PD.index, AltLayer, cell);
            

            //get the proper fucking temperature because the obvious answer fucks you over for 3 weeks

            float sunAxialDot = (float)(WeatherFunctions.GetSunriseFactor(PD.index, cell));
            float latitude = Mathf.Abs(WeatherFunctions.GetCellLatitude(cell));

            float diurnalRange = PD.body.latitudeTemperatureSunMultCurve.Evaluate(latitude);
            float latTempMod = PD.body.latitudeTemperatureBiasCurve.Evaluate(latitude);
            float axialTempMod = PD.body.axialTemperatureSunMultCurve.Evaluate(sunAxialDot);
            float atmoTempOffset = latTempMod + diurnalRange * sunAxialDot + axialTempMod;
            float altTempMult = PD.body.atmosphereTemperatureSunMultCurve.Evaluate(Altitude);

            float finalTempMod = atmoTempOffset * altTempMult;
            return (float)FlightGlobals.getExternalTemperature(Altitude, PD.body) + finalTempMod;
        }
        internal static void Logger(String s)
        {
            WeatherLogger.Log("[WS]" + s);
        }
    }
}
