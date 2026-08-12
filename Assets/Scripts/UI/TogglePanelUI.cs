using System.Collections;
using System.Collections.Generic;
using KitchenFight;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TogglePanelUI : MonoBehaviour {

    public static TogglePanelUI Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private int cardCount = 5;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float hiddenY = -320f;
    [SerializeField] private float shownY = -40f;
    [SerializeField] private float backgroundAlpha = 0.5f;
    [SerializeField] private Button testAddCardButton;

    private bool isVisible;
    private Coroutine animateCoroutine;
    private int nextCardId = 1;

    private void Awake() {
        Instance = this;

        testAddCardButton.onClick.AddListener(() => {
            AddCard();
        });
    }

    private void Start() {
        KitchenGameManager.Instance.OnLocalGamePaused += KitchenGameManager_OnLocalGamePaused;

        for (int i = 0; i < cardCount; i++) {
            AddCard();
        }

        cardContainer.anchoredPosition = new Vector2(0, hiddenY);
        backgroundImage.color = new Color(0, 0, 0, 0f);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        isVisible = false;
    }

    private void OnDestroy() {
        if (KitchenGameManager.Instance != null) {
            KitchenGameManager.Instance.OnLocalGamePaused -= KitchenGameManager_OnLocalGamePaused;
        }
    }

    private void KitchenGameManager_OnLocalGamePaused(object sender, System.EventArgs e) {
        if (isVisible) {
            Hide();
        }
    }

    private void Update() {
        if (Keyboard.current.tabKey.wasPressedThisFrame) {
            if (isVisible) {
                Hide();
            } else {
                Show();
            }
        }
    }

    public EffectCardUI AddCard() {
        GameObject cardGo = Instantiate(cardPrefab, cardContainer);
        EffectCardUI card = cardGo.GetComponent<EffectCardUI>();
        card.SetCardId(nextCardId++);
        return card;
    }

    public List<CardInfo> GetAllCardInfos() {
        List<CardInfo> infos = new List<CardInfo>();
        foreach (Transform child in cardContainer) {
            if (child.TryGetComponent(out EffectCardUI card)) {
                infos.Add(card.GetCardInfo());
            }
        }
        return infos;
    }

    public CardInfo GetCardInfo(int cardId) {
        foreach (Transform child in cardContainer) {
            if (child.TryGetComponent(out EffectCardUI card) && card.CardId == cardId) {
                return card.GetCardInfo();
            }
        }
        return default;
    }

    private void Show() {
        isVisible = true;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        if (animateCoroutine != null) StopCoroutine(animateCoroutine);
        animateCoroutine = StartCoroutine(AnimatePanel(shownY, backgroundAlpha));
    }

    private void Hide() {
        isVisible = false;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        if (animateCoroutine != null) StopCoroutine(animateCoroutine);
        animateCoroutine = StartCoroutine(AnimatePanel(hiddenY, 0f));
    }

    private IEnumerator AnimatePanel(float targetY, float targetBgAlpha) {
        Vector2 startPos = cardContainer.anchoredPosition;
        Vector2 endPos = new Vector2(0, targetY);
        Color startColor = backgroundImage.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, targetBgAlpha);

        float elapsed = 0f;
        while (elapsed < animationDuration) {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            cardContainer.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            backgroundImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        cardContainer.anchoredPosition = endPos;
        backgroundImage.color = endColor;
    }

}
