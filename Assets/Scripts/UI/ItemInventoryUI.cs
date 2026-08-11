using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInventoryUI : MonoBehaviour {


    [SerializeField] private Transform slot0Container;
    [SerializeField] private Transform slot1Container;
    [SerializeField] private Image slot0Icon;
    [SerializeField] private Image slot1Icon;
    [SerializeField] private TextMeshProUGUI slot0NameText;
    [SerializeField] private TextMeshProUGUI slot1NameText;
    [SerializeField] private GameObject slot0EmptyVisual;
    [SerializeField] private GameObject slot1EmptyVisual;


    private int prevSlot0 = -1;
    private int prevSlot1 = -1;


    private void Start() {
        if (Player.LocalInstance != null) {
            Player_OnAnyPlayerSpawned(null, System.EventArgs.Empty);
        } else {
            Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
        }
    }

    private void Player_OnAnyPlayerSpawned(object sender, System.EventArgs e) {
        if (Player.LocalInstance != null) {
            UpdateVisual();
        }
    }

    private void Update() {
        if (Player.LocalInstance != null) {
            UpdateVisual();
        }
    }

    private void UpdateVisual() {
        Player player = Player.LocalInstance;

        int slot0 = player.GetItemSlot(0);
        int slot1 = player.GetItemSlot(1);

        // Detect new item obtained
        if (slot0 != prevSlot0 && slot0 != -1) {
            SoundManager.Instance.PlayPickupSound(player.transform.position);
        }
        if (slot1 != prevSlot1 && slot1 != -1) {
            SoundManager.Instance.PlayPickupSound(player.transform.position);
        }

        prevSlot0 = slot0;
        prevSlot1 = slot1;

        int selectedSlot = player.GetSelectedSlot();

        // Slot 0
        UpdateSlot(slot0, slot0Icon, slot0NameText, slot0EmptyVisual, slot0Container, selectedSlot == 0);

        // Slot 1
        UpdateSlot(slot1, slot1Icon, slot1NameText, slot1EmptyVisual, slot1Container, selectedSlot == 1);
    }

    private void UpdateSlot(int itemIndex, Image icon, TextMeshProUGUI nameText, GameObject emptyVisual, Transform container, bool isSelected) {
        // Highlight selected slot
        container.localScale = isSelected ? Vector3.one * 1.15f : Vector3.one;

        if (itemIndex != -1) {
            SabotageItemSO item = KitchenGameMultiplayer.Instance.GetSabotageItemFromIndex(itemIndex);
            if (item != null) {
                icon.gameObject.SetActive(true);
                nameText.gameObject.SetActive(true);
                emptyVisual.SetActive(false);
                icon.sprite = item.icon;
                icon.color = item.icon != null ? Color.white : new Color(1f, 0.8f, 0.2f, 0.8f);
                float alpha = isSelected ? 1f : 0.6f;
                icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, alpha);
                nameText.alpha = alpha;
                nameText.text = item.itemName;
            }
        } else {
            icon.gameObject.SetActive(false);
            nameText.gameObject.SetActive(false);
            emptyVisual.SetActive(true);
        }
    }


}
