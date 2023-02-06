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
    [Header("Max Environment Steps")] public int MaxEnvironmentSteps;
    [SerializeField] public int m_DistanceThreshold;

    private EnvironmentParameters resetParams;

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
            if (agent.gameObject.activeSelf)
            {
                m_AgentGroup.RegisterAgent(agent.GetComponent<PlanarConstructionAgent>());
            }
        }

        m_Stadium.pucksRange = new Vector2Int(
            (int)resetParams.GetWithDefault("max_pucks", 3),
            (int)resetParams.GetWithDefault("min_pucks", 1));
        m_DistanceThreshold = (int)resetParams.GetWithDefault("distance_threshold", 10);
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
        } while (IsValidStart());

        foreach (Transform puck in m_Stadium.pucks)
        {
            if (puck.gameObject.activeSelf)
            {
                puck.GetComponent<Puck>().Reset(m_DistanceThreshold);
            }
        }
    }

    bool IsValidStart()
    {
        foreach (Transform puck in m_Stadium.pucks)
        {
            if (puck.gameObject.activeSelf && m_Stadium.ClosestGoalPuck(puck.transform)[0] < m_DistanceThreshold)
            {
                return true;
            }
        }

        return false;
    }

    bool HasWon()
    {
        foreach (Transform puck in m_Stadium.pucks)
        {
            if (puck.gameObject.activeSelf && !puck.GetComponent<Puck>().HasWon())
            {
                return false;
            }
        }

        return true;
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
        if (HasWon())
        {
            m_AgentGroup.AddGroupReward(10f);
            m_Stadium.winAnimation();
            m_AgentGroup.EndGroupEpisode();
            ResetScene();

            return;
        }

        // time penalty
        m_AgentGroup.AddGroupReward(-1f / MaxEnvironmentSteps);
    }
}
