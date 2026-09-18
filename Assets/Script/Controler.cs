using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Controler : MonoBehaviour
{
    [SerializeField] private int deviceId = 2;
    [SerializeField] private TextMeshProUGUI statusText;

    void Update()
    {
        if (statusText == null) return;

        float temp = DataProcessor.GetTemperature(deviceId);
        int light = DataProcessor.GetLight(deviceId);

        statusText.text = $"ID {deviceId:D2}\n" +
                          $"TMP: {temp:F2}\n";
    }
}
