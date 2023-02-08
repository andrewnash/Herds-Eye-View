using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StadiumCollisionChecker : MonoBehaviour
{
    private int m_PuckOverlaps = 0;
    private int m_AgentOverlaps = 0;

    // Start is called before the first frame update
    void Start() { }

    void Update() { }

    public bool IsColliding()
    {
        return m_PuckOverlaps > 0 || m_AgentOverlaps > 0;
    }

    public void ResetCollisions()
    {
        m_PuckOverlaps = 0;
        m_AgentOverlaps = 0;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Puck"))
        {
            m_PuckOverlaps++;
        }

        if (collision.gameObject.CompareTag("Agent"))
        {
            m_AgentOverlaps++;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Puck") && m_PuckOverlaps > 0)
        {
            m_PuckOverlaps--;
        }
        
        if (collision.gameObject.CompareTag("Agent") && m_AgentOverlaps > 0)
        {
            m_AgentOverlaps--;
        }
    }
}
