using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager I;

    [System.Serializable]
    public class ThemeEntry
    {
        public string themeId;     // مثلا: "Classic", "Galaxy", "Forest", ...
        public Sprite boardSprite;
    }

    [Header("Themes list")]
    public List<ThemeEntry> themes = new List<ThemeEntry>();

    [Header("Defaults")]
    public string defaultThemeId = "DarkSky";

    [Header("Free themes (always unlocked)")]
    public List<string> freeThemeIds = new List<string>(); // مثلا ["Classic","Galaxy"]

    private const string ActiveThemeKey = "ActiveThemeId";

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public ThemeEntry GetThemeById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        return themes.FirstOrDefault(t => t.themeId == id);
    }

    public ThemeEntry GetActiveTheme()
    {
        string id = PlayerPrefs.GetString(ActiveThemeKey, defaultThemeId);
        var entry = GetThemeById(id);
        if (entry == null)
        {
            entry = GetThemeById(defaultThemeId);
        }
        return entry;
    }

    public void SetActiveTheme(string id)
    {
        var entry = GetThemeById(id);
        if (entry == null)
        {
            Debug.LogWarning("[ThemeManager] Theme not found: " + id);
            return;
        }

        PlayerPrefs.SetString(ActiveThemeKey, id);
        PlayerPrefs.Save();
        Debug.Log("[ThemeManager] Active theme set to: " + id);
    }

    // ---- قفل/باز بودن ----

    public bool IsThemeFree(string themeId)
    {
        return freeThemeIds.Contains(themeId);
    }

    public bool IsThemeUnlocked(string themeId)
    {
        // رایگان‌ها همیشه باز هستند
        if (IsThemeFree(themeId))
            return true;

        // پولی‌ها (بعد از خرید توسط BazaarPaymentManager)
        int val = PlayerPrefs.GetInt("Theme_" + themeId, 0);
        return val == 1;
    }

    // این متد رو BazaarPaymentManager بعد از خرید موفق صدا می‌زنه
    public void UnlockTheme(string themeId)
    {
        PlayerPrefs.SetInt("Theme_" + themeId, 1);
        PlayerPrefs.Save();
        Debug.Log("[ThemeManager] Theme unlocked: " + themeId);
    }
}
