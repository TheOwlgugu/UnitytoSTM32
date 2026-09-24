using UnityEngine;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class DataProcessor : MonoBehaviour
{
    public class DeviceInfo
    {
        public int id;
        public int light;
        public float temperature;
        public float lastUpdateTime;
        public bool isOnline = true;
    }

    private static Dictionary<int, DeviceInfo> devices = new Dictionary<int, DeviceInfo>();
    private static ConcurrentQueue<string> jsonQueue = new ConcurrentQueue<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        var go = new GameObject("DataProcessor");
        go.AddComponent<DataProcessor>();
        DontDestroyOnLoad(go);
    }

    // WebSocket 收到消息时调这个
    public static void EnqueueRawMessage(string json)
    {
        jsonQueue.Enqueue(json);
    }

    void Update()
    {
        while (jsonQueue.TryDequeue(out string json))
        {
            ProcessJson(json);
        }
    }

    private void ProcessJson(string json)
    {
        try
        {
            // 简单 JSON 解析（避免依赖第三方库）
            // 格式：{"deviceId":7,"lightState":0,"temperature":22.56,"timestamp":...}
            int deviceId = ExtractInt(json, "deviceId");
            int lightState = ExtractInt(json, "lightState");
            float temperature = ExtractFloat(json, "temperature");

            if (!devices.ContainsKey(deviceId))
                devices[deviceId] = new DeviceInfo { id = deviceId };

            devices[deviceId].light = lightState;
            devices[deviceId].temperature = temperature;
            devices[deviceId].lastUpdateTime = Time.time;
            devices[deviceId].isOnline = true;

            Debug.Log($"设备 {deviceId:D2}：灯光 {(lightState == 0 ? "亮" : "暗")}，温度 {temperature:F2}℃");
        }
        catch (System.Exception e)
        {
            Debug.LogError("JSON解析失败: " + e.Message);
        }
    }

    private int ExtractInt(string json, string key)
    {
        string pattern = $"\"{key}\":";
        int idx = json.IndexOf(pattern);
        if (idx < 0) return 0;
        idx += pattern.Length;
        int end = json.IndexOfAny(new char[] { ',', '}' }, idx);
        return int.Parse(json.Substring(idx, end - idx));
    }

    private float ExtractFloat(string json, string key)
    {
        string pattern = $"\"{key}\":";
        int idx = json.IndexOf(pattern);
        if (idx < 0) return 0f;
        idx += pattern.Length;
        int end = json.IndexOfAny(new char[] { ',', '}' }, idx);
        return float.Parse(json.Substring(idx, end - idx));
    }

    // ---------- 外部查询接口 ----------
    public static float GetTemperature(int id)
        => devices.TryGetValue(id, out var info) ? info.temperature : 0f;

    public static int GetLight(int id)
        => devices.TryGetValue(id, out var info) ? info.light : -1;

    public static DeviceInfo GetDeviceInfo(int id)
        => devices.TryGetValue(id, out var info) ? info : null;

    public static IEnumerable<int> GetAllDeviceIds() => devices.Keys;

    public static bool IsOnline(int id)
        => devices.TryGetValue(id, out var info) && info.isOnline;
}