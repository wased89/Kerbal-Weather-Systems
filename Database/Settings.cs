using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using KerbalWeatherSystems;

namespace Database
{
    class WeatherSettings
    {
        public static SettingsData SD = new SettingsData();

        internal static void SaveSettings() // NOTE: currently no settings are set at runtime, none to be saved
        {
            String line = null;
            StreamWriter file = new StreamWriter(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/Settings.cfg");
            line = "debugLog = " + SD.debugLog;
            file.WriteLine(line);
            line = "debugNeighbors = " + SD.debugNeighbors;
            file.WriteLine(line);
            line = "debugCell = " + SD.debugCell;
            file.WriteLine(line);
            line = "LogStartCycle = " + SD.LogStartCycle;
            file.WriteLine(line);
            line = "cellsPerUpdate = " + SD.cellsPerUpdate;
            file.WriteLine(line);
            line = "statistics = " + SD.statistics;
            file.WriteLine(line);
            line = "SoilThCapMult = " + SD.SoilThCapMult;
            file.WriteLine(line);
            line = "AtmoThCapMult = " + SD.AtmoThCapMult;
            file.WriteLine(line);
            line = "SoilIRGFactor = " + SD.SoilIRGFactor;
            file.WriteLine(line);
            line = "AtmoIRGFactor = " + SD.AtmoIRGFactor;
            file.WriteLine(line);
            Logger("Settings saved");
        }

        internal static void LoadSettings()
        {
            SD.debugCell = 0;

            String line = null;
            StreamReader file = new StreamReader(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/Settings.cfg");
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("debugLog"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    SD.debugLog = bool.Parse(line);
                    continue;
                }
                if (line.StartsWith("debugNeighbors"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    SD.debugNeighbors = bool.Parse(line);
                    continue;
                }
                if (line.StartsWith("debugCell"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    SD.debugCell = UInt16.Parse(line);
                    continue;
                }
                if (line.StartsWith("LogStartCycle"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    SD.LogStartCycle = UInt32.Parse(line);
                    continue;
                }
                if (line.StartsWith("cellsPerUpdate"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    SD.cellsPerUpdate = byte.Parse(line);
                    continue;
                }
                if (line.StartsWith("statistics"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    SD.statistics = bool.Parse(line);
                    continue;
                }
                if (line.StartsWith("SoilThCapMult"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    SD.SoilThCapMult = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("AtmoThCapMult"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    SD.AtmoThCapMult = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("SoilIRGFactor"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    SD.SoilIRGFactor = float.Parse(line);
                    continue;
                }
                if (line.StartsWith("AtmoIRGFactor"))
                {
                    line = line.Substring(line.IndexOf("=") + 2);
                    SD.AtmoIRGFactor = float.Parse(line);
                    continue;
                }
            }
            Logger("Settings loaded");
        }

        internal static string InitializeSettings()
        {
            String line = null;
            StreamWriter file = new StreamWriter(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/Settings.cfg");
            line = "debugLog = false";
            file.WriteLine(line);
            line = "debugNeighbors = false";
            file.WriteLine(line);
            line = "debugCell = 0";
            file.WriteLine(line);
            line = "LogStartCycle = " + UInt32.MaxValue;
            file.WriteLine(line);
            line = "cellsPerUpdate = 5";
            file.WriteLine(line);
            line = "statistics = false";
            file.WriteLine(line);
            line = "SoilThCapMult = 1";
            file.WriteLine(line);
            line = "AtmoThCapMult = 1";
            file.WriteLine(line);
            line = "SoilIRGFactor = 1";
            file.WriteLine(line);
            line = "AtmoIRGFactor = 1";
            file.WriteLine(line);
            file.Close();
            return "Settings initialized";
        }

        internal static bool SettingsFileIntegrityCheck()
        {

            if (!File.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/Settings.cfg"))
            {
                Logger("Kerbin Settings Data Missing");
                Logger(InitializeSettings());
                return false;
            }
            return true;
        }

        private static void Logger(String s)
        {
            WeatherLogger.Log("[SD]" + s);
        }
    }
}
