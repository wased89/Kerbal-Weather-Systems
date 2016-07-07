using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Database
{
    public class SettingsData
    {
        public bool debugLog; //  activate logs ?
        public bool debugNeighbors; // log the neighbors
        public uint debugCell;  // weatherCell to be examined
        public uint LogStartCycle;  // cycle when debugLog writes start
        public byte cellsPerUpdate;  // weather cells updated at each frame
        public bool statistics; // activate the statistics ?
        // Temperature dynamic tuning
        public float SoilThCapMult;
        public float AtmoThCapMult;
        public float SoilIRGFactor;
        public float AtmoIRGFactor;
    }
}
