using UnityEngine;

public class CameraController2D : MonoBehaviour
{
    [Header("References")]
    public Camera cam;

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minOrthoSize = 3f;
    public float maxOrthoSize = 7f;

    [Header("Drag")]
    public float dragSensitivity = 1f;

    [Header("Bounds")]
    public float minX = -5.6f;
    public float maxX = 5.6f;
    public float minY = -5.6f;
    public float maxY = 5.6f;

    private Vector3 lastMouseWorld;
    private bool isDragging = false;

    private void Start()
    {
        if (cam == null)
            cam = Camera.main;

        ClampCameraPosition();
    }

    private void Update()
    {
        HandleZoom();
        HandleDrag();
    }

    private void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f)
            return;

        cam.orthographicSize -= scroll * zoomSpeed * Time.deltaTime * 10f;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minOrthoSize, maxOrthoSize);

        ClampCameraPosition();
    }

    private void HandleDrag()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isDragging = true;
            lastMouseWorld = GetMouseWorldOnCameraPlane();
        }

        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }

        if (!isDragging)
            return;

        Vector3 currentMouseWorld = GetMouseWorldOnCameraPlane();
        Vector3 delta = lastMouseWorld - currentMouseWorld;

        transform.position += delta * dragSensitivity;
        ClampCameraPosition();

        lastMouseWorld = GetMouseWorldOnCameraPlane();
    }

    private Vector3 GetMouseWorldOnCameraPlane()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = -cam.transform.position.z;
        Vector3 world = cam.ScreenToWorldPoint(mouse);
        world.z = transform.position.z;
        return world;
    }

    private void ClampCameraPosition()
    {
        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * cam.aspect;

        Vector3 pos = transform.position;

        float clampedX;
        float clampedY;

        if ((maxX - minX) <= horzExtent * 2f)
        {
            clampedX = (minX + maxX) * 0.5f;
        }
        else
        {
            clampedX = Mathf.Clamp(pos.x, minX + horzExtent, maxX - horzExtent);
        }

        if ((maxY - minY) <= vertExtent * 2f)
        {
            clampedY = (minY + maxY) * 0.5f;
        }
        else
        {
            clampedY = Mathf.Clamp(pos.y, minY + vertExtent, maxY - vertExtent);
        }

        transform.position = new Vector3(clampedX, clampedY, pos.z);
    }
}