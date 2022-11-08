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
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.transform.localPosition = stadium.RandomPos(1);
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
        foreach(Transform puck in stadium.pucks)
        {
            Debug.Log(Vector3.Distance(Vector3.zero, puck.localPosition));
        }

        float zVelocity = 0;
        switch (actionBuffers.DiscreteActions[0])
        {
            case 1:
                zVelocity = -0.3f;
                break;
            case 2:
                zVelocity = -0.15f;
                break;
            case 3:
                zVelocity = 0.15f;
                break;
            case 4:
                zVelocity = 0.3f;
                break;
        }

        rb.velocity = new Vector3(0.3f, 0f, zVelocity);
    }
}
