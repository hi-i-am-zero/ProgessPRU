using UnityEngine;

// Camera bam theo nhan vat
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(2f, 1.5f, -10f);
    public float smoothTime = 0.15f;
    public float minY = 3f;

    Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime);
    }

    Vector3 Desired()
    {
        Vector3 d = target.position + offset;
        d.y = Mathf.Max(d.y, minY);
        d.z = offset.z;
        return d;
    }

    // Dat camera ve dung vi tri ngay lap tuc (dung khi respawn)
    public void Snap()
    {
        if (target == null) return;
        transform.position = Desired();
        velocity = Vector3.zero;
    }
}
