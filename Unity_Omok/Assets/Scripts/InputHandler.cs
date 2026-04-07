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
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;

        if (boardRenderer.WorldToGrid(worldPos, out int gx, out int gy))
        {
            GameManager.Instance.OnCellClicked(gx, gy);
        }
    }
}