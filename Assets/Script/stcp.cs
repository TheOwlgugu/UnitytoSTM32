using UnityEngine;
using TMPro;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;

public class LightControllerServer : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Light pointLight;          // 点光源
    [SerializeField] private TextMeshProUGUI statusText; // TMP文本

    [Header("服务器设置")]
    [SerializeField] private int port = 8888;

    // TCP 相关
    private TcpListener server;
    private TcpClient client;
    private NetworkStream stream;
    private byte[] receiveBuffer = new byte[1024];

    // 线程安全的队列，用于主线程处理接收到的数据
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    void Start()
    {
        // 启动 TCP 服务器
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Debug.Log($"TCP服务器启动，端口 {port}，等待连接...");
            server.BeginAcceptTcpClient(OnClientConnected, null);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"服务器启动失败: {e.Message}");
        }

        // 初始化UI显示（默认关灯）
        UpdateUI(false, 0f);
    }

    // 客户端连接回调
    private void OnClientConnected(System.IAsyncResult ar)
    {
        try
        {
            client = server.EndAcceptTcpClient(ar);
            stream = client.GetStream();
            Debug.Log("ESP8266 已连接！");
            stream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, OnDataReceived, null);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"接受连接失败: {e.Message}");
        }
    }

    // 数据接收回调（在子线程中执行）
    private void OnDataReceived(System.IAsyncResult ar)
    {
        try
        {
            int bytesRead = stream.EndRead(ar);
            if (bytesRead > 0)
            {
                string rawData = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
                // 将收到的数据加入队列，由主线程处理
                messageQueue.Enqueue(rawData);
                // 继续接收后续数据
                stream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, OnDataReceived, null);
            }
            else
            {
                Debug.Log("ESP8266 连接已关闭");
                stream.Close();
                client.Close();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"接收数据失败: {e.Message}");
            stream?.Close();
            client?.Close();
        }
    }

    void Update()
    {
        // 从队列中取出消息（主线程安全）
        while (messageQueue.TryDequeue(out string message))
        {
            ProcessMessage(message);
        }
    }

    // 解析并执行消息（主线程）
    private void ProcessMessage(string message)
    {
        // 格式示例: "sL1T25.50" 或 "sL0T23.40\n"
        // 去除可能的换行符
        message = message.Trim('\r', '\n', ' ');
        if (string.IsNullOrEmpty(message) || message[0] != 's')
        {
            Debug.LogWarning("无效消息（不以s开头）: " + message);
            return;
        }

        // 解析灯光指令
        int lIdx = message.IndexOf('L');
        if (lIdx == -1 || lIdx + 1 >= message.Length)
        {
            Debug.LogWarning("未找到L字段");
            return;
        }
        char lightStateChar = message[lIdx + 1];
        bool lightOn = (lightStateChar == '1');

        // 解析温度值
        int tIdx = message.IndexOf('T');
        if (tIdx == -1 || tIdx + 1 >= message.Length)
        {
            Debug.LogWarning("未找到T字段");
            return;
        }
        string tempStr = message.Substring(tIdx + 1);
        // 温度值可能跟随换行，但已被Trim处理，也可能带有额外字符，尝试解析
        float temperature = 0f;
        if (!float.TryParse(tempStr, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out temperature))
        {
            Debug.LogWarning("温度解析失败: " + tempStr);
        }

        // 执行控制
        pointLight.enabled = lightOn;
        UpdateUI(lightOn, temperature);

        Debug.Log($"执行: 灯光 {(lightOn ? "开" : "关")}, 温度 {temperature:F2}°C");
    }

    // 更新UI（主线程）
    private void UpdateUI(bool lightOn, float temperature)
    {
        if (statusText == null) return;

        string lightStatus = lightOn ? "On" : "Off";
        statusText.text = $"TMP: {temperature:F2}°C\nLight: {lightStatus}";
    }

    void OnApplicationQuit()
    {
        stream?.Close();
        client?.Close();
        server?.Stop();
        Debug.Log("服务器已关闭");
    }
}