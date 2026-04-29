using UnityEngine;
public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle")]
    public float dayLengthSeconds = 120f;
    [Range(0f, 1f)] public float timeOfDay = 0.25f;
    [Header("Sun")]
    public Light sunLight;
    public Gradient sunColor;
    public AnimationCurve sunIntensity;
    [Header("Ambient")]
    public Gradient ambientColor;
    public bool IsDay => timeOfDay > 0.25f && timeOfDay < 0.75f;
    void Update()
    {
        timeOfDay += Time.deltaTime / dayLengthSeconds;
        if (timeOfDay >= 1f) timeOfDay = 0f;
        if (sunLight)
        {
            sunLight.transform.rotation = Quaternion.Euler(timeOfDay * 360f - 90f, 170f, 0f);
            sunLight.color = sunColor.Evaluate(timeOfDay);
            sunLight.intensity = sunIntensity.Evaluate(timeOfDay);
        }
        RenderSettings.ambientLight = ambientColor.Evaluate(timeOfDay);
    }
}