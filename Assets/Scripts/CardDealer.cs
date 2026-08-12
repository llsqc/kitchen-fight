using System.Collections.Generic;
using UnityEngine;

public static class CardDealer {

    private static readonly float[] rarityThresholds = { 0.6f, 0.9f }; // Common 60%, Rare 30%, Epic 10%


    public static int PickRandomCardIndex(CardListSO cardList, GameMode gameMode) {
        if (cardList == null || cardList.cardList.Count == 0) return -1;

        List<int> pool = new List<int>();
        for (int i = 0; i < cardList.cardList.Count; i++) {
            if (cardList.cardList[i].gameMode == gameMode) {
                pool.Add(i);
            }
        }

        if (pool.Count == 0) return -1;
        return PickByRarity(cardList, pool);
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

        return PickByRarity(cardList, pool);
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

        Rarity targetRarity;
        if (scoreGap <= 2) {
            targetRarity = Random.value < 0.5f ? Rarity.Common : Rarity.Rare;
        } else if (scoreGap <= 5) {
            targetRarity = Rarity.Rare;
        } else {
            targetRarity = Rarity.Epic;
        }

        var candidates = new List<int>();
        foreach (var idx in pool) {
            if (cardList.cardList[idx].rarity == targetRarity) {
                candidates.Add(idx);
            }
        }

        if (candidates.Count == 0) {
            foreach (var idx in pool) {
                if (cardList.cardList[idx].rarity == Rarity.Common) {
                    candidates.Add(idx);
                }
            }
        }

        if (candidates.Count == 0) {
            return pool[Random.Range(0, pool.Count)];
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private static int PickByRarity(CardListSO cardList, List<int> pool) {
        float roll = Random.value;
        Rarity targetRarity = roll < rarityThresholds[0]
            ? Rarity.Common
            : (roll < rarityThresholds[1] ? Rarity.Rare : Rarity.Epic);

        var candidates = new List<int>();
        foreach (var idx in pool) {
            if (cardList.cardList[idx].rarity == targetRarity) {
                candidates.Add(idx);
            }
        }

        if (candidates.Count == 0) {
            foreach (var idx in pool) {
                if (cardList.cardList[idx].rarity == Rarity.Common) {
                    candidates.Add(idx);
                }
            }
        }

        if (candidates.Count == 0) {
            return pool[Random.Range(0, pool.Count)];
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

}
