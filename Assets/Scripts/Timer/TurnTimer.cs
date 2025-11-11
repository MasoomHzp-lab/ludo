using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class TurnTimer : MonoBehaviour
{
   [Header("UI")]
    public TMP_Text label;     // متن شمارنده (10,9,8,...)
    public Image fillBar;      // Image با Fill Method = Horizontal

    [Header("Settings")]
    public float defaultSeconds = 20f;
    public bool useUnscaledTime = false;

    [Header("Events")]
    public UnityEvent onTimeout;
    public UnityEvent<float> onTick; // هر فریم مقدار باقیمانده

    public bool IsRunning { get; private set; }
    public float Remaining { get; private set; }

    float lastT;

    void Update()
    {
        if (!IsRunning) return;

        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        float dt = now - lastT;
        lastT = now;

        Remaining -= dt;
        if (Remaining < 0f) Remaining = 0f;

        // UI
        if (label) label.text = Mathf.CeilToInt(Remaining).ToString();
        if (fillBar)
        {
            float denom = Mathf.Max(0.01f, defaultSeconds);
            fillBar.fillAmount = Mathf.Clamp01(Remaining / denom);
        }
        onTick?.Invoke(Remaining);

        if (Remaining <= 0f)
            StopInternal(invokeTimeout: true);
    }

    public void StartTimer(float seconds)
    {
        defaultSeconds = seconds;
        Remaining = seconds;
        IsRunning = true;
        lastT = useUnscaledTime ? Time.unscaledTime : Time.time;

        if (label) label.text = Mathf.CeilToInt(Remaining).ToString();
        if (fillBar) fillBar.fillAmount = 1f;
    }

    public void StartTimer() => StartTimer(defaultSeconds);

    public void CancelTimer() => StopInternal(false);

    void StopInternal(bool invokeTimeout)
    {
        if (!IsRunning) return;
        IsRunning = false;
        if (invokeTimeout) onTimeout?.Invoke();
    }
}
