using TMPro;
using UnityEngine;
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

        if (idInputField != null)
        {
            idInputField.interactable = true;
            idInputField.ActivateInputField();
        }
    }

    public void ResetLoginState()
    {
        if (LoginState != null) LoginState.SetActive(true);
        if (QueueState != null) QueueState.SetActive(false);
    }

    public void ResetQueueState()
    {
        if (StartText != null) StartText.SetText("Start");
        if (MatchingText != null) MatchingText.gameObject.SetActive(false);
        if (LoginState != null) LoginState.SetActive(false);
        if (QueueState != null) QueueState.SetActive(true);

        inQueue = false;
    }

    public void ConfirmId()
    {
        string input = idInputField != null ? idInputField.text.Trim() : "";

        if (string.IsNullOrEmpty(input))
        {
            Debug.Log("empty id");
            ResetLoginState();
            return;
        }

        playerId = input;

        if (TitleSceneManager.Instance != null)
        {
            TitleSceneManager.Instance.RequestLogin(input);
        }
    }

    public void OnClickStartGame()
    {
        if (TitleSceneManager.Instance == null)
            return;

        if (!inQueue)
        {
            TitleSceneManager.Instance.RequestQueueEnter();
        }
        else
        {
            TitleSceneManager.Instance.RequestQueueCancel();
        }
    }

    public void SetLoginSuccess()
    {
        Debug.Log("로그인 성공");
        ResetQueueState();
    }

    public void SetLoginFail()
    {
        Debug.Log("로그인 실패");
        ResetLoginState();

        if (idInputField != null)
        {
            idInputField.interactable = true;
            idInputField.ActivateInputField();
        }
    }

    public void SetQueueState(bool queueing)
    {
        inQueue = queueing;

        if (StartText != null)
            StartText.SetText(queueing ? "Cancel" : "Start");

        if (MatchingText != null)
            MatchingText.gameObject.SetActive(queueing);
    }

    public void ShowMatchFound()
    {
        Debug.Log("매칭 성공");
        SetQueueState(false);
    }

    public void ShowMatchCanceled()
    {
        Debug.Log("매칭 취소");
        SetQueueState(false);
    }

    public void OnMatched()
    {
        SceneManager.LoadScene("GameScene");
    }

    public string GetPlayerId()
    {
        return playerId;
    }
}