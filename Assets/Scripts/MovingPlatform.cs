using UnityEngine;
public class MovingPlatform : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform[] waypoints;
    public float speed = 2f;
    public bool pingPong = true;
    private int index = 0;
    private int direction = 1;
    void Update()
    {
        if (waypoints.Length == 0) return;
        transform.position = Vector3.MoveTowards(
            transform.position, waypoints[index].position, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, waypoints[index].position) < 0.05f)
        {
            if (pingPong)
            {
                if (index == waypoints.Length - 1 || index == 0) direction *= -1;
                index += direction;
            }
            else index = (index + 1) % waypoints.Length;
        }
    }
    void OnCollisionEnter(Collision col) => col.transform.SetParent(transform);
    void OnCollisionExit(Collision col)  => col.transform.SetParent(null);
}