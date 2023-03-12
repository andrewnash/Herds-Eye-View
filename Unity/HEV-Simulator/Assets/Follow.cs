using UnityEngine;

public class Follow : MonoBehaviour
{
    public Rigidbody rbToFollow;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = rbToFollow.position;
        this.transform.rotation = rbToFollow.rotation;
    }
}
