using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SabotageSystem : NetworkBehaviour {

    private const float OPENING_DURATION = 20f;
    private const float FRENZY_START = 130f;
    private const float COMPENSATION_INTERVAL = 45f;
    private const float COMPENSATION_INTERVAL_FRENZY = 30f;
    private const float COMPENSATION_COOLDOWN = 45f;

    private float compensationTimer = 0f;
    private float[] teamCompensationCooldown = { 0f, 0f };
    private bool hasStarted = false;


    private void Update() {
        if (!IsServer) return;
        if (!KitchenGameManager.Instance.IsGamePlaying()) {
            hasStarted = false;
            return;
        }

        if (!hasStarted) {
            hasStarted = true;
            compensationTimer = COMPENSATION_INTERVAL;
        }

        float deltaTime = Time.deltaTime;

        // Decrement cooldowns
        for (int i = 0; i < 2; i++) {
            if (teamCompensationCooldown[i] > 0f) {
                teamCompensationCooldown[i] -= deltaTime;
            }
        }

        // Compensation timer
        compensationTimer -= deltaTime;
        if (compensationTimer <= 0f) {
            float interval = IsFrenzyPeriod() ? COMPENSATION_INTERVAL_FRENZY : COMPENSATION_INTERVAL;
            compensationTimer = interval;
            CheckCompensation();
        }
    }

    private void CheckCompensation() {
        int score0 = DeliveryManager.Instance.GetTeamScore(0);
        int score1 = DeliveryManager.Instance.GetTeamScore(1);

        if (score0 == score1) return; // Tie, skip

        int losingTeam = score0 < score1 ? 0 : 1;
        int gap = Mathf.Abs(score1 - score0);

        if (teamCompensationCooldown[losingTeam] > 0f) return;

        // Give items to losing team
        int playerCount = KitchenGameMultiplayer.Instance.GetPlayerDataCount();
        for (int i = 0; i < playerCount; i++) {
            PlayerData pd = KitchenGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(i);
            if (pd.teamId == losingTeam) {
                Player player = GetPlayerByClientId(pd.clientId);
                if (player != null) {
                    int itemIndex = PickCompensationItem(gap);
                    if (itemIndex != -1) {
                        player.AddCardClientRpc(itemIndex);
                    }
                }
            }
        }

        teamCompensationCooldown[losingTeam] = COMPENSATION_COOLDOWN;
    }

    private int PickCompensationItem(int gap) {
        SabotageItemListSO list = KitchenGameMultiplayer.Instance.GetSabotageItemListSO();
        if (list == null || list.sabotageItemList.Count == 0) return -1;

        Rarity targetRarity;
        if (gap <= 2) {
            targetRarity = Random.value < 0.5f ? Rarity.Common : Rarity.Rare;
        } else if (gap <= 5) {
            targetRarity = Rarity.Rare;
        } else {
            targetRarity = Rarity.Epic;
        }

        var candidates = new List<int>();
        for (int i = 0; i < list.sabotageItemList.Count; i++) {
            if (list.sabotageItemList[i].rarity == targetRarity) {
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0) {
            for (int i = 0; i < list.sabotageItemList.Count; i++) {
                if (list.sabotageItemList[i].rarity == Rarity.Common) {
                    candidates.Add(i);
                }
            }
        }

        if (candidates.Count == 0) {
            return Random.Range(0, list.sabotageItemList.Count);
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private Player GetPlayerByClientId(ulong clientId) {
        foreach (var player in FindObjectsByType<Player>(FindObjectsSortMode.None)) {
            if (player.OwnerClientId == clientId) {
                return player;
            }
        }
        return null;
    }

    public bool IsFrenzyPeriod() {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return false;
        float elapsed = 180f - KitchenGameManager.Instance.GetGamePlayingTimerNormalized() * 180f;
        return elapsed >= FRENZY_START;
    }

}
