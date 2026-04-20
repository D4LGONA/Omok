using UnityEngine;

public class TitleSceneManager : MonoBehaviour
{
    public static TitleSceneManager Instance;

    [SerializeField] private TitleUI titleUI;

    private bool isLoggedIn = false;
    private bool isMatching = false;

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

        if (titleUI == null)
            titleUI = FindObjectOfType<TitleUI>();
    }

    public void RequestLogin(string id)
    {
        if (NetworkClient.Instance == null)
        {
            Debug.LogWarning("NetworkClient 없음");
            return;
        }

        NetworkClient.Instance.Connect();
        NetworkClient.Instance.SendLogin(id);
    }

    public void RequestQueueEnter()
    {
        if (!isLoggedIn)
        {
            Debug.LogWarning("로그인 후 매칭 가능");
            return;
        }

        if (NetworkClient.Instance == null)
        {
            Debug.LogWarning("NetworkClient 없음");
            return;
        }

        isMatching = true;
        NetworkClient.Instance.SendQueueEnter();

        if (titleUI != null)
            titleUI.SetQueueState(true);
    }

    public void RequestQueueCancel()
    {
        if (!isMatching)
            return;

        if (NetworkClient.Instance == null)
        {
            Debug.LogWarning("NetworkClient 없음");
            return;
        }

        isMatching = false;
        NetworkClient.Instance.SendQueueCancel();

        if (titleUI != null)
            titleUI.SetQueueState(false);
    }

    public void OnLoginResult(Protocol.LOGIN_RESULT result)
    {
        Debug.Log("로그인 결과 수신: " + result);

        if (result == Protocol.LOGIN_RESULT.LOGIN_SUCCESS)
        {
            isLoggedIn = true;

            if (titleUI != null)
                titleUI.SetLoginSuccess();
        }
        else
        {
            isLoggedIn = false;

            if (titleUI != null)
                titleUI.SetLoginFail();
        }
    }

    public void OnMatchResult(Protocol.MATCHING_STATE result)
    {
        Debug.Log("매칭 결과 수신: " + result);

        switch (result)
        {
            case Protocol.MATCHING_STATE.MATCH_FOUND:
                isMatching = false;

                if (titleUI != null)
                    titleUI.ShowMatchFound();

                if (NetworkClient.Instance != null)
                    NetworkClient.Instance.SendMatchingResponse();
                break;

            case Protocol.MATCHING_STATE.MATCH_CANCELED:
                isMatching = false;

                if (titleUI != null)
                    titleUI.ShowMatchCanceled();
                break;
        }
    }

    public void OnJoinRoom(string otherId, bool bMyTurn)
    {
        Debug.Log($"룸 입장: 상대={otherId}, 내 차례={bMyTurn}");

        if (titleUI != null)
            titleUI.OnMatched();
    }
}