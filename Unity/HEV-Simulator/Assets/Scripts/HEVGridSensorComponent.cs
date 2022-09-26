using System;
using UnityEngine;

using Unity.MLAgents;
//using Unity.MLAgents.Sensors;

namespace Unity.MLAgents.Sensors
{
    public class HEVGridSensorComponent : GridSensorComponent
    {
/*        protected override GridSensorBase[] GetGridSensors()
        {
            return new GridSensorBase[] { new CustomGridSensor(...) };
        }*/
        public GridSensorBase[] grid() { return GetGridSensors(); }


    }
}
