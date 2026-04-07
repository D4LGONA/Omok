using System;

public class Board
{
    public const int SIZE = 15;

    // 0 = 빈칸, 1 = 흑, 2 = 백
    private int[,] grid = new int[SIZE, SIZE];

    public int GetCell(int x, int y) => grid[x, y];

    public bool IsEmpty(int x, int y) => grid[x, y] == 0;

    public bool IsInBounds(int x, int y) =>
        x >= 0 && x < SIZE && y >= 0 && y < SIZE;

    /// <summary>돌을 놓고 성공 여부 반환</summary>
    public bool PlaceStone(int x, int y, int player)
    {
        if (!IsInBounds(x, y) || !IsEmpty(x, y)) return false;
        grid[x, y] = player;
        return true;
    }

    //해당 좌표에 돌을 놓았을 때 player가 이기는지 확인
    public bool CheckWin(int x, int y, int player)
    {
        // 4방향: 가로, 세로, 대각선2종
        int[][] directions = {
            new[] {1, 0},
            new[] {0, 1},
            new[] {1, 1},
            new[] {1, -1}
        };

        foreach (var dir in directions)
        {
            int count = 1;
            count += CountDirection(x, y, dir[0], dir[1], player);
            count += CountDirection(x, y, -dir[0], -dir[1], player);
            if (count >= 5) return true;
        }
        return false;
    }

    private int CountDirection(int x, int y, int dx, int dy, int player)
    {
        int count = 0;
        int nx = x + dx, ny = y + dy;
        while (IsInBounds(nx, ny) && grid[nx, ny] == player)
        {
            count++;
            nx += dx;
            ny += dy;
        }
        return count;
    }

    public void Reset()
    {
        Array.Clear(grid, 0, grid.Length);
    }
}