using System;
using UnityEngine;
using Unity.MLAgents;

namespace Unity.MLAgents.Sensors
{
    public class HEVGridSensorComponent : GridSensorComponent
    {
        public GridSensorBase[] grid() { return GetGridSensors(); }
    }
}
