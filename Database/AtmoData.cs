using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Database
{
    public class AtmoData
    {
        public float specificHeatGas; //specific heat for the atmosphere
        public float specificHeatLiquid; //specific heat of water
        public float specificHeatSolid; //specific heat for the solid forms of the condensation
        
        public float SWA; //0.27 for kerbin something to do with the averaging of the bounces that would happen from sun rays
        public float IRA; //0.55 for kerbin something to do with the averaging of the bounces that would happen from the rays
        public float M; //molar mass of air

        public float k; //thermal conductivity
    }
}
