using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public BoardRenderer boardRenderer;
    public TextMeshProUGUI turnText;
    public GameObject winPanel;
    public TextMeshProUGUI winText;

    public Board Board { get; private set; }
    public int CurrentPlayer { get; private set; } = 1; // 1=Èæ, 2=¹é
    public bool IsGameOver { get; private set; } = false;

    private void Awake()
    {
        Instance = this;
        Board = new Board();
    }

    private void Start()
    {
        UpdateTurnUI();
    }

    public void OnCellClicked(int x, int y)
    {
        if (IsGameOver) return;

        bool placed = Board.PlaceStone(x, y, CurrentPlayer);
        if (!placed) return;

        boardRenderer.DrawStone(x, y, CurrentPlayer);

        if (Board.CheckWin(x, y, CurrentPlayer))
        {
            EndGame(CurrentPlayer);
            return;
        }

        SwitchPlayer();
        UpdateTurnUI();
    }

    private void SwitchPlayer()
    {
        CurrentPlayer = (CurrentPlayer == 1) ? 2 : 1;
    }

    private void EndGame(int winner)
    {
        IsGameOver = true;
        string name = winner == 1 ? "Èæ" : "¹é";
        winText.text = $"{name} ½Â¸®!";
        winPanel.SetActive(true);
    }

    public void RestartGame()
    {
        Board.Reset();
        boardRenderer.ClearStones();
        CurrentPlayer = 1;
        IsGameOver = false;
        winPanel.SetActive(false);
        UpdateTurnUI();
    }

    private void UpdateTurnUI()
    {
        string name = CurrentPlayer == 1 ? "Èæ" : "¹é";
        turnText.text = $"{name}ÀÇ Â÷·Ê";
    }
}