using UnityEngine;
using NativeWebSocket;
using System;

public class WebSocketClient : MonoBehaviour
{
    [SerializeField] private string serverUrl = "ws://192.168.48.102:8080";  // ← 改成你电脑的IP
    private WebSocket websocket;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        var go = new GameObject("WebSocketClient");
        go.AddComponent<WebSocketClient>();
        DontDestroyOnLoad(go);
    }

    async void Start()
    {
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () => Debug.Log("WebSocket 已连接");
        websocket.OnError += (e) => Debug.LogError("WebSocket 错误: " + e);
        websocket.OnClose += (e) => Debug.Log("WebSocket 已断开");

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            DataProcessor.EnqueueRawMessage(message);
        };

        await websocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    async void OnApplicationQuit()
    {
        if (websocket != null)
            await websocket.Close();
    }
}