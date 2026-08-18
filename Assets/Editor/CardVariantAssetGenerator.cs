#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CardVariantAssetGenerator {

    private const string CardsRoot = "Assets/ScriptableObjects/Cards";
    private const string VariantsFolder = CardsRoot + "/Variants";
    private const string CardListPath = "Assets/ScriptableObjects/CardList.asset";

    [MenuItem("KitchenFight/Cards/Generate Rarity Variants")]
    public static void Generate() {
        RecreateVariantsFolder();

        var cards = new List<CardSO>();

        // 单人：加速跑鞋（白 / 蓝 / 金）
        Add(cards, "Single_MoveSpeed_Common", "轻便跑鞋", "加速跑鞋.asset",
            GameMode.Single, CardCategory.Buff, Rarity.Common, EffectType.MoveSpeedUp, TargetType.Self, 3f, 1.2f);
        Add(cards, "Single_MoveSpeed_Rare", "疾风跑鞋", "加速跑鞋.asset",
            GameMode.Single, CardCategory.Buff, Rarity.Rare, EffectType.MoveSpeedUp, TargetType.Self, 10f, 1.5f);
        Add(cards, "Single_MoveSpeed_Legendary", "神速跑鞋", "加速跑鞋.asset",
            GameMode.Single, CardCategory.Buff, Rarity.Legendary, EffectType.MoveSpeedUp, TargetType.Self, 16f, 2f);

        // 单人：灵巧之手（绿 / 紫 / 金）
        Add(cards, "Single_Interaction_Uncommon", "灵巧之手", "灵巧之手.asset",
            GameMode.Single, CardCategory.Buff, Rarity.Uncommon, EffectType.InteractionSpeedUp, TargetType.Self, 7f, 2f);
        Add(cards, "Single_Interaction_Epic", "大师手艺", "灵巧之手.asset",
            GameMode.Single, CardCategory.Buff, Rarity.Epic, EffectType.InteractionSpeedUp, TargetType.Self, 10f, 3f);
        Add(cards, "Single_Interaction_Legendary", "厨神之手", "灵巧之手.asset",
            GameMode.Single, CardCategory.Buff, Rarity.Legendary, EffectType.InteractionSpeedUp, TargetType.Self, 15f, 4f);

        // 单人：得分增益（蓝 / 紫 / 金）
        Add(cards, "Single_Score_Rare", "双倍得分", "双倍得分.asset",
            GameMode.Single, CardCategory.Buff, Rarity.Rare, EffectType.DoubleScore, TargetType.Self, 10f, 2f);
        Add(cards, "Single_Score_Epic", "黄金时段", "双倍得分.asset",
            GameMode.Single, CardCategory.Buff, Rarity.Epic, EffectType.DoubleScore, TargetType.Self, 20f, 2f);
        Add(cards, "Single_Score_Legendary", "三倍盛宴", "双倍得分.asset",
            GameMode.Single, CardCategory.Buff, Rarity.Legendary, EffectType.DoubleScore, TargetType.Self, 20f, 3f);

        // 单人：菜单提交（蓝 / 紫 / 金；magnitude=0 表示全部）
        Add(cards, "Single_Submit_Rare", "快速上菜", "一键上菜.asset",
            GameMode.Single, CardCategory.Buff, Rarity.Rare, EffectType.InstantSubmitAllRecipes, TargetType.Self, 0f, 1f);
        Add(cards, "Single_Submit_Epic", "批量上菜", "一键上菜.asset",
            GameMode.Single, CardCategory.Buff, Rarity.Epic, EffectType.InstantSubmitAllRecipes, TargetType.Self, 0f, 2f);
        Add(cards, "Single_Submit_Legendary", "一键上菜", "一键上菜.asset",
            GameMode.Single, CardCategory.Buff, Rarity.Legendary, EffectType.InstantSubmitAllRecipes, TargetType.Self, 0f, 0f);

        // 单人：固定功能牌
        Add(cards, "Single_InstantComplete_Rare", "完美料理", "完美料理.asset",
            GameMode.Single, CardCategory.Buff, Rarity.Rare, EffectType.InstantComplete, TargetType.Self, 0f, 1f);

        // 多人：队友加速跑鞋（白 / 蓝 / 金）
        Add(cards, "Multi_MoveSpeed_Common", "轻便跑鞋（队友）", "加速跑鞋_多人.asset",
            GameMode.Multiplayer, CardCategory.Buff, Rarity.Common, EffectType.MoveSpeedUp, TargetType.Teammate, 3f, 1.2f);
        Add(cards, "Multi_MoveSpeed_Rare", "疾风跑鞋（队友）", "加速跑鞋_多人.asset",
            GameMode.Multiplayer, CardCategory.Buff, Rarity.Rare, EffectType.MoveSpeedUp, TargetType.Teammate, 10f, 1.5f);
        Add(cards, "Multi_MoveSpeed_Legendary", "神速跑鞋（队友）", "加速跑鞋_多人.asset",
            GameMode.Multiplayer, CardCategory.Buff, Rarity.Legendary, EffectType.MoveSpeedUp, TargetType.Teammate, 16f, 2f);

        // 多人：队友灵巧之手（绿 / 紫 / 金）
        Add(cards, "Multi_Interaction_Uncommon", "灵巧之手（队友）", "灵巧之手_多人.asset",
            GameMode.Multiplayer, CardCategory.Buff, Rarity.Uncommon, EffectType.InteractionSpeedUp, TargetType.Teammate, 7f, 2f);
        Add(cards, "Multi_Interaction_Epic", "大师手艺（队友）", "灵巧之手_多人.asset",
            GameMode.Multiplayer, CardCategory.Buff, Rarity.Epic, EffectType.InteractionSpeedUp, TargetType.Teammate, 10f, 3f);
        Add(cards, "Multi_Interaction_Legendary", "厨神之手（队友）", "灵巧之手_多人.asset",
            GameMode.Multiplayer, CardCategory.Buff, Rarity.Legendary, EffectType.InteractionSpeedUp, TargetType.Teammate, 15f, 4f);

        // 多人：队友得分增益（蓝 / 紫 / 金）
        Add(cards, "Multi_Score_Rare", "双倍得分（队友）", "双倍得分_多人.asset",
            GameMode.Multiplayer, CardCategory.Buff, Rarity.Rare, EffectType.DoubleScore, TargetType.Teammate, 10f, 2f);
        Add(cards, "Multi_Score_Epic", "黄金时段（队友）", "双倍得分_多人.asset",
            GameMode.Multiplayer, CardCategory.Buff, Rarity.Epic, EffectType.DoubleScore, TargetType.Teammate, 20f, 2f);
        Add(cards, "Multi_Score_Legendary", "三倍盛宴（队友）", "双倍得分_多人.asset",
            GameMode.Multiplayer, CardCategory.Buff, Rarity.Legendary, EffectType.DoubleScore, TargetType.Teammate, 20f, 3f);

        // 多人：眩晕（绿 / 紫 / 金）
        Add(cards, "Multi_Stun_Uncommon", "短暂眩晕", "Stun.asset",
            GameMode.Multiplayer, CardCategory.Debuff, Rarity.Uncommon, EffectType.Stun, TargetType.Player, 1f, 0f);
        Add(cards, "Multi_Stun_Epic", "强力眩晕", "Stun.asset",
            GameMode.Multiplayer, CardCategory.Debuff, Rarity.Epic, EffectType.Stun, TargetType.Player, 2.25f, 0f);
        Add(cards, "Multi_Stun_Legendary", "绝对眩晕", "Stun.asset",
            GameMode.Multiplayer, CardCategory.Debuff, Rarity.Legendary, EffectType.Stun, TargetType.Player, 3.5f, 0f);

        // 多人：反向操作（白 / 蓝 / 金）
        Add(cards, "Multi_Reverse_Common", "轻度混乱", "Reverse.asset",
            GameMode.Multiplayer, CardCategory.Debuff, Rarity.Common, EffectType.ReverseControls, TargetType.Player, 4f, 0f);
        Add(cards, "Multi_Reverse_Rare", "反向操作", "Reverse.asset",
            GameMode.Multiplayer, CardCategory.Debuff, Rarity.Rare, EffectType.ReverseControls, TargetType.Player, 8f, 0f);
        Add(cards, "Multi_Reverse_Legendary", "颠倒世界", "Reverse.asset",
            GameMode.Multiplayer, CardCategory.Debuff, Rarity.Legendary, EffectType.ReverseControls, TargetType.Player, 14f, 0f);

        // 多人：锁定台面（绿 / 蓝 / 紫）
        Add(cards, "Multi_Lock_Uncommon", "临时封锁", "Lock.asset",
            GameMode.Multiplayer, CardCategory.Debuff, Rarity.Uncommon, EffectType.LockCounter, TargetType.Counter, 5f, 0f);
        Add(cards, "Multi_Lock_Rare", "台面锁定", "Lock.asset",
            GameMode.Multiplayer, CardCategory.Debuff, Rarity.Rare, EffectType.LockCounter, TargetType.Counter, 9f, 0f);
        Add(cards, "Multi_Lock_Epic", "强制停业", "Lock.asset",
            GameMode.Multiplayer, CardCategory.Debuff, Rarity.Epic, EffectType.LockCounter, TargetType.Counter, 14f, 0f);

        // 多人：护盾（白 / 蓝 / 金）
        Add(cards, "Multi_Shield_Common", "简易护盾", "护盾.asset",
            GameMode.Multiplayer, CardCategory.Counter, Rarity.Common, EffectType.Shield, TargetType.Self, 5f, 1f);
        Add(cards, "Multi_Shield_Rare", "强化护盾", "护盾.asset",
            GameMode.Multiplayer, CardCategory.Counter, Rarity.Rare, EffectType.Shield, TargetType.Self, 8f, 2f);
        Add(cards, "Multi_Shield_Legendary", "绝对防御", "护盾.asset",
            GameMode.Multiplayer, CardCategory.Counter, Rarity.Legendary, EffectType.Shield, TargetType.Self, 12f, 3f);

        // 多人：反弹（紫 / 金）
        Add(cards, "Multi_Reflect_Epic", "反弹", "反弹.asset",
            GameMode.Multiplayer, CardCategory.Counter, Rarity.Epic, EffectType.Reflect, TargetType.Self, 6f, 1f);
        Add(cards, "Multi_Reflect_Legendary", "镜面反射", "反弹.asset",
            GameMode.Multiplayer, CardCategory.Counter, Rarity.Legendary, EffectType.Reflect, TargetType.Self, 10f, 2f);

        // 多人：固定功能牌。必须走即时 Buff 分支，不能标记为 Counter。
        Add(cards, "Multi_SelfClean_Uncommon", "清除疲劳", "清除疲劳.asset",
            GameMode.Multiplayer, CardCategory.Buff, Rarity.Uncommon, EffectType.SelfClean, TargetType.Self, 0f, 1f);
        Add(cards, "Multi_CleanWipe_Rare", "清场反制", "CleanWipe.asset",
            GameMode.Multiplayer, CardCategory.Buff, Rarity.Rare, EffectType.CleanWipe, TargetType.Self, 0f, 0f);

        CardListSO cardList = AssetDatabase.LoadAssetAtPath<CardListSO>(CardListPath);
        if (cardList == null) {
            Debug.LogError($"CardList not found at {CardListPath}");
            return;
        }

        cardList.cardList = cards;
        EditorUtility.SetDirty(cardList);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Generated {cards.Count} rarity card variants and rebuilt CardList.");
    }

    private static void Add(
        List<CardSO> cards,
        string fileName,
        string cardName,
        string templateFile,
        GameMode gameMode,
        CardCategory category,
        Rarity rarity,
        EffectType effectType,
        TargetType targetType,
        float duration,
        float magnitude) {

        CardSO template = AssetDatabase.LoadAssetAtPath<CardSO>($"{CardsRoot}/{templateFile}");
        if (template == null) {
            Debug.LogError($"Card template not found: {templateFile}");
            return;
        }

        CardSO card = ScriptableObject.CreateInstance<CardSO>();
        card.cardName = cardName;
        card.description = string.Empty;
        card.icon = template.icon;
        card.placeholderColor = GetRarityColor(rarity);
        card.gameMode = gameMode;
        card.cardCategory = category;
        card.rarity = rarity;
        card.effectType = effectType;
        card.targetType = targetType;
        card.duration = duration;
        card.magnitude = magnitude;

        AssetDatabase.CreateAsset(card, $"{VariantsFolder}/{fileName}.asset");
        cards.Add(card);
    }

    private static void RecreateVariantsFolder() {
        if (AssetDatabase.IsValidFolder(VariantsFolder)) {
            AssetDatabase.DeleteAsset(VariantsFolder);
        }
        AssetDatabase.CreateFolder(CardsRoot, "Variants");
    }

    private static Color GetRarityColor(Rarity rarity) {
        return rarity switch {
            Rarity.Common => new Color32(242, 242, 242, 255),
            Rarity.Uncommon => new Color32(85, 200, 120, 255),
            Rarity.Rare => new Color32(77, 141, 255, 255),
            Rarity.Epic => new Color32(166, 108, 255, 255),
            Rarity.Legendary => new Color32(255, 191, 63, 255),
            _ => Color.white,
        };
    }
}
#endif
