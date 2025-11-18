using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using RTLTMPro;

public class TutorialManager : MonoBehaviour
{ [Header("UI Elements")]
    public Transform pageContainer;
    public Button btnNext;
    public Button btnPrev;
    public Button btnExit;
    public RTLTextMeshPro pageIndicator;

    [Header("Page Prefab")]
    public GameObject pagePrefab;

    [Header("Page Data")]
    public List<Sprite> pageImages;
    public List<string> pageTexts;

    private List<GameObject> pages = new List<GameObject>();
    private int currentPage = 0;

    void Start()
    {
        GeneratePages();
        UpdateUI();

        btnNext.onClick.AddListener(NextPage);
        btnPrev.onClick.AddListener(PrevPage);
        btnExit.onClick.AddListener(ExitTutorial);

        btnExit.gameObject.SetActive(false);
    }

    void GeneratePages()
    {
        for (int i = 0; i < pageTexts.Count; i++)
        {
            GameObject p = Instantiate(pagePrefab, pageContainer);
            p.SetActive(i == 0);
            pages.Add(p);

            // متن با RTL
            var textComp = p.transform.Find("Text").GetComponent<RTLTextMeshPro>();
            textComp.text = pageTexts[i];

            // عکس
            if (i < pageImages.Count && pageImages[i] != null)
            {
                var img = p.transform.Find("Image").GetComponent<Image>();
                img.sprite = pageImages[i];
                img.gameObject.SetActive(true);
            }
        }
    }

    void NextPage()
    {
        if (currentPage < pages.Count - 1)
        {
            pages[currentPage].SetActive(false);
            currentPage++;
            pages[currentPage].SetActive(true);
            UpdateUI();
        }
    }

    void PrevPage()
    {
        if (currentPage > 0)
        {
            pages[currentPage].SetActive(false);
            currentPage--;
            pages[currentPage].SetActive(true);
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        pageIndicator.text = (currentPage + 1) + " / " + pages.Count;

        // مدیریت دکمه‌ها
        btnPrev.interactable = currentPage > 0;
        btnNext.interactable = currentPage < pages.Count - 1;

        // نمایش دکمه خروج روی آخرین صفحه
        if (currentPage == pages.Count - 1)
    {
        btnNext.gameObject.SetActive(false);   // مخفی کن
        btnExit.gameObject.SetActive(true);    // نمایش بده
    }
    else
    {
        btnNext.gameObject.SetActive(true);    // اگر صفحه آخر نیست، دکمه next را نمایش بده
        btnExit.gameObject.SetActive(false);   // دکمه exit مخفی شود
    }
    }

    void ExitTutorial()
    {
        gameObject.SetActive(false);  // یا Load Scene
    }
}


