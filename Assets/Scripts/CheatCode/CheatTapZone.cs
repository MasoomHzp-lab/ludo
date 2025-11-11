using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CheatTapZone : MonoBehaviour, IPointerDownHandler
{
   [Range(1, 6)] public int forcedValue = 6;
    public int tapsRequired = 3;
    public float multiTapWindow = 0.6f;
    public float forceTimeoutSeconds = 3f;

    private int tapCount = 0;
    private float lastTapTime = -10f;

    private void Reset()
    {
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
                CheatManager.Instance.ForceNextRoll(forcedValue, forceTimeoutSeconds);
        }
    }
}
