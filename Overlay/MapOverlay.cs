using GeodesicGrid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Database;
using KerbalWeatherSystems;
using Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using Resources;

namespace Overlay
{
    public class MapOverlay : MonoBehaviour
    {
        public static MapOverlay Instance { get; private set; }
        public static Cell? hoverCell;
        private BoundsMap BM;

        private Dictionary<CelestialBody, double> bodyRadii = new Dictionary<CelestialBody, double>();
        private double bodyradius = 1.012;
        private CelestialBody body;
        private static Func<Cell, float> heightRatio;
        private BoundsMap bounds;
        private static PlanetData PD;
        private static int mouseGUID;

        internal static int currentLayer = 0;

        internal static bool showOverlay = true;
        private static bool revealAll = true;

        private GameObject kerbGO;
        private static OverlayRenderer or;

        private static readonly Color32 colorEmpty = new Color32(128, 128, 128, 120);
        private static readonly Color32 colorUnknown = new Color32(0, 0, 0, 128);
        internal static byte alpha = 190;
        internal static ResourceDefinition resource = new ResourceDefinition();

        internal static List<ResourceDefinition> resources = new List<ResourceDefinition>();
        internal static Dictionary<String, Texture2D> overlayColourTextures = new Dictionary<string, Texture2D>();

        public static void ToggleOverlay()
        {
            showOverlay = !showOverlay;
        }
        public static Cell? getHoverCell()
        {
            return hoverCell;
        }
        public static PlanetData getPD()
        {
            return PD;
        }
        public static Texture2D getOverlayTextureScale(String s)
        {
            return overlayColourTextures[s];
        }
        public void Awake()
        {
            resources.Clear();
            enabled = true;
            PD = WeatherDatabase.PlanetaryData[0];
            
            mouseGUID = Guid.NewGuid().GetHashCode();
            #region Resource Definitions
            var tempResc = new ResourceDefinition();
            tempResc.Resource = "Temperature";
            tempResc.MinQuantity = 200;
            tempResc.MaxQuantity = 330;
            resources.Add(tempResc);
            
            

            var pressResc = new ResourceDefinition();
            pressResc.Resource = "Pressure";
            pressResc.MinQuantity = 100000;
            pressResc.MaxQuantity = 101500;
            resources.Add(pressResc);
            var WsHResc = new ResourceDefinition();
            WsHResc.Resource = "Wind H Speed";
            WsHResc.MinQuantity = 0;
            WsHResc.MaxQuantity = 100;
            resources.Add(WsHResc);
            var WsDirResc = new ResourceDefinition();
            WsDirResc.Resource = "Wind H Vector";
            WsDirResc.MinQuantity = 0;
            WsDirResc.MaxQuantity = 100;
            resources.Add(WsDirResc);
            var WsVResc = new ResourceDefinition();
            WsVResc.Resource = "Wind Vertical";
            WsVResc.MinQuantity = -10;
            WsVResc.MaxQuantity = 10;
            resources.Add(WsVResc);
            var RHresc = new ResourceDefinition();
            RHresc.Resource = "Rel. Humidity";
            RHresc.MinQuantity = 0;
            RHresc.MaxQuantity = 1;
            resources.Add(RHresc);
            var WCResc = new ResourceDefinition();
            WCResc.Resource = "Cloud water";
            WCResc.MinQuantity = 0;
            WCResc.MaxQuantity = 1E0;
            resources.Add(WCResc);
            var GeoResc = new ResourceDefinition();
            GeoResc.Resource = "Geodesy";
            GeoResc.MinQuantity = 0.9999999;
            GeoResc.MaxQuantity = 1.0000001;
            resources.Add(GeoResc);
            #endregion

            SetActiveResource(0);
            //MakeOverlayLegends();
            LoadTextureScales();
            WeatherSimulator.GetInitTemperature(PD, 0, hoverCell.Value);
        }
        
        public void LoadTextureScales()
        {
            overlayColourTextures.Clear();
            foreach(ResourceDefinition res in resources)
            {
                // overlayColourTextures.Add(res.Resource, new Texture2D(150,20));
                double Min = res.MinQuantity;
                double Max = res.MaxQuantity;
                Color32 myColor = new Color32();
                switch (res.Resource)
                {
                    /*
                    case "Temperature":
                        byte[] bytes = File.ReadAllBytes(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/Resources/overlayLegend" + res.Resource + ".png");
                        overlayColourTextures[res.Resource].LoadImage(bytes);
                        break;
                        */
                    case "Wind H Vector":
                        overlayColourTextures.Add(res.Resource, new Texture2D(150, 150));
                        Texture2D text0 = overlayColourTextures[res.Resource];
                        for (int x = 0; x < text0.width; x++)
                        {
                            for (int y = 0; y < text0.height; y++)
                            {
                                text0.SetPixel(x, y, getDepositColor(res, (-Max + (2 * Max * (float)y / text0.height)), (-Max + (2 * Max * (float)x / text0.width))));
                            }
                        }
                        text0.Apply();
                        break;
                    case "Cloud water":
                        overlayColourTextures.Add(res.Resource, new Texture2D(150, 64));
                        Texture2D text2 = overlayColourTextures[res.Resource];
                        for (int x = 0; x < text2.width; x++)
                        {
                            for (int y = 0; y < text2.height; y++)
                            {
                                text2.SetPixel(x, y, getDepositColor(res, ((Math.Pow((float)x * Max / text2.width, 10))), (float)y/text2.height));
                            }
                        }
                        text2.Apply();
                        break;
                    default:
                        overlayColourTextures.Add(res.Resource, new Texture2D(150, 20));
                        Texture2D text1 = overlayColourTextures[res.Resource];
                        for (int x = 0; x < text1.width; x++)
                        {
                            myColor = getDepositColor(res, ((Min + (Max - Min) * (float)x / text1.width)), 0);
                            for (int y = 0; y < text1.height; y++)
                            {
                                text1.SetPixel(x, y, myColor);
                            }
                        }
                        text1.Apply();
                        break;
                }
            }
        }
        public void MakeOverlayLegends()
        {
            //make the temperature legend
            var res = resources[0];
            Texture2D text = new Texture2D(150, 20);
            for (int y = 0; y < 20; y++)
            {
                for(int x = 0; x < 750; x++)
                {
                    text.SetPixel(x/5, y, getDepositColor(res, (((float)x / 750) * res.MaxQuantity), 0));
                }
            }
            byte[] bytes = text.EncodeToPNG();
            File.WriteAllBytes(KSPUtil.ApplicationRootPath + "/GameData/KerbalWeatherSystems/Resources/overlayLegend" + res.Resource + ".png", bytes);
        }
        public void SetActiveResource(int index)
        {
            resource = resources[index];
        }
        public static void nextResource()
        {
            if(resources.IndexOf(resource) + 1 >= resources.Count)
            {
                resource = resources[0];
            }
            else
            {
                resource = resources[resources.IndexOf(resource) + 1];
            }
            onActiveResourceChange();
        }
        public static void prevResource()
        {
            
            if (resources.IndexOf(resource) <= 0)
            {
                resource = resources[resources.Count - 1];
            }
            else
            {
                resource = resources[resources.IndexOf(resource) - 1];
            }
            onActiveResourceChange();
        }
        private static void onActiveResourceChange()
        {
            refreshCellColours();
        }
        public void Start()
        {
            GameObject.DontDestroyOnLoad(this);
            Instance = this;
            heightRatio = getHeightRatioMap(PD.body);
            BM = new BoundsMap(heightRatio, PD.gridLevel);
            kerbGO = new GameObject();
            kerbGO.name = "kerbGO";
            
            kerbGO.transform.parent = PD.body.MapObject.transform;
            kerbGO.transform.localScale = Vector3.one*1000;
            kerbGO.transform.localPosition = Vector3.zero;
            kerbGO.transform.localRotation = Quaternion.identity;

            
            or = gameObject.AddComponent<OverlayRenderer>();

            or.SetGridLevel(PD.gridLevel);

            foreach (PlanetData pd in WeatherDatabase.PlanetaryData)
            {
                double result;
                if (!bodyRadii.ContainsKey(pd.body))
                    bodyRadii.Add(pd.body, 1.012);
                else
                    bodyRadii[pd.body] = 1.012;
            }

        }
        public void OnDestroy()
        {
            Instance = null;
        }
        public void Update()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT && HighLogic.LoadedScene != GameScenes.TRACKSTATION)
            {
                or.IsVisible = false;
                return;
            }
            if (!MapView.MapIsEnabled || !showOverlay || MapView.MapCamera == null)
            {
                or.IsVisible = false;
                return;
            }

            or.IsVisible = true;
            
            var target = MapView.MapCamera.target;
            var newBody = getTargetBody(target);
            var bodyChanged = (newBody != null) && (newBody != body);

            if (bodyChanged)
            {
                body = newBody;

                heightRatio = getHeightRatioMap(body);
                bounds = new BoundsMap(heightRatio, PD.gridLevel);

                or.SetHeightMap(heightRatio);

                var radius = bodyRadii.ContainsKey(body) ? bodyRadii[body] : 1.025;
                or.SetRadiusMultiplier((float)radius);
                or.SetTarget(body.MapObject.transform);
            }
            if (bodyChanged)
            {
                refreshCellColours();
            }


            Ray ray = PlanetariumCamera.Camera.ScreenPointToRay(Input.mousePosition);
            hoverCell = Cell.Raycast(ray, PD.gridLevel, BM, heightRatio, kerbGO.transform);
            if(hoverCell.HasValue)
            {
                //or.SetCellColor(hoverCell.Value, getCellColor(0, hoverCell.Value));
                
            }
        }
        public delegate void BodyChanged(PlanetData PD);
        public static event BodyChanged OnBodyChange;
        protected virtual void OnBodyChanged()
        {
            PD = WeatherDatabase.GetPlanetData(body);
            if(OnBodyChange != null)
            {
                OnBodyChange(PD);
            }
        }
        /*
        internal static void refreshXCellColours(int cellIndex, int cellsToUpdate)
        {
            for(int i = cellIndex; i > (Math.Max(0, cellIndex - cellsToUpdate)); i--)
            {
                Cell cell = new Cell((uint)i);
                or.SetCellColor(cell, getCellColor(currentLayer, cell));
            }
        }
        */
        internal static void refreshCellColours()
        {
            var colors = new CellMap<Color32>(PD.gridLevel, c => getCellColor(currentLayer, c));
            or.SetCellColors(colors);
        }
        private static Color32 getCellColor(int layer, Cell cell)
        {
            double deposit = 0;
            double deposit2 = 0;
            switch(resource.Resource)
            {
                case "Temperature":
                    deposit = PD.LiveMap[layer][cell].temperature;
                    break;
                case "Pressure":
                    deposit = PD.LiveMap[layer][cell].pressure;
                    break;
                case "Wind H Speed":
                    deposit = WeatherFunctions.GetCellwindH(PD.index, layer, cell);
                    break;
                case "Wind H Vector":
                    //deposit = WeatherFunctions.GetCellwindDir(PD.index, layer, cell);
                    deposit = PD.LiveMap[layer][cell].windVector.x;
                    deposit2 = PD.LiveMap[layer][cell].windVector.z;
                    break;
                case "Wind Vertical":
                    deposit = PD.LiveMap[layer][cell].windVector.y;
                    break;
                case "Rel. Humidity":
                    deposit = PD.LiveMap[layer][cell].relativeHumidity;
                    break;
                case "Cloud water":
                    deposit = PD.LiveMap[layer][cell].cloud.getwaterContent();
                    deposit2 = WeatherFunctions.GetSunriseFactor(PD.index, cell);
                    break;
                case "Geodesy":
                    // deposit = cell.Position.magnitude;
                    deposit = Math.Sqrt(cell.Position.x * cell.Position.x + cell.Position.y * cell.Position.y + cell.Position.z * cell.Position.z);
                    break;
            }
            
            var scanned = true;
            var color = (revealAll ? deposit != null : scanned) ? getDepositColor(resource, deposit, deposit2) : colorUnknown;
            return color;
        }
        private static Color32 getDepositColor(ResourceDefinition definition, double? deposit, double? deposit2)
        {
            Color32 color= new Color32();
            if (deposit != null)
            {
                double thing1 = deposit.Value > definition.MaxQuantity ? definition.MaxQuantity : deposit.Value < definition.MinQuantity ? definition.MinQuantity : deposit.Value;
                float ratio = (float)Math.Min(thing1 / definition.MaxQuantity, 1);
                int val = (int)(ratio * (255 * 4));

                if (definition.Resource.Equals("Temperature"))
                {
                    int c0 = (int)((255*4)*Math.Min(273.15 / definition.MaxQuantity, 1));
                    int c25 = (int)((255 * 4) * Math.Min(298.15 / definition.MaxQuantity, 1));

                    byte r = (byte)Mathf.Clamp(deposit.Value > 273 ? (int)(Math.Abs(c0 - val) * 1.75) : (int)(Math.Abs(c0 - val) * 1.5), 0, 255);
                    byte g = (byte)Mathf.Clamp(deposit.Value > 273 ? val > c25 ? 255 - (val - 870) : 255 - (val - 844) : 0, 0, 255);
                    byte b = (byte)Mathf.Clamp(deposit.Value < 273 ? 255 : 0, 0, 255);
                    color = new Color32(r, g, b, alpha);
                    return color;
                }
                if (definition.Resource.Equals("Pressure"))
                {
                    ratio = (float)Math.Min(((thing1-definition.MinQuantity)) / ((definition.MaxQuantity-definition.MinQuantity)), 1);
                    val = (int)(ratio * (255 * 4));
                    //black -> red -> purple -> blue -> yellow -> white
                    int sec1 = 1020 / 4; //red will be 100% at this value, the first section from black -> red
                    byte r = (byte)Mathf.Clamp(val < sec1 ? 255 * ((float)val/sec1): val < sec1*2 ?  255*(1-((float)(val-sec1))/sec1):val<sec1*3 ? 255*((float)(val-sec1*2)/sec1):255,0,255);
                    byte g = (byte)Mathf.Clamp(val<sec1*2 ? 0: val<sec1*3 ? 255* (((float)(val -sec1*2))/sec1) : 255, 0, 255);
                    byte b = (byte)Mathf.Clamp(val<sec1 ? 0: val<sec1*2 ? 255*(((float)(val-sec1))/sec1) : val<sec1*3 ? 255*(1-(((float)(val-sec1*2))/sec1)) : 255*((float)(val-sec1*3)/sec1), 0, 255);


                    color = new Color32(r, g, b, alpha);
                    return color;
                }
                if (definition.Resource.Equals("Wind H Speed"))  // shows horizontal windspeed
                {
                    int sec1 = 1020 / 10;

                    byte r = (byte)Mathf.Clamp(val<sec1*2? 0 : val<sec1*4? (float)(255*((float)(val-sec1*2)/sec1/2.0f)) : 255, 0, 255);
                    byte g = (byte)Mathf.Clamp(val<sec1*2? (float)(255* ((float)val/sec1/2.0f)) : val<sec1*4 ? 255 : val<sec1*5 ?  (255*(1-(float)(val-sec1*4)/sec1)): (float)(255*((float)(val-sec1*5)/sec1/5.0f)), 0, 255);
                    byte b = (byte)Mathf.Clamp(val<sec1*2? 255* ((float)(1-val/sec1)/2.0f) : val<sec1*4 ? 0 : val<sec1*5 ?  (255*((float)(val-sec1*4)/sec1)): 255, 0, 255);
                    color = new Color32(r, g, b, alpha);
                    return color;
                }
                if (definition.Resource.Equals("Wind H Vector"))  //this shows horizontal wind direction and intensity
                {
                    double Ws = Math.Sqrt((double)(deposit.Value * deposit.Value + deposit2.Value * deposit2.Value));
                    byte brightness = 63;  // brightness (0..255) at 0 windspeed
                    if (Ws <= 0) { color = new Color32(brightness, brightness, brightness, alpha); return color; }
                    else
                    {
                        thing1 = Math.Sqrt(Ws);
                        ratio = (float)Math.Min(((thing1 - Math.Sqrt(definition.MinQuantity))) / (Math.Sqrt(definition.MaxQuantity) - Math.Sqrt(definition.MinQuantity)), 1);
                        double valN = ((double)deposit.Value / Ws * (ratio * 255));
                        double valE = ((double)deposit2.Value / Ws * (ratio * 255));

                        // colour wheel centered on (brightness) RGB triplet: increasingly red for Easterly winds, increasingly green for SSW, increasingly blue for NNW
                        byte r = (byte)Mathf.Clamp((brightness + (float)valE / 255 * (255 - brightness)), 0, 255);
                        byte g = (byte)Mathf.Clamp((brightness + ((-0.5f) * (float)valE / 255 + (-0.866f) * (float)valN / 255) * (255 - brightness)), 0, 255);
                        byte b = (byte)Mathf.Clamp((brightness + ((-0.5f) * (float)valE / 255 + (+0.866f) * (float)valN / 255) * (255 - brightness)), 0, 255);
                        color = new Color32(r, g, b, alpha);
                        return color;
                    }
                }
                if (definition.Resource.Equals("Wind Vertical"))
                {
                    ratio = (float)Math.Min(((Math.Sqrt(Math.Abs(thing1)) * Math.Sign(thing1) - Math.Sqrt(Math.Abs(definition.MinQuantity)) * Math.Sign(definition.MinQuantity)) 
                        / ((Math.Sqrt(Math.Abs(definition.MaxQuantity)) * Math.Sign(definition.MaxQuantity) - Math.Sqrt(Math.Abs(definition.MinQuantity)) * Math.Sign(definition.MinQuantity)))), 1);
                    val = (int)(ratio * (255 * 4));

                    // red -----> cyan
                    int sec1 = 1020;
                    byte r = (byte)Mathf.Clamp(255 * (1 - (float)val / sec1) , 0, 255);
                    byte g = (byte)Mathf.Clamp(255 * ((float)val / sec1) , 0, 255);
                    byte b = (byte)Mathf.Clamp(255 * ((float)val / sec1), 0, 255);
                    color = new Color32(r, g, b, alpha);
                    return color;
                }
                if (definition.Resource.Equals("Rel. Humidity"))
                {
                    ratio = (float)Math.Min(((thing1 - definition.MinQuantity)) / ((definition.MaxQuantity - definition.MinQuantity)), 1);
                    val = (int)(ratio * (255 * 4));

                    // ochre ----> dark grey --> blue --> cyan
                    int sec1 = 1020 / 4;
                    byte r = (byte)Mathf.Clamp(val < sec1 * 2 ? (255 - 160 * (float)val / (sec1 * 2)) : val < sec1 * 3 ? (95 * (1 - ((float)val - sec1 * 2) / sec1)) : 0, 0, 255);
                    byte g = (byte)Mathf.Clamp(val < sec1 * 2 ? (191 - 96 * (float)val / (sec1 * 2)) : val < sec1 * 3 ? (95 - 32 * ((float)val - sec1 * 2) / sec1) : (63 + 192 * ((float)val - sec1 * 3)/sec1), 0, 255);
                    byte b = (byte)Mathf.Clamp(val < sec1 * 2 ? (112 * (float)val / (sec1 * 2)) : val < sec1 * 3 ? (112 + 143 * ((float)val - sec1 * 2)/sec1) : 255, 0, 255);
                    color = new Color32(r, g, b, alpha);
                    return color;
                }
                if (definition.Resource.Equals("Cloud water"))
                {
                    ratio = (float)(deposit.Value / definition.MaxQuantity);
                    // have a log scale (WC goes from 0 up in terms of 10E-n)
                    byte alpha1 = (deposit <= 0 ? (byte)15 : (byte)Mathf.Clamp((float)(255 + Math.Round(15 * Math.Log10(ratio))), 15, 255));
                    byte brightness = (byte)(63 + (float)(192 * deposit2.Value)); // to have brightness increase with SunRiseFactor

                    color = new Color32(brightness, brightness, brightness, alpha1);
                    return color;
                }
                if (definition.Resource.Equals("Geodesy"))
                {
                    ratio = (float)Math.Min(((thing1 - definition.MinQuantity)) / ((definition.MaxQuantity - definition.MinQuantity)), 1);
                    val = (int)(ratio * (255 * 4));
                    byte r = (byte)Mathf.Clamp((255 - (float)val/4), 0, 255);
                    byte g = (byte)Mathf.Clamp((float)val/4, 0, 255);
                    byte b = g;
                    color = new Color32(r, g, b, alpha);
                    return color;
                }

            }
            else
            {
                color = colorEmpty;
            }
            return color;
        }
        public void OnGUI()
        {
            if (hoverCell != null)
            {
                Vector2 mouse = Event.current.mousePosition;
                Rect position = new Rect(mouse.x + 16, mouse.y + 4, 160, 32);
                GUILayout.Window(mouseGUID, position, mouseWindow, "KWS Debug Info~");
            }
        }

        public void mouseWindow(int windowID)
        {
            Cell cell = hoverCell.Value;
            
            float lat = WeatherFunctions.GetCellLatitude(cell);
            float lon = WeatherFunctions.GetCellLongitude(cell);

            GUILayout.BeginVertical();

            GUILayout.Label("Cell: " + cell.Index);
            GUILayout.Label("Lat: " + lat + " °");
            GUILayout.Label("Lon: " + lon + " °");
            /*
            GUILayout.Label("");
            if (WeatherSettings.SD.MWtemperature) { GUILayout.Label("Temperature: " + WeatherFunctions.GetCellTemperature(PD.index, currentLayer, cell)); }
            if (WeatherSettings.SD.MWpressure) { GUILayout.Label("Pressure: " + WeatherFunctions.GetCellPressure(PD.index, currentLayer, cell)); }
            if (WeatherSettings.SD.MWRH) { GUILayout.Label("Rel Humidity: " + WeatherFunctions.GetCellRH(PD.index, currentLayer, cell) * 100); }
            if (WeatherSettings.SD.MWdensity) { GUILayout.Label("Density: " + String.Format("{0:+0.000000}", WeatherFunctions.D_Wet(PD.index, cell, WeatherFunctions.GetCellAltitude(PD.index, currentLayer, cell)))); }
            if (WeatherSettings.SD.MWCCN) { GUILayout.Label("CCN %: " + WeatherFunctions.GetCellCCN(PD.index, currentLayer, cell) * 100); }
            if (WeatherSettings.SD.MWdDew) { GUILayout.Label("condensed Dew: " + WeatherFunctions.GetCellcDew(PD.index, currentLayer, cell)); }
            if (WeatherSettings.SD.MWcDew) { GUILayout.Label("deposited Dew: " + WeatherFunctions.GetCelldDew(PD.index, currentLayer, cell)); }
            if (WeatherSettings.SD.MWDropletSize) { GUILayout.Label("droplet Size: " + WeatherFunctions.GetCelldropletSize(PD.index, currentLayer, cell)); }
            if (WeatherSettings.SD.MWthickness) { GUILayout.Label("cloud thickness: " + WeatherFunctions.GetCellthickness(PD.index, currentLayer, cell)); }
            if (WeatherSettings.SD.MWrainDuration) { GUILayout.Label("rain duration: " + WeatherFunctions.GetCellrainDuration(PD.index, currentLayer, cell)); }
            if (WeatherSettings.SD.MWrainDecay) { GUILayout.Label("rain decay: " + WeatherFunctions.GetCellrainDecay(PD.index, currentLayer, cell)); }
            if (WeatherSettings.SD.MWwindH)
            {
                GUILayout.Label("wind horz: " + String.Format("{0:0.0000}", WeatherFunctions.GetCellwindH(PD.index, currentLayer, cell)));
                GUILayout.Label("wind Dir : " + String.Format("{0:000.0}", WeatherFunctions.GetCellwindDir(PD.index, currentLayer, cell)));
            }
            if (WeatherSettings.SD.MWwindV) { GUILayout.Label("wind vert: " + String.Format("{0:+0.00000;-0.00000}", WeatherFunctions.GetCellwindV(PD.index, currentLayer, cell))); }
            */
            GUILayout.Label("Geodesic: " + Math.Sqrt(cell.Position.x * cell.Position.x + cell.Position.y * cell.Position.y + cell.Position.z * cell.Position.z));
            GUILayout.EndVertical();

        }
        private static CelestialBody getTargetBody(MapObject target)
        {
            if (target.type == MapObject.ObjectType.CelestialBody)
            {
                return target.celestialBody;
            }
            else if (target.type == MapObject.ObjectType.ManeuverNode)
            {
                return target.maneuverNode.patch.referenceBody;
            }
            else if (target.type == MapObject.ObjectType.Vessel)
            {
                return target.vessel.mainBody;
            }

            return null;
        }
        private Func<Cell, float> getHeightRatioMap(CelestialBody body)
        {
            Func<Cell, float> heightRatioAt;

            try
            {
                var bodyTerrain = TerrainData.ForBody(body, PD.gridLevel);
                heightRatioAt = c => Math.Max(1, bodyTerrain.GetHeightRatio(c));
            }
            catch (ArgumentException)
            {
                heightRatioAt = c => 1;
            }

            return heightRatioAt;
        }
        private static double Magnify(double Min, double Max, double Value, double LensSize, float magnification, double LensCenter)
        /*
        Min, Max = extremes of the admitted range of values to be displayed
        Value = the value being processed for magnification
        LensSize, magnification, LensCenter = field of magnification, power, and center Value for the field: all to be set by user
        Magnification factor below magnifying field = (LensCenter - Min - LensSize / 2 / magnification) / (LensCenter - Min - LensSize / 2);
        Magnification factor above magnifying field = (Max - LensCenter - LensSize / 2 / magnification) / (Max - LensCenter - LensSize / 2);
        */
        {
            if ((LensCenter < Min) || (LensCenter > Max) || (LensSize <= 0) || (magnification <= 1))  // equations would produce "optical aberrations" unless checked
            {
                return Value;
            }
            if (LensSize > (Max-Min)/2) { LensSize = (Max - Min) / 2; }  // magnifying more than the allowed field cuts all outside values from view
            if (Value < LensCenter - LensSize / 2)  // Value below magnifying field
            {
                return Min + (Value - Min) * (LensCenter - Min - LensSize / 2) / (LensCenter - Min - LensSize / 2 / magnification);
            }
            else if (Value <= LensCenter + LensSize / 2) // Value within magnifying field
            {
                return LensCenter + (Value - LensCenter) / magnification;
            }
            else  // Value above magnifying field
            {
                return Max + (Value - Max) * (Max - LensCenter - LensSize / 2) / (Max - LensCenter - LensSize / 2 / magnification);
            }
        }
    }
}

