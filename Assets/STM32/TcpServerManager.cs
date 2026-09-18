using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

public class TcpServerManager : MonoBehaviour
{
    [Header("服务器设置")]
    [SerializeField] private int port = 8888;

    public static ConcurrentQueue<string> ReceivedMessages = new ConcurrentQueue<string>();

    private TcpListener server;
    private Thread acceptThread;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        var go = new GameObject("TcpServerManager");
        go.AddComponent<TcpServerManager>();
        DontDestroyOnLoad(go);
    }

    void Start()
    {
        acceptThread = new Thread(new ThreadStart(AcceptLoop));
        acceptThread.IsBackground = true;
        acceptThread.Start();
    }

    void AcceptLoop()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Debug.Log($"TCP服务器启动，端口 {port}");
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Debug.Log($"客户端连接: {client.Client.RemoteEndPoint}");
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.IsBackground = true;
                clientThread.Start();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"服务器错误: {e.Message}");
        }
    }

    void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        StringBuilder sb = new StringBuilder();
        try
        {
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
                string text = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                sb.Append(text);
                // 按换行分割
                string[] lines = sb.ToString().Split('\n');
                // 最后一部分可能不完整，保留
                for (int i = 0; i < lines.Length - 1; i++)
                {
                    string line = lines[i].Trim();
                    if (!string.IsNullOrEmpty(line))
                        ReceivedMessages.Enqueue(line);
                }
                sb.Clear();
                sb.Append(lines[lines.Length - 1]);
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"客户端断开: {e.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    void OnApplicationQuit()
    {
        server?.Stop();
        acceptThread?.Abort();
    }
}