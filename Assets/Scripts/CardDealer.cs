using System.Collections.Generic;
using UnityEngine;

public static class CardDealer {

    private static readonly float[] normalRarityWeights = { 20f, 30f, 25f, 15f, 10f };
    private static readonly float[] mediumComebackWeights = { 10f, 20f, 30f, 25f, 15f };
    private static readonly float[] largeComebackWeights = { 5f, 10f, 25f, 30f, 30f };


    public static int PickRandomCardIndex(CardListSO cardList, GameMode gameMode) {
        return PickRandomCardIndex(cardList, gameMode, null, false);
    }

    public static int PickRandomCardIndex(
        CardListSO cardList,
        GameMode gameMode,
        ISet<EffectType> excludedEffectTypes,
        bool excludeLegendary) {
        if (cardList == null || cardList.cardList.Count == 0) return -1;

        List<int> pool = new List<int>();
        for (int i = 0; i < cardList.cardList.Count; i++) {
            CardSO card = cardList.cardList[i];
            if (card == null || card.gameMode != gameMode) continue;
            if (excludeLegendary && card.rarity == Rarity.Legendary) continue;
            if (excludedEffectTypes != null && excludedEffectTypes.Contains(card.effectType)) continue;
            pool.Add(i);
        }

        if (pool.Count == 0) return -1;
        return PickByRarity(cardList, pool, normalRarityWeights);
    }

    public static int PickRandomCardIndex(CardListSO cardList, GameMode gameMode, CardCategory category) {
        if (cardList == null || cardList.cardList.Count == 0) return -1;

        List<int> pool = new List<int>();
        for (int i = 0; i < cardList.cardList.Count; i++) {
            var card = cardList.cardList[i];
            if (card.gameMode == gameMode && card.cardCategory == category) {
                pool.Add(i);
            }
        }

        if (pool.Count == 0) {
            // Fallback: any card in this game mode
            return PickRandomCardIndex(cardList, gameMode);
        }

        return PickByRarity(cardList, pool, normalRarityWeights);
    }

    public static int PickCompensationCardIndex(CardListSO cardList, GameMode gameMode, int scoreGap) {
        if (cardList == null || cardList.cardList.Count == 0) return -1;

        List<int> pool = new List<int>();
        for (int i = 0; i < cardList.cardList.Count; i++) {
            if (cardList.cardList[i].gameMode == gameMode) {
                pool.Add(i);
            }
        }

        if (pool.Count == 0) return -1;

        float[] weights = scoreGap <= 2
            ? normalRarityWeights
            : scoreGap <= 5
                ? mediumComebackWeights
                : largeComebackWeights;

        return PickByRarity(cardList, pool, weights);
    }

    private static int PickByRarity(CardListSO cardList, List<int> pool, float[] rarityWeights) {
        bool[] availableRarities = new bool[rarityWeights.Length];
        foreach (int index in pool) {
            int rarityIndex = (int)cardList.cardList[index].rarity;
            if (rarityIndex >= 0 && rarityIndex < availableRarities.Length) {
                availableRarities[rarityIndex] = true;
            }
        }

        float totalWeight = 0f;
        for (int i = 0; i < rarityWeights.Length; i++) {
            if (availableRarities[i]) totalWeight += rarityWeights[i];
        }
        if (totalWeight <= 0f) return pool[Random.Range(0, pool.Count)];

        float roll = Random.value * totalWeight;
        Rarity targetRarity = Rarity.Common;
        for (int i = 0; i < rarityWeights.Length; i++) {
            if (!availableRarities[i]) continue;
            if (roll < rarityWeights[i]) {
                targetRarity = (Rarity)i;
                break;
            }
            roll -= rarityWeights[i];
        }

        var candidates = new List<int>();
        foreach (var idx in pool) {
            if (cardList.cardList[idx].rarity == targetRarity) {
                candidates.Add(idx);
            }
        }

        return candidates.Count > 0
            ? candidates[Random.Range(0, candidates.Count)]
            : pool[Random.Range(0, pool.Count)];
    }

}
