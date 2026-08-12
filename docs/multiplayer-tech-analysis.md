# 多人对战技术方案分析

- **状态**：技术审查报告
- **日期**：2026-08-11
- **关联**：[MVP 主设计](./multi-team-sabotage-design.md) | [Phase 2 扩展](./multi-team-sabotage-phase2.md) | [垂直切片实施方案](./multi-team-sabotage-vertical-slice.md)
- **范围**：当前多人对战技术方案全面审查，含 NGO 集成评估、缺失功能、SO 数据驱动改造建议、已知缺陷

---

## 目录

1. [技术栈总览](#1-技术栈总览)
2. [NGO 集成评估](#2-ngo-集成评估)
3. [网络架构详解](#3-网络架构详解)
4. [已实现功能清单](#4-已实现功能清单)
5. [缺失功能](#5-缺失功能)
6. [SO 数据驱动改造建议](#6-so-数据驱动改造建议)
7. [已知缺陷与风险](#7-已知缺陷与风险)
8. [改进优先级](#8-改进优先级)

---

## 1. 技术栈总览

| 层级 | 技术 | 版本 | 说明 |
|------|------|------|------|
| 引擎 | Tuanjie Engine | 1.9.3（基于 Unity 2022.3.62） | 团结引擎（中国版 Unity） |
| 渲染管线 | URP | 14.2.0-t1 | 通用渲染管线 |
| 网络框架 | NGO (Netcode for GameObjects) | 1.13.1 | Unity 官方网络方案 |
| 匹配服务 | Unity Lobby | 1.2.2 | 大厅/房间管理 |
| 中继服务 | Unity Relay | 1.0.5 | NAT 穿透中继 |
| 网络工具 | Multiplayer Tools | 1.0.0 | 网络监控/调试 |
| 传输层 | UnityTransport (UTP) | 随 NGO | 支持 DTLS 加密 |
| 输入系统 | Input System | 1.14.4-t3 | 新版输入系统（Active Input Handler = 2） |
| UI | TextMeshPro | 3.0.10 | 文本渲染 |
| 相机 | Cinemachine | 2.10.7 | 虚拟相机 |

### 项目场景流

```
MainMenuScene → LobbyScene → CharacterSelectScene → LoadingScene → GameScene
```

- 5 个场景，均在 Build Settings 中启用
- 仅 CharacterSelectScene 允许新玩家连接（连接审批回调强制校验）
- GameScene 加载完成后服务端 `SpawnAsPlayerObject` 生成所有玩家

---

## 2. NGO 集成评估

### 2.1 NGO 选择评估

**结论：NGO 已选型并深度集成，方案合理。**

| 评估维度 | NGO 表现 | 项目现状 |
|----------|----------|----------|
| 官方支持 | Unity 官方维护，Tuanjie 兼容 | ✅ 1.13.1 稳定版 |
| 学习曲线 | 中等（ServerRpc/ClientRpc 模式清晰） | ✅ 全团队已掌握 |
| 生态集成 | Lobby + Relay + Tools 一体化 | ✅ 已全部安装 |
| 权威模型 | 灵活（可服务端/客户端权威） | ✅ 混合：状态服务端权威 + 移动客户端权威 |
| 性能 | 中等（适合 4 人小规模） | ✅ 4 人 2v2 完全胜任 |
| 反作弊 | 弱（需自行实现服务端校验） | ⚠️ 客户端权威移动，无校验 |
| Host 迁移 | 不支持 | ❌ Host 断线游戏中断 |
| 可扩展性 | 适合中小型游戏 | ✅ 当前规模足够 |

### 2.2 NGO 核心模式使用

```
┌─ NetworkList<PlayerData> ──── 玩家数据同步（clientId/colorId/teamId/playerName）
├─ NetworkVariable<T> ────────── 状态/计时器/分数/槽位同步
├─ ServerRpc(RequireOwnership=false) ─ 客户端→服务端请求（无所有权限制）
├─ ClientRpc ─────────────────── 服务端→所有客户端广播
├─ ClientNetworkTransform ────── 客户端权威位置同步
├─ OwnerNetworkAnimator ──────── 客户端权威动画同步
├─ NetworkSceneManager ────────── 网络场景加载（LoadScene + OnLoadEventCompleted）
├─ ConnectionApproval ─────────── 连接审批（人数上限 + 场景校验）
└─ NetworkPrefabs ─────────────── DefaultNetworkPrefabs.asset（24 个注册项）
```

### 2.3 未使用的 NGO 功能

| NGO 功能 | 说明 | 是否需要 |
|----------|------|----------|
| NetworkTransform（服务端权威） | 项目用 ClientNetworkTransform 代替 | 当前不需要，但反作弊改造时需要 |
| NetworkAnimator（服务端权威） | 项目用 OwnerNetworkAnimator 代替 | 同上 |
| NetworkRigidbody | 物理同步 | Phase 2 位移道具可能需要 |
| SceneEventProgress | 场景加载进度 | 当前用 LoadingScene 代替，可改进 |
| SessionManagement | 会话管理/重连 | ⚠️ 需要补充 |
| SoftSync | 软同步（对象池） | Phase 2 蟑螂大军等需要对象池 |
| Interest Management | 网络兴趣管理 | 4 人小规模不需要 |
| TickRate 自定义 | 服务端 Tick 频率 | 当前默认 30Hz 足够 |

---

## 3. 网络架构详解

### 3.1 连接流程

```
方式 A：Unity Relay + Lobby（云匹配）
  主机: CreateLobby → AllocateRelay → SetRelayServerData(DTLS) → StartHost → Load CharacterSelectScene
  客户: QuickJoin/JoinWithCode → JoinRelay → SetRelayServerData(DTLS) → StartClient

方式 B：直连 IP
  主机: ConfigureDirectTransport(0.0.0.0:7777) → StartHost → Load CharacterSelectScene
  客户: ConfigureDirectTransport(hostIP:7777) → StartClient

方式 C：单人模式
  playMultiplayer=false → StartHost → Load GameScene（跳过角色选择）
```

### 3.2 权威模型

| 系统 | 权威方 | 同步方式 | 安全性 |
|------|--------|----------|--------|
| 玩家移动 | **客户端** | ClientNetworkTransform | ⚠️ 无校验，可作弊 |
| 玩家动画 | **客户端** | OwnerNetworkAnimator | ⚠️ 无校验 |
| 游戏状态 | 服务端 | NetworkVariable\<State\> | ✅ 安全 |
| 游戏计时器 | 服务端 | NetworkVariable\<float\> | ✅ 安全 |
| 队伍分数 | 服务端 | NetworkVariable\<int\> | ✅ 安全 |
| 玩家数据 | 服务端 | NetworkList\<PlayerData\> | ✅ 安全 |
| 道具槽位 | 服务端 | NetworkVariable\<int\> | ✅ 安全 |
| 厨房对象 | 服务端 | ServerRpc→Spawn(true) | ✅ 安全 |
| 效果计时器 | 服务端 | NetworkVariable\<float\> | ✅ 安全 |
| 交互请求 | 客户端→服务端 | ServerRpc(RequireOwnership=false) | ✅ 服务端校验 |

### 3.3 RPC 调用链模式

项目中所有网络交互遵循统一模式：

```
客户端发起 → ServerRpc(RequireOwnership=false) → 服务端校验+执行 → ClientRpc 广播
```

示例（道具使用）：
```
Player.UseItemServerRpc(slotIndex, aimPosition, counterRef)
  → 服务端校验：槽位是否持有道具
  → 服务端执行：OverlapSphere 检测目标 → EffectHost.ApplyEffect
  → 服务端清除槽位：itemSlot0/1.Value = -1（NetworkVariable 自动同步）
```

### 3.4 关键数据结构

```csharp
// 玩家数据（INetworkSerializable，通过 NetworkList 同步）
public struct PlayerData : INetworkSerializable {
    public ulong clientId;
    public int colorId;
    public int teamId;
    public FixedString64Bytes playerName;
    public FixedString64Bytes playerId;
}

// 道具定义（ScriptableObject）
public class SabotageItemSO : ScriptableObject {
    public string itemName;
    public EffectType effectType;     // Stun, ReverseControls, LockCounter, CleanWipe
    public Rarity rarity;             // Common, Rare, Epic
    public TargetType targetType;     // Player, Counter, Self
    public float duration;
    public Sprite icon;
}
```

---

## 4. 已实现功能清单

### 4.1 联网基础

| 功能 | 文件 | 状态 |
|------|------|------|
| NGO 网络框架集成 | KitchenGameMultiplayer.cs | ✅ |
| Lobby + Relay 云匹配 | KitchenGameLobby.cs | ✅ |
| 直连 IP 模式 | KitchenGameMultiplayer.cs | ✅ |
| 连接审批（人数+场景校验） | KitchenGameMultiplayer.cs | ✅ |
| 玩家数据同步 | PlayerData.cs + NetworkList | ✅ |
| 网络场景管理 | Loader.cs + NetworkSceneManager | ✅ |
| 网络对象生成/销毁 | KitchenGameMultiplayer.cs | ✅ |
| 玩家生成 | KitchenGameManager.cs | ✅ |
| 断线处理（移除玩家数据） | KitchenGameMultiplayer.cs | ✅ |
| 踢人 | KitchenGameMultiplayer.cs | ✅ |
| 多人暂停 | KitchenGameManager.cs | ✅ |

### 4.2 游戏逻辑

| 功能 | 文件 | 状态 |
|------|------|------|
| 游戏状态机 | KitchenGameManager.cs | ✅ |
| 倒计时→游戏→结束 | KitchenGameManager.cs | ✅ |
| 每队独立订单（镜像配方） | DeliveryManager.cs | ✅ |
| 每队独立计分 | DeliveryManager.cs | ✅ |
| 厨房柜台交互（6 类） | Counters/* | ✅ |
| 厨房对象拾取/放置/销毁 | KitchenObject.cs | ✅ |
| 盘子多食材组合 | PlateKitchenObject.cs | ✅ |
| 烹饪/烧焦状态机 | StoveCounter.cs | ✅ |
| 切菜进度 | CuttingCounter.cs | ✅ |

### 4.3 队伍系统

| 功能 | 状态 |
|------|------|
| 2v2 队伍分配（index/2） | ✅ |
| 队伍颜色（红系/蓝系） | ✅ |
| 按队分区出生点 | ✅ |
| 对称厨房布局 | ✅ |
| 每队独立配送台 | ✅ |
| 镜像配方（公平） | ✅ |

### 4.4 道具系统

| 功能 | 文件 | 状态 |
|------|------|------|
| 道具 SO 定义 | SabotageItemSO.cs | ✅ |
| 道具列表 SO | SabotageItemListSO.cs | ✅ |
| 2 槽位库存（NetworkVariable） | Player.cs | ✅ |
| 道具箱获取（稀有度权重） | ItemBoxCounter.cs | ✅ |
| 落后补偿系统 | SabotageSystem.cs | ✅ |
| EffectHost 统一接口 | IEffectHost.cs | ✅ |
| 玩家效果宿主 | PlayerEffectHost.cs | ✅ |
| 柜台效果宿主 | CounterEffectHost.cs | ✅ |
| 准星瞄准系统 | CrosshairUI + GameInput | ✅ |
| 被整保护（10s 窗口减半） | PlayerEffectHost.cs | ✅ |
| 4 个已实施道具 | Stun/Reverse/Lock/CleanWipe | ✅ |

### 4.5 UI 系统

| 功能 | 状态 |
|------|------|
| 主菜单 → 大厅 → 角色选择 → 游戏 | ✅ |
| 大厅创建/加入/列表 | ✅ |
| 角色选择 + 颜色 + 准备 | ✅ |
| 游戏倒计时 UI | ✅ |
| 游戏时钟 UI | ✅ |
| 队伍分数 UI | ✅ |
| 配送结果 UI | ✅ |
| 道具库存 UI | ✅ |
| 效果卡片 UI | ✅ |
| 准星 UI | ✅ |
| 暂停 UI（单人+多人） | ✅ |
| 游戏结束 UI | ✅ |
| Host 断线 UI | ✅ |

### 4.6 音频

| 功能 | 状态 |
|------|------|
| 事件驱动音效系统 | ✅ |
| AudioClipRefsSO 引用 | ✅ |
| 3D 位置音效 | ✅ |
| 背景音乐 | ✅ |

---

## 5. 缺失功能

### 5.1 网络层缺失

| # | 缺失功能 | 严重度 | 说明 |
|---|----------|--------|------|
| N1 | **重连机制** | 🔴 高 | 玩家断线后无法重新加入游戏。当前仅移除 PlayerData，不保留游戏状态。 |
| N2 | **Host 迁移** | 🔴 高 | Host 断线 → 全体断开。NGO 原生不支持 Host 迁移，需自建或用 Lobby Host 迁移 API。 |
| N3 | **观战模式** | 🟡 中 | 玩家断线后无观战选项，剩余玩家无法继续正常游戏（人数不对等）。 |
| N4 | **AI 填充** | 🟡 中 | 无法以少于 4 人开始游戏（无 Bot 填充空位）。 |
| N5 | **网络音频同步** | 🟡 中 | SoundManager 在各客户端本地播放，仅传 Vector3 位置参数，不做网络同步播放。当前可接受（位置近似），但对关键音效（如成功配送）可能出现客户端间不同步。 |
| N6 | **场景加载进度** | 🟢 低 | LoadingScene 使用 LoaderCallback 单帧切换，无真实加载进度条。4 人小场景可接受。 |
| N7 | **专用服务器** | 🟢 低 | 无 Headless Server 模式。当前 Host=Server+Client 架构对 4 人派对游戏可接受。 |

### 5.2 游戏逻辑缺失

| # | 缺失功能 | 严重度 | 说明 |
|---|----------|--------|------|
| G1 | **送单奖励里程碑** | 🟡 中 | 设计文档中定义但未实现。每队配送数达到里程碑时触发道具奖励。 |
| G2 | **Phase 2 道具（19 个）** | 🟡 中 | 设计文档中定义但未实现。需先完成 Spike 验证。 |
| G3 | **比赛数据持久化** | 🟡 中 | 无胜负记录、无统计、无排行榜。PlayerPrefs 仅存玩家名和 ID。 |
| G4 | **练习/教程模式** | 🟢 低 | 无单人练习模式。单人模式仅是跳过大厅直接开始。 |
| G5 | **队伍选择** | 🟢 低 | 队伍按加入顺序自动分配（index/2），玩家无法选择队伍。 |

### 5.3 配置缺失

| # | 缺失功能 | 严重度 | 说明 |
|---|----------|--------|------|
| C1 | **Unity Services 未启用** | 🔴 高 | `ProjectSettings/UnityConnectSettings.asset` 中 `m_Enabled: 0`。Lobby/Relay 服务在 ProjectSettings 中被禁用，需启用后才能使用云匹配。 |
| C2 | **Application Identifier 未改** | 🟡 中 | 仍为模板默认值 `com.UnityTechnologies.com.unity.template-starter-kit`，发布前必须修改。 |
| C3 | ** productName/companyName** | 🟢 低 | productName=KitchenFight ✅，但 companyName=DefaultCompany（需改为实际公司名）。 |

---

## 6. SO 数据驱动改造建议

### 6.1 现状分析

当前项目已有 10 个 SO 类，覆盖了道具定义、配方、食材、音效引用。但大量**游戏平衡参数和配置值**仍以 `const` 或硬编码字段散落在各脚本中，修改需要重新编译，不利于策划调优。

### 6.2 已有 SO 清单

| SO 类 | 文件 | 用途 | 数据驱动程度 |
|-------|------|------|-------------|
| KitchenObjectSO | ScriptableObjects/KitchenObjectSO.cs | 食材/物品定义 | ✅ 完全 |
| KitchenObjectListSO | ScriptableObjects/KitchenObjectListSO.cs | 食材注册表 | ✅ 完全 |
| RecipeSO | ScriptableObjects/RecipeSO.cs | 配送配方 | ✅ 完全 |
| RecipeListSO | ScriptableObjects/RecipeListSO.cs | 配方池 | ✅ 完全 |
| CuttingRecipeSO | ScriptableObjects/CuttingRecipeSO.cs | 切菜配方 | ✅ 完全 |
| FryingRecipeSO | ScriptableObjects/FryingRecipeSO.cs | 煎炸配方 | ✅ 完全 |
| BurningRecipeSO | ScriptableObjects/BurningRecipeSO.cs | 烧焦配方 | ✅ 完全 |
| SabotageItemSO | ScriptableObjects/SabotageItemSO.cs | 道具定义 | ✅ 完全 |
| SabotageItemListSO | ScriptableObjects/SabotageItemListSO.cs | 道具注册表 | ✅ 完全 |
| AudioClipRefsSO | ScriptableObjects/AudioClipRefsSO.cs | 音效引用 | ✅ 完全 |

### 6.3 建议新增 SO

以下参数当前为硬编码 `const` 或 `[SerializeField]` 私有字段，建议提取为 SO 实现数据驱动：

#### 6.3.1 GameConfigSO（游戏全局配置）

```csharp
[CreateAssetMenu(fileName = "GameConfig", menuName = "KitchenFight/GameConfig")]
public class GameConfigSO : ScriptableObject {
    [Header("游戏时长")]
    public float gamePlayingTimerMax = 180f;       // 当前: KitchenGameManager 硬编码 180f
    public float countdownToStartTimer = 3f;        // 当前: NetworkVariable 初始值 3f

    [Header("订单系统")]
    public float spawnRecipeTimerMax = 4f;          // 当前: DeliveryManager 硬编码 4f
    public int waitingRecipesMax = 4;               // 当前: DeliveryManager 硬编码 4
}
```

**当前硬编码位置**：
- `KitchenGameManager.cs` → `gamePlayingTimerMax = 180f`
- `DeliveryManager.cs` → `spawnRecipeTimerMax = 4f`, `waitingRecipesMax = 4`

---

#### 6.3.2 SabotageBalanceSO（道具平衡配置）

```csharp
[CreateAssetMenu(fileName = "SabotageBalance", menuName = "KitchenFight/SabotageBalance")]
public class SabotageBalanceSO : ScriptableObject {
    [Header("道具箱")]
    public float itemBoxCooldown = 10f;              // 当前: ItemBoxCounter.COOLDOWN_MAX
    public float commonWeight = 0.6f;                // 当前: ItemBoxCounter 硬编码 0.6f
    public float rareWeight = 0.3f;                  // 当前: 0.9 - 0.6 = 0.3
    // Epic = 1 - common - rare

    [Header("落后补偿")]
    public float openingDuration = 20f;              // 当前: SabotageSystem.OPENING_DURATION
    public float frenzyStart = 130f;                 // 当前: SabotageSystem.FRENZY_START
    public float compensationInterval = 45f;         // 当前: SabotageSystem.COMPENSATION_INTERVAL
    public float compensationIntervalFrenzy = 30f;   // 当前: SabotageSystem.COMPENSATION_INTERVAL_FRENZY
    public float compensationCooldown = 45f;         // 当前: SabotageSystem.COMPENSATION_COOLDOWN

    [Header("补偿道具稀有度阈值")]
    public int gapThresholdRare = 2;                 // gap ≤ 2 → C/R 混合
    public int gapThresholdEpic = 5;                 // gap ≤ 5 → R, gap > 5 → Epic

    [Header("被整保护")]
    public float protectionWindow = 10f;             // 当前: PlayerEffectHost.PROTECTION_WINDOW
    public float protectionHalveRatio = 0.5f;        // 当前: 硬编码 0.5f

    [Header("道具节奏")]
    public int frenzyItemCount = 3;                  // 疯狂期道具箱数量（当前: 设计文档定义但未实现动态数量）
    public int normalItemCount = 2;                  // 正常期道具箱数量
}
```

**当前硬编码位置**：
- `ItemBoxCounter.cs` → `COOLDOWN_MAX = 10f`, `0.6f`, `0.9f` 稀有度权重
- `SabotageSystem.cs` → 6 个 `const float` 常量
- `PlayerEffectHost.cs` → `PROTECTION_WINDOW = 10f`, `0.5f` 减半比例

---

#### 6.3.3 PlayerConfigSO（玩家配置）

```csharp
[CreateAssetMenu(fileName = "PlayerConfig", menuName = "KitchenFight/PlayerConfig")]
public class PlayerConfigSO : ScriptableObject {
    [Header("移动")]
    public float moveSpeed = 7f;                    // 当前: Player.moveSpeed [SerializeField]
    public float rotateSpeed = 10f;                  // 当前: Player 硬编码 10f
    public float playerRadius = 0.6f;                // 当前: Player 硬编码 0.6f

    [Header("交互")]
    public float interactDistance = 2f;             // 当前: Player 硬编码 2f

    [Header("道具瞄准")]
    public float playerTargetRadius = 1.5f;          // 当前: Player.PLAYER_TARGET_RADIUS

    [Header("库存")]
    public int inventorySlotCount = 2;               // 当前: 硬编码 2 个 NetworkVariable<int>
}
```

**当前硬编码位置**：
- `Player.cs` → `moveSpeed`, `playerRadius`, `interactDistance`, `PLAYER_TARGET_RADIUS`, `rotateSpeed`

---

#### 6.3.4 TeamConfigSO（队伍配置）

```csharp
[CreateAssetMenu(fileName = "TeamConfig", menuName = "KitchenFight/TeamConfig")]
public class TeamConfigSO : ScriptableObject {
    public int maxPlayerAmount = 4;                  // 当前: KitchenGameMultiplayer.MAX_PLAYER_AMOUNT (const)
    public int maxTeamAmount = 2;                    // 当前: KitchenGameMultiplayer.MAX_TEAM_AMOUNT (const)
    public ushort defaultPort = 7777;                // 当前: KitchenGameMultiplayer.DEFAULT_PORT (const)
    public TeamColorSet[] teamColorSets;             // 当前: KitchenGameMultiplayer.teamColorSets [SerializeField]
    public List<Color> playerColorList;              // 当前: KitchenGameMultiplayer.playerColorList [SerializeField]
    public List<Vector3> spawnPositionList;          // 当前: Player.spawnPositionList [SerializeField]
}
```

> ⚠️ 注意：`MAX_PLAYER_AMOUNT` / `MAX_TEAM_AMOUNT` 当前为 `const`，被 `KitchenGameLobby.cs` 直接引用。改为 SO 后需确保所有引用点通过 SO 实例读取。Lobby 创建时 `CreateLobbyAsync` 的 maxPlayers 参数也需要从 SO 读取。

---

#### 6.3.5 DeliveryConfigSO（配送配置）

```csharp
// 可合并到 GameConfigSO 或独立
[CreateAssetMenu(fileName = "DeliveryConfig", menuName = "KitchenFight/DeliveryConfig")]
public class DeliveryConfigSO : ScriptableObject {
    public float spawnRecipeTimerMax = 4f;
    public int waitingRecipesMax = 4;
    // Phase 2 可扩展：过期食材扣分、小费大盗分数等
}
```

---

#### 6.3.6 EffectVisualConfigSO（效果视觉配置）

```csharp
[CreateAssetMenu(fileName = "EffectVisualConfig", menuName = "KitchenFight/EffectVisualConfig")]
public class EffectVisualConfigSO : ScriptableObject {
    [Header("锁定视觉")]
    public Vector3 lockVisualOffset = new Vector3(0f, 1.45f, 0f);  // 当前: CounterEffectHost 硬编码
    public Vector3 lockVisualScale = new Vector3(1.1f, 0.08f, 1.1f); // 当前: 硬编码
    public Color lockColor = new Color(1f, 0.1f, 0.1f, 0.5f);     // 当前: 硬编码
    public float lockPulseSpeed = 4f;                               // 当前: 硬编码
    public float lockPulseMin = 0.35f;                              // 当前: 硬编码
    public float lockPulseAmplitude = 0.15f;                       // 当前: 硬编码
}
```

**当前硬编码位置**：
- `CounterEffectHost.cs` → Start() 中 CreateLockVisual() 所有参数硬编码

---

### 6.4 SO 改造总览

| SO 名称 | 提取来源 | 改造优先级 | 策划收益 |
|---------|----------|-----------|---------|
| GameConfigSO | KitchenGameManager, DeliveryManager | 🔴 高 | 可调游戏时长、订单频率 |
| SabotageBalanceSO | SabotageSystem, ItemBoxCounter, PlayerEffectHost | 🔴 高 | 可调道具平衡（核心玩法调优） |
| PlayerConfigSO | Player | 🟡 中 | 可调移动手感 |
| TeamConfigSO | KitchenGameMultiplayer | 🟡 中 | 可调队伍/人数/端口 |
| EffectVisualConfigSO | CounterEffectHost | 🟢 低 | 可调视觉效果参数 |

### 6.5 改造建议

1. **渐进式改造**：先提取 `SabotageBalanceSO`（平衡调优需求最急迫），再逐步提取其他 SO。
2. **单例引用**：各 SO 作为 `[SerializeField]` 挂到对应 NetworkBehaviour 或通过 `Resources.Load` 加载。
3. **运行时不变**：SO 在运行时不应被修改（NetworkVariable 才是运行时状态）。SO 仅作为初始化配置。
4. **编辑器可视化**：利用 `[CreateAssetMenu]` + `[Header]` 让策划在 Inspector 中直观调参。
5. **版本控制友好**：SO 是 `.asset` 文件，可被 Git 跟踪，便于平衡迭代回溯。

---

## 7. 已知缺陷与风险

### 7.1 安全缺陷

| # | 缺陷 | 风险等级 | 位置 | 说明 |
|---|------|----------|------|------|
| S1 | **客户端权威移动无校验** | 🟡 中 | Player.cs + ClientNetworkTransform.cs | 客户端直接控制位置，服务端不校验移动合法性。可飞墙/加速。4 人派对游戏可接受，但公开匹配有风险。 |
| S2 | **ServerRpc 无来源校验** | 🟡 中 | Player.UseItemServerRpc 等 | `RequireOwnership=false` 意味着任何客户端可调用。服务端仅校验槽位是否有道具，不校验"是否是自己的 Player"。恶意客户端可对其他玩家的 Player 调用 UseItemServerRpc。 |
| S3 | **OverlapSphere 服务端执行** | ✅ 低 | Player.UseItemServerRpc | 瞄准检测在服务端执行（aimPosition 由客户端传入），客户端可伪造 aimPosition。但服务端校验了道具持有，风险可控。 |

### 7.2 性能缺陷

| # | 缺陷 | 影响 | 位置 | 说明 |
|---|------|------|------|------|
| P1 | **FindObjectsByType 每帧查找** | 🟡 中 | SabotageSystem.GetPlayerByClientId | 每次补偿检查都用 `FindObjectsByType<Player>()` 遍历场景。应缓存 Player 引用或维护字典。 |
| P2 | **CounterEffectHost 运行时创建 GameObject** | 🟡 中 | CounterEffectHost.CreateLockVisual | 每个柜台（62 个）在 Start() 中 `GameObject.CreatePrimitive(PrimitiveType.Cube)` 创建锁定视觉。应改为预制体实例化或对象池。62 个 Primitive 创建会在场景加载时产生卡顿。 |
| P3 | **NetworkVariable 数量多** | 🟢 低 | PlayerEffectHost, StoveCounter 等 | 每个效果一个 NetworkVariable<float>。4 人小规模可接受，但 Phase 2 扩展效果类型后 NetworkVariable 数量会膨胀。 |

### 7.3 架构缺陷

| # | 缺陷 | 影响 | 位置 | 说明 |
|---|------|------|------|------|
| A1 | **DeliveryManager 单例 + 硬编码 2 队** | 🟡 中 | DeliveryManager.cs | `team0Score` / `team1Score` 硬编码为 2 个 NetworkVariable，`teamWaitingLists` 硬编码为长度 2 数组。扩展队伍数量需重构。 |
| A2 | **SabotageSystem 硬编码 2 队** | 🟡 中 | SabotageSystem.cs | `teamCompensationCooldown` 硬编码 `{0f, 0f}`，`CheckCompensation` 硬编码比较 score0/score1。 |
| A3 | **Player 库存硬编码 2 槽** | 🟡 中 | Player.cs | `itemSlot0` / `itemSlot1` 两个独立 NetworkVariable<int>。改为动态数量需要数组方案（NGO 的 NetworkVariable 不直接支持 List，需 NetworkList 或自定义）。 |
| A4 | **RPC 使用旧版命名** | 🟢 低 | 全项目 | 使用 `[ServerRpc]` / `[ClientRpc]` 旧版属性而非新版 `[Rpc(SendTo.Server)]` / `[Rpc(SendTo.ClientsAndHost)]`。功能正常，但 NGO 未来版本可能弃用。 |
| A5 | **静态事件泄漏风险** | 🟡 中 | Player.cs | `OnAnyPlayerSpawned` / `OnAnyPickedSomething` 为 static event。有 `ResetStaticData()` 清理方法，但需确保在场景切换时正确调用。 |
| A6 | **Time.timeScale 暂停** | 🟡 中 | KitchenGameManager.cs | 多人暂停用 `Time.timeScale = 0f`。这会影响所有客户端的 Update（包括 NetworkVariable 计时器递减）。服务端 Update 也会被影响（因为 `if (!IsServer) return` 在 Update 中，但 timeScale 影响整个引擎 Time.deltaTime）。暂停时服务端计时器也会停止，这是设计意图，但需注意。 |

### 7.4 工程缺陷

| # | 缺陷 | 影响 | 位置 | 说明 |
|---|------|------|------|------|
| E1 | **异步异常吞没** | 🟡 中 | KitchenGameLobby.cs | 所有 async void 方法中 catch 仅 `Debug.Log(e)`，不通知用户具体错误。玩家看到的是 "Join Failed" 但不知道原因（网络超时？Lobby 满？Relay 不可用？）。 |
| E2 | **无重试机制** | 🟡 中 | KitchenGameLobby.cs | Relay 分配/加入失败后不重试。网络不稳定时体验差。 |
| E3 | **useUnityServices 序列化字段** | 🟡 中 | KitchenGameLobby.cs | `[SerializeField] private bool useUnityServices` 控制是否初始化 Unity Services。如果 Inspector 中未勾选，Lobby/Relay 功能静默失效（不报错，但不工作）。 |
| E4 | **DirectConnection 不走 Lobby** | 🟢 低 | KitchenGameMultiplayer.cs | 直连模式不创建/加入 Lobby。`KitchenGameLobby.GetLobby()` 返回 null，Lobby 相关 UI 在直连模式下不可用。设计如此，但需确保 UI 正确处理 null。 |
| E5 | **playMultiplayer 静态标志** | 🟢 低 | KitchenGameMultiplayer.cs | `public static bool playMultiplayer = true` 控制是否跳过大厅。静态标志在编辑器测试时方便，但生产环境无 UI 入口切换。 |

### 7.5 已知技术债

| # | 技术债 | 说明 |
|---|--------|------|
| T1 | **项目名残留 KitchenChaos** | `metroPackageName: KitchenChaos`，代码中多处引用旧名。 |
| T2 | **Application Identifier 未改** | 仍为 `com.UnityTechnologies.com.unity.template-starter-kit`。 |
| T3 | **缺少测试** | 项目中无单元测试、无集成测试。`TestingNetcodeUI` / `TestingLobbyUI` / `TestingCharacterSelectUI` 是手动测试 UI 脚本，非自动化测试。 |
| T4 | **缺少 CI/CD** | 无持续集成配置。代码变更仅靠手动编译验证。 |
| T5 | **代码路径硬编码** | 设计文档中关联代码库路径为 `C:\Users\guoju\Desktop\KitchenFight`，实际项目在 `D:\GameDevelopment\KitchenFight-Source`。 |

---

## 8. 改进优先级

### P0 — 阻塞发布（必须修复）

| # | 改进项 | 工作量 | 说明 |
|---|--------|--------|------|
| 1 | 启用 Unity Services | 0.5h | ProjectSettings → UnityConnect → 勾选 Enable |
| 2 | 修改 Application Identifier | 0.5h | Player Settings → `com.yourcompany.kitchenfight` |
| 3 | S2：ServerRpc 来源校验 | 2h | 在 UseItemServerRpc 中校验 `serverRpcParams.Receive.SenderClientId == OwnerClientId` |

### P1 — 核心体验（强烈建议）

| # | 改进项 | 工作量 | 说明 |
|---|--------|--------|------|
| 4 | SabotageBalanceSO 提取 | 4h | 道具平衡参数数据驱动化（第 6.3.2 节） |
| 5 | GameConfigSO 提取 | 2h | 游戏时长/订单频率数据驱动化（第 6.3.1 节） |
| 6 | P1：SabotageSystem 缓存 Player | 1h | 替换 FindObjectsByType 为字典缓存 |
| 7 | P2：CounterEffectHost 改用预制体 | 2h | 替换 CreatePrimitive 为 prefab 实例化 |
| 8 | E1：Lobby 错误信息细化 | 2h | 向用户展示可读的错误原因 |

### P2 — 体验增强（建议）

| # | 改进项 | 工作量 | 说明 |
|---|--------|--------|------|
| 9 | N1：重连机制 | 16h+ | 玩家断线后可重新加入（需 Lobby + Session 管理） |
| 10 | N3：观战模式 | 8h | 断线玩家可观战剩余比赛 |
| 11 | G1：送单奖励里程碑 | 4h | 设计文档已定义，需实现 |
| 12 | PlayerConfigSO 提取 | 2h | 移动手感参数数据驱动化 |
| 13 | TeamConfigSO 提取 | 3h | 队伍配置数据驱动化 |
| 14 | A4：RPC 迁移新版属性 | 8h | `[ServerRpc]` → `[Rpc(SendTo.Server)]` |

### P3 — 长期规划

| # | 改进项 | 工作量 | 说明 |
|---|--------|--------|------|
| 15 | N2：Host 迁移 | 24h+ | NGO 无原生支持，需自建方案或等 NGO 更新 |
| 16 | N4：AI 填充 | 24h+ | 需实现 Bot AI 控制器 |
| 17 | S1：服务端移动校验 | 16h | 需迁移到服务端权威 NetworkTransform + 客户端预测 |
| 18 | G3：比赛数据持久化 | 8h | 胜负记录/统计/排行榜 |
| 19 | T3：自动化测试 | 16h+ | 单元测试 + 集成测试框架 |

---

## 附录 A：文件结构总览

```
Assets/Scripts/
├── 核心网络
│   ├── KitchenGameMultiplayer.cs     # 多人管理器（连接/数据/生成）
│   ├── KitchenGameLobby.cs           # Lobby/Relay 大厅
│   ├── KitchenGameManager.cs         # 游戏状态机
│   ├── Player.cs                     # 玩家控制器
│   ├── PlayerData.cs                 # 网络序列化玩家数据
│   ├── ClientNetworkTransform.cs     # 客户端权威 Transform
│   └── OwnerNetworkAnimator.cs       # 客户端权威 Animator
├── Counters/
│   ├── BaseCounter.cs                # 柜台基类
│   ├── ClearCounter.cs               # 普通柜台
│   ├── ContainerCounter.cs           # 食材容器
│   ├── CuttingCounter.cs             # 切菜台
│   ├── StoveCounter.cs               # 灶台（煎/炸/焦）
│   ├── DeliveryCounter.cs            # 配送台（有 teamId）
│   ├── PlatesCounter.cs              # 盘子台
│   ├── TrashCounter.cs               # 垃圾桶
│   └── ItemBoxCounter.cs             # 道具箱
├── Effects/
│   ├── IEffectHost.cs                # 效果宿主接口
│   ├── PlayerEffectHost.cs           # 玩家效果宿主
│   ├── CounterEffectHost.cs          # 柜台效果宿主
│   └── PlayerEffectVisual.cs         # 效果视觉
├── UI/ (30+ 文件)                    # 全流程 UI
├── ScriptableObjects/ (10 个 SO 类)  # 数据定义
├── SabotageSystem.cs                 # 落后补偿系统
├── DeliveryManager.cs                # 订单/计分管理
├── KitchenObject.cs                  # 厨房对象基类
├── PlateKitchenObject.cs             # 盘子（多食材）
├── GameInput.cs                      # 输入系统
├── SoundManager.cs                   # 音效管理
├── MusicManager.cs                   # 音乐管理
├── Loader.cs / LoaderCallback.cs     # 场景加载
└── ...其他支持脚本
```

## 附录 B：网络 Prefab 注册表（24 项）

| # | Prefab | 类型 |
|---|--------|------|
| 1 | Player | 玩家 |
| 2 | CharacterSelectPlayer | 角色选择 |
| 3-9 | _BaseCounter + 6 柜台变体 | 柜台 |
| 10-20 | 11 个 KitchenObject | 食材/盘子 |
| 21-24 | 其他变体 | 辅助 |

## 附录 C：场景清单

| # | 场景 | 用途 | 连接准入 |
|---|------|------|---------|
| 0 | MainMenuScene | 标题/入口 | ❌ |
| 1 | LobbyScene | 大厅创建/加入 | ❌ |
| 2 | GameScene | 2v2 对战 | ❌ |
| 3 | LoadingScene | 加载过渡 | ❌ |
| 4 | CharacterSelectScene | 角色选择 | ✅ 唯一允许连接 |
