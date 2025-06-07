using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] private GameObject menuUIPanel;
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private ToggleGroup toggleGroup;
    [SerializeField] private Button btnStart;

    string levelName = "";

    private void Awake()
    {
        btnStart.onClick.AddListener(onBtnStartClick);
    }

    void Start()
    {
        hideAllPanels();
        menuUIPanel.SetActive(true);
        CheckActiveToggle();

        foreach (var toggle in toggleGroup.GetComponentsInChildren<Toggle>())
        {
            Toggle capturedToggle = toggle; // capture in local variable
            capturedToggle.onValueChanged.AddListener((isOn) => {
                if (isOn) CheckActiveToggle();
            });
        }
    }

    public void CheckActiveToggle()
    {
        Toggle activeToggle = toggleGroup.ActiveToggles().FirstOrDefault();

        if (activeToggle != null)
        {
            Debug.Log("Active Toggle: " + activeToggle.name);
            levelName = activeToggle.name;
        }
    }

    private void onBtnStartClick()
    {
        menuUIPanel.GetComponent<RectTransform>().DOAnchorPosY(1080, 0.5f).OnComplete(() =>
        {
            menuUIPanel.SetActive(false);
            gameUIPanel.SetActive(true);
            Canvas.ForceUpdateCanvases();
        });

    }

    void hideAllPanels()
    {
        menuUIPanel.SetActive(false);
        gameUIPanel.SetActive(false);
    }
}
