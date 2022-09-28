using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors.Reflection;

namespace UnityEngine
{
    public class StadiumArea : Agent
    {
        public bool collectData;
        private int frame = 0;

        private string time;
        private string rootFolder = "C:data/";

        GameObject pucks;
        GameObject agents;
        GameObject floors;

        void Start()
        {
            pucks = GameObject.Find("Pucks");
            floors = GameObject.Find("Floor");
            agents = GameObject.Find("Agents");

            setupHEVSaving();
            ResetStadium();

        }

        void setupHEVSaving()
        {
            time = string.Format("{0}-{1}-{2}_{3}-{4}-{5}",
                System.DateTime.Now.Year.ToString(), System.DateTime.Now.Month.ToString(), System.DateTime.Now.Day.ToString(),
                System.DateTime.Now.Hour.ToString(), System.DateTime.Now.Minute.ToString(), System.DateTime.Now.Second.ToString());
            rootFolder = "C:data/" + time + "/";

            Directory.CreateDirectory(rootFolder);
            Directory.CreateDirectory(rootFolder + "/hev");
            Directory.CreateDirectory(rootFolder + "/data");
            for (int i = 0; i < agents.transform.childCount; i++)
                Directory.CreateDirectory(rootFolder + "/robot_" + i.ToString());
        }

        void ResetStadium()
        {
            foreach (Transform puck in pucks.transform)
            {
                Vector3 pos = RandomPos();
                pos.y = 0f;
                ResetObject(puck, pos);
            }
            foreach (Transform agent in agents.transform)
            {
                Vector3 pos = RandomPos();
                pos.y = 1f;
                ResetObject(agent, pos);
            }
        }

        void ResetObject(Transform obj, Vector3 position)
        {
            obj.transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
            obj.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            obj.transform.eulerAngles = new Vector3(0, Random.Range(0, 360), 0);
            obj.transform.position = position;
        }

        Vector3 RandomPos()
        {
            var pos = Vector3.zero;
            var cube = floors.transform.GetChild(0);
            var arch = floors.transform.GetChild(2);

            // 50% chance to spawan in middle or sides
            if (Random.Range(0, 2) == 1)
            {
                var archSize = arch.GetComponent<Renderer>().bounds.size.x - 2;
                var cubeSize = cube.GetComponent<Renderer>().bounds.size.z / 2 - 2;

                pos = Random.insideUnitCircle * archSize;
                pos.z = pos.y > 0 ? pos.y + cubeSize : pos.y - cubeSize;
                pos += transform.position;
            }
            else
            {
                var bounds = floors.transform.GetChild(Random.Range(0, 2)).GetComponent<Renderer>().bounds;
                pos.x = Random.Range(bounds.min.x + 2, bounds.max.x - 2);
                pos.z = Random.Range(bounds.min.z + 2, bounds.max.z - 2);
            }

            pos.y = 0;
            return pos;
        }

        void CaptureHEVFrames()
        {
            for (int i = 0; i < agents.transform.childCount; i++)
            {
                Camera camera = agents.transform.GetChild(i).transform.Find("AgentCamera").GetComponent<Camera>();
                Capture(camera, string.Format("{0}/robot_{1}/", rootFolder, i.ToString()));
            }


            ISensor grid = GetComponent<HEVGridSensorComponent>().CreateSensors()[0];
            //print(sensors[0].GetCompressedObservation()); ;
            //GridSensorBase grid = GetComponent<HEVGridSensorComponent>().grid()[0];

            grid.Reset();
            grid.Update();
            //print(grid.ObservationSize());
            byte[] bytes = grid.GetCompressedObservation();
            string filename = string.Format("{0}/hev/{1}.png", rootFolder, frame.ToString());
            System.IO.File.WriteAllBytes(filename, bytes);
        }

        public void Capture(Camera cam, string path)
        {
            int resWidth = cam.pixelWidth;
            int resHeight = cam.pixelHeight;
            RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
            cam.targetTexture = rt;
            cam.Render();

            Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            screenShot.ReadPixels(cam.pixelRect, 0, 0);
            screenShot.Apply();

            byte[] bytes = screenShot.EncodeToPNG();
            string filename = string.Format("{0}/{1}.png", path, frame.ToString());
            System.IO.File.WriteAllBytes(filename, bytes);

            cam.targetTexture = null;
            RenderTexture.active = null;
            rt.Release();
        }

        void Update()
        {
            var keyboard = Keyboard.current;

            if (keyboard.escapeKey.wasPressedThisFrame)
                ResetStadium();

            if (collectData == true)
            {
                ResetStadium();
                CaptureHEVFrames();
            }

            // reset objects if they fall off the map
            foreach (Transform puck in pucks.transform)
            {
                if (puck.transform.position.y < -1)
                    ResetObject(puck, RandomPos());
            }
        }
    }
}
