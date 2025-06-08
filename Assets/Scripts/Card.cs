using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Card : MonoBehaviour
{
    public int cardIndex;
    public bool isMatched = false;
    private Sprite frontSprite;
    public Sprite backSprite;

    private Image image;
    private Button button;//edfgdfgdgf
    private GameController controller;
    private bool isFlipping = false;

    void Awake()
    {
        image = this.transform.GetComponent<Image>();
        button = this.transform.GetComponent<Button>();
        button.onClick.AddListener(onClickToFlip);
    }

    public void Init(GameController controller, int index, Sprite front)
    {
        this.controller = controller;
        cardIndex = index;
        frontSprite = front;
        image.sprite = backSprite;
        isMatched = false;
    }

    private void onClickToFlip()
    {
        if (isMatched || isFlipping) return;
        controller.OnCardClicked(this);
    }

    public void FlipOpen()
    {
        isFlipping = true;
        transform.DOScaleX(0, 0.25f).OnComplete(() => {
            image.sprite = frontSprite;
            transform.DOScaleX(1, 0.25f).OnComplete(() => isFlipping = false);
        });
    }

    public void FlipClose()
    {
        isFlipping = true;
        transform.DOScaleX(0, 0.25f).OnComplete(() => {
            image.sprite = backSprite;
            transform.DOScaleX(1, 0.25f).OnComplete(() => isFlipping = false);
        });
    }

    public void MarkMatched()
    {
        isMatched = true;
    }
}
