using System;
using UnityEngine;
using Unity.MLAgents;

namespace Unity.MLAgents.Sensors
{
    public class HEVGridSensorComponent : GridSensorComponent
    {
        public GridSensorBase[] grid()
        {
            //print(m_Sensors);
            return GetGridSensors();
        }

/*        protected override GridSensorBase[] GetGridSensors()
        {
            return new GridSensorBase[] { new HEVOneHotGridSensor(SensorName, m_CellScale, ) };
        }*/
    }
}
