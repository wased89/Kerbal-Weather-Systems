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
        
        public float SWA; //0.27 for kerbin something to do with the averaging of the bounces that would happen from sun rays --Short wave average
        public float IRA; //0.55 for kerbin something to do with the averaging of the bounces that would happen from the rays --infrared average
        public float M; //molar mass of air
        public float Spec_gas_const_dry;  //specific gas constant for dry air

        public float ks; //thermal conductivity as a gas

        public float n1; // refractive index (real part)
        // public float Xi_SW; // refractive index (imaginary part, visible band)
        // public float Xi_IR; // refractive index (imaginary part, IR band)

        public float a_VdW; // average attraction between particles (Van der Waals equation)
        public float b_VdW; // volume excluded by a mole of particles (Van der Waals equation)
    }
}
