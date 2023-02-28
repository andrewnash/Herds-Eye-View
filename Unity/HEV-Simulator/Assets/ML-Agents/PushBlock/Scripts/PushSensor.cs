using UnityEngine;

public class PushSensor : MonoBehaviour
{
    private int m_BlockOverlaps = 0;

    private void Reset()
    {
        m_BlockOverlaps = 0;
    }

    public bool IsPushing() { return m_BlockOverlaps > 0; }

    void OnCollisionEnter(Collision col)
    {
        // Touched goal.
        if (col.gameObject.CompareTag("Block"))
        {
            m_BlockOverlaps++;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Block") && m_BlockOverlaps > 0)
        {
            m_BlockOverlaps--;
        }
    }
}
