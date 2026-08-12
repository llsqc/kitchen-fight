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

    public int CardId { get; private set; }

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

    public void SetCardId(int id) {
        CardId = id;
    }

    public CardInfo GetCardInfo() {
        return new CardInfo {
            cardId = CardId,
            name = nameText != null ? nameText.text : string.Empty,
            description = descriptionText != null ? descriptionText.text : string.Empty,
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
