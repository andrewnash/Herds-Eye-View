using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using MBaske.Sensors.Grid;

public class PlanarConstructionAgent : Agent
{
    Rigidbody rb;
    StadiumArea stadium;

    private int m_puckOverlaps = 0;
    private int m_wallOverlaps = 0;

    float MOVEMENT_SPEED = 1.0f;
    float TURN_SPEED = 1.25f;

    private EnvironmentParameters resetParams;

    private int distanceThreshold = 0;

    private float lastFitness = 0;
    private float changeInFitness = 0;

    public bool isTraining;
    public bool isSaving;

    public bool LocalADController;
    public bool GlobalTurnController;
    public bool GlobalVectorController;

    void Start()
    {
        // rb located in child for HEV but same component for BEV
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = GetComponentInChildren<Rigidbody>();
        }
        
        stadium = GetComponentInParent<StadiumArea>();
        resetParams = Academy.Instance.EnvironmentParameters;
    }

    public override void OnEpisodeBegin()
    {
        if (isSaving) { return; }

        stadium.pucksRange = new Vector2Int(
            (int)resetParams.GetWithDefault("max_pucks", 2),
            (int)resetParams.GetWithDefault("min_pucks", 1));
        distanceThreshold = (int)resetParams.GetWithDefault("distance_threshold", 10);
        stadium.obstructionMax = (int)resetParams.GetWithDefault("obstacle_max", 0);

        stadium.ResetObstructions();
        do
        {
            stadium.ResetStadium();
        } while (isCompeted());
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(rb.velocity.x);
        sensor.AddObservation(rb.velocity.z);

        Vector2 closestGoalPuck = stadium.ClosestGoalPuck(rb.transform);
        sensor.AddObservation(closestGoalPuck[0] / 32.0f);  // distance diff normalized
        sensor.AddObservation(closestGoalPuck[1] / 180.0f); // angle diff normalized

        //sensor.AddObservation(stadium.AvgDistToGoalPuck());
        //sensor.AddObservation(rb.transform.eulerAngles.y / 180.0f);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // move agent
        if (LocalADController)
        {
            MoveAgentADController(actionBuffers);
        }
        else if (GlobalTurnController)
        {
            MoveAgentGlobalTurn(actionBuffers);
        }
        else if (GlobalVectorController)
        {
            MoveAgentGlobalVector(actionBuffers);
        }

        UpdateChangeInFitness();

        // if fitness has increased, positive reward
        if (changeInFitness > 0.0001f)
        {
            AddReward(1f / MaxStep);
        }
        else if (changeInFitness < 0f)
        {
            AddReward(-2f / MaxStep);
        }
        else // time penalty
        {
            AddReward(-1f / MaxStep);
        }

        // check if completed
        if (isCompeted())
        {
            AddReward(10f);
            stadium.winAnimation();
            EndEpisode();
        }
    }

    void MoveAgentADController(ActionBuffers actionBuffers)
    {
        float rotate = 0;
        switch (actionBuffers.DiscreteActions[0])
        {
            case 0:
                rotate = -TURN_SPEED;
                break;
            case 1:
                rotate = 0;
                break;
            case 2:
                rotate = TURN_SPEED;
                break;
        }
        rb.transform.Rotate(rb.transform.up * rotate, Time.fixedDeltaTime * 100);

        float speed = MOVEMENT_SPEED;
        if (m_wallOverlaps > 0)
        {
            speed /= 2;
        }
        rb.AddForce(rb.transform.forward * speed, ForceMode.VelocityChange);
    }

    void MoveAgentGlobalTurn(ActionBuffers actionBuffers)
    {
        float goalAngle = ControlsToGoalAngle(actionBuffers);

        if (float.IsNaN(goalAngle))
            return;

        float currentAngle = rb.transform.eulerAngles.y;
        float angleDifference = (goalAngle - currentAngle % 360 + 180) % 360 - 180;

        if (angleDifference > 5 || angleDifference < -180)
        {
            rb.transform.Rotate(rb.transform.up * TURN_SPEED*2, Time.fixedDeltaTime * 100);
        }
        else if (angleDifference < -5)
        {
            rb.transform.Rotate(rb.transform.up * -TURN_SPEED*2, Time.fixedDeltaTime * 100);
        }

        // if angle difference is small && any key pressed, move forward 
        if (30 > angleDifference && angleDifference > -30)
            rb.AddForce(rb.transform.forward * MOVEMENT_SPEED, ForceMode.VelocityChange);
    }

    void MoveAgentGlobalVector(ActionBuffers actionBuffers)
    {
        float goalAngle = ControlsToGoalAngle(actionBuffers);
        
        if (float.IsNaN(goalAngle))
            return;

        rb.transform.eulerAngles = new Vector3(0, goalAngle, 0);
        rb.AddForce(rb.transform.forward * MOVEMENT_SPEED, ForceMode.VelocityChange);
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

    bool isCompeted()
    {
        if (stadium.AvgDistToGoalPuck() < distanceThreshold * (stadium.currentMaxPucks + 1))
        {
            return true;
        }

        return false;
    }

    private void UpdateChangeInFitness()
    {
        float fitness = stadium.Fitness();
        changeInFitness = fitness - lastFitness;
        lastFitness = fitness;
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

    // Set global goal angle with WASD keys
    private void HeuristicWASD(ActionBuffers actionsOut)
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

    // A&D Keyboard turn
    private void HeuristicAD(ActionBuffers actionsOut)
    {
        var DiscreteActionsOut = actionsOut.DiscreteActions;

        if (Input.GetKey(KeyCode.A))
        {
            DiscreteActionsOut[0] = 0;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            DiscreteActionsOut[0] = 2;
        }
        else
        {
            DiscreteActionsOut[0] = 1;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Puck"))
        {
            m_puckOverlaps++;
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            m_wallOverlaps++;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Puck"))
        {
            m_puckOverlaps--;
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            m_wallOverlaps--;
        }
    }
}
