using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using MBaske.Sensors.Grid;

public class PlanarConstructionAgent : Agent
{
    Rigidbody rb;
    StadiumArea stadium;

    int m_puckOverlaps = 0;

    float MOVEMENT_SPEED = 0.8f;
    float TURN_SPEED = 0.8f;

    private EnvironmentParameters resetParams;

    private int distanceThreshold = 0; 
    
    private float lastFitness = 0;
    private float changeInFitness = 0;

    public bool isTraining = true;
    
    public bool turnController = false;
    public bool NESWController = true;

    void Start()
    {
        rb = GetComponentInChildren<Rigidbody>();
        stadium = GetComponentInParent<StadiumArea>();
        resetParams = Academy.Instance.EnvironmentParameters;
    }

    public override void OnEpisodeBegin()
    {
        stadium.pucksRange = new Vector2Int(
            (int)resetParams.GetWithDefault("max_pucks", 1),
            (int)resetParams.GetWithDefault("min_pucks", 2));
        distanceThreshold = (int)resetParams.GetWithDefault("distance_threshold", 10);

        do
        { 
            stadium.ResetStadium();
        } while (isCompeted());
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // sensor.AddObservation(rb.transform.localPosition.x);
        // sensor.AddObservation(rb.transform.localPosition.z);
        
        sensor.AddObservation(stadium.AvgDistToGoalPuck());
        sensor.AddObservation(rb.transform.eulerAngles.y / 180.0f - 1);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // move agent
        if (turnController)
        {
            MoveAgentTurnSpeed(actionBuffers);
        }
        else
        {
            MoveAgentGoalAngle(actionBuffers);
        }
        
        UpdateChangeInFitness();

        // if fitness has increased, positive reward
        if (changeInFitness > 0.0001f)
        {
            AddReward(1f / MaxStep);
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


    void MoveAgentTurnSpeed(ActionBuffers actionBuffers)
    {
        float rotate = 0;
        switch (actionBuffers.DiscreteActions[0])
        {
            case 1:
                rotate = -TURN_SPEED;
                break;
            case 2:
                rotate = 0;
                break;
            case 3:
                rotate = TURN_SPEED;
                break;
        }

        rb.AddForce(rb.transform.forward * MOVEMENT_SPEED, ForceMode.VelocityChange);
        rb.transform.Rotate(rb.transform.up * rotate, Time.fixedDeltaTime * 100);
    }

    void MoveAgentGoalAngle(ActionBuffers actionBuffers)
    {
        float currentAngle = rb.transform.eulerAngles.y;

        // 8 possible goal angles
        int goalAngle = 0;
        switch (actionBuffers.DiscreteActions[0])
        {
            case 1:
                goalAngle = 45;
                break;
            case 2:
                goalAngle = 90;
                break;
            case 3:
                goalAngle = 135;
                break;
            case 4:
                goalAngle = 180;
                break;
            case 5:
                goalAngle = 225;
                break;
            case 6:
                goalAngle = 270;
                break;
            case 7:
                goalAngle = 315;
                break;

        }

        float angleDifference = (goalAngle - currentAngle % 360 + 180) % 360 - 180;

        if (angleDifference > 5 || angleDifference < -180)
        {
            rb.transform.Rotate(transform.up * TURN_SPEED, Time.fixedDeltaTime * 100);
        }
        else if (angleDifference < -5)
        {
            rb.transform.Rotate(transform.up * -TURN_SPEED, Time.fixedDeltaTime * 100);
        }

        if (actionBuffers.DiscreteActions[1] == 1)
            rb.AddForce(rb.transform.forward * MOVEMENT_SPEED, ForceMode.VelocityChange);
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
        if (turnController)
        {
            HeuristicTurn(actionsOut);
        }
        else
        {
            HeuristicGoalAngle(actionsOut);
        }
    }

    // Set global goal angle with WEDSXZAQ keys
    private void HeuristicGoalAngle(ActionBuffers actionsOut)
    {
        var DiscreteActionsOut = actionsOut.DiscreteActions;

        // default go up
        DiscreteActionsOut[0] = 0;
        
        if (Input.GetKey(KeyCode.E))
            DiscreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.D))
            DiscreteActionsOut[0] = 2;
        else if (Input.GetKey(KeyCode.C))
            DiscreteActionsOut[0] = 3;
        else if (Input.GetKey(KeyCode.X))
            DiscreteActionsOut[0] = 4;
        else if (Input.GetKey(KeyCode.Z))
            DiscreteActionsOut[0] = 5;
        else if (Input.GetKey(KeyCode.A))
            DiscreteActionsOut[0] = 6;
        else if (Input.GetKey(KeyCode.Q))
            DiscreteActionsOut[0] = 7;

        DiscreteActionsOut[1] = 1;
        if (Input.GetKey(KeyCode.S))
            DiscreteActionsOut[1] = 0;
    }

    // A&D Keyboard turn
    private void HeuristicTurn(ActionBuffers actionsOut)
    {
        var DiscreteActionsOut = actionsOut.DiscreteActions;

        if (Input.GetKey(KeyCode.A))
        {
            DiscreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            DiscreteActionsOut[0] = 3;
        }
        else
        {
            DiscreteActionsOut[0] = 2;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Puck"))
        {
            m_puckOverlaps++;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Puck"))
        {
            m_puckOverlaps--;
        }
    }
}
