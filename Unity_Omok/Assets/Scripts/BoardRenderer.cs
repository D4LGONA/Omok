using UnityEngine;
using System.Collections.Generic;

public class BoardRenderer : MonoBehaviour
{
    [Header("Board Settings")]
    public int lineCount = 15;

    [Header("Stone Prefabs")]
    public GameObject blackStonePrefab;
    public GameObject whiteStonePrefab;

    [Header("Ghost Stone")]
    public GameObject GhostPrefab;

    private readonly List<GameObject> spawnedStones = new();
    private int[,] board;

    private GameObject ghost;

    private const float MinX = -5.6f;
    private const float MinY = -5.6f;
    private const float MaxX = 5.6f;
    private const float MaxY = 5.6f;

    private float cellSizeX;
    private float cellSizeY;

    private void Awake()
    {
        board = new int[lineCount, lineCount];

        cellSizeX = (MaxX - MinX) / (lineCount - 1); // 0.8
        cellSizeY = (MaxY - MinY) / (lineCount - 1); // 0.8

        if (GhostPrefab != null)
        {
            ghost = Instantiate(GhostPrefab, Vector3.zero, Quaternion.identity, transform);
            ghost.SetActive(false);
        }
    }

    public void DrawStone(int x, int y, int player)
    {
        if (!IsInRange(x, y)) return;
        if (board[x, y] != 0) return;

        GameObject prefab = (player == 1) ? blackStonePrefab : whiteStonePrefab;

        Vector3 pos = GridToWorld(x, y);
        pos.z = -0.05f;

        GameObject stone = Instantiate(prefab, pos, Quaternion.identity, transform);
        spawnedStones.Add(stone);

        board[x, y] = player;
    }

    public bool HasStone(int x, int y)
    {
        if (!IsInRange(x, y)) return false;
        return board[x, y] != 0;
    }

    public void ShowGhost(int x, int y)
    {
        HideGhost();

        if (!IsInRange(x, y)) return;
        if (HasStone(x, y)) return;

        Vector3 pos = GridToWorld(x, y);
        pos.z = -0.04f;

        if (ghost != null)
        {
            ghost.transform.position = pos;
            ghost.SetActive(true);
        }
    }

    public void HideGhost()
    {
        if (ghost != null) ghost.SetActive(false);
    }

    public Vector3 GridToWorld(int x, int y)
    {
        return new Vector3(
            MinX + x * cellSizeX,
            MinY + y * cellSizeY,
            0f
        );
    }

    public bool WorldToGrid(Vector3 worldPos, out int gx, out int gy)
    {
        float fx = (worldPos.x - MinX) / cellSizeX;
        float fy = (worldPos.y - MinY) / cellSizeY;

        gx = Mathf.RoundToInt(fx);
        gy = Mathf.RoundToInt(fy);

        if (!IsInRange(gx, gy))
            return false;

        Vector3 snapped = GridToWorld(gx, gy);

        float dx = Mathf.Abs(worldPos.x - snapped.x);
        float dy = Mathf.Abs(worldPos.y - snapped.y);

        if (dx > cellSizeX * 0.45f || dy > cellSizeY * 0.45f)
            return false;

        return true;
    }

    public void ClearStones()
    {
        foreach (GameObject s in spawnedStones)
            Destroy(s);

        spawnedStones.Clear();
        board = new int[lineCount, lineCount];
        HideGhost();
    }

    private bool IsInRange(int x, int y)
    {
        return x >= 0 && x < lineCount && y >= 0 && y < lineCount;
    }
}