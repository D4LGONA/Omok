using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject LoginState;
    public GameObject QueueState;
    public TMP_InputField idInputField;
    public TextMeshProUGUI StartText;
    public TextMeshProUGUI MatchingText;

    private string playerId = "";
    private bool inQueue = false;

    private void Start()
    {
        ResetLoginState();

        idInputField.interactable = true;
        idInputField.ActivateInputField();
    }

    private void ResetLoginState()
    {
        LoginState.gameObject.SetActive(true);
        QueueState.gameObject.SetActive(false);
    }
    private void ResetQueueState()
    {
        StartText.SetText("Start");
        MatchingText.gameObject.SetActive(false);
        LoginState.gameObject.SetActive(false);
        QueueState.gameObject.SetActive(true);
    }

    private void Update()
    {
    }

    public void ConfirmId()
    {
        string input = idInputField.text.Trim();
        if (string.IsNullOrEmpty(input)) // 아이디 안적음
        {
            Debug.Log("empty id");
            ResetLoginState();
            return;
        }

        // 아이디 로그인 완료
        // todo: 원래 여기서 서버랑 연동해야 함
        playerId = input;

        ResetQueueState();
    }

    public void OnClickStartGame() // 게임시작 버튼을 누름
    {
        //GameSession.Instance.SetPlayerId(playerId);
        //NetworkClient.Instance.Connect();
        inQueue = !inQueue;

        if (true == inQueue)
        {
            StartText.SetText("Cancel");
            MatchingText.gameObject.SetActive(true);
        }
        else
        {
            StartText.SetText("Start");
            MatchingText.gameObject.SetActive(false);
        }
    }

    public void OnMatched(string myColor)
    {
        SceneManager.LoadScene("GameScene");
    }

}