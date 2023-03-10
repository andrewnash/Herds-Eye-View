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
    public int colorChangeInterval;

    int frameCount = 0;
    string time;
    string rootFolder = "C:data/";

    StadiumArea stadium;

    struct Config
    {
        public int num_frames;
        public int num_robots;
    }

    void Start()
    {
        stadium = GetComponent<StadiumArea>();

        stadium.currentMaxPucks = 24;
        stadium.obstructionMax = 3;

        init();
    }

    void init()
    {
        time = string.Format("{0}-{1}-{2}_{3}-{4}-{5}",
            System.DateTime.Now.Year.ToString(), System.DateTime.Now.Month.ToString(), System.DateTime.Now.Day.ToString(),
            System.DateTime.Now.Hour.ToString(), System.DateTime.Now.Minute.ToString(), System.DateTime.Now.Second.ToString());
        rootFolder = "C:/data/" + time + "/";

        Directory.CreateDirectory(rootFolder);
        Directory.CreateDirectory(rootFolder + "/data");
        Directory.CreateDirectory(rootFolder + "/debug_cam");
        for (int i = 0; i < stadium.agents.childCount; i++)
        {
            Directory.CreateDirectory(rootFolder + "/hev_" + i);
            Directory.CreateDirectory(rootFolder + "/robot_" + i);
        }
        
        Config config;
        config.num_frames = numFrames;
        config.num_robots = numRobots;

        File.WriteAllText(rootFolder + "/config.json", JsonUtility.ToJson(config, true));
    }

    void CaptureHEVFrames()
    {
        // Save intrinsics & extrinsics
        CameraData data = new CameraData();

        for (int i = 0; i < stadium.agents.childCount; i++)
        {
            // Save Robot Cameras
            Transform agent = stadium.agents.GetChild(i);
            Camera camera = agent.GetComponentInChildren<Camera>();
            Capture(camera, string.Format("{0}/robot_{1}/", rootFolder, i.ToString()));
            data.AddCamera(camera, agent);

            // Save HEV Frame
            agent.GetComponent<GridSensorComponent2D>().GridSensor.Update();
            byte[] bytes = agent.GetComponent<GridSensorComponent2D>().GridSensor.GetCompressedObservation();
            File.WriteAllBytes(string.Format("{0}/hev_{1}/{2}.png", rootFolder, i.ToString(), frameCount), bytes);
        }
        
        // dump frame data
        File.WriteAllText(string.Format("{0}/data/{1}.json", rootFolder, frameCount), JsonUtility.ToJson(data.Objectify(), true));

        // Save Debug Camera
        Camera debugCam = GameObject.Find("Main Camera").GetComponent<Camera>();
        Capture(debugCam, string.Format("{0}/debug_cam/", rootFolder));

        // Save HEV
        
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
        string filename = string.Format("{0}/{1}.png", path, frameCount);
        File.WriteAllBytes(filename, bytes);

        cam.targetTexture = null;
        RenderTexture.active = null;
        rt.Release();
    }

    void Update()
    {
        if (collectData)
        {
            CaptureHEVFrames();

            /*if (Application.isEditor && ++frameCount >= numFrames) 
                UnityEditor.EditorApplication.isPlaying = false;*/

            stadium.ResetStadium();
            //stadium.ResetObstructions();

            if (frameCount % colorChangeInterval == 0)
                stadium.ResetColors();
        }
    }
}
