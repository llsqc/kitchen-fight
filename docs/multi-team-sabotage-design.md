# 队伍互相整蛊玩法设计（MVP 版）

- **状态**：已实施（垂直切片 4 道具 + 完整 UX）
- **关联代码库**：`C:\Users\guoju\Desktop\KitchenFight`（Tuanjie Engine 1.9.3 / NGO 1.13.1 多人厨房游戏）
- **配套文档**：[Phase 2 扩展道具设计](./multi-team-sabotage-phase2.md) | [垂直切片实施方案](./multi-team-sabotage-vertical-slice.md)

---

## 1. 概述

`KitchenFight` 是 Tuanjie Engine 1.9.3 / Unity Netcode for GameObjects (NGO **1.13.1**) 的**2v2 队伍对抗型**多人厨房游戏：每队 2 人各自在对称厨房中烹饪冲分，通过厨房主题整蛊道具干扰对手。游戏时长 180 秒，分数最高的队伍获胜。

### 核心体验目标
1. **派对混乱感**：弱队有翻盘机会，强队优势不稳。
2. **主题一致性**：所有道具都是厨房/餐厅真实会发生的事，非通用奇幻。
3. **节奏递进**：从冷静厨艺博弈逐渐演变成灾难派对。
4. **平衡可玩**：被整者有反制手段，不会被连环整到无法操作。

---

## 2. 代码库架构

### 2.1 联网架构
- **方案**：Unity Netcode for GameObjects (NGO) **1.13.1**。Tuanjie Engine 1.9.3。
- **权威模型**：客户端权威 transform/animator（`ClientNetworkTransform.cs`、`OwnerNetworkAnimator.cs`）+ ServerRpc 网关。
- **同步方式**：`NetworkList<PlayerData>`（`KitchenGameMultiplayer.playerDataNetworkList`）+ 多个 `NetworkVariable`（KGM 状态/计时器、StoveCounter 状态/计时器、队伍分数等）。
- **玩家生成**：`KitchenGameManager.SceneManager_OnLoadEventCompleted` 中 `Instantiate(playerPrefab)` + `SpawnAsPlayerObject(clientId, true)`。Player 有 `spawnPositionList`，`OnNetworkSpawn` 中按 playerDataIndex 选位置，按队伍分区。
- **网络对象生成**：`KitchenGameMultiplayer.SpawnKitchenObject` -> ServerRpc -> `NetworkObject.Spawn(true)`。
- **Lobby/Relay**：完整系统（`KitchenGameLobby.cs`，使用 `com.unity.services.lobby` 1.2.2 + `com.unity.services.relay` 1.0.5）。同时支持直连。
- **角色选择**：`CharacterSelectScene`、`CharacterSelectPlayer`、`CharacterSelectReady`、`CharacterSelectUI`。
- **PlayerData**：`INetworkSerializable` struct，含 `clientId`、`colorId`、`teamId`、`playerName`(FixedString64Bytes)、`playerId`(FixedString64Bytes)。通过 `NetworkList<PlayerData>` 同步。

### 2.2 游戏循环
- **状态机**（`KitchenGameManager.cs`）：`WaitingToStart -> CountdownToStart -> GamePlaying -> GameOver`。`NetworkBehaviour`，用 `NetworkVariable<State>` / `NetworkVariable<float>` / `NetworkVariable<bool>` 服务端权威推进阶段与计时。
- **时长**：`gamePlayingTimerMax = 180f`（180 秒）。
- **订单**（`DeliveryManager.cs`）：每队独立订单队列（MIRRORED - 两队同索引配方保证公平），`NetworkVariable<int>` 分数，服务端权威递增。
- **计分**：`NetworkVariable<int>` 每队一个，服务端权威。胜负判定：180s 结束分数最高队获胜。
- **MAX_PLAYER_AMOUNT**：4（2v2）。

### 2.3 队伍系统
- `PlayerData` 有 `teamId`（0/1），通过 `NetworkList<PlayerData>` 同步。
- 队伍颜色：红系/蓝系，同队同色系不同色调。
- `KitchenGameMultiplayer` 分配 teamId = playerDataIndex / 2。
- `DeliveryCounter` 有 teamId 字段，去单例化。
- 玩家按队伍分区出生。

### 2.4 关键扩展点
- `IKitchenObjectParent` 接口有 6 个方法（含 `GetNetworkObject()`）。
- `BaseCounter.Interact/InteractAlternate` 是 `virtual`，装饰器可包裹。
- `KitchenObject.Awake()` 是 `protected virtual`，`PlateKitchenObject` 已示范子类化。
- `KitchenObjectListSO` 索引注册表：加新道具往 SO 列表加 + 注册 prefab。
- `SoundManager` 事件订阅模式：加 `AudioClipRefsSO` 字段 + 订阅事件。

---

## 3. 设计原则
1. **厨房主题一致性**：道具必须是厨房/餐厅真实会发生或能想象到的事。
2. **混合型**：既有瞬时，也有持续。
3. **递增节奏**：道具密度随进程递增。
4. **平衡可玩**：落后补偿 + 反制道具 + 被整保护（三层，不过载）。
5. **最小侵入**：优先复用扩展点，装饰器/宿主组件注入效果。

---

## 4. 核心架构

### 4.1 队伍与计分
- **队伍分配**：游戏开始时按 `OwnerClientId` 分组，每 2 人一队。`teamId` 通过 `NetworkList<PlayerData>` 同步。
- **队伍颜色**：红系/蓝系，同队同色系不同色调。
- **独立计分**：`DeliveryManager` 按队伍独立结构，每队有独立的 `waitingRecipeSOList` 与 `NetworkVariable<int>` score。订单 MIRRORED（两队同索引配方）。配送校验：客户端发 plate 内容 + teamId，服务端按队验证。
- **胜负**：180 秒结束，分数最高的队伍获胜。

### 4.2 道具系统三层抽象
```
┌─ ItemBoxSystem（道具获取层）
│   地图道具箱 / 落后补偿盲盒
│
├─ SabotageItem（道具定义层）
│   ScriptableObject：名称、类型、稀有度、效果、目标类型
│   玩家持有：Player.Inventory（两个固定槽位 NetworkVariable<int>）
│
└─ SabotageEffect（效果执行层）
    ├── 瞬时效果 -> ServerRpc 触发 + ClientRpc 广播视觉/音效
    └── 持续状态 -> NetworkVariable<float> 落到宿主，Update 递减
```

### 4.3 EffectHost 统一宿主
给所有可能被整的对象挂 `EffectHost`，实现统一接口：

```csharp
public enum VictimState { Normal, ProtectedHalve, ImmuneBlock }

public interface IEffectHost {
    void ApplyEffect(EffectType type, float duration, int sourceTeamId);
    VictimState GetVictimState();
    void ClearEffect(EffectType type);
    float GetEffectRemaining(EffectType type);
}
```
- `PlayerEffectHost`（挂 `Player`）：眩晕/反转/被整保护。有 `cleanWipeFlash` NetworkVariable 支持清洁湿巾闪光视觉。
- `CounterEffectHost`（挂 `BaseCounter`）：锁定。Start() 中自动创建 LockVisual（红色半透明脉冲覆盖层）。

**效果堆叠规则**：
- **同种效果后到覆盖**，取较长时长（不累加）。
- **异种不冲突效果共存**。
- **异种冲突效果**：后到者覆盖前者。

### 4.4 道具库存与输入

**库存**：每个玩家 2 个槽位，用 2 个 `NetworkVariable<int>`（道具 SO 索引，-1 表示空）同步。

**输入**：
- `Interact`(E)/`InteractAlternate`(F)：厨房交互
- `SelectSlot1`(Keyboard/1)：选择槽位 1（Player-target 道具选中，Counter/Self 道具立即触发）
- `SelectSlot2`(Keyboard/2)：选择槽位 2（同上）
- `UseItem`(Mouse/leftButton)：Player-target 道具发射（准星瞄准后左键点击）

### 4.5 网络同步方案
- **瞬时效果**：ServerRpc -> 服务端校验 -> ClientRpc 广播事件+位置+特效。
- **持续状态**：`NetworkVariable<float>` 计时器落宿主，NGO 自动同步，宿主 Update 递减到 0 清除。
- **服务端权威校验**：校验"是否真持有、目标是否合法"。

### 4.6 瞄准系统
- **CrosshairUI**：72px 圆环（96x96 纹理，6px 线宽），跟随鼠标移动。白色=无目标，绿色=准星范围内有玩家。仅在持有 Player-target 道具且游戏进行中时显示。
- **Player-target 道具**：左键点击后，服务端在鼠标世界坐标处 OverlapSphere 检测玩家（半径 1.5f），友伤开启，可同时命中多个玩家。
- **Counter/Self 道具**：按 1/2 键直接触发，无需瞄准。
- `GameInput.GetMouseWorldPosition()`：鼠标屏幕坐标投射到 y=0 地面平面获取世界坐标。

### 4.7 厨房布局：对称分区
- 每队一个对称烹饪区域，配置相同柜台组合。
- Team0: z=-5.5~3.5, Team1: z=8.5~17.5 (z_new=12-z_old), 中央通道 z=3.5~8.5。
- 各队配送台独立（有 teamId 字段）。
- 区域间中央通道有 2 个 ItemBoxCounter 可穿行获取道具。
- 玩家按队伍分区出生：team0 z=-1, team1 z=13。
- 62 个柜台均挂有 CounterEffectHost。
- 相机：(-6,22,6) rot(75,90,0) FOV40，队伍左右分布（Blue/Team1 左, Red/Team0 右）。玩家移动为相机相对方向。

---

## 5. 道具获取机制

### 5.1 三轨获取
**① 地图道具箱（主获取）**
- 厨房中央通道 2 个 `ItemBoxCounter`，交互键抽取道具。
- 10 秒冷却（NetworkVariable<float> 同步）。
- 服务端 `Random` + 稀有度权重（60% C / 30% B / 10% A）。

**② 落后补偿盲盒（平衡核心）**
- 比较各队分数，最低分队成员收一个补偿盲盒。
- 落后越多道具越强。
- `SabotageSystem` 实现。

**③ 送单奖励（里程碑制）**
- 每队配送数达到里程碑时各触发一次。

---

## 6. 已实施道具

### 6.1 整玩家（2 个）
| # | 道具 | 效果 | 类型 | 稀有度 | 目标 | 视觉效果 |
|---|------|------|------|--------|------|---------|
| 1 | **平底锅敲头(Stun)** | 眩晕 2 秒，无法移动和交互 | 持续 | B | Player | 头顶 3 颗黄色球体旋转 |
| 2 | **料酒上头(Reverse)** | 方向键反转 8 秒 | 持续 | B | Player | 脚下紫色圆环脉冲 |

### 6.2 整设备（1 个）
| # | 道具 | 效果 | 类型 | 稀有度 | 目标 | 视觉效果 |
|---|------|------|------|--------|------|---------|
| 3 | **保鲜膜封存(Lock)** | 柜台锁定 15 秒不能用 | 持续 | C | Counter | 柜台顶部红色半透明脉冲覆盖层 |

### 6.3 反制道具（1 个）
| 道具 | 效果 | 稀有度 | 目标 | 视觉效果 |
|------|------|--------|------|---------|
| **清洁湿巾(CleanWipe)** | 清除自身所有持续负面状态 + 当前选中柜台负面状态 | C | Self | 白色球体扩散淡出 |

### 6.4 道具交互矩阵
| 目标类型 | 选择方式 | 触发方式 | 友伤 | 多目标 |
|---------|---------|---------|------|--------|
| Player | 1/2 键选中 | 左键点击准星 | 开启 | 支持（OverlapSphere） |
| Counter | 1/2 键 | 立即触发 | - | - |
| Self | 1/2 键 | 立即触发 | - | - |

### 6.5 被整保护
- 每个玩家 `recentVictimTimer`（10 秒窗口）。
- 仅对 Player 状态类效果（眩晕、反转）生效。
- 10 秒内再次被整：效果减半。

---

## 7. 平衡机制

### 7.1 落后补偿
- `SabotageSystem` 定期比较各队分数，最低分队获得补偿道具。
- gap 越大道具越强（C/B/A 梯度）。

### 7.2 反制道具
- 清洁湿巾(C)：清除自身所有持续负面状态 + 选中柜台负面状态。

### 7.3 被整保护
- 10 秒窗口，Player 状态类效果减半。

---

## 8. 道具节奏（180s）

| 时段 | 时间 | 地图箱刷新 | 地图箱数 | 补偿判定 |
|------|------|-----------|---------|---------|
| 开局期 | 0-20s | 不刷新 | 2 | 不触发 |
| 中期 | 20-130s | 20s | 2 | 45s |
| 疯狂期 | 130-180s | 15s | 3 | 30s |

---

## 9. 工程实现要点

### 9.1 关键扩展点复用
- `ItemBoxCounter` 继承 `BaseCounter`，override `Interact`。
- 音效：`AudioClipRefsSO` 加字段 + `SoundManager.Start()` 订阅。

### 9.2 EffectHost 宿主
- `PlayerEffectHost`/`CounterEffectHost` 实现 `IEffectHost`。
- 持续效果宿主 Update 递减，到 0 清除。
- `PlayerEffectVisual.cs` 在 Player 预制体上，读取 NetworkVariable 控制视觉效果显隐。
- `CounterEffectHost.cs` 在 Start() 中自动创建 LockVisual。

### 9.3 输入系统
- 移动反转修饰器在 `GameInput.GetMovementVectorNormalized()` 输出端，不侵入 Player。
- 准星瞄准使用 `GameInput.GetMouseWorldPosition()` 投射鼠标到地面平面。
- `PlayerEffectVisual.cs` 在 Player 预制体上处理效果视觉。

---

## 10. 后续步骤

> 垂直切片（4 道具 + 完整 UX）已实施完毕。验证好玩后扩展全量 MVP 剩余道具（疯狂锅、爆单、改单、停电、油锅起火），详见 [Phase 2 文档](./multi-team-sabotage-phase2.md)。
