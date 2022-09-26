using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.MLAgents.Sensors
{
    public class HEVOneHotGridSensor : OneHotGridSensor
    {
        public HEVOneHotGridSensor(string name, Vector3 cellScale, Vector3Int gridSize, string[] detectableTags, SensorCompressionType compression) :
            base(name, cellScale, gridSize, detectableTags, compression)  
        {}
    }
}
