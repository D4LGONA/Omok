using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class NetworkClient : MonoBehaviour
{
    public static NetworkClient Instance;

    [Header("Server")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 6789;

    private TcpClient client;
    private NetworkStream stream;
    private Thread recvThread;
    private bool running = false;

    private readonly ConcurrentQueue<string> recvQueue = new ConcurrentQueue<string>();

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

    private void Start()
    {
        Connect();
    }

    public void Connect()
    {
        if (client != null && client.Connected)
            return;

        try
        {
            client = new TcpClient();
            client.Connect(serverIP, serverPort);
            stream = client.GetStream();

            running = true;
            recvThread = new Thread(ReceiveLoop);
            recvThread.IsBackground = true;
            recvThread.Start();

            Debug.Log("서버 연결 성공");
        }
        catch (Exception e)
        {
            Debug.LogError("서버 연결 실패: " + e.Message);
        }
    }

    private void ReceiveLoop()
    {
        byte[] buffer = new byte[1024];

        while (running)
        {
            try
            {
                int len = stream.Read(buffer, 0, buffer.Length);

                if (len <= 0)
                {
                    running = false;
                    break;
                }

                string msg = Encoding.UTF8.GetString(buffer, 0, len);
                recvQueue.Enqueue(msg);
            }
            catch (Exception e)
            {
                Debug.LogError("수신 오류: " + e.Message);
                running = false;
                break;
            }
        }
    }

    private void Update()
    {
        while (recvQueue.TryDequeue(out string msg))
        {
            PacketHandler.Handle(msg);
        }
    }

    public void Send(string msg)
    {
        if (client == null || !client.Connected || stream == null)
        {
            Debug.LogWarning("서버에 연결되어 있지 않음");
            return;
        }

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            stream.Write(data, 0, data.Length);
            Debug.Log("Send: " + msg);
        }
        catch (Exception e)
        {
            Debug.LogError("전송 오류: " + e.Message);
        }
    }

    private void OnDestroy()
    {
        running = false;

        try
        {
            stream?.Close();
            client?.Close();
        }
        catch { }
    }

    private void OnApplicationQuit()
    {
        running = false;

        try
        {
            stream?.Close();
            client?.Close();
        }
        catch { }
    }
}