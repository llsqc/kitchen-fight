using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour {

    private const float PLAYER_TARGET_RADIUS = 1.5f;
    private const int CIRCLE_SIZE = 96;
    private const int CIRCLE_THICKNESS = 6;
    private const float CROSSHAIR_DISPLAY_SIZE = 72f;

    private RectTransform crosshairRect;
    private Image crosshairImage;
    private LayerMask playerLayerMask;
    private bool isVisible = false;


    private void Start() {
        playerLayerMask = LayerMask.GetMask("Players");

        crosshairRect = GetComponent<RectTransform>();
        if (crosshairRect == null) {
            crosshairRect = gameObject.AddComponent<RectTransform>();
        }

        crosshairImage = gameObject.GetComponent<Image>();
        if (crosshairImage == null) {
            crosshairImage = gameObject.AddComponent<Image>();
        }
        crosshairImage.sprite = CreateCircleSprite();
        crosshairImage.color = Color.white;
        crosshairImage.raycastTarget = false;
        crosshairRect.sizeDelta = new Vector2(CROSSHAIR_DISPLAY_SIZE, CROSSHAIR_DISPLAY_SIZE);

        SetVisible(false);
    }

    private Sprite CreateCircleSprite() {
        Texture2D tex = new Texture2D(CIRCLE_SIZE, CIRCLE_SIZE, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float center = CIRCLE_SIZE / 2f;
        float outerRadius = center - 1f;
        float innerRadius = outerRadius - CIRCLE_THICKNESS;
        Color transparent = new Color(0, 0, 0, 0);

        for (int y = 0; y < CIRCLE_SIZE; y++) {
            for (int x = 0; x < CIRCLE_SIZE; x++) {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist >= innerRadius && dist <= outerRadius) {
                    tex.SetPixel(x, y, Color.white);
                } else {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, CIRCLE_SIZE, CIRCLE_SIZE), new Vector2(0.5f, 0.5f));
    }

    private void Update() {
        if (Player.LocalInstance == null) {
            SetVisible(false);
            return;
        }

        if (KitchenGameManager.Instance == null || !KitchenGameManager.Instance.IsGamePlaying()) {
            SetVisible(false);
            return;
        }

        if (!TogglePanelUI.Instance.HasPlayerTargetCard()) {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        // Follow mouse position
        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        crosshairRect.position = mousePos;

        // Check for players in range for color feedback
        Vector3 aimWorldPos = GameInput.Instance.GetMouseWorldPosition();
        bool hasTarget = false;
        Collider[] hits = Physics.OverlapSphere(aimWorldPos, PLAYER_TARGET_RADIUS, playerLayerMask);
        foreach (var hit in hits) {
            if (hit.TryGetComponent(out Player p) && p != Player.LocalInstance) {
                hasTarget = true;
                break;
            }
        }

        Color color = hasTarget ? Color.green : Color.white;
        crosshairImage.color = color;
    }

    private void SetVisible(bool visible) {
        if (isVisible == visible) return;
        isVisible = visible;
        crosshairImage.enabled = visible;
    }

}
