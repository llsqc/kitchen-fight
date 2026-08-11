using Unity.Netcode;
using UnityEngine;

public class ItemBoxCounter : BaseCounter {

    private const float COOLDOWN_MAX = 10f;

    private NetworkVariable<float> cooldownTimer = new NetworkVariable<float>(0f);


    public float GetCooldownTimer() {
        return cooldownTimer.Value;
    }

    public bool IsReady() {
        return cooldownTimer.Value <= 0f;
    }


    private void Update() {
        if (!IsServer) return;

        if (cooldownTimer.Value > 0f) {
            cooldownTimer.Value -= Time.deltaTime;
            if (cooldownTimer.Value < 0f) cooldownTimer.Value = 0f;
        }
    }

    public override void Interact(Player player) {
        TryGetItemServerRpc(player.GetNetworkObject());
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryGetItemServerRpc(NetworkObjectReference playerRef) {
        if (cooldownTimer.Value > 0f) return;

        if (!playerRef.TryGet(out NetworkObject playerNetObj)) return;
        Player player = playerNetObj.GetComponent<Player>();

        int emptySlot = player.GetEmptySlot();
        if (emptySlot == -1) return; // Inventory full

        int itemIndex = PickRandomItemIndex();
        if (itemIndex == -1) return;

        player.SetItemSlot(emptySlot, itemIndex);
        cooldownTimer.Value = COOLDOWN_MAX;
    }

    private int PickRandomItemIndex() {
        SabotageItemListSO list = KitchenGameMultiplayer.Instance.GetSabotageItemListSO();
        if (list == null || list.sabotageItemList.Count == 0) return -1;

        float roll = Random.value;
        Rarity targetRarity;
        if (roll < 0.6f) {
            targetRarity = Rarity.Common;
        } else if (roll < 0.9f) {
            targetRarity = Rarity.Rare;
        } else {
            targetRarity = Rarity.Epic;
        }

        // Find items of target rarity
        var candidates = new System.Collections.Generic.List<int>();
        for (int i = 0; i < list.sabotageItemList.Count; i++) {
            if (list.sabotageItemList[i].rarity == targetRarity) {
                candidates.Add(i);
            }
        }

        // Fallback to Common if no items of rolled rarity
        if (candidates.Count == 0) {
            for (int i = 0; i < list.sabotageItemList.Count; i++) {
                if (list.sabotageItemList[i].rarity == Rarity.Common) {
                    candidates.Add(i);
                }
            }
        }

        // Fallback to any item
        if (candidates.Count == 0) {
            return Random.Range(0, list.sabotageItemList.Count);
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

}
