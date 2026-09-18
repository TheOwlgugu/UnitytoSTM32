using UnityEngine;
using System.Collections.Generic;

public class DataProcessor : MonoBehaviour
{
    public class DeviceInfo
    {
        public int id;
        public int light;          // 0 = 亮，1 = 暗
        public float temperature;  // 摄氏度
        public float lastUpdateTime;
    }

    private static Dictionary<int, DeviceInfo> devices = new Dictionary<int, DeviceInfo>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        var go = new GameObject("DataProcessor");
        go.AddComponent<DataProcessor>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        // 从队列取原始数据并解析
        while (TcpServerManager.ReceivedMessages.TryDequeue(out string msg))
        {
            ProcessMessage(msg);
        }
    }

    private void ProcessMessage(string msg)
    {
        // 协议：IIILTTTT（7位数字）
        // III：设备ID（2位），L：灯光（1位），TTTT：温度×100（4位）
        if (msg.Length != 7) return;
        if (!int.TryParse(msg.Substring(0, 2), out int id)) return;
        if (!int.TryParse(msg.Substring(2, 1), out int light)) return;
        if (!int.TryParse(msg.Substring(3, 4), out int tempRaw)) return;

        float temp = tempRaw / 100.0f;

        if (!devices.ContainsKey(id))
            devices[id] = new DeviceInfo { id = id };

        devices[id].light = light;
        devices[id].temperature = temp;
        devices[id].lastUpdateTime = Time.time;

        Debug.Log($"设备 {id:D2}：灯光 {(light == 0 ? "亮" : "暗")}，温度 {temp:F2}℃");
    }

    // ---------- 外部获取接口 ----------

    /// <summary>获取指定设备的温度（℃），无数据返回 0</summary>
    public static float GetTemperature(int id)
    {
        return devices.TryGetValue(id, out var info) ? info.temperature : 0f;
    }

    /// <summary>获取指定设备的灯光状态（0=亮，1=暗），无数据返回 -1</summary>
    public static int GetLight(int id)
    {
        return devices.TryGetValue(id, out var info) ? info.light : -1;
    }

    /// <summary>获取指定设备的完整信息，无数据返回 null</summary>
    public static DeviceInfo GetDeviceInfo(int id)
    {
        return devices.TryGetValue(id, out var info) ? info : null;
    }

    /// <summary>获取所有已连接设备的 ID 列表</summary>
    public static IEnumerable<int> GetAllDeviceIds()
    {
        return devices.Keys;
    }
}