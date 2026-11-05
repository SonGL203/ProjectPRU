using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightCycle : MonoBehaviour
{
    public Light2D globalLight;

    public float dawnLength = 10f;
    public float dayHold = 30f;
    public float duskLength = 20f;
    public float nightHold = 20f;

    public Color dayColor = Color.white;
    public Color nightColor = new Color(0.2f, 0.25f, 0.5f);

    float timer;

    public event System.Action OnDusk;

    public static DayNightCycle Instance { get; private set; }

    float DayLength => dawnLength + dayHold + duskLength + nightHold;

    public bool IsNight => timer < dawnLength || timer >= dawnLength + dayHold;

    void Awake() => Instance = this;

    public void SkipToNextMorning()
    {
        timer = dawnLength;
        if (GameManager.HasInstance) GameManager.Instance.NextDay();
    }

    void Update()
    {
        float prev = timer;
        timer = (timer + Time.deltaTime) % DayLength;
        float duskAt = dawnLength + dayHold;
        if (prev < duskAt && timer >= duskAt) OnDusk?.Invoke();
        if (timer < prev && GameManager.HasInstance) GameManager.Instance.NextDay();

        float t;
        if (timer < dawnLength)
            t = 1f - timer / dawnLength;
        else if (timer < dawnLength + dayHold)
            t = 0f;
        else if (timer < dawnLength + dayHold + duskLength)
            t = (timer - dawnLength - dayHold) / duskLength;
        else
            t = 1f;

        globalLight.color = Color.Lerp(dayColor, nightColor, Mathf.SmoothStep(0f, 1f, t));
    }
}