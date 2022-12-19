using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using static UnityEditor.PlayerSettings;

public class PlanarConstructionAgent : Agent
{
    Rigidbody rb;
    StadiumArea stadium;

    int m_puckOverlaps = 0;
    private int pucks = 0;

    float MOVEMENT_SPEED = 0.8f;

    private EnvironmentParameters resetParams;

    private float lastFitness = 0;
    private float changeInFitness = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        stadium = GetComponentInParent<StadiumArea>();
        resetParams = Academy.Instance.EnvironmentParameters;
    }

    public override void OnEpisodeBegin()
    {
        stadium.pucksRange = new Vector2Int(
            (int)resetParams.GetWithDefault("max_pucks", 1),
            (int)resetParams.GetWithDefault("min_pucks", 2));

        do
        {
            stadium.ResetStadium();
        } while (isCompeted());
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        /*sensor.AddObservation(rb.velocity.x);
        sensor.AddObservation(rb.velocity.z);*/

        sensor.AddObservation(rb.transform.localPosition.x);
        sensor.AddObservation(rb.transform.localPosition.z);

        sensor.AddObservation(stadium.AvgDistToGoalPuck());
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        moveAgent(actionBuffers);
        UpdateChangeInFitness();

        // if fitness has increased, positive reward
        if (changeInFitness > 0.0001f)
        {
            AddReward(0.01f);
        }
        else // time penalty
        {
            AddReward(-0.01f);
        }

        // check if completed
        if (isCompeted())
        {
            AddReward(100f);
            stadium.winAnimation();
            EndEpisode();
        }
    }


    void moveAgent(ActionBuffers actionBuffers)
    {
        float rotate = 0;
        switch (actionBuffers.DiscreteActions[0])
        {
            case 1:
                rotate = -0.5f;
                break;
            case 2:
                rotate = 0;
                break;
            case 3:
                rotate = 0.5f;
                break;
        }

        rb.AddForce(transform.forward * MOVEMENT_SPEED, ForceMode.VelocityChange);
        transform.Rotate(transform.up * rotate, Time.fixedDeltaTime * 100);
    }

    bool isCompeted()
    {
        if (stadium.AvgDistToGoalPuck() < 5 * (stadium.currentMaxPucks + 1))
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
