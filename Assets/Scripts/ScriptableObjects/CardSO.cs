using System.Collections.Generic;
using UnityEngine;

// ── 枚举定义（从 SabotageItemSO.cs 迁移至此） ──

public enum GameMode {
    Single,
    Multiplayer,
}

public enum CardCategory {
    Buff,       // 增益（正面效果）
    Debuff,     // 干扰（负面效果）
    Counter,    // 反制（防御/反弹）
}

public enum EffectType {
    // ── 单人·Buff ──
    MoveSpeedUp,
    InteractionSpeedUp,
    DoubleScore,
    InstantSubmitAllRecipes,
    InstantComplete,
    SelfClean,

    // ── 多人·Debuff（保留原有） ──
    Stun,
    ReverseControls,
    LockCounter,

    // ── 多人·Counter ──
    Shield,
    Reflect,

    // ── 兼容旧代码 ──
    CleanWipe,
}

public enum Rarity {
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
}

public enum TargetType {
    Self,
    Player,
    Counter,
    Teammate,
}

// ── 卡牌数据 ──

[CreateAssetMenu(fileName = "Card", menuName = "KitchenFight/Card")]
public class CardSO : ScriptableObject {

    [Header("身份信息")]
    public string cardName;
    [TextArea] public string description;
    public Sprite icon;
    public Color placeholderColor = Color.white;

    [Header("分类标签")]
    public GameMode gameMode;
    public CardCategory cardCategory;
    public Rarity rarity;

    [Header("执行参数")]
    public EffectType effectType;
    public TargetType targetType;
    public float duration;       // 持续时间（秒），0 = 即时效果
    public float magnitude;      // 效果强度（1.5 = 1.5倍，2 = 翻倍）

}
