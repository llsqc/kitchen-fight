using UnityEngine;

// 枚举已迁移至 CardSO.cs
// 此文件保留以兼容旧 .asset 文件，新卡牌请使用 CardSO

[CreateAssetMenu(fileName = "SabotageItem", menuName = "KitchenFight/SabotageItem")]
public class SabotageItemSO : ScriptableObject {

    public string itemName;
    public EffectType effectType;
    public Rarity rarity;
    public TargetType targetType;
    public float duration;
    public Sprite icon;

}
