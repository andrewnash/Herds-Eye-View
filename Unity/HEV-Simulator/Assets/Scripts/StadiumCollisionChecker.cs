using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StadiumCollisionChecker : MonoBehaviour
{
    private int m_PuckOverlaps = 0;

    // Start is called before the first frame update
    void Start() { }

    void Update() { }

    public bool IsColliding()
    {
        return m_PuckOverlaps > 0;
    }

    public void ResetCollisions()
    {
        m_PuckOverlaps = 0;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Puck"))
        {
            m_PuckOverlaps++;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Puck"))
        {
            m_PuckOverlaps--;
        }
    }
}
