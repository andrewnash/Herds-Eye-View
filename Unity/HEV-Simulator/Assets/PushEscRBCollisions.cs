using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class PushEscRBCollisions : MonoBehaviour
{
    public PushAgentEscape agent;

    void OnCollisionEnter(Collision col)
    {
        agent.OverrideCollisionEnter(col);
    }

    void OnTriggerEnter(Collider col)
    {
        agent.OverrideTriggerEnter(col);
    }
}
