using UnityEngine;

public enum EffectType {
    Stun,
    ReverseControls,
    LockCounter,
    CleanWipe,
}

public enum Rarity {
    Common,
    Rare,
    Epic,
}

public enum TargetType {
    Player,
    Counter,
    Self,
}

[CreateAssetMenu(fileName = "SabotageItem", menuName = "KitchenFight/SabotageItem")]
public class SabotageItemSO : ScriptableObject {

    public string itemName;
    public EffectType effectType;
    public Rarity rarity;
    public TargetType targetType;
    public float duration;
    public Sprite icon;

}
