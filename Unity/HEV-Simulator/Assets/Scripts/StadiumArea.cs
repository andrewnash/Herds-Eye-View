using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents;

public class StadiumArea : Agent
{
    public GameObject walls;
    public GameObject floors;
    public GameObject pucks;
    public GameObject agents;

    Transform cube;
    Transform arch;

    Bounds bounds;
    float archSize;
    float cubeoffset;


    void Start()
    {
        walls = GameObject.Find("Walls");
        floors = GameObject.Find("Floors");
        pucks = GameObject.Find("Pucks");
        agents = GameObject.Find("Agents");

        cube = floors.transform.GetChild(0);
        arch = floors.transform.GetChild(2);
            
        archSize = arch.GetComponent<Renderer>().bounds.size.x - 2;
        cubeoffset = cube.GetComponent<Renderer>().bounds.size.z / 2 - 2;
        
        ResetStadium();
        ResetColors();
    }

    public void ResetStadium()
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
            u = 2.0f * Random.value - 1.0f;
            v = 2.0f * Random.value - 1.0f;
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

    Vector3 RandomPos()
    {
        var pos = Vector3.zero;

        // 50% chance to spawan in middle or sides
        if (Random.Range(0, 2) == 1)
        {
            pos = Random.insideUnitCircle * archSize;
            pos.z = pos.y > 0 ? pos.y + cubeoffset : pos.y - cubeoffset;
            pos += transform.position;
        }
        else
        {
            // 50% chance to spawn in top or bottom middle rectangles
            bounds = floors.transform.GetChild(Random.Range(0, 2)).GetComponent<Renderer>().bounds;
            pos.x = Random.Range(bounds.min.x + 2, bounds.max.x - 2);
            pos.z = Random.Range(bounds.min.z + 2, bounds.max.z - 2);
        }

        return pos;
    }   

    Color RandomColor()
    {
        return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
    }

    public void ResetColors()
    {
        // floor & wall pairs have matching indexs
        for (int i=0; i < walls.transform.childCount; i++)
        {
            Color randColor = RandomColor();
            walls.transform.GetChild(i).GetComponent<Renderer>().material.SetColor("_Color", randColor);
            floors.transform.GetChild(i).GetComponent<Renderer>().material.SetColor("_Color", randColor);
        }
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            ResetStadium();

        // reset objects if they fall off the map
        foreach (Transform puck in pucks.transform)
        {
            if (puck.transform.position.y < -1)
                ResetObject(puck, RandomPos());
        }
    }
}
