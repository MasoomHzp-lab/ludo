using UnityEngine;

public class ThemeApplier : MonoBehaviour
{
      [Header("Where to apply board sprite")]
    public SpriteRenderer boardRenderer;  // اختیاری، اگه خالی باشه خودش از همین آبجکت می‌گیره

    private void Awake()
    {
        // اگر یادت رفت دستی ست کنی، خودش می‌ره روی همون آبجکت دنبال SpriteRenderer می‌گرده
        if (boardRenderer == null)
            boardRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (ThemeManager.I == null)
        {
            Debug.LogWarning("[ThemeApplier] ThemeManager not found in scene.");
            return;
        }

        var theme = ThemeManager.I.GetActiveTheme();
        if (theme == null)
        {
            Debug.LogWarning("[ThemeApplier] No active theme found.");
            return;
        }

        if (boardRenderer != null && theme.boardSprite != null)
        {
            boardRenderer.sprite = theme.boardSprite;
        }
    }
}
