using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class StadiumController : MonoBehaviour
{
    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    /// <returns></returns>
    [Header("Max Environment Steps")] public int MaxEnvironmentSteps = 5000;

    private EnvironmentParameters resetParams;
    private int distanceThreshold = 0;

    private StadiumArea m_Stadium;
    private SimpleMultiAgentGroup m_AgentGroup;

    private int m_ResetTimer;

    // Start is called before the first frame update
    void Start()
    {
        m_Stadium = GetComponent<StadiumArea>();
        resetParams = Academy.Instance.EnvironmentParameters;

        // Initialize TeamManager
        m_AgentGroup = new SimpleMultiAgentGroup();
        foreach (Transform agent in m_Stadium.agents)
        {
            m_AgentGroup.RegisterAgent(agent.GetComponent<Agent>());
        }

        m_Stadium.pucksRange = new Vector2Int(
            (int)resetParams.GetWithDefault("max_pucks", 3),
            (int)resetParams.GetWithDefault("min_pucks", 1));
        distanceThreshold = (int)resetParams.GetWithDefault("distance_threshold", 10);
        m_Stadium.obstructionMax = (int)resetParams.GetWithDefault("obstacle_max", 0);

        ResetScene();
    }

    void ResetScene()
    {
        //m_Stadium.ResetObstructions();
        m_Stadium.ResetColors();
        m_ResetTimer = 0;

        do
        {
            m_Stadium.ResetStadium();
        } while (isCompeted());
    }
    
    bool isCompeted()
    {
        if (m_Stadium.AvgDistToGoalPuck() < distanceThreshold * (m_Stadium.currentMaxPucks + 1))
        {
            return true;
        }

        return false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            ResetScene();
        }
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_AgentGroup.GroupEpisodeInterrupted();
            m_Stadium.looseAnimation();
            ResetScene();
        }

        // check if completed
        if (isCompeted())
        {
            m_AgentGroup.AddGroupReward(10f);
            m_Stadium.winAnimation();
            m_AgentGroup.EndGroupEpisode();
            ResetScene();

            //changeInFitness = 0f;
            return;
        }

        // time penalty
        m_AgentGroup.AddGroupReward(-1f / MaxEnvironmentSteps);
    }
}
