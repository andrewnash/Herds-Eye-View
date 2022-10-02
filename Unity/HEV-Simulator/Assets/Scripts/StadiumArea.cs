using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents;
using Unity.Mathematics;
using MBaske.Sensors.Grid;

public class StadiumArea : Agent
{
    public bool collectData;
    public int numFrames;
    private int numRobots;

    string time;
    string rootFolder = "C:data/";

    GameObject pucks;
    GameObject agents;
    GameObject floors;
    GridSensor grid;

    Transform cube;
    Transform arch;

    Bounds bounds;
    float archSize;
    float cubeSize;

    void Start()
    {
        pucks = GameObject.Find("Pucks");
        floors = GameObject.Find("Floor");
        agents = GameObject.Find("Agents");
        grid = GetComponentInChildren<GridSensorComponent2D>().GridSensor;

        cube = floors.transform.GetChild(0);
        arch = floors.transform.GetChild(2);
            
        archSize = arch.GetComponent<Renderer>().bounds.size.x - 2;
        cubeSize = cube.GetComponent<Renderer>().bounds.size.z / 2 - 2;
        bounds = floors.transform.GetChild(UnityEngine.Random.Range(0, 2)).GetComponent<Renderer>().bounds;

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
        Directory.CreateDirectory(rootFolder + "/debug_cam");
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
        obj.transform.eulerAngles = new Vector3(0, BiasRandomAngle(position), 0);
        obj.transform.position = position;
    }

    public static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
    {
        float u, v, S;

        do
        {
            u = 2.0f * UnityEngine.Random.value - 1.0f;
            v = 2.0f * UnityEngine.Random.value - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        // Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

        // Normal Distribution centered between the min and max value
        // and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }

    // Random Angle Biased towards the center of the stadium
    float BiasRandomAngle(Vector3 pos)
    {
        float mid = Mathf.Atan2(transform.position.x - pos.x, transform.position.z - pos.z) * Mathf.Rad2Deg;
        return RandomGaussian(mid - 180, mid + 180);
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

    Vector3 RandomPos()
    {
        var pos = Vector3.zero;

        // 50% chance to spawan in middle or sides
        if (UnityEngine.Random.Range(0, 2) == 1)
        {
            pos = UnityEngine.Random.insideUnitCircle * archSize;
            pos.z = pos.y > 0 ? pos.y + cubeSize : pos.y - cubeSize;
            pos += transform.position;
        }
        else
        {
            pos.x = UnityEngine.Random.Range(bounds.min.x + 2, bounds.max.x - 2);
            pos.z = UnityEngine.Random.Range(bounds.min.z + 2, bounds.max.z - 2);
        }

        return pos;
    }

    void CaptureHEVFrames()
    {
        for (int i = 0; i < agents.transform.childCount; i++)
        {
            Camera camera = agents.transform.GetChild(i).transform.Find("AgentCamera").GetComponent<Camera>();
            GetIntrinsics(camera);
            Capture(camera, string.Format("{0}/robot_{1}/", rootFolder, i.ToString()));
        }

        Camera debugCam = GameObject.Find("Main Camera").GetComponent<Camera>();
        Capture(debugCam, string.Format("{0}/debug_cam/", rootFolder));

        grid.Update();
        byte[] bytes = grid.GetCompressedObservation();
        string filename = string.Format("{0}/hev/{1}.png", rootFolder, Time.frameCount.ToString());
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
        string filename = string.Format("{0}/{1}.png", path, Time.frameCount.ToString());
        System.IO.File.WriteAllBytes(filename, bytes);

        cam.targetTexture = null;
        RenderTexture.active = null;
        rt.Release();
    }   

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            ResetStadium();

        if (collectData == true)
        {
            CaptureHEVFrames();
            ResetStadium();
        }

        // reset objects if they fall off the map
        foreach (Transform puck in pucks.transform)
        {
            if (puck.transform.position.y < -1)
                ResetObject(puck, RandomPos());
        }
    }
}
