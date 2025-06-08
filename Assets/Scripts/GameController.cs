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
    [SerializeField] private Button btnFinish;
    [SerializeField] private RectTransform cardParent;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private TMP_Text matchText;
    [SerializeField] private TMP_Text turnText;

    private List<CardScriptableObject> cardDataList;


    private List<Card> allCards = new List<Card>();
    private Card firstCard, secondCard;
    private int score = 0;
    private int turns = 0;
    private string levelName = "Easy";
    private bool inputLocked = false;

    private void Awake()
    {
        cardDataList = Resources.LoadAll<CardScriptableObject>("CardData").ToList();

        btnStart.onClick.AddListener(OnStartClicked);
        btnFinish.onClick.AddListener(onFinishClicked);
    }

    private void Start()
    {
        HideAllPanels();
        menuUIPanel.SetActive(true);
        CheckActiveToggle();

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
        gameUIPanel.SetActive(true);

        StartCoroutine(GenerateCardGrid(levelName));
        menuUIPanel.GetComponent<RectTransform>().DOAnchorPosY(1080, 0.5f).OnComplete(() =>
        {
            menuUIPanel.SetActive(false);
            
        });
    }

    void onFinishClicked()
    {
        HideAllPanels();
        menuUIPanel.SetActive(true);
    }

    private IEnumerator GenerateCardGrid(string level)
    {
        yield return new WaitForEndOfFrame();
        turns = 0;
        UpdateTurnUI();

        // 1. Set grid size based on difficulty
        int rows = 2, cols = 2;
        switch (level)
        {
            case "Easy": rows = 2; cols = 2; break;
            case "Normal": rows = 2; cols = 3; break;
            case "Hard": rows = 5; cols = 6; break;
        }

        cardParent.GetComponent<GridLayoutGroup>().constraintCount = rows;
        int totalCards = rows * cols;
        int uniqueCount = totalCards / 2;

        // 2. Validate available SOs
        if (cardDataList.Count < uniqueCount)
        {
            Debug.LogError($"Not enough CardScriptableObjects. Need at least {uniqueCount} for {level}.");
            yield break;
        }

        // 3. Shuffle and pick unique SOs
        List<CardScriptableObject> shuffledSO = cardDataList.OrderBy(x => Random.value).ToList();
        List<CardScriptableObject> selectedPairs = shuffledSO.Take(uniqueCount).ToList();

        // 4. Duplicate each SO to make pairs
        List<CardScriptableObject> finalCardSet = new List<CardScriptableObject>();
        foreach (var card in selectedPairs)
        {
            finalCardSet.Add(card);
            finalCardSet.Add(card);
        }

        // 5. Shuffle the full deck
        finalCardSet = finalCardSet.OrderBy(x => Random.value).ToList();

        // 6. Clear previous cards
        foreach (Transform child in cardParent)
            Destroy(child.gameObject);

        // 7. Get parent rect info
        float spacingX = 20f, spacingY = 20f;
        float parentWidth = cardParent.rect.width;
        float parentHeight = cardParent.rect.height;

        float cardWidth = (parentWidth - (cols - 1) * spacingX) / cols;
        float cardHeight = (parentHeight - (rows - 1) * spacingY) / rows;
        float cardSize = Mathf.Min(cardWidth, cardHeight);

        float gridWidth = cols * cardSize + (cols - 1) * spacingX;
        float gridHeight = rows * cardSize + (rows - 1) * spacingY;

        Vector2 startPos = new Vector2(
            -gridWidth / 2f + cardSize / 2f,
            gridHeight / 2f - cardSize / 2f
        );

        // 8. Spawn the cards
        allCards.Clear();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int index = row * cols + col;
                CardScriptableObject data = finalCardSet[index];

                Vector2 position = new Vector2(
                    startPos.x + col * (cardSize + spacingX),
                    startPos.y - row * (cardSize + spacingY)
                );

                GameObject go = Instantiate(cardPrefab, cardParent);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.localPosition = position;
                rt.sizeDelta = new Vector2(cardSize, cardSize);

                Card card = go.GetComponent<Card>();
                card.Init(this, data.id, data.cardImage); // or use card.Init(this, data);
                allCards.Add(card);
            }
        }

        UpdateScore(0);
    }


    public void OnCardClicked(Card card)
    {
        if (inputLocked || card == firstCard || card.isMatched) return;

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

            firstCard.MarkMatched();
            secondCard.MarkMatched();
            UpdateScore(score + 1);
            
        }
        else
        {

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
    const string KEY = "card_match_save";

    public static void Save(CardData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }

    public static CardData Load()
    {
        if (!PlayerPrefs.HasKey(KEY)) return null;
        string json = PlayerPrefs.GetString(KEY);
        return JsonUtility.FromJson<CardData>(json);
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KEY);
    }
}

