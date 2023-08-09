using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Give rewards to agents that push pucks closer to goal positions
public class Puck : MonoBehaviour
{
    private PlanarConstructionAgent m_MostRecentAgent = null;
    public StadiumArea m_Stadium;
    private StadiumController m_Controller;

    private float m_DistanceThreshold = 0;
    private float m_StartDistance = 0;
    private float m_ClosestDistance = 0;
    
    private bool m_HasWon = false;

    private void Start()
    {
        m_Controller = m_Stadium.GetComponent<StadiumController>();
    }

    public void Reset(float distanceThreshold)
    {
        m_DistanceThreshold = distanceThreshold;
        
        m_StartDistance = m_Stadium.ClosestGoalPuck(transform)[0];
        m_ClosestDistance = m_StartDistance;
        m_MostRecentAgent = null;
        m_HasWon = false;
    }

    private void CheckWin()
    {
        if (!m_HasWon && m_ClosestDistance < m_DistanceThreshold)
        {
            //m_MostRecentAgent.AddReward(5f);
            m_Controller.Scored(); //Group reward for score
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
        if (currentDistance < m_ClosestDistance)
        {
            // ** OLD Reward per agent pushing closer to goal - probably overkill **
            // scale distance moved by required distance to move to be within distance threshold
            // float reward = (m_ClosestDistance - currentDistance) / (m_StartDistance - m_DistanceThreshold);
            // print(reward*5);
            // Add 0-5 Scaled Reward
            // m_MostRecentAgent.AddReward(reward*5);

            m_ClosestDistance = currentDistance;
        }
    }


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Agent"))
        {
            m_MostRecentAgent = collision.gameObject.GetComponentInParent<PlanarConstructionAgent>();
        }
    }
}
