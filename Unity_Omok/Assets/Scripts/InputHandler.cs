using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public BoardRenderer boardRenderer;
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        UpdateGhostPreview();

        if (!Input.GetMouseButtonDown(0))
            return;

        Vector3 mouse = Input.mousePosition;
        mouse.z = -mainCam.transform.position.z;

        Vector3 worldPos = mainCam.ScreenToWorldPoint(mouse);
        worldPos.z = 0f;

        if (boardRenderer.WorldToGrid(worldPos, out int gx, out int gy))
        {
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.TryPlaceStone((ushort)gx, (ushort)gy);
            }
        }
    }

    private void UpdateGhostPreview()
    {
        if (GameSceneManager.Instance == null || !GameSceneManager.Instance.CanPlaceStone())
        {
            boardRenderer.HideGhost();
            return;
        }

        Vector3 mouse = Input.mousePosition;
        mouse.z = -mainCam.transform.position.z;

        Vector3 worldPos = mainCam.ScreenToWorldPoint(mouse);
        worldPos.z = 0f;

        if (boardRenderer.WorldToGrid(worldPos, out int gx, out int gy))
        {
            boardRenderer.ShowGhost(gx, gy);
        }
        else
        {
            boardRenderer.HideGhost();
        }
    }
}