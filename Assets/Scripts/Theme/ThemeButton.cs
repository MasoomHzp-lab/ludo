using UnityEngine;

public class ThemeButton : MonoBehaviour
{
   [Header("Theme info")]
    public string themeId;     // باید با ThemeManager.themeId یکی باشه

    [Header("Paid settings (for future)")]
    public bool isPaidTheme = false; 
    public string productId;   // اسم محصول در بازار، بعدا پرش می‌کنیم

    public void OnClickSelectTheme()
    {
        if (ThemeManager.I == null)
        {
            Debug.LogWarning("[ThemeButton] ThemeManager not found.");
            return;
        }

        // اگر باز شده → انتخابش کن
        if (ThemeManager.I.IsThemeUnlocked(themeId))
        {
            ThemeManager.I.SetActiveTheme(themeId);
            Debug.Log("Theme selected: " + themeId);
            return;
        }

        // اگر قفله → الان هنوز پرداخت نداریم، فقط پیام بده و هیچ‌کاری نکن
        Debug.Log("Theme is locked: " + themeId + " (payment not wired yet)");
        
        // --- بعدا اینجا خرید بازار رو صدا می‌زنیم ---
        // if (isPaidTheme && BazaarPaymentManager.Instance != null)
        // {
        //     BazaarPaymentManager.Instance.BuyTheme(productId, themeId);
        // }
    }

}
