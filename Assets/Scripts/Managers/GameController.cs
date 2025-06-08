using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [SerializeField] private GameObject menuUIPanel;
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private GameObject finishUIPanel;
    [SerializeField] private ToggleGroup toggleGroup;
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnLoad;
    [SerializeField] private Button btnHome;
    [SerializeField] private Button btnFinish;
    [SerializeField] private RectTransform cardParent;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private TMP_Text matchText;
    [SerializeField] private TMP_Text turnText;

    private List<CardScriptableObject> cardDataList;
    private SoundManager sm;

    private List<Card> allCards = new List<Card>();
    private List<int> matchedCardIds = new List<int>();
    private Card firstCard, secondCard;
    private int score = 0;
    private int turns = 0;
    private string levelName = "Easy";
    private bool inputLocked = false;
    private GameSaveData loadedSaveData = null;


    private void Awake()
    {
        cardDataList = Resources.LoadAll<CardScriptableObject>("CardData").ToList();
        sm = FindObjectOfType<SoundManager>();
        btnStart.onClick.AddListener(OnStartClicked);
        btnFinish.onClick.AddListener(onFinishClicked);
        btnHome.onClick.AddListener(OnBackToHomeClicked);
        btnLoad.onClick.AddListener(OnLoadGameClicked);
        btnLoad.gameObject.SetActive(SaveSystem.HasSave() && SaveSystem.Load().cardIdOrder?.Count > 0);
    }

    private void Start()
    {
        HideAllPanels();
        menuUIPanel.SetActive(true);
        CheckActiveToggle();
        btnLoad.gameObject.SetActive(SaveSystem.HasSave());

        foreach (var toggle in toggleGroup.GetComponentsInChildren<Toggle>())
        {
            toggle.onValueChanged.AddListener((isOn) => {
                if (isOn) CheckActiveToggle();
            });
        }
    }

    private void CheckActiveToggle()
    {
        var activeToggle = toggleGroup.ActiveToggles().FirstOrDefault();
        if (activeToggle != null)
        {
            levelName = activeToggle.name;
            Debug.Log("Selected Level: " + levelName);
        }
    }

    private void OnStartClicked()
    {
        loadedSaveData = null;
        gameUIPanel.SetActive(true);
        sm.PlayEnter();
        StartCoroutine(GenerateCardGrid(levelName));
        menuUIPanel.GetComponent<RectTransform>().DOAnchorPosY(1080, 0.5f).OnComplete(() =>
        {
            menuUIPanel.SetActive(false);
            menuUIPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

        });
    }

    void onFinishClicked()
    {
        HideAllPanels();
        menuUIPanel.SetActive(true);
    }

    private void OnBackToHomeClicked()
    {
        if (!allCards.All(c => c.isMatched))
        {
            SaveProgress();
        }

        gameUIPanel.SetActive(false);
        menuUIPanel.SetActive(true);

        // Clear current game state
        firstCard = null;
        secondCard = null;
        allCards.Clear();

        btnLoad.gameObject.SetActive(SaveSystem.HasSave() && SaveSystem.Load().cardIdOrder?.Count > 0);
    }


    private void SaveProgress()
    {
        GameSaveData data = new GameSaveData
        {
            score = score,
            turnCount = turns,
            level = levelName,
            cardIdOrder = allCards.Select(c => c.cardIndex).ToList(),
            matchedCardIndices = allCards
                .Select((card, index) => new { card, index })
                .Where(x => x.card.isMatched)
                .Select(x => x.index)
                .ToList()
        };

        SaveSystem.Save(data);
        Debug.Log("Game Saved");
        btnLoad.gameObject.SetActive(SaveSystem.HasSave() && SaveSystem.Load().cardIdOrder?.Count > 0);
    }

    private void OnLoadGameClicked()
    {
        loadedSaveData = SaveSystem.Load();
        if (loadedSaveData == null) return;

        // Set the loaded data
        score = loadedSaveData.score;
        turns = loadedSaveData.turnCount;
        levelName = loadedSaveData.level;

        // Update UI immediately
        UpdateScore(score);
        UpdateTurnUI();

        // Set the correct toggle
        SetActiveToggle(loadedSaveData.level);

        btnLoad.gameObject.SetActive(false);
        menuUIPanel.SetActive(false);
        gameUIPanel.SetActive(true);

        StartCoroutine(GenerateCardGrid(levelName));
    }

    private void SetActiveToggle(string levelName)
    {
        foreach (Toggle toggle in toggleGroup.GetComponentsInChildren<Toggle>())
        {
            toggle.isOn = toggle.name == levelName;
        }
    }

    private IEnumerator GenerateCardGrid(string level)
    {
        yield return new WaitForEndOfFrame();
        if (loadedSaveData == null)
        {
            turns = 0;
            score = 0;
        }
        UpdateTurnUI();
        UpdateScore(score);

        // 1. Determine grid size
        int rows = 2, cols = 2;
        switch (level)
        {
            case "Easy": rows = 2; cols = 2; break;
            case "Normal": rows = 2; cols = 3; break;
            case "Hard": rows = 5; cols = 6; break;
        }

        int totalCards = rows * cols;
        int uniqueCount = totalCards / 2;

        if (cardDataList.Count < uniqueCount)
        {
            Debug.LogError($"Not enough CardScriptableObjects. Need at least {uniqueCount} for {level}.");
            yield break;
        }

        List<CardScriptableObject> finalCardSet = new();

        if (loadedSaveData != null && loadedSaveData.cardIdOrder != null)
        {
            foreach (int id in loadedSaveData.cardIdOrder)
            {
                var cardData = cardDataList.FirstOrDefault(c => c.id == id);
                if (cardData != null)
                    finalCardSet.Add(cardData);
            }
        }
        else
        {
            // Generate random deck for new game
            List<CardScriptableObject> shuffledSO = cardDataList.OrderBy(x => Random.value).ToList();
            List<CardScriptableObject> selectedPairs = shuffledSO.Take(uniqueCount).ToList();

            foreach (var card in selectedPairs)
            {
                finalCardSet.Add(card);
                finalCardSet.Add(card);
            }

            finalCardSet = finalCardSet.OrderBy(x => Random.value).ToList();
        }



        foreach (Transform child in cardParent)
            Destroy(child.gameObject);

        // 6. Configure GridLayoutGroup dynamically
        GridLayoutGroup grid = cardParent.GetComponent<GridLayoutGroup>();
        RectTransform parentRect = cardParent.GetComponent<RectTransform>();

        float spacingX = grid.spacing.x;
        float spacingY = grid.spacing.y;
        float paddingX = grid.padding.left + grid.padding.right;
        float paddingY = grid.padding.top + grid.padding.bottom;

        float availableWidth = parentRect.rect.width - paddingX - spacingX * (cols - 1);
        float availableHeight = parentRect.rect.height - paddingY - spacingY * (rows - 1);

        float cardWidth = availableWidth / cols;
        float cardHeight = availableHeight / rows;
        float cardSize = Mathf.Min(cardWidth, cardHeight); // square card layout

        grid.cellSize = new Vector2(cardSize, cardSize);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;

        // 7. Spawn and animate cards
        allCards.Clear();
        for (int i = 0; i < finalCardSet.Count; i++)
        {
            CardScriptableObject data = finalCardSet[i];

            GameObject go = Instantiate(cardPrefab, cardParent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.localScale = Vector3.zero;

            Card card = go.GetComponent<Card>();
            card.Init(this, data.id, data.cardImage);
            allCards.Add(card);

            // Animate scale pop-in
            float delay = i * 0.05f;
            rt.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetDelay(delay);

            if (loadedSaveData != null && loadedSaveData.matchedCardIndices.Contains(i))
            {
                StartCoroutine(RestoreMatchedCard(card, delay + 0.3f));
            }
        }

        UpdateScore(0);
    }

    private IEnumerator RestoreMatchedCard(Card card, float delay)
    {
        yield return new WaitForSeconds(delay);

        card.FlipOpen();
        card.MarkMatched();

        sm.PlayFlip();
    }



    public void OnCardClicked(Card card)
    {
        if (inputLocked || card == firstCard || card.isMatched) return;
        sm.PlayFlip();
        card.FlipOpen();
        

        if (firstCard == null)
        {
            firstCard = card;
        }
        else
        {
            secondCard = card;
            inputLocked = true;
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        yield return new WaitForSeconds(1f);

        turns++;
        UpdateTurnUI();

        if (firstCard.cardIndex == secondCard.cardIndex)
        {
            sm.PlayMatch();
            firstCard.MarkMatched();
            secondCard.MarkMatched();
            UpdateScore(score + 1);
            if (allCards.All(c => c.isMatched))
            {
                sm.PlayGameOver();
                HideAllPanels();
                finishUIPanel.SetActive(true);
                SaveSystem.Clear();
                loadedSaveData = null;
                btnLoad.gameObject.SetActive(false);
            }
        }
        else
        {
            sm.PlayMismatch();
            firstCard.FlipClose();
            secondCard.FlipClose();
            
        }

        firstCard = null;
        secondCard = null;
        inputLocked = false;
    }

    private void UpdateTurnUI()
    {
        if (turnText != null)
            turnText.text = turns.ToString();
    }

    private void UpdateScore(int newScore)
    {
        score = newScore;
        if (matchText != null)
            matchText.text = score.ToString();
    }

    private void HideAllPanels()
    {
        menuUIPanel.SetActive(false);
        gameUIPanel.SetActive(false);
        finishUIPanel.SetActive(false);
    }
}

public static class SaveSystem
{
    private const string KEY = "CardMatchSave";

    public static void Save(GameSaveData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }

    public static GameSaveData Load()
    {
        if (!PlayerPrefs.HasKey(KEY)) return null;
        return JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString(KEY));
    }

    public static bool HasSave() => PlayerPrefs.HasKey(KEY);

    public static void Clear() => PlayerPrefs.DeleteKey(KEY);
}


