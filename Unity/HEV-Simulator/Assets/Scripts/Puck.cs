using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Give rewards to agents that push pucks closer to goal positions
public class Puck : MonoBehaviour
{
    private PlanarConstructionAgent m_MostRecentAgent = null;
    public StadiumArea m_Stadium;

    private float m_DistanceThreshold = 0;
    private float m_StartDistance = 0;
    private float m_LastDistance = 0;
    
    private bool m_HasWon = false;
    
    public void Reset(float distanceThreshold)
    {
        m_DistanceThreshold = distanceThreshold;
        
        m_StartDistance = m_Stadium.ClosestGoalPuck(transform)[0];
        m_LastDistance = m_StartDistance;
        m_MostRecentAgent = null;
        m_HasWon = false;
    }

    private void CheckWin()
    {
        if (!m_HasWon && m_LastDistance < m_DistanceThreshold)
        {
            m_MostRecentAgent.AddReward(1f);
            m_HasWon = true;
        }
    }

    public bool HasWon()
    {
        return m_HasWon;
    }
    
    void Update()
    {
        if (m_MostRecentAgent == null || HasWon()) { return; }
        
        CheckWin();

        float currentDistance = m_Stadium.ClosestGoalPuck(transform)[0];
        if (currentDistance < m_LastDistance)
        {
            // scale distance moved by required distance to move to be within distance threshold
            float reward = (m_LastDistance - currentDistance) / (m_StartDistance - m_DistanceThreshold);

            // Add 0-1 Scaled Reward
            m_MostRecentAgent.AddReward(reward / (1 + reward));
        }
        m_LastDistance = currentDistance;
    }


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Agent"))
        {
            m_MostRecentAgent = collision.gameObject.GetComponentInParent<PlanarConstructionAgent>();
        }
    }
}
