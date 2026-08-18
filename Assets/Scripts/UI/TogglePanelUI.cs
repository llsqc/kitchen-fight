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
    [SerializeField] private CardListSO cardListSO;

    private bool isVisible;
    private bool hasInitializedPlayerCards;
    private Coroutine animateCoroutine;
    private int nextCardId = 1;
    private GameMode currentGameMode = GameMode.Single;

    private void Awake() {
        Instance = this;
        currentGameMode = KitchenGameMultiplayer.playMultiplayer
            ? GameMode.Multiplayer
            : GameMode.Single;

        testAddCardButton.onClick.AddListener(() => {
            int itemIndex = CardDealer.PickRandomCardIndex(cardListSO, currentGameMode);
            if (itemIndex != -1) {
                AddCard(itemIndex);
            }
        });
    }

    private void Start() {
        KitchenGameManager.Instance.OnLocalGamePaused += KitchenGameManager_OnLocalGamePaused;

        if (Player.LocalInstance != null) {
            Player_OnSpawned();
        }
        Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;

        cardContainer.anchoredPosition = new Vector2(0, hiddenY);
        backgroundImage.color = new Color(0, 0, 0, 0f);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        isVisible = false;
    }

    private void Player_OnAnyPlayerSpawned(object sender, System.EventArgs e) {
        if (Player.LocalInstance != null) {
            Player_OnSpawned();
        }
    }

    private void Player_OnSpawned() {
        if (hasInitializedPlayerCards) return;
        hasInitializedPlayerCards = true;

        Player.LocalInstance.OnCardAdded += Player_OnCardAdded;

        var dealtEffectTypes = new HashSet<EffectType>();
        bool hasLegendary = false;
        for (int i = 0; i < cardCount; i++) {
            int itemIndex = CardDealer.PickRandomCardIndex(
                cardListSO,
                currentGameMode,
                dealtEffectTypes,
                hasLegendary);
            if (itemIndex != -1) {
                AddCard(itemIndex);
                CardSO dealtCard = cardListSO.GetFromIndex(itemIndex);
                dealtEffectTypes.Add(dealtCard.effectType);
                hasLegendary |= dealtCard.rarity == Rarity.Legendary;
            }
        }
    }

    private void Player_OnCardAdded(int itemIndex) {
        AddCard(itemIndex);
    }

    private void OnDestroy() {
        if (KitchenGameManager.Instance != null) {
            KitchenGameManager.Instance.OnLocalGamePaused -= KitchenGameManager_OnLocalGamePaused;
        }
        if (Player.LocalInstance != null) {
            Player.LocalInstance.OnCardAdded -= Player_OnCardAdded;
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

    public EffectCardUI AddCard(int itemIndex) {
        if (cardListSO == null) return null;
        CardSO card = cardListSO.GetFromIndex(itemIndex);
        if (card == null) return null;

        GameObject cardGo = Instantiate(cardPrefab, cardContainer);
        EffectCardUI cardUI = cardGo.GetComponent<EffectCardUI>();
        cardUI.SetCardData(nextCardId++, itemIndex, card);
        cardUI.OnCardDismissed = HandleCardDismissed;
        return cardUI;
    }

    private void HandleCardDismissed(EffectCardUI dismissedCard) {
        if (Player.LocalInstance == null) return;

        CardSO card = cardListSO.GetFromIndex(dismissedCard.ItemIndex);
        if (card == null) return;

        Vector3 aimPosition = Vector3.zero;
        Unity.Netcode.NetworkObjectReference counterRef = default;

        switch (card.targetType) {
            case TargetType.Player:
            case TargetType.Teammate:
                aimPosition = GameInput.Instance.GetMouseWorldPosition();
                break;
            case TargetType.Counter:
            case TargetType.Self:
                counterRef = Player.LocalInstance.GetSelectedCounterRef();
                break;
        }

        Player.LocalInstance.UseCardServerRpc(dismissedCard.ItemIndex, aimPosition, counterRef);
    }

    public bool HasPlayerTargetCard() {
        foreach (Transform child in cardContainer) {
            if (child.TryGetComponent(out EffectCardUI card)) {
                if (card.TargetType == TargetType.Player) return true;
            }
        }
        return false;
    }

    public bool HasTeammateTargetCard() {
        foreach (Transform child in cardContainer) {
            if (child.TryGetComponent(out EffectCardUI card)) {
                if (card.TargetType == TargetType.Teammate) return true;
            }
        }
        return false;
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

    public void SetGameMode(GameMode mode) {
        currentGameMode = mode;
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
