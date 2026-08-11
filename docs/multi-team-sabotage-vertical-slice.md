# 多队伍对战 + 捣乱道具 垂直切片实施方案

- **状态**：已实施完毕
- **关联**：[MVP 主设计](./multi-team-sabotage-design.md) | [Phase 2 扩展](./multi-team-sabotage-phase2.md)
- **关联代码库**：`C:\Users\guoju\Desktop\KitchenFight`（Tuanjie Engine 1.9.3 / NGO 1.13.1）

---

## 决策摘要
- **范围**：垂直切片（4 道具 + 完整 UX），验证好玩后再扩展全量 MVP
- **队伍**：2v2（max 4 人）
- **时长**：180s（3 分钟）
- **布局**：完全分离对称厨房，中间通道可穿行干扰
- **队伍颜色**：同队同色系不同色调（红系/蓝系）
- **道具瞄准**：Player-target 道具用准星 + 左键点击；Counter/Self 道具按键直接触发
- **音效定位**：事件参数传 Vector3 位置

---

## 代码库现状

| 项目 | 状态 |
|---|---|
| NGO 版本 | 1.13.1（NetworkVariable 成熟） |
| KitchenGameManager | ✅ NetworkBehaviour + NetworkVariable 服务端权威 |
| NetworkList | ✅ 已用（playerDataNetworkList） |
| StoveCounter | ✅ IsServer 守卫 + NetworkVariable 计时器 |
| Lobby/Relay/角色选择 | ✅ 完整系统 |
| 玩家出生点 | ✅ Player.spawnPositionList 按队分区 |
| KitchenObject 网络销毁 | ✅ DestroyKitchenObject 走 ServerRpc |
| DeliveryManager | ✅ 按队独立订单 + NetworkVariable<int> 分数（MIRRORED） |
| DeliveryCounter | ✅ 去单例 + teamId 字段 |
| 队伍系统 | ✅ PlayerData 有 teamId (0/1) |
| 道具系统 | ✅ 4 道具 + EffectHost + 准星瞄准 + 视觉效果 |
| 游戏时长 | 180s |
| 对称厨房 | ✅ Team0 z=-5.5~3.5, Team1 z=8.5~17.5 |
| 相机 | ✅ (-6,22,6) rot(75,90,0) FOV40, 左右分屏 |
| 准星系统 | ✅ CrosshairUI 圆环跟随鼠标 |
| 道具视觉效果 | ✅ PlayerEffectVisual + CounterEffectHost LockVisual |
| 菜单紧凑化 | ✅ OptionsUI+GamePauseUI 居中面板 |

---

## 实施内容

### Phase 1: 队伍系统基础 ✅
1. PlayerData 加 teamId + NetworkSerialize
2. KitchenGameMultiplayer 队伍分配（teamId = count/2）
3. 队伍颜色系统（同队同色系不同色调：红系/蓝系）
4. Player 按队出生（spawnPositionList 4 位置按队分区）

### Phase 2: 多队订单与计分系统 ✅
1. DeliveryManager 按队独立（teamWaitingLists[2], teamScores[2] NV, 事件参数带 teamId+position）
2. 配送校验重构（DeliverRecipe 加 teamId）
3. DeliveryCounter 去单例 + 加 teamId
4. SoundManager 适配（用事件参数位置）
5. UI 适配（DeliveryManagerUI 按本队, GameOverUI 多队排名, DeliveryResultUI 按队过滤）
6. 游戏时长 180s

### Phase 3: 道具系统框架 ✅
1. SabotageItemSO + SabotageItemListSO（ScriptableObject）
2. 玩家库存（2 槽位 NetworkVariable<int>）
3. ItemBoxCounter（继承 BaseCounter，交互抽取道具，10s 冷却）
4. EffectHost 接口 + PlayerEffectHost + CounterEffectHost
5. 输入扩展（SelectSlot1/2 = 1/2键, UseItem = 鼠标左键, GameInput 反转修饰器 + GetMouseWorldPosition）

### Phase 4: 道具实现 ✅
| # | 道具 | 效果 | 目标 | 视觉效果 |
|---|------|------|------|---------|
| 1 | Stun(平底锅) | 眩晕 2s | Player | 头顶黄色球体旋转 |
| 2 | Reverse(料酒) | 方向反转 8s | Player | 脚下紫色圆环脉冲 |
| 3 | Lock(保鲜膜) | 柜台锁定 15s | Counter | 柜台红色覆盖层脉冲 |
| 4 | CleanWipe(湿巾) | 清除自身负面 | Self | 白色球体扩散淡出 |

- Player-target 道具：1/2 键选中 -> 准星瞄准 -> 左键发射（OverlapSphere, 友伤, 多目标）
- Counter/Self 道具：1/2 键直接触发
- 被整保护（10s 窗口减半）
- 全部 4 道具通过 ServerRpc 全链路验证

### Phase 5: 场景布局 + 平衡 ✅
1. 对称厨房场景（镜像布局 + 各队配送台 + 中央道具箱）
2. 道具节奏（180s: 开局 20s / 中期 / 疯狂期 50s）
3. 落后补偿（SabotageSystem, 45s 周期）

### Phase 6-11: UX 与打磨 ✅
1. 对称厨房 UI + 相机调整（90° 旋转, 左右分屏）
2. UX 审计修复（分数方向, 芝士柜台碰撞箱, 道具标签）
3. 准星瞄准系统（CrosshairUI 圆环, 移除 LineRenderer）
4. 槽位选择（1/2 键, 选中高亮）
5. 菜单紧凑化（OptionsUI/GamePauseUI 居中面板）
6. 碰撞箱调优（芝士柜台 x-size=1.3, 道具箱缩小）
7. 道具视觉效果（PlayerEffectVisual + CounterEffectHost LockVisual）

---

## 道具节奏（180s）

| 时段 | 时间 | 地图箱刷新 | 地图箱数 | 补偿判定 |
|------|------|-----------|---------|---------|
| 开局期 | 0-20s | 不刷新 | 2 | 不触发 |
| 中期 | 20-130s | 20s | 2 | 45s |
| 疯狂期 | 130-180s | 15s | 3 | 30s |

---

## 后续扩展
- 补全 MVP 剩余道具（疯狂锅、爆单、改单、停电、油锅起火）
- 送单奖励里程碑
- Phase 2 扩展道具（见 [Phase 2 文档](./multi-team-sabotage-phase2.md)）
