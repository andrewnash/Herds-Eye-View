using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StadiumCollisionChecker : MonoBehaviour
{
    public float y;
    public StadiumArea stadiumArea;

    // Start is called before the first frame update
    void Start()
    {
        StadiumArea stadiumArea = GetComponentInParent<StadiumArea>();
    }

    void Update()
    {
        
    }

    void OnTriggerEnter(Collider c)
    {
        if (c.tag != "Floor" && transform.position.y > y-5)
        {
            stadiumArea.ChildReset(transform, y);
        }
    }
}
