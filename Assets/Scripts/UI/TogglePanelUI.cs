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
    [SerializeField] private SabotageItemListSO sabotageItemListSO;

    private bool isVisible;
    private Coroutine animateCoroutine;
    private int nextCardId = 1;

    private void Awake() {
        Instance = this;

        testAddCardButton.onClick.AddListener(() => {
            int itemIndex = PickRandomItemIndex();
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
        Player.LocalInstance.OnCardAdded += Player_OnCardAdded;

        for (int i = 0; i < cardCount; i++) {
            int itemIndex = PickRandomItemIndex();
            if (itemIndex != -1) {
                AddCard(itemIndex);
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
        if (sabotageItemListSO == null) return null;
        SabotageItemSO itemSO = sabotageItemListSO.GetFromIndex(itemIndex);
        if (itemSO == null) return null;

        GameObject cardGo = Instantiate(cardPrefab, cardContainer);
        EffectCardUI card = cardGo.GetComponent<EffectCardUI>();
        card.SetCardData(nextCardId++, itemIndex, itemSO);
        card.OnCardDismissed = HandleCardDismissed;
        return card;
    }

    private void HandleCardDismissed(EffectCardUI card) {
        if (Player.LocalInstance == null) return;

        SabotageItemSO item = sabotageItemListSO.GetFromIndex(card.ItemIndex);
        if (item == null) return;

        Vector3 aimPosition = Vector3.zero;
        Unity.Netcode.NetworkObjectReference counterRef = default;

        switch (item.targetType) {
            case TargetType.Player:
                aimPosition = GameInput.Instance.GetMouseWorldPosition();
                break;
            case TargetType.Counter:
                // Will be handled by server if a counter is selected
                counterRef = Player.LocalInstance.GetSelectedCounterRef();
                break;
            case TargetType.Self:
                counterRef = Player.LocalInstance.GetSelectedCounterRef();
                break;
        }

        Player.LocalInstance.UseCardServerRpc(card.ItemIndex, aimPosition, counterRef);
    }

    public bool HasPlayerTargetCard() {
        foreach (Transform child in cardContainer) {
            if (child.TryGetComponent(out EffectCardUI card)) {
                if (card.TargetType == TargetType.Player) return true;
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

    private int PickRandomItemIndex() {
        if (sabotageItemListSO == null || sabotageItemListSO.sabotageItemList.Count == 0) return -1;

        float roll = Random.value;
        Rarity targetRarity = roll < 0.6f ? Rarity.Common : (roll < 0.9f ? Rarity.Rare : Rarity.Epic);

        var candidates = new List<int>();
        for (int i = 0; i < sabotageItemListSO.sabotageItemList.Count; i++) {
            if (sabotageItemListSO.sabotageItemList[i].rarity == targetRarity) {
                candidates.Add(i);
            }
        }
        if (candidates.Count == 0) {
            for (int i = 0; i < sabotageItemListSO.sabotageItemList.Count; i++) {
                if (sabotageItemListSO.sabotageItemList[i].rarity == Rarity.Common) {
                    candidates.Add(i);
                }
            }
        }
        if (candidates.Count == 0) {
            return Random.Range(0, sabotageItemListSO.sabotageItemList.Count);
        }
        return candidates[Random.Range(0, candidates.Count)];
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
