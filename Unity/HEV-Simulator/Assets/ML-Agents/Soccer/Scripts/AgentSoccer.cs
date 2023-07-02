using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public enum Team
{
    Blue = 0,
    Purple = 1
}

public class AgentSoccer : Agent
{
    // Note that that the detectable tags are different for the blue and purple teams. The order is
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player

    // For HEV the order is:
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * itself
    // * own teammate
    // * opposing player 1 
    // * opposing player 2

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

    [HideInInspector]
    public Team team;
    float m_KickPower;
    // The coefficient for the reward for colliding with a ball. Set using curriculum.
    float m_BallTouch;
    public Position position;

    const float k_Power = 2000f;
    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;


    //[HideInInspector]
    public Rigidbody agentRb;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;

    EnvironmentParameters m_ResetParams;

    public bool GlobalController;
    public bool LocalController;

    public override void Initialize()
    {
        SoccerEnvController envController = GetComponentInParent<SoccerEnvController>();
        if (envController != null)
        {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;
            initialPos = new Vector3(transform.position.x - 5f, .5f, transform.position.z);
            rotSign = 1f;
        }
        else
        {
            team = Team.Purple;
            initialPos = new Vector3(transform.position.x + 5f, .5f, transform.position.z);
            rotSign = -1f;
        }
        if (position == Position.Goalie)
        {
            m_LateralSpeed = 1.0f;
            m_ForwardSpeed = 1.0f;
        }
        else if (position == Position.Striker)
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.3f;
        }
        else
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.0f;
        }
        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        if (agentRb == null) { agentRb = GetComponentInChildren<Rigidbody>(); }
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }

    public void MoveAgentLocal(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_KickPower = 0f;

        var forwardAxis = act[0];
        var rightAxis = act[1];
        var rotateAxis = act[2];

        switch (forwardAxis)
        {
            case 1:
                dirToGo = agentRb.transform.forward * m_ForwardSpeed;
                m_KickPower = 1f;
                break;
            case 2:
                dirToGo = agentRb.transform.forward * -m_ForwardSpeed;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo = agentRb.transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo = agentRb.transform.right * -m_LateralSpeed;
                break;
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir = agentRb.transform.up * -1f;
                break;
            case 2:
                rotateDir = agentRb.transform.up * 1f;
                break;
        }

        agentRb.transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    public void MoveAgentGlobal(ActionSegment<int> act)
    {
        float goalAngle = ControlsToGoalAngle(act);

        if (float.IsNaN(goalAngle))
            return;

        agentRb.transform.eulerAngles = new Vector3(0, goalAngle, 0);
        agentRb.AddForce(agentRb.transform.forward * m_ForwardSpeed, ForceMode.VelocityChange);
    }


    private float ControlsToGoalAngle(ActionSegment<int> act)
    {
        // 8 possible goal angles
        float goalAngle = float.NaN;

        var horz = act[0];
        var vert = act[1];

        if (vert == 0 && horz == 1)
        {
            goalAngle = 0;
        }
        else if (vert == 0 && horz == 0)
        {
            goalAngle = 45;
        }
        else if (vert == 0 && horz == 2)
        {
            goalAngle = 315;
        }
        else if (vert == 2 && horz == 1)
        {
            goalAngle = 180;
        }
        else if (vert == 2 && horz == 0)
        {
            goalAngle = 135;
        }
        else if (vert == 2 && horz == 2)
        {
            goalAngle = 225;
        }
        else if (vert == 1 && horz == 0)
        {
            goalAngle = 90;
        }
        else if (vert == 1 && horz == 2)
        {
            goalAngle = 270;
        }

        if (team == Team.Purple)
        {
            goalAngle = (goalAngle + 180) % 360;
        }

        return goalAngle;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {

        if (position == Position.Goalie)
        {
            // Existential bonus for Goalies.
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            // Existential penalty for Strikers
            AddReward(-m_Existential);
        }

        if (LocalController)
        {
            MoveAgentLocal(actionBuffers.DiscreteActions);
        }
        else
        {
            MoveAgentGlobal(actionBuffers.DiscreteActions);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (LocalController)
        {
            HeuristicLocal(actionsOut);
        }
        else
        {
            HeuristicGlobal(actionsOut);
        }
    }
    void HeuristicLocal(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[2] = 2;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[1] = 2;
        }
    }

    // Set global goal angle with WASD keys
    void HeuristicGlobal(ActionBuffers actionsOut)
    {
        var DiscreteActionsOut = actionsOut.DiscreteActions;

        // default off
        DiscreteActionsOut[0] = 1;
        DiscreteActionsOut[1] = 1;

        if (Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
            DiscreteActionsOut[0] = 0;
        else if (!Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.S))
            DiscreteActionsOut[0] = 2;

        if (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
            DiscreteActionsOut[1] = 0;
        else if (!Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.D))
            DiscreteActionsOut[1] = 2;
    }

    /// <summary>
    /// Used to provide a "kick" to the ball.
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        var force = k_Power * m_KickPower;
        if (position == Position.Goalie)
        {
            force = k_Power;
        }
        if (c.gameObject.CompareTag("ball"))
        {
            AddReward(.2f * m_BallTouch);
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
    }

}
