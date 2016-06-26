using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEngine;
using GeodesicGrid;
using KerbalWeatherSystems;
using Database;

namespace Simulation
{
    class Statistics
    {
        public static void PrintStat(PlanetData PD)  //TODO: make for multi-threading, this method will be inherently safe (doesn't write, and only uses objects not being changed)
        {
            int layerCount = PD.LiveMap.Count;
            int stratoCount = PD.LiveStratoMap.Count;
            KWSCellMap<WeatherCell> cellMap = new KWSCellMap<WeatherCell>(PD.gridLevel);

            float avgValue = 0;
            float maxValue = 0;
            float minValue = 0;
            uint maxIndex = 0;
            uint minIndex = 0;
            // Cell thisCell = new Cell();

            string[] values = { "temperature","pressure", "relativeHumidity", "CCN", "cloud.dropletSize", "cloud.rainyDuration", "windvector.y"}; //the values we want to get from the cell

            // check if Stat folder exists, create it
            if (!Directory.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/Stat"))
            {
                Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/Stat");
            }
            //write to Statistics Log file
            Debug.Log("Writing Statistics file");
            int num = Directory.GetFiles(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/Stat/").Length;
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/Stat/Stat" + num + "cy" + CellUpdater.run + ".txt"))
            {
                // int layer = 0;
                String STS = "  |  ";


                file.WriteLine("Body: " + PD.body.bodyName + STS + "Update Cycle: " + CellUpdater.run + STS + "AvgProcessTime (μs): " + WeatherSimulator.AvgCycleTime);
                file.WriteLine();
                // print: Total cells, other general data about the collection
                file.WriteLine("Number of cells: " + Cell.CountAtLevel(PD.gridLevel));
                file.WriteLine();
                    
                foreach(string s in values)  // each valid s prints a section
                {
                    file.WriteLine(s);
                    file.WriteLine("layer" + STS + "     average  " + STS + "      minimum  " + " cell " + STS + "      maximum  " + " cell " + STS);
                    for (int i = stratoCount - 1; i >= 0; i--)
                    {
                        cellMap = PD.LiveStratoMap[i];
                            
                        if (cellMap[new Cell(0)].GetType().GetProperty(s) != null && cellMap[new Cell(0)].GetType().GetProperty(s).Name.Equals(s))
                        {
                            avgValue = cellMap.Average(cell =>  (float)cell.Value.GetType().GetProperty(s).GetValue(cell.Value, null));
                            minValue = cellMap.Min(cell => (float)cell.Value.GetType().GetProperty(s).GetValue(cell.Value, null));
                            maxValue = cellMap.Max(cell => (float)cell.Value.GetType().GetProperty(s).GetValue(cell.Value, null));
                            foreach (Cell cell in Cell.AtLevel(PD.gridLevel))
                            {
                                if ((float)cellMap[cell].GetType().GetProperty(s).GetValue(cellMap[cell], null) == minValue) { minIndex = cell.Index; }
                                if ((float)cellMap[cell].GetType().GetProperty(s).GetValue(cellMap[cell], null) == maxValue) { maxIndex = cell.Index; }
                            }
                        }
                        else{ avgValue = 0; minValue = 0; maxValue = 0; minIndex = 0; maxIndex = 0; }
                        file.WriteLine("{0,-4} {1,5} {2,12:N4} {3,5} {4,12:N4} {5,6} {6,5} {7,12:N4} {8,6} {9,5}", "St_" + i, STS, avgValue, STS, minValue, minIndex, STS, maxValue, maxIndex, STS);
                    }
                    for (int i = layerCount - 1; i >= 0; i--)
                    {
                        cellMap = PD.LiveMap[i];
                        if (cellMap[new Cell(0)].GetType().GetProperty(s) != null && cellMap[new Cell(0)].GetType().GetProperty(s).Name.Equals(s))
                        {
                            avgValue = cellMap.Average(cell => cell.Value.GetType().GetProperty(s).Name.Equals(s) ? (float)cell.Value.GetType().GetProperty(s).GetValue(cell.Value, null) : 0f);
                            minValue = cellMap.Min(cell => cell.Value.GetType().GetProperty(s).Name.Equals(s) ? (float)cell.Value.GetType().GetProperty(s).GetValue(cell.Value, null) : 0f);
                            maxValue = cellMap.Max(cell => cell.Value.GetType().GetProperty(s).Name.Equals(s) ? (float)cell.Value.GetType().GetProperty(s).GetValue(cell.Value, null) : 0f);
                            foreach (Cell cell in Cell.AtLevel(PD.gridLevel))
                            {
                                if ((float)cellMap[cell].GetType().GetProperty(s).GetValue(cellMap[cell], null) == minValue) { minIndex = cell.Index; }
                                if ((float)cellMap[cell].GetType().GetProperty(s).GetValue(cellMap[cell], null) == maxValue) { maxIndex = cell.Index; }
                            }
                        }
                        else { avgValue = 0; minValue = 0; maxValue = 0; minIndex = 0; maxIndex = 0; }
                        file.WriteLine("{0,-4} {1,5} {2,12:N4} {3,5} {4,12:N4} {5,6} {6,5} {7,12:N4} {8,6} {9,5}", "Tp_" + i, STS, avgValue, STS, minValue, minIndex, STS, maxValue, maxIndex, STS);
                    }
                    // Soil layer

                    if (PD.LiveSoilMap[new Cell(0)].GetType().GetProperty(s) != null && cellMap[new Cell(0)].GetType().GetProperty(s).Name.Equals(s))
                    {
                        avgValue = PD.LiveSoilMap.Average(cell => (float)cell.Value.GetType().GetProperty(s).GetValue(cell.Value, null));
                        minValue = PD.LiveSoilMap.Min(cell => (float)cell.Value.GetType().GetProperty(s).GetValue(cell.Value, null));
                        maxValue = PD.LiveSoilMap.Max(cell => (float)cell.Value.GetType().GetProperty(s).GetValue(cell.Value, null));
                        foreach (Cell cell in Cell.AtLevel(PD.gridLevel))
                        {
                            if ((float)PD.LiveSoilMap[cell].GetType().GetProperty(s).GetValue(cellMap[cell], null) == minValue) { minIndex = cell.Index; }
                            if ((float)PD.LiveSoilMap[cell].GetType().GetProperty(s).GetValue(cellMap[cell], null) == maxValue) { maxIndex = cell.Index; }
                        }
                        file.WriteLine("{0,-4} {1,5} {2,12:N4} {3,5} {4,12:N4} {5,6} {6,5} {7,12:N4} {8,6} {9,5}", "Soil", STS, avgValue, STS, minValue, minIndex, STS, maxValue, maxIndex, STS);
                    }
                         
                    file.WriteLine(); // closing section

                    // avgValue = cellMap.Average(cell => cell.Value.cloud.GetType().GetProperty(s).GetValue(cell.Value, null));
                }
            }
        }
    }
}
