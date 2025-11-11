using UnityEngine;

public class CheatManager : MonoBehaviour
{
  
 public static CheatManager Instance { get; private set; }

    private int? forcedNextRoll = null;
    private float expireAt = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// مقدار تاس بعدی را به طور موقت قفل می‌کند (مثلاً ۶) و بعد از timeout ثانیه منقضی می‌شود.
    /// </summary>
    public void ForceNextRoll(int value, float timeoutSeconds = 3f)
    {
        forcedNextRoll = Mathf.Clamp(value, 1, 6);
        expireAt = Time.unscaledTime + Mathf.Max(0.1f, timeoutSeconds);
#if UNITY_EDITOR
        Debug.Log($"[Cheat] Next roll forced to {forcedNextRoll} for {timeoutSeconds} sec.");
#endif
    }

    /// <summary>
    /// اگر مقدار زورکی موجود و منقضی نشده باشد، آن را مصرف می‌کند.
    /// </summary>
    public bool TryConsumeForcedRoll(out int value)
    {
        if (forcedNextRoll.HasValue && Time.unscaledTime <= expireAt)
        {
            value = forcedNextRoll.Value;
            forcedNextRoll = null;
            expireAt = -1f;
            return true;
        }
        value = 0;
        return false;
    }

}
