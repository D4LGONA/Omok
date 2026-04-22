using TMPro;
using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI myIdText;
    [SerializeField] private TextMeshProUGUI otherIdText;
    [SerializeField] private TextMeshProUGUI myColorText;
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Board")]
    [SerializeField] private BoardRenderer boardRenderer;

    private bool isMyTurn = false;
    private bool gameEnded = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializeFromSession();
    }

    private void InitializeFromSession()
    {
        if (GameSession.Instance == null)
        {
            Debug.LogWarning("GameSession.Instance 없음");
            return;
        }

        if (myIdText != null)
            myIdText.SetText($"Me : {GameSession.Instance.PlayerId}");

        if (otherIdText != null)
            otherIdText.SetText($"Other : {GameSession.Instance.OtherPlayerId}");

        if (myColorText != null)
            myColorText.SetText($"Color : {GameSession.Instance.MyColor}");

        isMyTurn = GameSession.Instance.IsMyTurn;
        gameEnded = false;

        RefreshTurnUI();

        if (resultText != null)
            resultText.SetText("");

        Debug.Log($"GameScene Init | Me={GameSession.Instance.PlayerId}, Other={GameSession.Instance.OtherPlayerId}, Color={GameSession.Instance.MyColor}, MyTurn={GameSession.Instance.IsMyTurn}");
    }

    private void RefreshTurnUI()
    {
        if (turnText == null)
            return;

        if (gameEnded)
            turnText.SetText("게임 종료");
        else
            turnText.SetText(isMyTurn ? "내 차례" : "상대 차례");
    }

    public bool CanPlaceStone()
    {
        return !gameEnded && isMyTurn;
    }

    public void TryPlaceStone(ushort x, ushort y)
    {
        if (!CanPlaceStone())
        {
            Debug.Log("지금은 돌을 둘 수 없음");
            return;
        }

        if (boardRenderer != null && boardRenderer.HasStone(x, y))
        {
            Debug.Log($"이미 돌이 놓인 자리: ({x}, {y})");
            return;
        }

        if (NetworkClient.Instance == null)
        {
            Debug.LogWarning("NetworkClient.Instance 없음");
            return;
        }

        Debug.Log($"착수 요청 전송: ({x}, {y})");
        NetworkClient.Instance.SendPlayTurn(x, y);
    }

    public void OnStonePlaced(ushort x, ushort y, bool nextMyTurn)
    {
        Debug.Log($"돌 반영: ({x}, {y}), 다음 내 차례 여부={nextMyTurn}");

        int player = GetPlacedStonePlayer(nextMyTurn);

        if (player == 0)
        {
            Debug.LogWarning("돌 색 계산 실패");
            return;
        }

        if (boardRenderer != null)
        {
            boardRenderer.DrawStone(x, y, player);
        }
        else
        {
            Debug.LogWarning("BoardRenderer 참조 없음");
        }

        isMyTurn = nextMyTurn;

        if (GameSession.Instance != null)
            GameSession.Instance.SetMyTurn(nextMyTurn);

        RefreshTurnUI();
    }

    public void OnGameResult(Protocol.GAME_RESULT result)
    {
        gameEnded = true;
        isMyTurn = false;

        if (GameSession.Instance != null)
            GameSession.Instance.SetMyTurn(false);

        RefreshTurnUI();

        string text = result.ToString();

        switch (result)
        {
            case Protocol.GAME_RESULT.WIN:
                text = "승리";
                break;

            case Protocol.GAME_RESULT.LOSE:
                text = "패배";
                break;

            case Protocol.GAME_RESULT.WIN_TIMEOUT:
                text = "시간 초과 승리";
                break;

            case Protocol.GAME_RESULT.LOSE_TIMEOUT:
                text = "시간 초과 패배";
                break;

            case Protocol.GAME_RESULT.WIN_DISCONNECT:
                text = "상대 연결 종료로 승리";
                break;

            case Protocol.GAME_RESULT.LOSE_DISCONNECT:
                text = "연결 종료로 패배";
                break;
        }

        if (resultText != null)
            resultText.SetText(text);

        Debug.Log("게임 종료: " + text);
    }

    private int GetPlacedStonePlayer(bool nextMyTurn)
    {
        if (GameSession.Instance == null)
            return 0;

        bool iPlacedThisStone = !nextMyTurn;
        bool amBlack = GameSession.Instance.MyColor == "Black";

        if (iPlacedThisStone)
            return amBlack ? 1 : 2;
        else
            return amBlack ? 2 : 1;
    }
}