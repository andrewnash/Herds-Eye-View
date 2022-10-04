using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using MBaske.Sensors.Grid;

public class StadiumSaver : MonoBehaviour
{
    public bool collectData;
    public int numFrames;
    public int numRobots;

    string time;
    string rootFolder = "C:data/";

    GridSensor grid;
    StadiumArea stadium;

    struct Config
    {
        public int num_frames;
        public int num_robots;
    }

    void Start()
    {
        stadium = GetComponent<StadiumArea>();
        grid = GetComponentInChildren<GridSensorComponent2D>().GridSensor;

        init();
    }

    void init()
    {
        time = string.Format("{0}-{1}-{2}_{3}-{4}-{5}",
            System.DateTime.Now.Year.ToString(), System.DateTime.Now.Month.ToString(), System.DateTime.Now.Day.ToString(),
            System.DateTime.Now.Hour.ToString(), System.DateTime.Now.Minute.ToString(), System.DateTime.Now.Second.ToString());
        rootFolder = "C:data/" + time + "/";

        Directory.CreateDirectory(rootFolder);
        Directory.CreateDirectory(rootFolder + "/hev");
        Directory.CreateDirectory(rootFolder + "/data");
        Directory.CreateDirectory(rootFolder + "/debug_cam");
        for (int i = 0; i < stadium.agents.transform.childCount; i++)
            Directory.CreateDirectory(rootFolder + "/robot_" + i.ToString());

        Config config;
        config.num_frames = numFrames;
        config.num_robots = numRobots;
        System.IO.File.WriteAllText(string.Format("{0}/config.json", rootFolder), JsonUtility.ToJson(config, true));
    }

    void CaptureHEVFrames()
    {
        // Save intrinsics & extrinsics
        CameraData data = new CameraData();

        for (int i = 0; i < stadium.agents.transform.childCount; i++)
        {
            // Save Robot Cameras
            Camera camera = stadium.agents.transform.GetChild(i).transform.Find("AgentCamera").GetComponent<Camera>();
            Capture(camera, string.Format("{0}/robot_{1}/", rootFolder, i.ToString()));
            data.AddCamera(camera);
        }
        //print(JsonUtility.ToJson(data.Objectify(), true));
        System.IO.File.WriteAllText(string.Format("{0}/data/{1}.json", rootFolder, Time.frameCount), JsonUtility.ToJson(data.Objectify(), true));

        // Save Debug Camera
        Camera debugCam = GameObject.Find("Main Camera").GetComponent<Camera>();
        Capture(debugCam, string.Format("{0}/debug_cam/", rootFolder));

        // Save HEV
        grid.Update();
        byte[] bytes = grid.GetCompressedObservation();
        System.IO.File.WriteAllBytes(string.Format("{0}/hev/{1}.png", rootFolder, Time.frameCount), bytes);
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
        string filename = string.Format("{0}/{1}.png", path, Time.frameCount);
        System.IO.File.WriteAllBytes(filename, bytes);

        cam.targetTexture = null;
        RenderTexture.active = null;
        rt.Release();
    }

    void Update()
    {
        if (collectData == true)
        {
            CaptureHEVFrames();
            stadium.ResetStadium();

            if (Time.frameCount >= numFrames)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
        }
    }
}

class CameraData
{
    public List<string> intrinsics;
    public List<string> extrinsics;

    public struct Data { public string intrinsics; public string extrinsics; }

    public CameraData()
    {
        intrinsics = new List<string>();
        extrinsics = new List<string>();
    }

    float3x3 GetIntrinsics(Camera cam)
    {
        float pixel_aspect_ratio = (float)cam.pixelWidth / (float)cam.pixelHeight;

        float alpha_u = cam.focalLength * ((float)cam.pixelWidth / cam.sensorSize.x);
        float alpha_v = cam.focalLength * pixel_aspect_ratio * ((float)cam.pixelHeight / cam.sensorSize.y);

        float u_0 = (float)cam.pixelWidth / 2;
        float v_0 = (float)cam.pixelHeight / 2;

        //IntrinsicMatrix in row major
        float3x3 camIntriMatrix = new float3x3(new float3(alpha_u, 0f, u_0),
                                               new float3(0f, alpha_v, v_0),
                                               new float3(0f, 0f, 1f));
        return camIntriMatrix;
    }

    public void AddCamera(Camera camera)
    {
        float3x3 i = GetIntrinsics(camera);
        Matrix4x4 e = Matrix4x4.TRS(camera.transform.localPosition, camera.transform.localRotation, camera.transform.localScale);

        intrinsics.Add(string.Format("[[{0}, {1}, {2}], [{3}, {4}, {5}], [{6}, {7}, {8}]]",
            i.c0.x, i.c0.y, i.c0.z,
            i.c1.x, i.c1.y, i.c1.z,
            i.c2.x, i.c2.y, i.c2.z));

        extrinsics.Add(string.Format("[[{0}, {1}, {2}, {3}], [{4}, {5}, {6}, {7}], [{8}, {9}, {10}, {11}], [{12}, {13}, {14}, {15}]]",
            e.m00, e.m01, e.m02, e.m03,
            e.m10, e.m11, e.m12, e.m13,
            e.m20, e.m21, e.m22, e.m23,
            e.m30, e.m31, e.m32, e.m33));
    }

    public Data Objectify()
    {
        Data data = new Data();
        data.intrinsics = "[";
        data.extrinsics = "[";

        for (int i=0; i < intrinsics.Count; i++)
        {
            if (i > 0)
            {
                data.intrinsics += ", ";
                data.extrinsics += ", ";
            }

            data.intrinsics += intrinsics[i];
            data.extrinsics += extrinsics[i];
        }

        data.intrinsics += "]";
        data.extrinsics += "]";

        return data;
    }
}
