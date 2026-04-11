using UnityEngine;
using UnityEngine.SceneManagement;

public static class PacketHandler
{
    public static void Handle(string msg)
    {
        Debug.Log("Recv: " + msg);

        string[] parts = msg.Split('|');
        string type = parts[0];

        switch (type)
        {
            default:
                break;
        }
    }
}