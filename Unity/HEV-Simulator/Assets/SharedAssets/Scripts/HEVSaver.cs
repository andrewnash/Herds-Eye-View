using System.IO;
using UnityEngine;
using MBaske.Sensors.Grid;
using System.Collections.Generic;

public class HEVSaver : MonoBehaviour
{
    public List<Camera> Cameras;
    public List<GridSensorComponent2D> Grids;

    public Camera DebugCamera;

    public bool collectData;
    public int numFrames;
    public int colorChangeInterval;

    int frameCount = 0;
    string time;
    string rootFolder = "C:data/";

    struct Config
    {
        public int num_frames;
        public int num_robots;
        public int num_cameras;
    }

    void Start()
    {
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
        for (int i = 0; i < Cameras.Count; i++)
        {
            Directory.CreateDirectory(rootFolder + "/robot_" + i);
        }
        for (int i = 0; i < Grids.Count; i++)
        {
            Directory.CreateDirectory(rootFolder + "/hev_" + i);
        }

        Config config;
        config.num_frames = numFrames;
        config.num_robots = Grids.Count;
        config.num_cameras = Cameras.Count;

        File.WriteAllText(rootFolder + "/config.json", JsonUtility.ToJson(config, true));
    }

    void CaptureHEVFrames()
    {
        // Save intrinsics & extrinsics
        CameraData data = new CameraData();

        // save agent cameras
        for (int i = 0; i < Cameras.Count; i++)
        {
            Camera camera = Cameras[i];

            Capture(camera, string.Format("{0}/robot_{1}/", rootFolder, i.ToString()));
            data.AddCamera(camera, camera.transform);
        }

        // Save HEV
        for (int i = 0; i < Grids.Count; i++)
        {
            GridSensorComponent2D grid = Grids[i];
            
            // grid.GridSensor.Update();
            byte[] bytes = grid.GridSensor.GetCompressedObservation();
            File.WriteAllBytes(string.Format("{0}/hev_{1}/{2}.png", rootFolder, i.ToString(), frameCount), bytes);
        }

        // dump frame data
        File.WriteAllText(string.Format("{0}/data/{1}.json", rootFolder, frameCount), JsonUtility.ToJson(data.Objectify(), true));

        // Save Debug Camera
        Capture(DebugCamera, string.Format("{0}/debug_cam/", rootFolder));
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

            // comment out if building, haven't found a better way of handling this :(
            //if (Application.isEditor && ++frameCount >= numFrames) 
            //    UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
