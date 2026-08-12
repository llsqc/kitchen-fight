using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardList", menuName = "KitchenFight/CardList")]
public class CardListSO : ScriptableObject {

    public List<CardSO> cardList;

    public int GetIndex(CardSO card) {
        return cardList.IndexOf(card);
    }

    public CardSO GetFromIndex(int index) {
        if (index < 0 || index >= cardList.Count) return null;
        return cardList[index];
    }

    public List<CardSO> GetCardsByMode(GameMode mode) {
        return cardList.FindAll(c => c.gameMode == mode);
    }

    public List<CardSO> GetCardsByModeAndCategory(GameMode mode, CardCategory category) {
        return cardList.FindAll(c => c.gameMode == mode && c.cardCategory == category);
    }

}
