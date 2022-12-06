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

    float MOVEMENT_SPEED = 0.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        stadium = GetComponentInParent<StadiumArea>();
    }

    public override void OnEpisodeBegin()
    {
        /*        rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.transform.localPosition = stadium.RandomPos(1);*/
        stadium.ResetStadium();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        /*sensor.AddObservation(rb.velocity.x);
        sensor.AddObservation(rb.velocity.z);*/

        sensor.AddObservation(rb.transform.localPosition.x);
        sensor.AddObservation(rb.transform.localPosition.z);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // time penalty
        AddReward(-0.01f);

        // if agent currently colliding with puck
        if (m_puckOverlaps > 0)
        {
            AddReward(0.005f);

            // and if agent is moving puck towards the center of the stadium
            float centerAngle = Vector3.Angle(rb.transform.forward, stadium.transform.position - rb.transform.position);
            if (-10 < centerAngle && centerAngle < 10)
            {
                AddReward(0.005f);
            }
        }
        else
        {
            // if agent is moving towards any puck
            foreach (Transform puck in stadium.pucks)
            {
                float puckAngle = Vector3.Angle(rb.transform.forward, puck.position - rb.transform.position);
                if (-10 < puckAngle && puckAngle < 10)
                {
                    AddReward(0.005f);
                    break;
                }
            }
        }

        checkCompleted();
        moveAgent(actionBuffers);
    }

    void checkCompleted()
    {
        float totalDist = 0;
        int countDist = 0;
        foreach (Transform puck in stadium.pucks)
        {
            if (puck.transform.position.y > 0)
            {
                totalDist += Vector3.Distance(stadium.transform.position, puck.position);
                countDist++;
            }
        }
        if (totalDist / countDist < 10)
        {
            AddReward(100f);
            EndEpisode();
        }
    }

    void moveAgent(ActionBuffers actionBuffers)
    {
        float rotate = 0;
        switch (actionBuffers.DiscreteActions[0])
        {
            case 1:
                rotate = -0.3f;
                break;
            case 2:
                rotate = -0.15f;
                break;
            case 3:
                rotate = 0;
                break;
            case 4:
                rotate = 0.15f;
                break;
            case 5:
                rotate = 0.3f;
                break;
        }

        rb.AddForce(transform.forward * MOVEMENT_SPEED, ForceMode.VelocityChange);
        transform.Rotate(transform.up * rotate, Time.fixedDeltaTime * 100);
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
            DiscreteActionsOut[0] = 5;
        }
        else
        {
            DiscreteActionsOut[0] = 3;
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
