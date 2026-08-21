using System.Collections;
using System.Collections.Generic;
using KitchenFight;
using UnityEngine;
using UnityEngine.UI;

public class TogglePanelUI : MonoBehaviour {

    public static TogglePanelUI Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float hiddenY = -320f;
    [SerializeField] private float shownY = -40f;
    [SerializeField] private float backgroundAlpha = 0.5f;
    [SerializeField] private CardListSO cardListSO;

    private bool isVisible;
    private bool hasInitializedPlayerCards;
    private Coroutine animateCoroutine;
    private int nextCardId = 1;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        KitchenGameManager.Instance.OnLocalGamePaused += KitchenGameManager_OnLocalGamePaused;
        GameInput.Instance.OnTogglePanelAction += GameInput_OnTogglePanelAction;

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
        Player.LocalInstance.ReplayCurrentCards(Player_OnCardAdded);
    }

    private void Player_OnCardAdded(int itemIndex) {
        AddCard(itemIndex);
    }

    private void OnDestroy() {
        if (KitchenGameManager.Instance != null) {
            KitchenGameManager.Instance.OnLocalGamePaused -= KitchenGameManager_OnLocalGamePaused;
        }
        if (GameInput.Instance != null) {
            GameInput.Instance.OnTogglePanelAction -= GameInput_OnTogglePanelAction;
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

    private void GameInput_OnTogglePanelAction(object sender, System.EventArgs e) {
        if (isVisible) {
            Hide();
        } else {
            Show();
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

    private bool HandleCardDismissed(EffectCardUI dismissedCard) {
        if (Player.LocalInstance == null) return false;

        CardSO card = cardListSO.GetFromIndex(dismissedCard.ItemIndex);
        if (card == null) return false;

        if (card.effectType == EffectType.InstantComplete && !CanInstantCompleteSelectedCounter()) {
            return false;
        }

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
        return true;
    }

    private bool CanInstantCompleteSelectedCounter() {
        Unity.Netcode.NetworkObjectReference counterRef = Player.LocalInstance.GetSelectedCounterRef();
        if (!counterRef.TryGet(out Unity.Netcode.NetworkObject selectedCounter)) return false;

        CuttingCounter cuttingCounter = selectedCounter.GetComponent<CuttingCounter>();
        if (cuttingCounter != null) return cuttingCounter.CanInstantComplete();

        StoveCounter stoveCounter = selectedCounter.GetComponent<StoveCounter>();
        return stoveCounter != null && stoveCounter.CanInstantComplete();
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
