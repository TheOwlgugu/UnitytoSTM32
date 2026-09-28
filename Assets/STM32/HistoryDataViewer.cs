using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

public class HistoryDataViewer : MonoBehaviour
{
    [Header("服务器设置")]
    [SerializeField] private string hostIP = "192.168.200.102";
    [SerializeField] private int httpPort = 3000;

    [System.Serializable]
    public class Reading
    {
        public int light_state;
        public float temperature;
        public string recorded_at;
    }

    [System.Serializable]
    private class ReadingArray
    {
        public Reading[] items;
    }

    // 单例，方便外部直接调用
    public static HistoryDataViewer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 查询指定设备最近 N 小时的历史数据
    /// </summary>
    /// <param name="deviceId">设备 ID</param>
    /// <param name="hours">最近多少小时</param>
    /// <param name="onSuccess">成功回调，返回数据列表</param>
    /// <param name="onError">失败回调，返回错误信息</param>
    public void GetHistory(int deviceId, int hours,
                           Action<List<Reading>> onSuccess,
                           Action<string> onError = null)
    {
        StartCoroutine(RequestHistory(deviceId, hours, onSuccess, onError));
    }

    private IEnumerator RequestHistory(int deviceId, int hours,
                                       Action<List<Reading>> onSuccess,
                                       Action<string> onError)
    {
        string url = $"http://{hostIP}:{httpPort}/api/data/{deviceId}?hours={hours}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error);
                yield break;
            }

            string json = req.downloadHandler.text;

            // JsonUtility 不支持数组作为根节点，包一层
            string wrapped = "{\"items\":" + json + "}";
            ReadingArray arr = JsonUtility.FromJson<ReadingArray>(wrapped);

            List<Reading> result = new List<Reading>();
            if (arr != null && arr.items != null)
                result.AddRange(arr.items);

            onSuccess?.Invoke(result);
        }
    }

    /// <summary>
    /// 查询某设备最近 N 小时的统计信息（平均/最高/最低温度）
    /// </summary>
    /// <param name="deviceId">设备 ID</param>
    /// <param name="hours">最近多少小时</param>
    /// <param name="onSuccess">成功回调，返回 (条数, 平均温度, 最低温度, 最高温度)</param>
    /// <param name="onError">失败回调</param>
    public void GetStats(int deviceId, int hours,
                         Action<int, float, float, float> onSuccess,
                         Action<string> onError = null)
    {
        StartCoroutine(RequestStats(deviceId, hours, onSuccess, onError));
    }

    [System.Serializable]
    private class StatsResult
    {
        public int count;
        public float avg_temp;
        public float min_temp;
        public float max_temp;
    }

    private IEnumerator RequestStats(int deviceId, int hours,
                                     Action<int, float, float, float> onSuccess,
                                     Action<string> onError)
    {
        string url = $"http://{hostIP}:{httpPort}/api/stats/{deviceId}?hours={hours}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error);
                yield break;
            }

            StatsResult s = JsonUtility.FromJson<StatsResult>(req.downloadHandler.text);
            if (s != null)
                onSuccess?.Invoke(s.count, s.avg_temp, s.min_temp, s.max_temp);
            else
                onError?.Invoke("解析失败");
        }
    }

    /// <summary>
    /// 一键输出指定设备的历史信息（可绑定到按钮）
    /// </summary>
    public void PrintHistoryOnClick()
    {
        int deviceId = 2;      // 可以改成从 Inspector 读
        int hours = 24;

        Debug.Log($"===== 设备 {deviceId:D2} 最近 {hours} 小时的历史数据 =====");

        GetHistory(deviceId, hours,
            onSuccess: (list) =>
            {
                if (list.Count == 0)
                {
                    Debug.Log("暂无数据");
                    return;
                }

                // 逐条打印
                foreach (var r in list)
                {
                    Debug.Log($"{r.recorded_at} | 温度 {r.temperature:F2}℃ | 灯光 {(r.light_state == 0 ? "亮" : "暗")}");
                }

                Debug.Log($"===== 共 {list.Count} 条数据 =====");
            },
            onError: (err) => Debug.LogError("查询失败: " + err)
        );
    }

    /// <summary>
    /// 一键输出统计信息（平均/最高/最低温度）
    /// </summary>
    public void PrintStatsOnClick()
    {
        int deviceId = 2;
        int hours = 24;

        GetStats(deviceId, hours,
            onSuccess: (count, avg, min, max) =>
            {
                Debug.Log($"===== 设备 {deviceId:D2} 最近 {hours} 小时统计 =====");
                Debug.Log($"数据条数: {count}");
                Debug.Log($"平均温度: {avg:F2}℃");
                Debug.Log($"最低温度: {min:F2}℃");
                Debug.Log($"最高温度: {max:F2}℃");
            },
            onError: (err) => Debug.LogError("查询失败: " + err)
        );
    }
}