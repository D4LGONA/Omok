using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
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
    private Thread sendThread;
    private volatile bool running = false;
    private bool cleanedUp = false;

    private readonly ConcurrentQueue<byte[]> recvQueue = new ConcurrentQueue<byte[]>();
    private readonly ConcurrentQueue<byte[]> sendQueue = new ConcurrentQueue<byte[]>();
    private readonly AutoResetEvent sendEvent = new AutoResetEvent(false);

    private readonly List<byte> recvBuffer = new List<byte>(4096);
    private readonly object recvBufferLock = new object();

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

            recvThread = new Thread(ReceiveLoop) { IsBackground = true };
            recvThread.Start();

            sendThread = new Thread(SendLoop) { IsBackground = true };
            sendThread.Start();

            Debug.Log("서버 연결 성공");
        }
        catch (Exception e)
        {
            Debug.LogError("서버 연결 실패: " + e.Message);
        }
    }

    private void ReceiveLoop()
    {
        byte[] tempBuffer = new byte[1024];

        while (running)
        {
            try
            {
                int len = stream.Read(tempBuffer, 0, tempBuffer.Length);

                if (len <= 0)
                {
                    running = false;
                    break;
                }

                lock (recvBufferLock)
                {
                    for (int i = 0; i < len; i++)
                        recvBuffer.Add(tempBuffer[i]);

                    ParseReceivedPackets();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("수신 오류: " + e.Message);
                running = false;
                break;
            }
        }
    }

    private void ParseReceivedPackets()
    {
        while (true)
        {
            if (recvBuffer.Count < 2)
                return;

            ushort packetSize = BitConverter.ToUInt16(recvBuffer.ToArray(), 0);

            if (packetSize < 3)
            {
                recvBuffer.Clear();
                return;
            }

            if (recvBuffer.Count < packetSize)
                return;

            byte[] packet = recvBuffer.GetRange(0, packetSize).ToArray();
            recvBuffer.RemoveRange(0, packetSize);
            recvQueue.Enqueue(packet);
        }
    }

    private void SendLoop()
    {
        while (running)
        {
            while (sendQueue.TryDequeue(out var data))
            {
                try
                {
                    if (stream != null && stream.CanWrite)
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    else
                    {
                        running = false;
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("전송 오류: " + e.Message);
                    running = false;
                    break;
                }
            }

            sendEvent.WaitOne(100);
        }
    }

    private void Update()
    {
        while (recvQueue.TryDequeue(out byte[] packet))
        {
            PacketHandler.Handle(packet);
        }
    }

    public void SendRaw(byte[] packet)
    {
        if (packet == null || packet.Length == 0)
            return;

        if (client == null || stream == null)
        {
            Debug.LogWarning("서버에 연결되어 있지 않음");
            return;
        }

        sendQueue.Enqueue(packet);
        sendEvent.Set();
    }

    private void CleanupNetworking()
    {
        if (cleanedUp) return;
        cleanedUp = true;

        running = false;

        try { sendEvent.Set(); } catch { }

        try
        {
            stream?.Close();
            client?.Close();
        }
        catch { }

        try { recvThread?.Join(500); } catch { }
        try { sendThread?.Join(500); } catch { }
    }

    private void OnDestroy()
    {
        CleanupNetworking();
        sendEvent.Dispose();
    }

    private void OnApplicationQuit()
    {
        CleanupNetworking();
    }
}