using UnityEngine;
public class CameraZoom : MonoBehaviour
{
    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minFOV = 20f;
    public float maxFOV = 80f;
    private Camera cam;
    void Awake() => cam = GetComponent<Camera>();
    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f) return;
        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll * zoomSpeed, minFOV, maxFOV);
    }
}