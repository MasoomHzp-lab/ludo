using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CheatTapZone : MonoBehaviour, IPointerDownHandler
{
    [Header("Config")]
    [Range(1, 6)] public int forcedValue = 6;
    [Tooltip("چند تاپ پشت‌سرهم لازم است؟")]
    public int tapsRequired = 3;
    [Tooltip("حداکثر فاصلهٔ زمانی بین تاپ‌ها (ثانیه)")]
    public float multiTapWindow = 0.6f;
    [Tooltip("تا چه مدت بعد از فعال شدن، رول زورکی معتبر است؟")]
    public float forceTimeoutSeconds = 3f;

    private int tapCount = 0;
    private float lastTapTime = -10f;

    private void Reset()
    {
        // اگر Image دارید، شفافش کنید اما Raycast فعال بماند
        var img = GetComponent<Image>();
        if (img != null)
        {
            var c = img.color; c.a = 0f; img.color = c;
            img.raycastTarget = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Time.unscaledTime - lastTapTime > multiTapWindow)
            tapCount = 0;

        tapCount++;
        lastTapTime = Time.unscaledTime;

        if (tapCount >= tapsRequired)
        {
            tapCount = 0;
            if (CheatManager.Instance != null)
            {
                CheatManager.Instance.ForceNextRoll(forcedValue, forceTimeoutSeconds);
            }
        }
    }

#if UNITY_EDITOR
    // فقط برای اینکه در Scene یک باکس نیمه‌شفاف ببینید
    private void OnDrawGizmos()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null || rt.rect.width <= 0 || rt.rect.height <= 0) return;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;
        Vector3 size = new Vector3(Vector3.Distance(corners[0], corners[3]),
                                   Vector3.Distance(corners[0], corners[1]), 1f);
        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.identity, size);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
