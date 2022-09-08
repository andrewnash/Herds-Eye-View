using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class StadiumArea : MonoBehaviour
{
    GameObject pucks;
    GameObject agents;
    GameObject floors;

    // Start is called before the first frame update
    void Start()
    {
        pucks = GameObject.Find("Pucks");
        floors = GameObject.Find("Floor");
        agents = GameObject.Find("Agents");

        ResetStadium();
    }

    void ResetStadium()
    {
        foreach (Transform puck in pucks.transform)
        {
            ResetObject(puck);
        }
        foreach (Transform agent in agents.transform)
        {
            ResetObject(agent);
        }
    }

    void ResetObject(Transform obj)
    {
        obj.transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
        obj.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        obj.transform.eulerAngles = new Vector3(0, Random.Range(0, 360), 0);
        obj.transform.position = RandomPos();
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
            var cubeSize = cube.GetComponent<Renderer>().bounds.size.z/2 - 2;

            pos = Random.insideUnitCircle * archSize;
            pos.z = pos.y > transform.position.z ? pos.y + cubeSize : pos.y - cubeSize;
            pos.y = 0;
        }
        else
        {
            var bounds = cube.GetComponent<Renderer>().bounds;
            pos.x = Random.Range(bounds.min.x - bounds.max.x, bounds.max.x);
            pos.z = Random.Range(bounds.min.z, bounds.max.z);
        }

        pos.y = 0.1f;
        pos.x = pos.x - 1;
        pos.z = pos.z - 1;
        return pos;
    }

    // Update is called once per frame
    void Update()
    {
        var keyboard = Keyboard.current;

        if (keyboard.escapeKey.wasPressedThisFrame)
            ResetStadium();

        foreach (Transform puck in pucks.transform)
        {
            if (puck.transform.position.y < -1)
                ResetObject(puck);
        }
    }
}
