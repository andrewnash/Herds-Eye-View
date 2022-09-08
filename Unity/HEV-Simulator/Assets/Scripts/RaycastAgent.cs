using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RaycastAgent : MonoBehaviour
{
    Rigidbody m_AgentRb;

    // Start is called before the first frame update
    void Start()
    {
        m_AgentRb = GetComponent<Rigidbody>();
    }


    // Update is called once per frame
    void Update()
    {
        var keyboard = Keyboard.current;

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
            
        if (keyboard.wKey.IsPressed(0))
            dirToGo = transform.forward * 1f;
        else if (keyboard.sKey.IsPressed(0))
            dirToGo = transform.forward * -1f;

        if (keyboard.dKey.IsPressed(0))
            rotateDir = transform.up * 1f;
        else if (keyboard.aKey.IsPressed(0))
            rotateDir = transform.up * -1f;


        transform.Rotate(rotateDir, Time.fixedDeltaTime * 20f);
        m_AgentRb.AddForce(dirToGo * 0.4f, ForceMode.VelocityChange);

        var rotation = transform.rotation;
        rotation.x = 0;
        rotation.z = 0;
        transform.rotation = rotation;

        var position = transform.position;
        position.y = 1;
        transform.position = position;
    }

    void EndEpisode()
    {
        transform.position.Set(0, 1, 0);
    }
}
