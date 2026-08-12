// ItemBoxCounterVisual depends on ItemBoxCounter which has been disabled.
// This file is kept for reference but the class is disabled.
/*
using TMPro;
using UnityEngine;

public class ItemBoxCounterVisual : MonoBehaviour {


    [SerializeField] private ItemBoxCounter itemBoxCounter;
    [SerializeField] private MeshRenderer cubeMeshRenderer;
    [SerializeField] private TextMeshPro cooldownText;
    [SerializeField] private GameObject readyVisual;


    private Material material;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");


    private void Start() {
        if (cubeMeshRenderer != null) {
            material = cubeMeshRenderer.material;
        }

        UpdateVisual();
    }

    private void Update() {
        UpdateVisual();
    }

    private void UpdateVisual() {
        if (itemBoxCounter == null) return;

        float cooldown = itemBoxCounter.GetCooldownTimer();

        if (cooldown <= 0f) {
            // Ready
            if (material != null) {
                material.SetColor(BaseColor, new Color(0.3f, 0.9f, 0.2f));
            }

            if (cooldownText != null) {
                cooldownText.gameObject.SetActive(false);
            }

            if (readyVisual != null) {
                readyVisual.SetActive(true);
            }
        } else {
            // Cooling down
            if (material != null) {
                float t = Mathf.Clamp01(cooldown / 10f);
                material.SetColor(BaseColor, new Color(0.9f, 0.3f, 0.2f) * (0.5f + t * 0.5f));
            }

            if (cooldownText != null) {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = Mathf.CeilToInt(cooldown).ToString();
            }

            if (readyVisual != null) {
                readyVisual.SetActive(false);
            }
        }
    }


}
*/
