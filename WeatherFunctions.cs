﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using GeodesicGrid;
using Database;
using Simulation;

namespace KerbalWeatherSystems
{
    public class WeatherFunctions
    {
        internal static float UGC = 8.3144621f;

        public static double GetDeltaTime(int database)
        {
            return WeatherDatabase.PlanetaryData[database].updateTime;
        }
        public static double GetUTC()
        {
            return Planetarium.GetUniversalTime();
        }
        public static float GetDistanceBetweenCells(int database, Cell a, Cell b, float altitude)
        {
            //we use the obsolete AngleBetween because it returns in radians instead of degrees
            return (float)(Vector3.AngleBetween(a.Position, b.Position) * (WeatherDatabase.PlanetaryData[database].body.Radius + altitude));
            //Vector3 ab = a.position - b.position;
            //Vector3 resultant = ab - Vector3.Project(ab, a.position);
            //return (float)((a.position - b.position)*(WeatherDatabase.PlanetaryData[database].body.Radius + altitude));
        }
        public static Vector3 GetTheFuckingUpVector(Cell cell)
        {
            return cell.Position;
        }
        public static Vector3 GetTheUpVector(float latitude, float longitude)
        {
            return new Vector3(Mathf.Cos(latitude * Mathf.Deg2Rad) * Mathf.Cos(longitude * Mathf.Deg2Rad),
                Mathf.Sin(latitude * Mathf.Deg2Rad), Mathf.Cos(latitude * Mathf.Deg2Rad) * Mathf.Sin(longitude * Mathf.Deg2Rad));
        }
        public static Cell GetCellFromLatLong(int database, float lat, float lon)
        {
           return Cell.Containing(-GetTheUpVector(lat, lon).normalized, 
                WeatherDatabase.PlanetaryData[database].gridLevel);
        }

        public static Cell GetCellFromWorldPos(int database, Vector3 worldPos)
        {
           return Cell.Containing(WeatherDatabase.PlanetaryData[database].body.transform.InverseTransformPoint(worldPos),
               WeatherDatabase.PlanetaryData[database].gridLevel);
        }
        public static double GetBasePressurefromStock(int database, int layer, Cell cell)
        {
            return FlightGlobals.getStaticPressure(GetCellAltitude(database, layer, cell), WeatherDatabase.PlanetaryData[database].body);
        }
        public static string GetBiome(int database, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].biomes[cell];
        }
        public static WeatherCell GetCellData(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell];
        }
        public static float GetCellTemperature(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].temperature;
        }
        public static float GetCellPressure(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].pressure;
        }
        public static float GetCellRH(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].relativeHumidity;
        }
        public static float GetCellCCN(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].CCN;
        }
        public static float GetCelldDew(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].cloud.dDew;
        }
        public static float GetCellcDew(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].cloud.cDew;
        }
        public static float GetCellthickness(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].cloud.thickness;
        }
        public static float GetCelldropletSize(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].cloud.dropletSize;
        }
        public static float GetCellrainDuration(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].cloud.rainyDuration;
        }
        public static float GetCellrainDecay(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].cloud.rainyDecay;
        }
        public static float GetCellwindV(int database, int layer, Cell cell)
        {
            Vector3 windV = Vector3.Project(WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].windVector, cell.Position);
            return Vector3.Dot(windV.normalized, cell.Position) * windV.magnitude;
        }
        public static float GetCellwindH(int database, int layer, Cell cell)
        {
            WeatherCell wCell = WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell];
            return (wCell.windVector - Vector3.Project(wCell.windVector, cell.Position)).magnitude;
        }
        public static Vector3 GetCellWindDirection(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].windVector;
        }
        public static float GetCellWaterContent(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].cloud.getwaterContent();
        }
        public static UInt16 GetCellCloudThickness(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].LiveMap[layer][cell].cloud.thickness;
        }
        public static Vector3 GetCellWorldPos(int database, int layer, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].body.GetWorldSurfacePosition(
                GetCellLatitude(cell), GetCellLongitude(cell), GetCellAltitude(database, layer, cell));
        }
        public static float GetCellLongitude(Cell cell)
        {
            return (float)(Math.Atan2(cell.Position.z, cell.Position.x) * 180 / Math.PI);
        }

        public static float GetCellLatitude(Cell cell)
        {
            return (float)(Math.Asin(cell.Position.y) * 180 / Math.PI);
        }

        public static float GetCellAltitude(int database, int AltLayer, Cell cell) //soil should be layer 0, strato layers is Livemap.count + current strato layer
        {
            return AltLayer * GetDeltaLayerAltitude(database, cell);
        }
        public static double GetStockPressureAtAltitude(double alt, CelestialBody body = null)
        {
            return  body == null ? FlightGlobals.getStaticPressure(alt) : FlightGlobals.getStaticPressure(alt, body);
        }
        public static float GetDeltaLayerAltitude(int database, Cell cell)
        {
            return WeatherDatabase.PlanetaryData[database].TropAlts[GetCellLatitude(cell)] / (WeatherDatabase.PlanetaryData[database].layers);
        }
        public static float GetSunriseFactor(int database, Cell cell)
        {
            return (float)Math.Max(0, Math.Cos(GetSunlightAngle(database, cell) * Mathf.Deg2Rad)); // need this to stay related to GetSunlightAngle for now
           
        }
        public static double GetSunlightAngle(int database, Cell cell)
        {
            Vector3 cellPos = cell.Position;
            Vector3d sunPos = WeatherDatabase.PlanetaryData[database].body.transform.InverseTransformPoint(GetSunPosition());
            Vector3d sub = sunPos - cellPos;
            sub.Normalize();
            return Mathf.Rad2Deg*Math.Acos(Mathf.Clamp(Vector3.Dot(cellPos, sub), -1f ,1f));
        }

        public static double GetSunlightAngle(int database, Vector3 worldPos3D)
        {
            Vector3 cellPos = Cell.Containing(WeatherDatabase.PlanetaryData[database].body.transform.TransformPoint(worldPos3D),
                WeatherDatabase.PlanetaryData[database].gridLevel).Position;
            Vector3d sunPos = WeatherDatabase.PlanetaryData[database].body.transform.InverseTransformPoint(GetSunPosition());
            Vector3d sub = sunPos - cellPos;
            sub.Normalize();
            return Mathf.Rad2Deg * Math.Acos(Mathf.Clamp(Vector3.Dot(cellPos, sub), -1f, 1f));
        }
        public static Vector3 GetSunPosition()
        {
            return Planetarium.fetch.Sun.position;
        }
        /*
        public static float GetCellArea(int database, int AltLayer, Cell cell)  //TODO: this holds true only for regular polygons, however hexagons often aren't
        {
            Cell neighbor = cell.GetNeighbors(WeatherDatabase.PlanetaryData[database].gridLevel).First();
            int neighborCount = cell.GetNeighbors(WeatherDatabase.PlanetaryData[database].gridLevel).ToList().Count;
            float altitude = GetCellAltitude(database, AltLayer, cell);
            float distance = GetDistanceBetweenCells(database, cell, neighbor, altitude);
            
            return neighborCount == 6 ? (float)((distance / Math.Sqrt(3)) * distance * (3f / 2f))
                : (float)((distance * Math.Sqrt(5 - 2 * Math.Sqrt(5))) * distance * 5f / 4f);
        }
        
        public static float GetCellPerimeter(int database, int AltLayer, Cell cell)  //TODO: redo to consider non-regular cell hexagons
        {
            Cell neighbor = cell.GetNeighbors(WeatherDatabase.PlanetaryData[database].gridLevel).First();
            int neighborCount = cell.GetNeighbors(WeatherDatabase.PlanetaryData[database].gridLevel).ToList().Count;
            float altitude = GetCellAltitude(database, AltLayer, cell);
            float distance = GetDistanceBetweenCells(database, cell, neighbor, altitude);
            
            return neighborCount == 6 ? (float)(distance / Math.Sqrt(3)) * neighborCount 
                : (float)(distance * Math.Sqrt(5 - 2 * Math.Sqrt(5)) * neighborCount);
        }
        */
        public static float SphereSize2Volume(float size)
        {
            return (float)(4.0 / 3.0 * Math.PI * size*size*size);
        }
        public static float SphereVolume2Size(float volume)
        {
            return (float)Math.Pow(3d / 4d / Math.PI * volume, 1.0 / 3d);
        }
        internal static Int16 AverageDropletSize16(Int16 Size, byte decay, UInt16 duration)
        {
            if (decay == 0 || duration == 0) { return Math.Abs(Size); }
            else // Size *= rate at each duration term, the series converges to an integral as returned
            {
                double rate = (double)(256 - decay) / 256;
                double intK = -1 / Math.Log(rate);  // integration constant
                return (short)(Size * (Math.Pow(rate, (duration)) / Math.Log(rate) + intK) / (duration));
            }
        }
        internal static double AverageDropletSize(Int16 Size, byte decay, UInt16 duration)
        {
            if (decay == 0 || duration == 0) { return Math.Abs(Size)/1E7; }
            else // Size *= rate at each duration term, the series converges to an integral as returned
            {
                double rate = (double)(256 - decay) / 256;
                double intK = -1 / Math.Log(rate);  // integration constant
                return (Size * (Math.Pow(rate, duration) / Math.Log(rate) + intK) / duration / 1E7);
            }
        }
        public static double DensityAtVessel(int database, Vessel vessel)
        {
            PlanetData PD = WeatherDatabase.PlanetaryData[database];
            Cell cell = GetCellFromLatLong(database, (float)(vessel.latitude), (float)(vessel.longitude));
            if (vessel.altitude > PD.TropAlts[GetCellLatitude(cell)])
            {
                return vessel.atmDensity;
            }
            else
            {
                return D_Wet(database, GetLayerAtAltitude(database, (float)vessel.altitude, cell), cell);
            }
        }
        public static int GetLayerAtAltitude(int database, float altitude, Cell cell)
        {
            if(altitude > (WeatherDatabase.PlanetaryData[database].LiveMap.Count + WeatherDatabase.PlanetaryData[database].LiveStratoMap.Count)*GetDeltaLayerAltitude(database, cell))
            {
                return WeatherDatabase.PlanetaryData[database].LiveStratoMap.Count-1;
            }
            else
            {
                return (int)(altitude / GetDeltaLayerAltitude(database, cell));
            }
        }
        internal static double D_Wet(int database, int AltLayer, Cell cell)  //TODO: provide public access for D_wet (at any altitude)
        {  // to: find D_wet above and below required altitude; interpolate for P at that altitude and compute with P/rho^k_ad = constant (ideal gas law)
            PlanetData PD = WeatherDatabase.PlanetaryData[database];
            float deltaAltitude = GetDeltaLayerAltitude(database, cell);
            float altitude= AltLayer * deltaAltitude;
            float altitudeRem = (altitude - AltLayer * deltaAltitude);
            if (AltLayer < PD.LiveMap.Count) // troposphere: compute density with interpolation of relativeHumidity
            {
                float RHRate;
                float LapseRate;
                if (AltLayer < PD.LiveMap.Count - 1)
                {
                    RHRate = (PD.LiveMap[AltLayer][cell].relativeHumidity - PD.LiveMap[AltLayer + 1][cell].relativeHumidity) / deltaAltitude;
                    LapseRate = (PD.LiveMap[AltLayer][cell].temperature - PD.LiveMap[AltLayer + 1][cell].temperature) / deltaAltitude;
                }
                else
                {
                    RHRate = (PD.LiveMap[AltLayer][cell].relativeHumidity) / deltaAltitude;
                    LapseRate = (PD.LiveMap[AltLayer][cell].temperature - PD.LiveStratoMap[0][cell].temperature) / deltaAltitude;
                }
                float RHint = PD.LiveMap[AltLayer][cell].relativeHumidity + RHRate * altitudeRem;
                float Tint = PD.LiveMap[AltLayer][cell].temperature + LapseRate * altitudeRem;
                float Pint = (float)(PD.LiveMap[AltLayer][cell].pressure * Math.Exp(-altitudeRem / (SH(PD.index, AltLayer, Tint, cell))));
                double ew_eq = getEwEq(database, Tint);
                double ew = ew_eq * RHint;
                return ((Pint - ew) * PD.atmoData.M + ew * PD.dewData.M) / (CellUpdater.UGC * Tint);
            }
            else  // stratosphere
            {
                return 0;
            }
        }  
        internal static float getEwEq(int database, float temperature)  // Antoine equation for water vapor pressure, in Pa
        {
            PlanetData PD = WeatherDatabase.PlanetaryData[database];
            if (temperature >= 304)
            {
                return Mathf.Pow(10, PD.dewData.A1 - PD.dewData.B1 / (temperature + PD.dewData.C1));
            }
            else
            {
                return Mathf.Pow(10, PD.dewData.A2 - PD.dewData.B2 / (temperature + PD.dewData.C2));
            }
        }
        public static double VdW(AtmoData substance,  float pressure, float temperature, out double error)  // computes gas density according to real gas equation
        {
            /*
            Density of a gas substance (ρ) = m/V; mass of substance (m); amount of substance in moles (n) = m/M
            from ideal gas law {PV = nRT}:  ρ = PM/(RT) (https://en.wikipedia.org/wiki/Ideal_gas_law)
            from Van der Waals law {(P+an²/V²)(V-nb) = nRT}:   (https://en.wikipedia.org/wiki/Van_der_Waals_equation)
            a = particles_attraction; b = volume_excluded (http://chemwiki.ucdavis.edu/Reference/Reference_Tables/Atomic_and_Molecular_Properties/A8%3A_van_der_Waal's_Constants_for_Real_Gases)
            */
            // NOTE: database substances must have data for a = particles_attraction; b = volume_excluded
            // double a = substance.particles_attraction; // (J * m³ / mol²)
            // double b = substance.volume_excluded; // (m³ / mol)
            double a = substance.a_VdW;
            double b = substance.b_VdW;
            float M = substance.M;
            double n = pressure / temperature / CellUpdater.UGC;
            double Dideal = n * M;
            double Pcorr = pressure + a * n * n;
            double Vcorr = 1 - b * n;
            double Dcorr = Pcorr/temperature*M/CellUpdater.UGC*Vcorr;
            error = (Dcorr - Dideal) / Dcorr;  // gives how much error is done with the ideal gas law
            return Dcorr;
        }
        public static double SH(int database, int layer, float temperature, Cell cell)
        {
            //Mean mass of molecule Mm = M * Na (Molar mass * Avogadro constant) = M * UGC/kb (Molar mass * UniversalGasConstant / Boltzmann constant)
            //ScaleHeight SH = R*T/g = Kb*T/(Mm*g) (https://en.wikipedia.org/wiki/Scale_height) = UGC * T / (M * g)
            //ScaleHeight = UniversalGasConstant * Temperature / (AtmosphereMolarMass * gravity(altitude))
            //NOTE: PD.SH_correction is applied to have the resulting pressure curve match on average with the pressure curve in KSP
            PlanetData PD = WeatherDatabase.PlanetaryData[database];
            return UGC * PD.SH_correction / PD.atmoData.M / (float)G(PD.index, layer, cell) * temperature;
        }
        public static double G(int database, int layer, Cell cell)
        {
            PlanetData PD = WeatherDatabase.PlanetaryData[database];
            return PD.body.gravParameter / ((PD.body.Radius + (layer * GetDeltaLayerAltitude(database, cell))) * (PD.body.Radius + (layer * GetDeltaLayerAltitude(database, cell))));
            // m/s^2 =         m^3/s^2      /        m^2
        }
        internal static Vector3 GetHorizontalVector(int database, Cell cell, Vector3 vector) //gets the horizontal vector of a vector relative to the cell in planet space
        {
            return vector - Vector3.Project(vector, cell.Position);
        }
        internal static Vector3 GetVerticalVector(int database, Cell cell, Vector3 vector)//gets the vertical vector of a vector relative to the cell in planet space
        {
            return Vector3.Project(vector, cell.Position);
        }
    }
}
