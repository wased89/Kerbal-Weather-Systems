using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalWeatherSystems
{
    internal static class WeatherLogger
    {
        private static Queue<String> queue = new Queue<string>();
        public static void Log(String message)
        {
            lock (queue)
            {
                queue.Enqueue("[KWS]"+message);
            }
        }
        public static void Update()
        {
            lock (queue)
            {
                while (queue.Count > 0)
                {
                    Debug.Log(queue.Dequeue());
                }
            }
        }
    }
}
