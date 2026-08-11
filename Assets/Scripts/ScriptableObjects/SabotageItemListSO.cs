using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SabotageItemList", menuName = "KitchenFight/SabotageItemList")]
public class SabotageItemListSO : ScriptableObject {

    public List<SabotageItemSO> sabotageItemList;

    public int GetIndex(SabotageItemSO item) {
        return sabotageItemList.IndexOf(item);
    }

    public SabotageItemSO GetFromIndex(int index) {
        if (index < 0 || index >= sabotageItemList.Count) {
            return null;
        }
        return sabotageItemList[index];
    }

}
