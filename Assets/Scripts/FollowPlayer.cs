using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform player;  // Drag your player here
    public float smoothSpeed = 3f;
    public Vector3 offset = new Vector3(0, 0, -10);

    void LateUpdate()
    {
        if (player == null) return;
        transform.position = player.position + offset;
    }
}
