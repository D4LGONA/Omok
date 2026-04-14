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
        NetworkClient.Instance.Connect();
        NetworkClient.Instance.SendRaw(ClientPacketBuilder.MakeLogin(input, ""));

        // todo: 여기 아니고 ok 패킷 왔을때 넘어가야 함. 일단은 이렇게 두기
        playerId = input;

        ResetQueueState();
    }

    public void OnClickStartGame() // 게임시작 버튼을 누름
    {
        inQueue = !inQueue;

        if (true == inQueue)
        {
            // 큐 들어간다는 패킷 전송
            NetworkClient.Instance.SendRaw(ClientPacketBuilder.MakeQueue(true));

            StartText.SetText("Cancel");
            MatchingText.gameObject.SetActive(true);
        }
        else
        {
            NetworkClient.Instance.SendRaw(ClientPacketBuilder.MakeQueue(false));

            StartText.SetText("Start");
            MatchingText.gameObject.SetActive(false);
        }
    }

    public void OnMatched(string myColor)
    {
        SceneManager.LoadScene("GameScene");
    }
}