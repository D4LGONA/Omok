using UnityEngine;
using System.Collections.Generic;

public class BoardRenderer : MonoBehaviour
{
    [Header("Board Settings")]
    public float cellSize = 1f;        // 격자 간격
    public int lineCount = 15;
    public Color lineColor = Color.black;
    public Color boardColor = new Color(0.85f, 0.65f, 0.3f); // 나무색

    [Header("Stone Prefabs")]
    public GameObject blackStonePrefab;
    public GameObject whiteStonePrefab;

    private List<GameObject> spawnedStones = new();

    private void Start()
    {
        DrawBoard();
    }

    private void DrawBoard()
    {
        float boardLength = (lineCount - 1) * cellSize; // 14칸 = 실제 격자 크기
        float padding = cellSize;                        // 테두리 여백

        // 배경 크기 = 격자 + 양쪽 여백
        float bgSize = boardLength + padding * 2;

        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.transform.SetParent(transform);
        bg.transform.localPosition = new Vector3(
            boardLength / 2f,   // 격자 중앙
            boardLength / 2f,
            0.1f);
        bg.transform.localScale = new Vector3(bgSize, bgSize, 1f);
        bg.GetComponent<Renderer>().material.color = boardColor;
        Destroy(bg.GetComponent<Collider>());

        // 격자선 (변경 없음)
        for (int i = 0; i < lineCount; i++)
        {
            CreateLine(
                new Vector3(i * cellSize, 0, 0),
                new Vector3(i * cellSize, boardLength, 0));
            CreateLine(
                new Vector3(0, i * cellSize, 0),
                new Vector3(boardLength, i * cellSize, 0));
        }

        DrawStarPoints();
    }

    private void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject go = new GameObject("Line");
        go.transform.SetParent(transform);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = lineColor;
        lr.startWidth = lr.endWidth = 0.05f;
        lr.SetPositions(new Vector3[] { start, end });
        lr.sortingOrder = 1;
    }

    private void DrawStarPoints()
    {
        int[] starCoords = { 3, 7, 11 }; // 15x15 기준 화점
        foreach (int x in starCoords)
            foreach (int y in starCoords)
                DrawDot(x, y);
    }

    private void DrawDot(int x, int y)
    {
        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.transform.SetParent(transform);
        dot.transform.localPosition = new Vector3(x * cellSize, y * cellSize, -0.01f);
        dot.transform.localScale = Vector3.one * 0.15f;
        dot.GetComponent<Renderer>().material.color = Color.black;
        Destroy(dot.GetComponent<Collider>());
    }

    public void DrawStone(int x, int y, int player)
    {
        var prefab = player == 1 ? blackStonePrefab : whiteStonePrefab;
        var pos = new Vector3(x * cellSize, y * cellSize, -0.05f);
        var stone = Instantiate(prefab, pos, Quaternion.identity, transform);
        spawnedStones.Add(stone);
    }

    public void ClearStones()
    {
        foreach (var s in spawnedStones) Destroy(s);
        spawnedStones.Clear();
    }

    /// <summary>월드 좌표 → 격자 인덱스 변환</summary>
    public bool WorldToGrid(Vector3 worldPos, out int gx, out int gy)
    {
        gx = Mathf.RoundToInt(worldPos.x / cellSize);
        gy = Mathf.RoundToInt(worldPos.y / cellSize);
        return gx >= 0 && gx < lineCount && gy >= 0 && gy < lineCount;
    }
}