using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PlanarConstructionAgent : Agent
{
    Rigidbody rb;
    StadiumArea stadium;

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
        sensor.AddObservation(rb.velocity.x);
        sensor.AddObservation(rb.velocity.z);

        sensor.AddObservation(rb.transform.localPosition.x);
        sensor.AddObservation(rb.transform.localPosition.z);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-0.1f);

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
        if (totalDist/countDist < 5)
        {
            AddReward(10f);
            EndEpisode();
        }


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

        float moveSpeed = 0.5f;
        rb.AddForce(transform.forward * moveSpeed, ForceMode.VelocityChange);
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
}
