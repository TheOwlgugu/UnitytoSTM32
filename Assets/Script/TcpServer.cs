using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class TcpServer : MonoBehaviour
{
    private TcpListener server;
    private Thread serverThread;
    private TcpClient client;
    private string receivedData = "";
    private bool isConnected = false;

    void Start()
    {
        serverThread = new Thread(new ThreadStart(ListenForClients));
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    private void ListenForClients()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, 8888);
            server.Start();
            Debug.Log("Unity TCP服务器已启动，等待连接...");

            client = server.AcceptTcpClient();
            isConnected = true;
            Debug.Log("ESP8266已连接！");

            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            while (true)
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    // 把数据存到共享变量，主线程会去取
                    receivedData = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("服务器错误: " + e.Message);
        }
    }

    void Update()
    {
        // 在主线程中处理接收到的数据
        if (!string.IsNullOrEmpty(receivedData))
        {
            Debug.Log("收到数据: " + receivedData);
            // 在这里更新UI或控制3D对象
            // 例如：textMesh.text = receivedData;
            receivedData = ""; // 清空，避免重复处理
        }
    }

    void OnApplicationQuit()
    {
        server?.Stop();
        serverThread?.Abort();
        client?.Close();
    }
}