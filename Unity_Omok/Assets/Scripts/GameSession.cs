using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance;

    public string PlayerId { get; private set; }
    public string MyColor { get; private set; }
    public bool IsMatched { get; private set; }

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

    public void SetMyColor(string color)
    {
        MyColor = color;
    }

    public void SetMatched(bool matched)
    {
        IsMatched = matched;
    }
}