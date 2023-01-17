using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StadiumCollisionChecker : MonoBehaviour
{
    public float y;
    private int m_overlaps = 0;

    // Start is called before the first frame update
    void Start()
    {
        StadiumArea stadiumArea = GetComponentInParent<StadiumArea>();
    }

    void Update()
    {
        
    }

    public bool IsColliding()
    {
        return m_overlaps > 0;
    }

    public void ResetCollisions()
    {
        m_overlaps = 0;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Puck") || collision.gameObject.CompareTag("Wall"))
        {
            m_overlaps++;
        }
    }

    /*void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Puck") || collision.gameObject.CompareTag("Wall"))
        {
            m_overlaps--;
        }
    }*/
}
