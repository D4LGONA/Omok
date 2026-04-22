using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance;

    public string PlayerId { get; private set; }
    public string OtherPlayerId { get; private set; }

    public string MyColor { get; private set; }
    public string OtherColor { get; private set; }

    public bool IsMatched { get; private set; }
    public bool IsMyTurn { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetPlayerId(string id)
    {
        PlayerId = id;
    }

    public void SetOtherPlayerId(string id)
    {
        OtherPlayerId = id;
    }

    public void SetMyColor(string color)
    {
        MyColor = color;
    }

    public void SetOtherColor(string color)
    {
        OtherColor = color;
    }

    public void SetMatched(bool matched)
    {
        IsMatched = matched;
    }

    public void SetMyTurn(bool isMyTurn)
    {
        IsMyTurn = isMyTurn;
    }

    public void ResetMatchData()
    {
        OtherPlayerId = "";
        MyColor = "";
        OtherColor = "";
        IsMatched = false;
        IsMyTurn = false;
    }
}