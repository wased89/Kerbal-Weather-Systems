using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Database;
using Overlay;
using Simulation;

namespace KerbalWeatherSystems
{
    [KSPAddon(KSPAddon.Startup.MainMenu,true)]
    public class HeadMaster:MonoBehaviour
    {
        private static WeatherSimulator sim;
        private static bool prepToNew = true;
        private static bool prepToDestroy = false;
        public void Awake()
        {
            //this is a comment
            Logger("Awoke!");
            DontDestroyOnLoad(this);
            Logger("Checking basic files");
            WeatherDatabase.BasicSanityCheck();
            WeatherSettings.SettingsFileIntegrityCheck();
            if(WeatherDatabase.BasicFileIntegrityCheck())
            {
                Logger("Integrity check passed, continuing");
            }
            else
            {
                Logger("Integrity check failed, quitting.");
                return;
            }
            //init planet
            WeatherDatabase.LoadInitPlanetaryData();
            WeatherSettings.LoadSettings();
            GameEvents.onGameSceneSwitchRequested.Add(scene =>
            {
                if(scene.from == GameScenes.MAINMENU && scene.to == GameScenes.SPACECENTER)
                {
                    prepToNew = true;
                }
                else if(scene.from == GameScenes.SPACECENTER && scene.to == GameScenes.MAINMENU)
                {
                    prepToDestroy = true;
                    sim = null;
                    Destroy(this.gameObject.GetComponent<WeatherSimulator>());
                    Destroy(this.gameObject.GetComponent<MapOverlay>());
                    Destroy(this.gameObject.GetComponent<WeatherGUI.WeatherGUI>());
                    prepToDestroy = false;
                }
            });
            GameEvents.onGameStateLoad.Add(node =>
            {
                if(HighLogic.LoadedScene == GameScenes.SPACECENTER && prepToNew)
                {
                    //new up simulator
                    
                    sim = this.gameObject.AddComponent<WeatherSimulator>();
                    this.gameObject.AddComponent<MapOverlay>();
                    this.gameObject.AddComponent<WeatherGUI.WeatherGUI>();
                    prepToNew = false;
                }
                if(HighLogic.LoadedScene == GameScenes.MAINMENU && prepToDestroy)
                {
                    sim = null;
                    Destroy(this.gameObject.GetComponent<WeatherSimulator>());
                    Destroy(this.gameObject.GetComponent<MapOverlay>());
                    Destroy(this.gameObject.GetComponent<WeatherGUI.WeatherGUI>());
                    prepToDestroy = false;
                }
            });
        }
        public void FixedUpdate()
        {
            WeatherLogger.Update();
        }
        public void Logger(String s)
        {
            WeatherLogger.Log("[HM]" + s);
        }
    }
}
