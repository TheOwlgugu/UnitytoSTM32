using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LCtr : MonoBehaviour
{
    [SerializeField] private int deviceId = 2;
    [SerializeField] private Light pointLight;
    [SerializeField] private TextMeshProUGUI statusText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (statusText == null) return;

        
        int light = DataProcessor.GetLight(deviceId);
        pointLight.enabled = (light == 0);
        statusText.text = $"Light: {(light == 0 ? "On" : "Off")}";
    }
}
