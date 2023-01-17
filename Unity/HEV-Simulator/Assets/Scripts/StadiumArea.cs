using MBaske.Sensors.Grid;
using System.Collections;
using System.Net.WebSockets;
using NaughtyAttributes.Test;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class StadiumArea : MonoBehaviour
{
    public Transform walls;
    public Transform floors;
    public Transform pucks;
    public Transform agents;
    public Transform obstructions;

    //vector of obstacles
    private Transform[] currentObstructions = new Transform[0];

    public Transform GoalStadiumArea;

    public Transform win;
    public Transform loose;

    Transform cube;
    Transform arch;

    Bounds bounds;
    float archSize;
    float cubeoffset;

    public float currentMaxPucks;
    public int obstructionMax;

    public Vector2Int pucksRange;

    void Start()
    {
        cube = floors.transform.GetChild(0);
        arch = floors.transform.GetChild(2);
            
        archSize = arch.GetComponent<Renderer>().bounds.size.x - 2;
        cubeoffset = cube.GetComponent<Renderer>().bounds.size.z / 2 - 2;
        
        ResetStadium();
        ResetObstructions();
        ResetColors();
    }

    // Randomize the scale of all pucks
    public void ResetObstructions()
    {
        int numObs = Random.Range(0, obstructionMax + 1);

        foreach (Transform obs in currentObstructions)
        {
            obs.gameObject.SetActive(false);
        }

        currentObstructions = new Transform[numObs];
        for (int i = 0; i < numObs; i++)
        {
            Transform randObs = null;
            do // get a random obstruction that's not active
            { 
                randObs = obstructions.GetChild(Random.Range(0, obstructions.childCount));
            } while (randObs.gameObject.activeSelf);

            randObs.gameObject.SetActive(true);
            randObs.position = RandomPos();
            randObs.eulerAngles = new Vector3(90, Random.Range(0f, 360f), 0);
            randObs.GetComponent<Renderer>().material.SetColor("_Color", RandomColor());

            currentObstructions[i] = randObs;
        }
    }

    // int pucks to reset, int agents to reset
    public void ResetStadium()
    {
        currentMaxPucks = Random.Range(pucksRange.x-1, pucksRange.y);

        for (int i=0; i < pucks.childCount; i++)
        {
            Transform puck = pucks.GetChild(i);
            if (i > currentMaxPucks)
            {
                // disable puck over current max
                puck.gameObject.SetActive(false);
                continue;
            }

            puck.gameObject.SetActive(true);
            RandomResetObject(puck, 0f);
        }
        foreach (Transform agent in agents)
        {
            RandomResetObject(agent.GetComponentInChildren<Rigidbody>().transform, 1f);
        }
    }

    void ResetObject(Transform obj, Vector3 position)
    {
        obj.GetComponent<Rigidbody>().velocity = Vector3.zero;
        obj.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        obj.eulerAngles = new Vector3(0, BiasRandomAngle(position), 0);
        obj.position = position;
    }

    void RandomResetObject(Transform obj, float y)
    {
        ResetObject(obj, RandomPos(y));

        // check if has StadiumCollisionChecker component
        if (obj.GetComponent<StadiumCollisionChecker>() != null && obj.GetComponent<StadiumCollisionChecker>().isActiveAndEnabled)
        {
            // check if colliding
            while (obj.GetComponent<StadiumCollisionChecker>().IsColliding())
            {
                obj.GetComponent<StadiumCollisionChecker>().ResetCollisions();
                ResetObject(obj, RandomPos(y));
            }
        }
    }

    public void ChildReset(Transform child, float y)
    {
        child.GetComponent<Rigidbody>().velocity = Vector3.zero;
        child.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        child.eulerAngles = new Vector3(0, BiasRandomAngle(child.transform.position), 0);
        child.position = RandomPos(y);
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

    public Vector3 RandomPos(float y = 0f)
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
            bounds = floors.GetChild(Random.Range(0, 2)).GetComponent<Renderer>().bounds;
            pos.x = Random.Range(bounds.min.x + 2, bounds.max.x - 2);
            pos.z = Random.Range(bounds.min.z + 2, bounds.max.z - 2);
        }

        pos.y = y;
        return pos;
    }   

    Color RandomColor()
    {
        return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
    }

    public void ResetColors()
    {
        // floor & wall pairs have matching indexs
        for (int i=0; i < walls.childCount; i++)
        {
            Color randColor = RandomColor();
            walls.GetChild(i).GetComponent<Renderer>().material.SetColor("_Color", randColor);
            floors.GetChild(i).GetComponent<Renderer>().material.SetColor("_Color", randColor);
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            ResetStadium();
            ResetObstructions();
            ResetColors();
        }

    }

    public void winAnimation()
    {
        StartCoroutine(TargetReachedSwapGroundMaterial(win, 0.5f));
    }

    public void looseAnimation()
    {
        StartCoroutine(TargetReachedSwapGroundMaterial(loose, 0.5f));
    }

    IEnumerator TargetReachedSwapGroundMaterial(Transform ind, float time)
    {
        ind.position = new Vector3(transform.position.x, 1, transform.position.z);
        yield return new WaitForSeconds(time); // Wait for 2 sec
        ind.position = new Vector3(transform.position.x, -2, transform.position.z);
    }
    
    public float AvgDistToGoalPuck()
    {
        float dist = 0;

        foreach (Transform puck in pucks)
        {
            if (puck.gameObject.activeSelf)
            {
                dist += ClosestGoalPuck(puck)[0];
            }
        }

        return dist;
    }

    public Vector2 ClosestGoalPuck(Transform obj)
    {
        Transform goalPucks = GoalStadiumArea.GetComponent<StadiumArea>().pucks;
        
        float minDist = float.MaxValue;
        float minAngle = 0;
        foreach (Transform goalPuck in goalPucks)
        {
            if (goalPuck.gameObject.activeSelf)
            {
                float d = Vector3.Distance(obj.localPosition, goalPuck.localPosition);
                if (d < minDist)
                {
                    minDist = d;
                    minAngle = Vector3.Angle(obj.forward, goalPuck.localPosition - obj.localPosition);
                }
            }
        }

        return new Vector2(minDist, minAngle);
    }

    public float Fitness()
    {
        // scaled average distance to goal puck
        return 1 - (AvgDistToGoalPuck() / 60);
    }
}
