
using System;
using TMPro;
using UnityEngine;
using TMPro;

public class FractalUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text powerChangeText;
    [SerializeField]
    private TMP_Text powerText;

    private RayMarchingFractal fractalController;

    private void Start()
    {
        fractalController = FindObjectOfType<RayMarchingFractal>();
    }

    public void OnPowerChange(float power)
    {
        power = Mathf.Round(power * 1000) / 1000;
        powerText.text = power.ToString();
    }
    public void OnPowerSpeedChange(float speed)
    {
        speed = Mathf.Round(speed * 1000) / 1000;

        powerChangeText.text = speed.ToString();
    }
    
    public void OnFractalChanged(Int32 state)
    {
        fractalController.fractalType = (RayMarchingFractal.FractalType) state;
    }
}
