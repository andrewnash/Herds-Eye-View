//Put this script on your blue cube.
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

public class PushAgentCollab : Agent
{

    private PushBlockSettings m_PushBlockSettings;
    private Rigidbody m_AgentRb;  //cached on initialization

    public bool LocalController;
    public bool GlobalController;

    void Awake()
    {
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
    }

    public override void Initialize()
    {
        // Cache the agent rb
        m_AgentRb = GetComponent<Rigidbody>();

        if (m_AgentRb == null)
        {
            m_AgentRb = GetComponentInChildren<Rigidbody>();
        }
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    public void MoveAgentTurn(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];

        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
            case 5:
                dirToGo = transform.right * -0.75f;
                break;
            case 6:
                dirToGo = transform.right * 0.75f;
                break;
        }
        m_AgentRb.transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        m_AgentRb.AddForce(dirToGo * m_PushBlockSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    
    public void MoveAgentGlobal(ActionSegment<int> act)
    {
        float goalAngle = float.NaN;
        var action = act[0];

        switch (action)
        {
            case 1:
                goalAngle = 0;
                break;
            case 2:
                goalAngle = 180;
                break;
            case 3:
                goalAngle = 90;
                break;
            case 4:
                goalAngle = 270;
                break;
            case 5:
                goalAngle = 45;
                break;
            case 6:
                goalAngle = 315;
                break;
            case 7:
                goalAngle = 225;
                break;
            case 8:
                goalAngle = 135;
                break;
        }

        if (float.IsNaN(goalAngle))
            return;

        m_AgentRb.transform.eulerAngles = new Vector3(0, goalAngle, 0);
        m_AgentRb.AddForce(m_AgentRb.transform.forward * m_PushBlockSettings.agentRunSpeed, ForceMode.VelocityChange);
        //m_AgentRb.transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        //m_AgentRb.AddForce(dirToGo * m_PushBlockSettings.agentRunSpeed,
        //    ForceMode.VelocityChange);
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Move the agent using the action.
        if (LocalController)
        {  
            MoveAgentTurn(actionBuffers.DiscreteActions);
        }
        else
        {
            MoveAgentGlobal(actionBuffers.DiscreteActions);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 4;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
    }
}
