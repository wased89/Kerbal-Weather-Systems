using UnityEngine;

namespace KerbalWeatherSystems.Overlay
{
    public class ResourceDefinition
    {
        public string Resource { get; internal set; }
        public Color ColorFull { get; internal set; }
        public Color ColorEmpty { get; internal set; }
        public double MaxQuantity { get; internal set; }
        public double MinQuantity { get; internal set; }

        public ResourceDefinition()
        {
            Resource = "";
            ColorFull = Color.white;
            ColorEmpty =  Color.black;
            MaxQuantity = 1; //normally 330 for temp
            MinQuantity = 0; //normally 200 for temp
        }
    }
}
