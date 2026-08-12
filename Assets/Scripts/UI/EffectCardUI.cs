using KitchenFight;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EffectCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {

    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image innerBackground;
    [SerializeField] private Image iconFrame;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image rarityGem;
    [SerializeField] private Image divider;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Hover Effects")]
    [SerializeField] private float hoverOffsetY = 30f;
    [SerializeField] private float hoverBrightness = 1.5f;
    [SerializeField] private float lerpSpeed = 15f;

    [Header("Drag Settings")]
    [SerializeField] private float dragThreshold = 100f;

    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color rareColor = new Color(0.2f, 0.5f, 1f, 1f);
    [SerializeField] private Color epicColor = new Color(0.8f, 0.3f, 0.9f, 1f);

    public int CardId { get; private set; }
    public int ItemIndex { get; private set; } = -1;
    public TargetType TargetType { get; private set; }

    public System.Action<EffectCardUI> OnCardDismissed;

    private CardSO cardSO;

    private bool isHovered;
    private float currentOffset;
    private float targetOffset;
    private float lastAppliedOffset;

    private bool isDragging;
    private Vector2 dragStartLocalPos;

    private Graphic[] graphics;
    private Color[] originalColors;

    private void Awake() {
        graphics = GetComponentsInChildren<Graphic>(true);
        originalColors = new Color[graphics.Length];
        for (int i = 0; i < graphics.Length; i++) {
            originalColors[i] = graphics[i].color;
        }
    }

    private void OnEnable() {
        isHovered = false;
        isDragging = false;
        targetOffset = 0f;
        currentOffset = 0f;
        lastAppliedOffset = 0f;
        Canvas.willRenderCanvases += OnWillRenderCanvases;
    }

    private void OnDisable() {
        Canvas.willRenderCanvases -= OnWillRenderCanvases;
    }

    public void SetCardData(int id, int itemIndex, CardSO cardSO) {
        CardId = id;
        ItemIndex = itemIndex;
        this.cardSO = cardSO;
        TargetType = cardSO.targetType;

        if (nameText != null) nameText.text = cardSO.cardName;
        if (descriptionText != null) {
            descriptionText.text = !string.IsNullOrEmpty(cardSO.description)
                ? cardSO.description
                : GenerateDescription(cardSO);
        }
        if (iconImage != null) {
            if (cardSO.icon != null) {
                iconImage.sprite = cardSO.icon;
                iconImage.color = Color.white;
            } else {
                iconImage.sprite = null;
                iconImage.color = cardSO.placeholderColor;
            }
        }

        ApplyRarityVisual(cardSO.rarity);
    }

    private string GenerateDescription(CardSO card) {
        if (!string.IsNullOrEmpty(card.description)) return card.description;

        string target = card.targetType switch {
            TargetType.Player => "对目标玩家",
            TargetType.Counter => "对目标台面",
            TargetType.Self => "对自身",
            TargetType.Teammate => "对队友",
            _ => string.Empty,
        };
        string effect = card.effectType switch {
            EffectType.Stun => $"眩晕 {card.duration:F0}秒",
            EffectType.ReverseControls => $"反向操作 {card.duration:F0}秒",
            EffectType.LockCounter => $"锁定 {card.duration:F0}秒",
            EffectType.MoveSpeedUp => $"移动速度提升 {card.magnitude:F1}倍，持续 {card.duration:F0}秒",
            EffectType.InteractionSpeedUp => $"交互速度提升 {card.magnitude:F1}倍，持续 {card.duration:F0}秒",
            EffectType.DoubleScore => $"得分翻倍，持续 {card.duration:F0}秒",
            EffectType.ExtraTime => $"延长游戏时间 {card.magnitude:F0}秒",
            EffectType.InstantComplete => "立即完成当前烹饪",
            EffectType.SelfClean or EffectType.CleanWipe => "清除所有负面效果",
            EffectType.Shield => $"护盾，免疫下一次负面效果，持续 {card.duration:F0}秒",
            EffectType.Reflect => $"反弹，将下一次负面效果反弹给攻击者，持续 {card.duration:F0}秒",
            _ => string.Empty,
        };
        return $"{target}{effect}";
    }

    private void ApplyRarityVisual(Rarity rarity) {
        Color color = rarity switch {
            Rarity.Common => commonColor,
            Rarity.Rare => rareColor,
            Rarity.Epic => epicColor,
            _ => commonColor,
        };
        if (backgroundImage != null) backgroundImage.color = color;
        if (rarityGem != null) rarityGem.color = color;

        graphics = GetComponentsInChildren<Graphic>(true);
        originalColors = new Color[graphics.Length];
        for (int i = 0; i < graphics.Length; i++) {
            originalColors[i] = graphics[i].color;
        }
    }

    public CardInfo GetCardInfo() {
        return new CardInfo {
            cardId = CardId,
            itemIndex = ItemIndex,
            name = nameText != null ? nameText.text : string.Empty,
            description = descriptionText != null ? descriptionText.text : string.Empty,
            effectType = cardSO != null ? cardSO.effectType : default,
            rarity = cardSO != null ? cardSO.rarity : default,
            targetType = TargetType,
        };
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (isDragging) return;
        isHovered = true;
        targetOffset = hoverOffsetY;
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (isDragging) return;
        isHovered = false;
        targetOffset = 0f;
    }

    public void OnBeginDrag(PointerEventData eventData) {
        isDragging = true;
        isHovered = false;
        targetOffset = 0f;

        var le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        dragStartLocalPos = transform.localPosition;
    }

    public void OnDrag(PointerEventData eventData) {
        RectTransform parentRT = (RectTransform)transform.parent;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRT, eventData.position, eventData.pressEventCamera, out localPoint);
        transform.localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData) {
        float upDistance = transform.localPosition.y - dragStartLocalPos.y;

        if (upDistance >= dragThreshold) {
            OnCardDismissed?.Invoke(this);
            Destroy(gameObject);
        } else {
            var le = GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = false;
            isDragging = false;
            lastAppliedOffset = 0f;
            currentOffset = 0f;
        }
    }

    private void Update() {
        if (isDragging) return;

        currentOffset = Mathf.Lerp(currentOffset, targetOffset, Time.deltaTime * lerpSpeed);

        if (graphics != null) {
            float targetBrightness = isHovered ? hoverBrightness : 1f;
            for (int i = 0; i < graphics.Length; i++) {
                if (graphics[i] != null) {
                    Color targetColor = originalColors[i] * targetBrightness;
                    graphics[i].color = Color.Lerp(graphics[i].color, targetColor, Time.deltaTime * lerpSpeed);
                }
            }
        }
    }

    private void OnWillRenderCanvases() {
        if (isDragging) return;

        Vector3 pos = transform.localPosition;
        pos.y = (pos.y - lastAppliedOffset) + currentOffset;
        lastAppliedOffset = currentOffset;
        transform.localPosition = pos;
    }

}
