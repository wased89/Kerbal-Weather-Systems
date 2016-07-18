using System;
using Database;
using GeodesicGrid;
using Overlay;
using UnityEngine;
using KerbalWeatherSystems;

using GUI = UnityEngine.GUI;

namespace WeatherGUI
{
    public class WeatherGUI : MonoBehaviour
    {
        public PlanetData PD;
        public Rect mainGUI = new Rect(200, 200, 200, 400);
        public Rect overlayGUI = new Rect(300, 300, 200, 350);

        public int mainGUID;
        public int overlayGUID;

        public bool showMainGUI = true;
        public bool showOverlayWindow = false;

        public int currentLayer = 0;

        public string minVal= "0";
        public string maxVal = "1";
        public string alpha = "1.0";
        public byte opacity = 190;
        

        public void Awake()
        {
            DontDestroyOnLoad(this);
            mainGUID = Guid.NewGuid().GetHashCode();
            overlayGUID = Guid.NewGuid().GetHashCode();
            minVal = MapOverlay.resource.MinQuantity.ToString();
            maxVal = MapOverlay.resource.MaxQuantity.ToString();
            MapOverlay.OnBodyChange += (pd => { PD = pd; RegrabVals(); });
            PD = MapOverlay.getPD();
            
        }
        public void OnGUI()
        {
            mainGUI = GUI.Window(mainGUID, mainGUI, mainWindow, "Weather Data~");
            if(showOverlayWindow)
            {
                overlayGUI = GUI.Window(overlayGUID, overlayGUI, overlayWindow, "Overlay~");
            }
        }
        public void mainWindow(int windowid)
        {
            GUILayout.BeginVertical();
            if(GUILayout.Button("Overlay")) { showOverlayWindow = !showOverlayWindow; }
            GUILayout.Space(1);
            GUILayout.Space(1);
            if(MapOverlay.getHoverCell().HasValue)
            {
                Cell cell = MapOverlay.getHoverCell().Value;
                GUILayout.Label("Temperature: " + WeatherFunctions.GetCellTemperature(PD.index, currentLayer, cell) + " °K");
                GUILayout.Label("Pressure: " + WeatherFunctions.GetCellPressure(PD.index, currentLayer, cell) + " Pa");
                GUILayout.Label("Rel Humidity: " + WeatherFunctions.GetCellRH(PD.index, currentLayer, cell) * 100 + " %");
                GUILayout.Label("Air Density: " + String.Format("{0:0.000000}", WeatherFunctions.D_Wet(PD.index, cell, currentLayer)) + " Kg/m³");
                GUILayout.Label("wind horz: " + String.Format("{0:0.0000}", WeatherFunctions.GetCellwindH(PD.index, currentLayer, cell)) + " m/s");
                GUILayout.Label("wind Dir : " + String.Format("{0:000.0}", WeatherFunctions.GetCellwindDir(PD.index, currentLayer, cell)) + " °");
                GUILayout.Label("wind vert : " + String.Format("{0:+0.00000;-0.00000}", WeatherFunctions.GetCellwindV(PD.index, currentLayer, cell)) + " m/s");
                GUILayout.Label("CCN : " + WeatherFunctions.GetCellCCN(PD.index, currentLayer, cell) * 100 + " %");
                GUILayout.Label("Cloud water : " + WeatherFunctions.GetCellWaterContent(PD.index, currentLayer, cell) + " Kg/m³");
                int Iced = Math.Sign(WeatherFunctions.GetCelldropletSize(PD.index, currentLayer, cell));
                GUILayout.Label("droplet Size: " + Math.Abs(WeatherFunctions.GetCelldropletSize(PD.index, currentLayer, cell) / 10000.0f) + " mm " + (Iced < 0 ? "Iced" : Iced > 0 ? "Liqd" : "None"));
                GUILayout.Label("cloud thickness: " + WeatherFunctions.GetCellthickness(PD.index, currentLayer, cell) + " m");
                GUILayout.Label("rain duration: " + WeatherFunctions.GetCellrainDuration(PD.index, currentLayer, cell) + " cycles");
                GUILayout.Label("rain decay: " + WeatherFunctions.GetCellrainDecay(PD.index, currentLayer, cell)/256.0f);
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        public void overlayWindow(int windowid)
        {
            GUILayout.BeginVertical();
            if(GUILayout.Button("Show overlay")){ MapOverlay.ToggleOverlay(); }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Opacity: ");
            //byte temp0 = 0;
            opacity = (byte)GUILayout.HorizontalSlider(opacity,0,255);
            MapOverlay.alpha = opacity;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("<")) {MapOverlay.prevResource(); RegrabVals();}
            GUILayout.TextField(MapOverlay.resource.Resource);
            if(GUILayout.Button(">")) {MapOverlay.nextResource(); RegrabVals();}
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            minVal = GUILayout.TextField(minVal);
            maxVal = GUILayout.TextField(maxVal);
            double temp1, temp2;
            double.TryParse(maxVal, out temp1);
            double.TryParse(minVal, out temp2);
            MapOverlay.resource.MaxQuantity = temp1;
            MapOverlay.resource.MinQuantity = temp2;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Layer:");
            if (GUILayout.Button("-")) { currentLayer = currentLayer > 0 ? currentLayer - 1 : 0; MapOverlay.currentLayer = currentLayer; }
            GUILayout.Label("" + currentLayer);
            if (GUILayout.Button("+")) { currentLayer = currentLayer == PD.LiveMap.Count - 1 ? PD.LiveMap.Count - 1 : currentLayer + 1; MapOverlay.currentLayer = currentLayer; }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Apply")) { MapOverlay.refreshCellColours(); }
            int height = MapOverlay.getOverlayTextureScale(MapOverlay.resource.Resource).height;
            GUI.DrawTexture(new Rect(25,192,150,height), MapOverlay.getOverlayTextureScale(MapOverlay.resource.Resource));
            GUILayout.Label("Colour Scale");
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        public void RegrabVals()
        {
            minVal = MapOverlay.resource.MinQuantity.ToString();
            maxVal = MapOverlay.resource.MaxQuantity.ToString();
        }
    }
}
