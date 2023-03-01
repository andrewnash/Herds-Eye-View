using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Diagnostics.Tracing;

public class PushAgentEscape : Agent
{
    public bool LocalADController;
    public bool GlobalWASDController;

    public GameObject MyKey; //my key gameobject. will be enabled when key picked up.
    public bool IHaveAKey; //have i picked up a key
    private PushBlockSettings m_PushBlockSettings;
    private Rigidbody m_AgentRb;
    private DungeonEscapeEnvController m_GameController;

    public override void Initialize()
    {
        m_GameController = GetComponentInParent<DungeonEscapeEnvController>();
        m_AgentRb = GetComponent<Rigidbody>();
        if (m_AgentRb == null) { m_AgentRb = GetComponentInChildren<Rigidbody>(); }

        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
        MyKey.SetActive(false);
        IHaveAKey = false;
    }

    public void OverrideCollisionEnter(Collision col)
    {
        OnCollisionEnter(col);
    }

    public void OverrideTriggerEnter(Collider col)
    {
        OnTriggerEnter(col);
    }

    public override void OnEpisodeBegin()
    {
        MyKey.SetActive(false);
        IHaveAKey = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(IHaveAKey);
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
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        m_AgentRb.AddForce(dirToGo * m_PushBlockSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    public void MoveAgentGlobal(ActionBuffers actionBuffers)
    {
        float goalAngle = ControlsToGoalAngle(actionBuffers);

        if (float.IsNaN(goalAngle))
            return;

        m_AgentRb.transform.eulerAngles = new Vector3(0, goalAngle, 0);
        m_AgentRb.AddForce(m_AgentRb.transform.forward * m_PushBlockSettings.agentRunSpeed, ForceMode.VelocityChange);
    }

    private float ControlsToGoalAngle(ActionBuffers actionBuffers)
    {
        // 8 possible goal angles
        float goalAngle = float.NaN;
        var vert = actionBuffers.DiscreteActions[0];
        var horz = actionBuffers.DiscreteActions[1];

        if (vert == 0 && horz == 1)
        {
            goalAngle = 0;
        }
        else if (vert == 0 && horz == 0)
        {
            goalAngle = 315;
        }
        else if (vert == 0 && horz == 2)
        {
            goalAngle = 45;
        }
        else if (vert == 2 && horz == 1)
        {
            goalAngle = 180;
        }
        else if (vert == 2 && horz == 0)
        {
            goalAngle = 225;
        }
        else if (vert == 2 && horz == 2)
        {
            goalAngle = 135;
        }
        else if (vert == 1 && horz == 0)
        {
            goalAngle = 270;
        }
        else if (vert == 1 && horz == 2)
        {
            goalAngle = 90;
        }

        return goalAngle;
    }


    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Move the agent using the action.
        if (LocalADController)
        {
            MoveAgentTurn(actionBuffers.DiscreteActions);
        }
        else
        {
            MoveAgentGlobal(actionBuffers);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.transform.CompareTag("lock"))
        {
            if (IHaveAKey)
            {
                MyKey.SetActive(false);
                IHaveAKey = false;
                m_GameController.UnlockDoor();
            }
        }
        if (col.transform.CompareTag("dragon"))
        {
            m_GameController.KilledByBaddie(this, col);
            MyKey.SetActive(false);
            IHaveAKey = false;
        }
        if (col.transform.CompareTag("portal"))
        {
            m_GameController.TouchedHazard(this);
        }
    }

    void OnTriggerEnter(Collider col)
    {
        //if we find a key and it's parent is the main platform we can pick it up
        if (col.transform.CompareTag("key") && col.transform.parent == transform.parent && gameObject.activeInHierarchy)
        {
            print("Picked up key");
            MyKey.SetActive(true);
            IHaveAKey = true;
            col.gameObject.SetActive(false);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // move agent
        if (LocalADController)
        {
            HeuristicAD(actionsOut);
        }
        else
        {
            HeuristicWASD(actionsOut);
        }
    }

    void HeuristicAD(in ActionBuffers actionsOut)
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
    
    // Set global goal angle with WASD keys
    void HeuristicWASD(ActionBuffers actionsOut)
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
}
