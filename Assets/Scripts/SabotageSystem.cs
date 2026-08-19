using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SabotageSystem : NetworkBehaviour {

    private const int OPENING_HAND_SIZE = 3;
    private const float CARD_DEAL_INTERVAL = 8f;
    // 追赶补偿与狂热阶段已停用，保留原参数便于后续恢复。
    // private const float FRENZY_START = 130f;
    // private const float COMPENSATION_INTERVAL = 45f;
    // private const float COMPENSATION_INTERVAL_FRENZY = 30f;
    // private const float COMPENSATION_COOLDOWN = 45f;

    private float cardDealTimer = 0f;
    // private float compensationTimer = 0f;
    // private float[] teamCompensationCooldown = { 0f, 0f };
    private bool hasStarted = false;


    private void Update() {
        if (!IsServer) return;
        if (!KitchenGameManager.Instance.IsGamePlaying()) {
            hasStarted = false;
            return;
        }

        if (!hasStarted) {
            hasStarted = true;
            DealOpeningHands();
            cardDealTimer = CARD_DEAL_INTERVAL;
            // compensationTimer = COMPENSATION_INTERVAL;
        }

        float deltaTime = Time.deltaTime;

        cardDealTimer -= deltaTime;
        if (cardDealTimer <= 0f) {
            cardDealTimer += CARD_DEAL_INTERVAL;
            DealRegularCards();
        }

        /* 落后队伍补偿发牌与狂热阶段已停用。
        if (!KitchenGameMultiplayer.playMultiplayer) return;

        for (int i = 0; i < 2; i++) {
            if (teamCompensationCooldown[i] > 0f) {
                teamCompensationCooldown[i] -= deltaTime;
            }
        }

        compensationTimer -= deltaTime;
        if (compensationTimer <= 0f) {
            float interval = IsFrenzyPeriod() ? COMPENSATION_INTERVAL_FRENZY : COMPENSATION_INTERVAL;
            compensationTimer = interval;
            CheckCompensation();
        }
        */
    }

    private void DealOpeningHands() {
        GameMode gameMode = KitchenGameMultiplayer.playMultiplayer
            ? GameMode.Multiplayer
            : GameMode.Single;

        int playerCount = KitchenGameMultiplayer.Instance.GetPlayerDataCount();
        for (int i = 0; i < playerCount; i++) {
            PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(i);
            Player player = GetPlayerByClientId(playerData.clientId);
            if (player == null) continue;

            var dealtEffectTypes = new HashSet<EffectType>();
            bool hasLegendary = false;
            for (int cardIndex = 0; cardIndex < OPENING_HAND_SIZE; cardIndex++) {
                int itemIndex = CardDealer.PickRandomCardIndex(
                    KitchenGameMultiplayer.Instance.GetCardListSO(),
                    gameMode,
                    dealtEffectTypes,
                    hasLegendary);
                if (itemIndex == -1) break;

                player.DealCard(itemIndex);
                CardSO dealtCard = KitchenGameMultiplayer.Instance.GetCardFromIndex(itemIndex);
                dealtEffectTypes.Add(dealtCard.effectType);
                hasLegendary |= dealtCard.rarity == Rarity.Legendary;
            }
        }
    }

    private void DealRegularCards() {
        GameMode gameMode = KitchenGameMultiplayer.playMultiplayer
            ? GameMode.Multiplayer
            : GameMode.Single;

        int playerCount = KitchenGameMultiplayer.Instance.GetPlayerDataCount();
        for (int i = 0; i < playerCount; i++) {
            PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(i);
            Player player = GetPlayerByClientId(playerData.clientId);
            if (player == null) continue;

            int itemIndex = CardDealer.PickRandomCardIndex(
                KitchenGameMultiplayer.Instance.GetCardListSO(),
                gameMode);
            if (itemIndex != -1) {
                player.DealCard(itemIndex);
            }
        }
    }

    /* 落后队伍补偿发牌已停用。
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
                    int itemIndex = CardDealer.PickCompensationCardIndex(
                        KitchenGameMultiplayer.Instance.GetCardListSO(), GameMode.Multiplayer, gap);
                    if (itemIndex != -1) {
                        player.DealCard(itemIndex);
                    }
                }
            }
        }

        teamCompensationCooldown[losingTeam] = COMPENSATION_COOLDOWN;
    }
    */

    private Player GetPlayerByClientId(ulong clientId) {
        foreach (var player in FindObjectsByType<Player>(FindObjectsSortMode.None)) {
            if (player.OwnerClientId == clientId) {
                return player;
            }
        }
        return null;
    }

    /* 狂热阶段已停用。
    public bool IsFrenzyPeriod() {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return false;
        float elapsed = 180f - KitchenGameManager.Instance.GetGamePlayingTimerNormalized() * 180f;
        return elapsed >= FRENZY_START;
    }
    */

}
