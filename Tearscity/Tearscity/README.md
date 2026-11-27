# 泪水之城 / City of Tears

《杀戮尖塔 2》mod 项目。基于 **RitsuLib** 新增一个阶段（Act），当前目标：**替换第一幕，能正常进入即可**。

当前状态：第一幕被强制替换为自建内容 —— **17 种敌人 / 22 个遭遇（弱 4 / 普通 11 / 精英 4 / Boss 3）/ 13 个自建事件**，全部中文文案已补（无占位 "1"；**英文本地化已删除**，见下）；美术：怪物/事件用《空洞骑士》官方贴图与 AI 重绘，战斗背景/地图/休息处已换成 AI 生成的雨城图（战斗背景为“单张竖构图 + 前景动态雨幕”，避免多层拼接）。

## 环境

| 项 | 位置 |
|---|---|
| 游戏（v0.113.0） | `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2` |
| Godot 4.5.1 mono | `C:\Users\ASUS\Desktop\sts2 tools\Godot_v4.5.1-stable_mono_win64\...\Godot_v4.5.1-stable_mono_win64_console.exe` |
| RitsuLib 0.6.13 | 创意工坊 `workshop\content\2868840\3747602295` |
| .NET | 项目目标 `net9.0`（游戏运行时是 .NET 9，不要改成 net10） |

## 目录结构

```
Tearscity/
├─ Tearscity.csproj          编译配置（引用 sts2 / 0Harmony / RitsuLib，构建后收集到 dist\）
├─ Tearscity.json            mod 清单（id=Tearscity，依赖 STS2-RitsuLib）
├─ project.godot             Godot 工程（导出 pck 用）
├─ export_presets.cfg        导出预设 "PCK"（排除 dist/bin/obj/*.dll）
├─ Scripts/                  C# 源码（.gdignore，不进 pck）
│  ├─ Entry.cs               入口：Harmony + 脚本注册 + RegisterAct + 第一幕强制替换
│  ├─ Acts/Tearscity.cs      阶段本体（Index=0 → 第一幕）
│  ├─ Audio/TearscityMusic.cs 自建 BGM 播放层（不走 FMOD bank）
│  └─ Patches/                 Harmony 补丁（战斗背景 / 音乐 / 事件池过滤）
├─ assets/music/             运行时从磁盘直读的音频（.gdignore，不进 pck，由 csproj 复制）
├─ tools/gen_music.py        程序化配乐生成器（改参数就能换一版 BGM）
├─ scenes/backgrounds/tearscity_act_tearscity/tearscity_act_tearscity_background.tscn   占位背景场景（仅为了让预加载有东西）
├─ scenes/backgrounds/tearscity_act_tearscity/layers/  战斗背景层（bg_00 = 单张全景图，fg_00 = 前景雨幕动画）
├─ scenes/rest_site/tearscity_act_tearscity_rest_site.tscn                休息处场景
├─ images/packed/map/map_bgs/tearscity_act_tearscity/                            地图上/中/下段背景图
├─ Tearscity/images/                                        美术资源
├─ Tearscity/localization/zhs/acts.json                    本地化（只有中文；eng 已删，备份在 <工作区>/_backup/localization_eng）
├─ tools/gen_placeholder_art.py                             占位美术生成脚本
├─ build.ps1                 一键：导入资源 -> 导出 pck -> 编译
└─ deploy.ps1                把 dist\Tearscity 复制到游戏 mods\Tearscity
```

## 构建与部署

```powershell
# 一键构建（导入资源 + 导出 pck + 编译 dll）
powershell -ExecutionPolicy Bypass -File .\build.ps1

# 部署到游戏
powershell -ExecutionPolicy Bypass -File .\deploy.ps1
```

只改 C# 时 `dotnet build` + `deploy.ps1` 就够；**改了美术/场景/本地化必须重新导出 pck**（build.ps1 的前两步）。

部署后的正确结构（不要多套一层文件夹）：

```
...\Slay the Spire 2\mods\Tearscity\
	Tearscity.dll
	Tearscity.json
	Tearscity.pck
```

## 它现在怎么替换第一幕

`Scripts/Acts/Tearscity.cs`：
- `Index => 0`（第一幕）
- `IsDefault => false`（不与原版 Overgrowth 抢默认位）
- 内容列表全部照抄原版 Overgrowth，所以每个怪、Boss、事件、先古都一定存在，不会因为缺资源崩溃

`Scripts/Entry.cs`：
```csharp
public const int ActSlot = 0;
content.RegisterAct(typeof(Acts.Tearscity));
content.RegisterActEnterForce<Acts.Tearscity>(ActSlot, 100, ctx => ctx.EnteringActIndex == ActSlot);
```
`ActEnterForce` 是 RitsuLib 的"章节进入强制规则"：进入第 1 幕（下标 0）时强制选用本阶段。
想换回其它幕只改 `ActSlot`（0/1/2），同时把 Act 类里的 `Index` 改成相同值、并把内容列表换成对应原版幕的内容。

## 自定义遭遇 / 怪物怎么接进来

```csharp
// Scripts/encounters.cs  —— 所有遭遇都放这一个文件里
public abstract class SingleMonsterEncounter<TMonster> : ModEncounterTemplate
	where TMonster : MonsterModel
{
	public override RoomType RoomType => Rooms.RoomType.Monster;                 // 属性名和类型名撞车，必须写全
	public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { ModelDb.Monster<TMonster>() };
	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() // 战斗里用 .ToMutable()
		=> new (MonsterModel, string?)[] { (ModelDb.Monster<TMonster>().ToMutable(), null) };
	public override bool IsValidForAct(ActModel act) => act is TearscityAct;
}

// 一个新遭遇 = 一行
[RegisterActEncounter(typeof(TearscityAct))]
public class HunksentryEncounter : SingleMonsterEncounter<HunkSentryMonster> { }

[RegisterActEncounter(typeof(TearscityAct))]
public class FollyEncounter : SingleMonsterWeakEncounter<FollyMonster> { }

// 精英：基类只差 RoomType => Elite
[RegisterActEncounter(typeof(TearscityAct))]
public class SoulEncounter : SingleMonsterEliteEncounter<SoulMonster> { }

// Monsters/hunksentry.cs
[RegisterMonster]
public class hunksentry : ModMonsterTemplate { ... }
```

要点：

1. **遭遇必须是各自一个类**（模型 ID 由类名推导 `<MODID>_ENCOUNTER_<类名>`），
   但**不需要各占一个文件**——`Scripts/encounters.cs` 里堆着 `SingleMonsterEncounter<T>`、
   `SingleMonsterEliteEncounter<T>`、`PairEncounter<T1,T2>` 三个泛型基类，加新遭遇只要一行。
2. **精英怪必须有 `RoomType.Elite` 的遭遇**：只写 `[RegisterMonster]` 的怪物不会进精英池，
   它得被一个 `RoomType => Rooms.RoomType.Elite` 的遭遇带出场（`SoulEncounter` 就是）。
3. **自动注册**：`[RegisterMonster]` / `[RegisterActEncounter]` 由
   `ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly)`（在 `Entry.Init`）扫描处理，
   不需要再手写 `content.RegisterActEncounter(...)`。RitsuLib 给 `ActModel.GenerateAllEncounters`
   打了 postfix，所以遭遇会自动追加进该幕的池子。
   **追加是按模型 ID 去重的**（`AppendDistinctByIdEnumerableStrategy` 里就是
   `Concat(source, additional).DistinctBy(x => x.Id)`），所以在 `GenerateAllEncounters` 里
   再显式列一遍只是冗余，不会出现两次、也不会让出现概率翻倍。
4. **`RoomType` 命名冲突**：`public override RoomType RoomType => RoomType.Monster;` 编译不过
   （`RoomType` 解析成属性而不是类型），要写成 `Rooms.RoomType.Monster`。
5. **名字和命名空间撞车**：`hunksentry` / `folly` / `great` 既是命名空间（`hunksentry.Scripts`）又是类名，
   在类型参数位置写 `SingleMonsterEncounter<hunksentry>` 会报 CS0118（当成命名空间），
   要像 `encounters.cs` 里那样起别名：`using HunkSentryMonster = hunksentry.Scripts.hunksentry;`。
6. **`GenerateMonsters` 用 `.ToMutable()`**，`AllPossibleMonsters` 用规范实例（不 ToMutable）；
   元组第二项是站位槽名，`null` = 自动安排。
7. **`VisualsScenePath` 必须指向存在的资源**：它会进 `MonsterModel.AssetPaths` 被预加载，
   路径不存在会让进战斗时的预加载抛异常（就是之前"进遭遇战卡死"那类问题）。
   现在四个怪都有自己做的场景（都在 `scenes/creature_visuals/`，贴图在 `Tearscity/images/monsters/`）：
   `tearscity_monster_hunksentry.tscn`（`hunksentry.png` 112x222 ×1.6 ≈ 179x355）、
   `tearscity_monster_folly.tscn`（`folly.png` 166x169 ×1.78 ≈ 295x301）、
   `tearscity_monster_soul.tscn`（`soul.png` 218x238 ×1.7 ≈ 371x405）、
   `tearscity_monster_great.tscn`（`great.png` 307x341 ×1.15 ≈ 353x392）。
   注意这个场景的根只能是**普通 `Node2D`**：mod pck 引用不了游戏的 `NCreatureVisuals.cs`
   （导出会丢 script），RitsuLib 的工厂会把普通场景转成真正的 `NCreatureVisuals`。
   但 `NCreatureVisuals._Ready` 用 `GetNode`（不是 GetNodeOrNull）硬取四个子节点，
   所以场景里必须备齐并设 `unique_name_in_owner = true`：
   `Visuals`（Sprite2D，脚底对齐 y=0）、`Bounds`（Control，可点击范围）、
   `CenterPos`（Marker2D，居中点）、`IntentPos`（Marker2D，意图图标位置）。
   尺寸可参照原版：axebot `Bounds` 约 ±167/-388、frog_knight ±159/-280、mawler ±255/-327。
   当前 `hunksentry` 用 112x222 的图 ×1.6 ≈ 179x355，`Bounds` ±100/-370。
8. **所有 .cs 必须放在带 `.gdignore` 的目录里**（现在是 `Scripts/`、`Monsters/`），
   否则 Godot 会认为工程里有 C# 文件、导出时要求 `.sln` 并失败。
9. **本地化 key**：mod 内容 ID 会被加前缀（阶段是 `TEARSCITY_ACT_TEARSCITY`）。
   遭遇/怪物的名字若显示成原始 key，看日志里的 `Missing localization key '...'`，
   把那行里的 key 填进 `Tearscity/localization/<lang>/{encounters,monsters}.json` 即可。
   注意 `Scripts/hunksentry.json` 目前没有被任何地方引用（在 `.gdignore` 目录里、且不是本地化表格式），
   里面那份 `HUNK_SENTRY_*` key 是从别处抄来的，前缀不对。

## soul（精英怪）的出招表

`Monsters/soul.cs` 演示了"固定开局 + 随机三选一"的写法：

| 状态 ID | 意图 | 效果 |
|---|---|---|
| `BASIC_ATTACK` | `SingleAttackIntent` + `DefendIntent` | 伤害 13/16，格挡 8（**开局固定出这招**） |
| `TP` | `BuffIntent` | 自己 +2/3 力量（权重 1，最低） |
| `CURSE` | `DebuffIntent(false)` | 玩家 +2/3 虚弱（权重 2） |
| `SMALL_ATTACK` | `SingleAttackIntent` | 伤害 2/3（权重 5，最高） |

结构是 `RandomBranchState("RAND")` 当中枢，三个 `FollowUpState` 都指向它：

```csharp
var rand = new RandomBranchState("RAND");
rand.AddBranch(smallAttack, MoveRepeatType.CannotRepeat, 5f);  // 权重最高
rand.AddBranch(curse,       MoveRepeatType.CannotRepeat, 2f);
rand.AddBranch(powerUp,     MoveRepeatType.CannotRepeat, 1f);  // 权重最低

basicAttack.FollowUpState = rand;
smallAttack.FollowUpState = rand;
curse.FollowUpState = rand;
powerUp.FollowUpState = rand;

return new MonsterMoveStateMachine(
	[basicAttack, powerUp, curse, smallAttack, rand],
	basicAttack);   // ← 初始状态 = 固定的"攻击+格挡"
```

- `MoveRepeatType.CannotRepeat` = 不会连着两回合出同一招（更耐打、更像随机）；
- 权重是相对的：5:2:1 → 62.5% / 25% / 12.5%；
- 想让"攻击+格挡"也进随机池，把它也 `AddBranch` 进去即可（现在它是开局专用）；
- 初始状态可以直接写成 `rand`（原版 Flyconid 就这么做），那样第一回合就开始随机。

## great（精英怪）

`Monsters/great.cs` 是"两招死循环"的最小写法：

| 状态 ID | 意图 | 效果 |
|---|---|---|
| `BASIC_ATTACK` | `SingleAttackIntent` | 伤害 9/12（**开局第一回合**） |
| `DEFEND` | `DefendIntent` | 格挡 6/8 |

```csharp
attack.FollowUpState = defend;
defend.FollowUpState = attack;
return new MonsterMoveStateMachine([attack, defend], attack);
```

HP 79-83（苦难关 90-95）。精英遭遇是 `GreatEncounter`（`Scripts/encounters.cs` 末尾），
因为它带 `[RegisterActEncounter]`，RitsuLib 会自动把它追加进该幕的池子；
`Tearscity.cs` 里那行显式列举只是显式表明意图，重复也不会算两次（见上一节第 3 条）。

美术 `Tearscity/images/monsters/great.png`（307x341，场景 ×1.15 ≈ 353x392）。

## heavy（精英怪，伤害逐回合成长）

`Monsters/heavy.cs`：开局两个回合加力量，之后一直攻击，且每次攻击后伤害 +2/+3。

| 状态 ID | 意图 | 效果 |
|---|---|---|
| `POWER_UP` | `BuffIntent` | 自己 +2/3 力量（**第 1 回合**） |
| `POWER_UP_2` | `BuffIntent` | 自己 +2/3 力量（**第 2 回合**） |
| `ATTACK` | `SingleAttackIntent` | 伤害 = 基础 9/12 + 已累加的加成，之后自循环 |

```csharp
firstPowerUp.FollowUpState = secondPowerUp;
secondPowerUp.FollowUpState = attack;
attack.FollowUpState = attack;          // 自己指向自己 = 一直攻击
```

伤害成长的做法：**攻击意图用 `SingleAttackIntent(Func<Decimal>)` 读字段，攻击结算完再把字段加上去**。

```csharp
private int _attackBonus;

new SingleAttackIntent(() => (decimal)(BaseDamage + _attackBonus))   // 意图显示
// ...
var damage = BaseDamage + _attackBonus;      // 实际打出的伤害
await DamageCmd.Attack(damage)...;
_attackBonus += DamageStep;                  // 结算完才加，所以显示值和实际值始终一致
```

注意 `GetSingleDamage` 里还会走 `Hook.ModifyDamage`，所以力量、易伤等修正会自动叠在上面——
意图上显示的数字和真正打出的数字是同一套计算，不用自己再补。

HP 63-70（苦难关 67-75）。精英遭遇是 `HeavyEncounter`（`Scripts/encounters.cs`），已进精英池；
美术 `Tearscity/images/monsters/heavy.png`（206x257，场景 ×1.4 ≈ 288x360）。

## master（Boss，两条血 + 空窗复活）

`Monsters/master.cs`：**一条血打空 → 空窗一回合（打不到、不打人）→ 回满第二形态的血 → 只重复"重击 / 多段"**。

| 状态 ID | 意图 | 效果（普通/苦难） | 阶段 |
|---|---|---|---|
| `DIVE` | `SingleAttackIntent` | 12/14（**开局第一招**） | 第一形态 |
| `SOUL_ORB` | `MultiAttackIntent` | 3×2 / 4×2 | 第一形态 |
| `FOCUS` | `BuffIntent`+`DebuffIntent` | 自己 +2/3 力量，玩家 1/2 虚弱 | 第一形态 |
| `RESPAWN` | `HealIntent` | 不攻击：`SetMaxHp(60/70)` + `Heal` 回满 | 一次性空窗 |
| `DIVE_2` | `SingleAttackIntent` | 14/16 | 第二形态 |
| `SOUL_STORM` | `MultiAttackIntent` | 4×3 / 5×3 | 第二形态 |

HP 130-140（苦难 140-150），复活后 MaxHp 变成 60/70。第一形态随机池 `DIVE`/`SOUL_ORB` 各权重 2、`FOCUS` 权重 1；第二形态只有 `DIVE_2`/`SOUL_STORM` 各权重 1。

复活不靠自定义 power，直接覆写模型自己的钩子：

```csharp
public override Task BeforeDeath(Creature creature)          // 死亡判定前就置标志
{
	if (creature == Creature && !_reviveUsed) BeginRevive();  // _reviving = true + SetMoveImmediate(_respawnState, true)
	return Task.CompletedTask;
}

public override bool ShouldStopCombatFromEnding() => _reviving;                                  // 空窗期不判胜
public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature c) => c != Creature || !_reviving;
public override bool ShouldAllowHitting(Creature c) => c != Creature || !_reviving;              // 空窗期打不到

private async Task RespawnMove(IReadOnlyList<Creature> targets)   // RESPAWN 回合执行
{
	await CreatureCmd.SetMaxHp(Creature, ReviveHp);
	await CreatureCmd.Heal(Creature, ReviveHp, true);
	_reviving = false;                                            // 解除空窗，进入第二形态
}
```

- `_reviveUsed` 保证只复活一次；第二次打空时钩子恢复默认语义，正常结算。
- 这套链条与原版 `TestSubject`（多形态 boss）同源，它用的是 `SetMoveImmediate` + `AdaptablePower`；我们改成模型自己实现，血条上就不会多一个原版能力图标。
- 如果空窗期 boss 因为 0 血拿不到回合（表现为卡住不出招），Plan B 是改成给它挂 `AdaptablePower`，并在 `RESPAWN` 里 `PowerCmd.Remove<AdaptablePower>(Creature)`。

Boss 遭遇是 `MasterEncounter`（`Scripts/encounters.cs`），已在 `BossDiscoveryOrder` 与测试池 Boss 段里。

**地图上的 Boss 图标走的是 `EncounterModel.BossNodePath`，不是 `MapNodeAssetPaths`**（踩过）：

- `NBossMapPoint._Ready` 在没有 spine 资源时，直接用 `BossNodePath + ".png"` 和 `BossNodePath + "_outline.png"` 当贴图；
- `BossNodePath` 默认值是 `res://animations/map/<entry 小写>/<entry 小写>_node_skel_data.tres`，appending `.png` 就变成 `..._node_skel_data.tres.png` —— 这个文件当然不存在，日志里是：
  `ERROR: No loader found for resource: res://animations/map/tearscity_encounter_watcher_encounter/tearscity_encounter_watcher_encounter_node_skel_data.tres.png`
  结果就是地图上 Boss 节点**完全没图标**；
- 修法和原版 `VantomBoss` / `TheKinBoss` 一样，覆写 `BossNodePath` 成自己的前缀（不带扩展名）：

```csharp
public override string BossNodePath => "res://Tearscity/images/map/tearscity_encounter_master_encounter_nodeicon";
```

| 文件 | 规格 | 用在哪 |
|---|---|---|
| `..._nodeicon.png` | 352×352 白色剪影 | 地图 Boss 节点（游戏会按访问状态染色，所以必须纯白） |
| `..._nodeicon_outline.png` | 352×352 白色剪影外扩 5px | 同上，描边层 |
| `..._icon.png` / `..._icon_outline.png` | 128×128 彩色 | 跑图历史（`RunHistoryIconPath`），**另一套** |

`EncounterAssetProfile.MapNodeAssetPaths` 只是"进幕时要预加载哪些资源"（会并进 `ActModel.AssetPaths`），必须和 `BossNodePath` 推导出的那两个路径完全一致，否则资源没进 `PreloadManager.Cache`，`NBossMapPoint` 拿到的还是空贴图；指向不存在的文件则会在进幕时抛 `AssetLoadException`。

美术是双形态 + 交叉淡入淡出：`scenes/creature_visuals/tearscity_monster_master.tscn` 里有两个 Sprite2D——

| 节点 | 贴图 | 尺寸 | 初始 alpha |
|---|---|---|---|
| `Visuals`（必需节点） | `master_idle.png`（323x283） | ×1.63 ≈ 526x461，`y = -230` | 1 |
| `VisualsStagger` | `master_stagger.png`（124x256） | ×1.80 ≈ 223x461，`y = -230` | 0 |

两个贴图都按"可见高度 461、脚底对齐 y=0"缩放，所以淡入淡出时不会跳位置。切换在 `MASTER` 里做：

```csharp
private void SetStaggerVisual(bool staggered)
{
	var visuals = Creature?.GetCreatureNode()?.Visuals;      // NCreature.GetCreatureNode() -> NCreature -> .Visuals
	if (visuals == null) return;
	var idle = visuals.GetNodeOrNull<CanvasItem>("Visuals");
	var stagger = visuals.GetNodeOrNull<CanvasItem>("VisualsStagger");
	if (idle == null || stagger == null) return;

	_fadeTween?.Kill();
	_fadeTween = visuals.CreateTween();
	_fadeTween.SetParallel(true);
	_fadeTween.TweenProperty(idle, "modulate:a", staggered ? 0f : 1f, 0.35f);
	_fadeTween.TweenProperty(stagger, "modulate:a", staggered ? 1f : 0f, 0.35f);
}
```

- `BeginRevive()`（`BeforeDeath` 里触发）→ `SetStaggerVisual(true)`：血条打空的瞬间 0.35 秒淡到"空窗"形态；
- `RespawnMove()` 末尾（`_reviving = false` 之后）→ `SetStaggerVisual(false)`：恢复常态，同时开始出二形态的招。
- `NCreature.GetSpecialNode<T>(name)` 就是 `Visuals.GetNodeOrNull<T>(name)`，所以这两个节点按名字从 `NCreatureVisuals` 底下取。

## watcher（Boss，三只同屏）

`Monsters/watcher.cs` + `WatcherEncounter`：**同屏 3 只**守望者骑士，各自独立血量与状态机，四招随机。

| 状态 ID | 意图 | 效果（普通/苦难） | 权重 |
|---|---|---|---|
| `SLASH` | `SingleAttackIntent` | 4/5 | 3 |
| `TWIN_SLASH` | `MultiAttackIntent` | 3×2 / 4×2 | 2 |
| `ROLL` | `SingleAttackIntent` + `DefendIntent` | 9/11 伤害 + 自身格挡 4/6 | 1 |
| `SHIELD_BASH` | `SingleAttackIntent` + `DebuffIntent` | 3/4 伤害 + 玩家 1/2 虚弱 | 1 |

每只 HP 54-62（苦难 62-70），三只合计 162-186。`RandomBranchState` 全部 `CannotRepeat`，所以同一只不会连着出同一招；三只是三个独立模型实例，出招自然错开。

**死亡狂怒**：任何一只倒下时，其余存活同类 +2/+3 力量。

```csharp
var allies = Creature.CombatState.Enemies
	.Where(c => c != Creature && c.IsAlive && c.Monster is WATCHER)
	.ToList();
if (allies.Count > 0)
	await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), allies, RageStrength, Creature, null, false);
```

多体遭遇的基类是 `GroupBossEncounter<TMonster>`（`Scripts/encounters.cs`），`Count` 默认 3：

```csharp
protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
	Enumerable.Range(0, Count)
		.Select(_ => (ModelDb.Monster<TMonster>().ToMutable(), (string?)null))
		.ToArray();
```

- 槽位传 `null` 交给引擎自动排布——原版 `SlimesNormal` 就是 4 只全 `null` 且不覆写 `Slots`；
- 每次都要 `.ToMutable()`，否则三只会共享同一个可变实例；
- 想改成 4 只只需调 `Count`，但要同步下调伤害，否则总输出会爆。

多段招式用一条命令打多次：`DamageCmd.Attack(TwinDamage).WithHitCount(2)...`，意图上的 `MultiAttackIntent(3,2)` 必须和它保持一致。

美术：`scenes/creature_visuals/tearscity_monster_watcher.tscn` 用 `Tearscity/images/monsters/watcher.png`（295x339，×0.83 ≈ 245x281，`Bounds` ±133/-285）。

## collector（Boss，开罐放小怪）

`Monsters/collector.cs` + `CollectorEncounter`：**收藏家本体 + 罐子里的藏品（`folly`）**。它自己输出中等，压力来自时不时砸开一个罐子，放出同一幕的弱怪来咬你。

| 状态 ID | 意图 | 效果（普通/苦难） | 权重 |
|---|---|---|---|
| `CLAW` | `SingleAttackIntent` | 11/13 | 3 |
| `JAR_TOSS` | `MultiAttackIntent` | 3×2 / 4×2 | 2 |
| `COLLECT` | `DefendIntent` + `DebuffIntent` | 自身格挡 6/8 + 玩家 1/2 易伤 | 1 |
| `RELEASE` | `SummonIntent` | 开罐，召唤 1 只藏品（第二形态最多同时 2 只） | 2 |
| `JAR_SLAM` | `SingleAttackIntent` | 9/11（**罐位不够时替换 `RELEASE`**） | — |
| `RAGE` | `SingleAttackIntent` + `BuffIntent` | 10/12 伤害 + 自身 +1/2 力量（半血时插入一次） | — |
| `STORM` | `MultiAttackIntent` | 3×3 / 4×3（第二形态） | — |

HP 136-144（苦难 146-154）。三条规则：

1. **藏品阵亡 → 收藏家 +1 力量，最多叠 3 层**（`AfterDeath` 里判断死的是不是 `folly`）。所以"清小怪"不是纯赚：小怪不打你，但它的死会让 boss 越打越疼 —— 不过**力量有上限**，长局不会无限滚雪球。
   > 力量是**每段都加**的，所以多段招式（`JAR_TOSS` 2 段、`STORM` 3 段）会被放大 N 倍：因此这两招的基础值压到 3/4，并且力量总量封顶 —— 正常 +4（3 层罐子 + 1 点 `RAGE`）、苦难 +5，满层时 `STORM` 是 7×3=21（苦难 9×3=27）、`CLAW` 15 / 18、`JAR_TOSS` 14 / 18。
2. **半血进入第二形态**：出一次 `RAGE`，之后招式池换成 `STORM`，罐位上限从 1 变 2。
3. **收藏家阵亡 → 剩下的罐子一起碎**（`ClearJars()` 里 `CreatureCmd.Kill`），免得打完 boss 还要再补刀。

`RELEASE` / `JAR_SLAM` 靠条件状态二选一（罐位不够时自动改成砸过去，意图不会骗人）：

```csharp
var summon = new ConditionalBranchState("SUMMON_SEL");
summon.AddState(release, CanSummon);   // 有空槽 && 罐子数 < MaxJars
summon.AddState(jarSlam, () => true);  // 兜底必须有，否则抛 "No valid next state found."

var next = new ConditionalBranchState("NEXT");
next.AddState(rage,   () => !_phase2 && IsHalfHp);
next.AddState(phase2, () => _phase2);
next.AddState(phase1, () => true);
```

### 为什么这场遭遇必须自带一个场景

小怪是**战斗中途**加进场的。原版的 `NCombatRoom.PositionEnemies` 只在开打时跑一次，之后补进来的怪只能按 `EncounterModel.Slots` 里的名字去场景里找 `Marker2D` 定位；`EncounterSlots` 为 null 时它只打一条 warning，怪就停在原地。所以：

- `EncounterModel.HasScene` 为 true，且 `ScenePath` 必须存在 → RitsuLib 侧写 `EncounterAssetProfile.EncounterScenePath`；
- 场景路径由内容 ID 推出来：`res://scenes/encounters/tearscity_encounter_collector_encounter.tscn`；
- 场景根必须是 `Control`（`CreateScene()` 是 `Instantiate<Control>()`），子节点是槽位标记：

| 槽位 | 坐标 | 谁占 |
|---|---|---|
| `jar1` | (1110, 710) | 第一只藏品 |
| `collector` | (1430, 715) | 收藏家本体（`GenerateMonsters` 里写死） |
| `jar2` | (1750, 710) | 第二形态的第二只 |

坐标照抄原版 `fabricator_normal.tscn` / `two_tailed_rats_normal.tscn` 的量级（脚底 y≈700，敌人在右半屏）。召唤本身是两行：

```csharp
var slot = encounter.GetNextSlot(combatState);              // Slots 里第一个没人占的
if (!string.IsNullOrEmpty(slot))
	await CreatureCmd.Add<FollyMonster>(combatState, slot); // 内部 ModelDb.Monster<T>().ToMutable()
```

`ExtraAssetPaths` 里补了`folly`的视觉场景，让它进战斗预加载，避免第一次开罐时才去读资源。

美术：`tearscity_monster_collector.tscn` 用 `Tearscity/images/monsters/collector.png`（307x417，×1.04 ≈ 319x431，`Bounds` ±162/-437，`IntentPos` y=-485，四只手臂举过头顶所以 `Bounds` 比 watcher 高）。

## 事件（gorgeous / 华美的躯壳）

`Events/gorgeous.cs` + `[RegisterActEvent(typeof(Acts.Tearscity))]`，进幕的 `AllEvents` 由 RitsuLib 自动追加。

| 选项 | 文本 key | 效果 |
|---|---|---|
| 侧身离开 | `...pages.INITIAL.options.LEAVE` | 失去 5 点生命（`ValueProp.Unblockable \| Unpowered`），然后 `SetEventFinished` |
| 夺走它的宝物 | `...pages.INITIAL.options.FIGHT` | `EnterCombatWithoutExitingEvent<GorgeousEventEncounter>([GoldReward(150)], false)`（打的是真·华美躯壳：会偷金币） |

内容 ID：类 `GorgeousEvent` → `TEARSCITY_EVENT_GORGEOUS_EVENT`，本地化表是 **`events.json`**（文件名=表名），key：

```
TEARSCITY_EVENT_GORGEOUS_EVENT.title
TEARSCITY_EVENT_GORGEOUS_EVENT.loss
TEARSCITY_EVENT_GORGEOUS_EVENT.pages.INITIAL.description
TEARSCITY_EVENT_GORGEOUS_EVENT.pages.INITIAL.options.<LEAVE|FIGHT>.title
TEARSCITY_EVENT_GORGEOUS_EVENT.pages.INITIAL.options.<LEAVE|FIGHT>.description
TEARSCITY_EVENT_GORGEOUS_EVENT.pages.LEFT.description
```

关键点（和怪物完全不同的地方）：

- **`IsShared => true` 必须写**：`EnterCombatWithoutExitingEvent` 对非 shared 事件直接抛 `Tried to enter combat in non-shared event`；
- 战斗用 `EnterCombatWithoutExitingEvent<TEncounter>(extraRewards, shouldResumeAfterCombat: false)`：`false` = 打完不回事件页面，直接结算；额外奖励（金币/遗物/药水）在战斗胜利的奖励界面发放；
- 这句只有泛型版本好用：`<TEncounter>` 内部就是 `ModelDb.Encounter<T>()`；
- 数值走 `CanonicalVars`（`HpLossVar(5)` / `GoldVar(150)`），本地化里用 `{HpLoss}`、`{Gold}` 占位；
- 选项上想显示"会造成伤害"的提示，用 `EventOption.ThatDoesDamage(decimal)`（链式返回自身）；
- 事件没有"场景"，只有**大图**：`EventAssetProfile.InitialPortraitPath` → `res://Tearscity/images/events/gorgeous.png`（1920×1080；原版是 3440×1613 的横图，UI 里 `Portrait` 用 KeepAspectCentered，所以非 2.13 比例会左右留透明边）；
- 背景场景 `BackgroundScenePath` 只有**先古之民**那类事件才需要，普通事件会去找 `events/background_scenes/<entry>.tscn`，`PreloadManager.Cache` 取不到就是 null，不报错；
- 换成自己的怪：新增一个**事件专用**遭遇（照旧打 `[RegisterActEncounter(typeof(Acts.Tearscity))]` 才能在 `ModelDb` 里取到，但把 `IsValidForAct` 覆写成 `false`——RitsuLib 的 `ModEncounterActValidityFilter` 会把它从该幕的地图遭遇池里过滤掉，只留给事件用），再把 `EnterCombatWithoutExitingEvent<...>` 的类型换掉。现在用的就是这条路：`GorgeousEventEncounter` + `GORGEOUS`（会偷金币的华美躯壳）。

测试：控制台 `event TEARSCITY_EVENT_GORGEOUS_EVENT`（`event` 命令读的是 `ModelDb.AllEvents`，包含 mod 事件，Tab 可补全）。

## 事件一览（13 个自建，与原版第一幕等量）

都是 `ModEventTemplate` + `[RegisterActEvent(typeof(Acts.Tearscity))]`；`Acts.Tearscity.AllEvents` 现在**只返回这 13 个自建事件**（`AllEvents` 显式列出，不依赖自动追加；`TrialEvent` 因为要开战所以 `IsShared => true`）。数值写死在代码里，文本里也写死数字（改的时候两处一起改）。

> **光覆写 `AllEvents` 不够**：`ActModel.GenerateRooms(rng, unlockState, ...)` 拼事件池时写的是
> `AllEvents.Concat(ModelDb.AllSharedEvents).ToList()`，而 `ModelDb.AllSharedEvents` 是游戏里**写死的 19 个原版事件**：
> BrainLeech / CrystalSphere / DollRoom / FakeMerchant / MuseumOfTorture / PotionCourier / RanwidTheElder /
> RelicTrader / RoomFullOfCheese / SelfHelpBook / SlipperyBridge / StoneOfAllTime / Symbiote / TeaMaster /
> TheFutureOfPotions / TheLegendsWereTrue / ThisOrThat / WarHistorianRepy / WelcomeToWongos。
> 这 19 个跟当前是哪一幕无关，每一幕都会混进去，所以随机事件房会抽到原版事件。
> `Scripts/Patches/TearscityEventPoolPatch.cs` 在房间生成之后按「只保留本幕自建事件」过滤一遍 `RoomSet.events`，
> 只对泪水之城生效；生效时日志会打印 `[Tearscity] 事件池：保留 13 个自建事件，剔除原版共享事件 19 个`。
> 注意事件池是**进幕时**生成的，改完要开新的一局（或重新进第一幕）才看得到效果。

| 事件 | ID | 选项（数值） |
|---|---|---|
| 华美的躯壳 | `TEARSCITY_EVENT_GORGEOUS_EVENT` | 侧身离开（-5 HP）/ 夺走宝物（进战斗，胜利 +150 金币） |
| 圣所祭坛 | `TEARSCITY_EVENT_SOUL_ALTAR_EVENT` | 伸手触碰（-8 HP，**最大生命 +4**）/ 刮下金屑（+55 金币） |
| 泪泉 | TEARSCITY_EVENT_FOUNTAIN_EVENT | **三个愿望任选，代价都是最大生命 -4**：回满 HP / +120 金币 / 1 件随机遗物；或离开 |
| 看守者的地窖 | `TEARSCITY_EVENT_CELLAR_EVENT` | 撬开钱箱（+70 金币、-7 HP）/ 喝一杯陈酒（回 12、+1 随机药水） |
| 收藏者的壁橱 | `TEARSCITY_EVENT_CLOSET_EVENT` | 砸开罐子（-6 HP，+25 金币 +1 随机遗物）/ 拿走地上金币（+40 金币） |
| 雨中的长椅 | `TEARSCITY_EVENT_BENCH_EVENT` | 坐下歇一会儿（回 20）→ **二段**：睡到天亮（回满 HP，但获得 1 张「受伤」）/ 起身；或翻凳子底下（+30 金币） |
| 下水道口 | `TEARSCITY_EVENT_SEWER_EVENT` | 钻下去（**进战斗**：钟蝇群，胜利 +80 金币 +1 药水）/ 只伸手进去摸（**一半得遗物、一半 -8 HP**） |
| 泪水集市 | TEARSCITY_EVENT_MARKET_EVENT | 买旧护符（-60 金币 → 1 遗物）/ 卖东西（+35 金币、-6 HP）/ **顺走钱匣**（+90 金币、-10 HP、获得 1 张「怀疑」）/ 离开 |
| 灵魂贩卖机 | TEARSCITY_EVENT_VENDING_EVENT | 投币（-40 金币 → 1 药水）/ **把一点灵魂塞进细缝（最大生命 -4 → 1 遗物）** / 砸开（-6 HP，+30 金币）/ 离开 |
| 废弃的手推车 | `TEARSCITY_EVENT_CART_EVENT` | 翻车斗（+45 金币）→ **二段**：继续往深处翻（**+60 金币，但带出 1 张「受伤」**）/ 收手；或撬底板（-4 HP → 1 药水） |
| 城市档案室 | TEARSCITY_EVENT_ARCHIVE_EVENT | 读下去（-30 金币，最大生命 +3）/ **抄一页（获得 1 张「受伤」，最大生命 +2）** / 塞背包（-5 HP，+60 金币）/ 离开 |
| 守望者高塔 | `TEARSCITY_EVENT_SPIRE_EVENT` | 爬上去（-8 HP，+70 金币）→ **二段**：把供奉还回去（**-60 金币 → 1 遗物 + 回 10 血**，钱不够时只回血）/ 抱着供奉下塔；或塔下祈祷（回 15） |
| 看守者的试炼 | `TEARSCITY_EVENT_TRIAL_EVENT` | 接受试炼（**进战斗**：灵魂战士，胜利 +150 金币 +1 遗物）/ 低头认输（-3 HP） |

**"离开"只保留在"其他选项全都要付出代价"的 4 个事件上**（泪水集市 / 灵魂贩卖机 / 城市档案室 / 泪泉），另外「华美的躯壳」保留带 5 HP 代价的「侧身离开」；其余 8 个事件已经删掉纯"离开"选项，逼你在几种"要付什么"里选。

设计原则：**每个事件都要有一个"真取舍"，而不是纯加金币/加减血**。四类做法来回用：

1. **代价换收益**：最大生命 -4 换遗物（贩卖机 / 泪泉三愿望）、获得诅咒「受伤」换回满/换最大生命（长椅 / 档案室抄页）、诅咒「怀疑」换 90 金币（集市顺钱匣）；
2. **二段决策**（`SetEventState(PageDescription("PAGE"), options)`）：长椅（坐下→要不要睡到天亮）、手推车（翻车斗→要不要往深里翻）、高塔（爬上去→要不要掀供台）——第一段小赚，第二段拿大件但代价更高；
3. **赌**：下水道口"伸手摸"用 `Rng.NextBool()` 现掷，一半遗物一半 -8 HP（文案明说"一半的机会"，不骗人）；
4. **战斗**：下水道口钻下去打钟蝇群（+80 金币 +药水）、看守者的试炼打灵魂战士（+150 金币 +遗物）。

"金币不够"的三个事件（集市 / 贩卖机 / 档案室）会退化成离开页面，不会卡住。

**坑**：`EventModel.OptionKey(page, option)` 在 `ModEventTemplate` 里拿不到（编译不过），二段选项的 key 用 `InitialOptionKey(opt).Replace(".pages.INITIAL.options.", ".pages." + page + ".options.")` 推出来（`PageOption` 辅助方法，三个二段事件里各一份）。

美术统一 1920×1080。12 张合成图现在**直接铺在《空洞骑士》原版的泪水之城背景上**（`CityofTearsBack.png`，1920×1080，从本机 `ActsOfHallownestBeta` pck 里提取），再叠怪物贴图 + 光晕/暗角/雨，风格和原版游戏一致；`gorgeous.png` 是用户提供的手绘图。

| 图片 | 合成素材 |
|---|---|
| `events/gorgeous.png` | 用户提供的手绘图 |
| `events/soul_altar.png` | 原版 CoT 背景 + `soulwarrior.png` + 蓝光 |
| `events/fountain.png` | 原版 CoT 背景 + 手绘水池 + 冷光 |
| `events/cellar.png` | 原版 CoT 背景暖调 + `heavy.png` + 光点 |
| `events/closet.png` | 原版 CoT 背景紫调 + `collector.png` + 手绘罐子 |
| `events/bench.png` | 原版 CoT 背景 + 手绘石凳 + 暖光 |
| `events/sewer.png` | 原版 CoT 背景绿调 + `belfly.png` + 栅栏 |
| `events/market.png` | 原版 CoT 背景金调 + `huskdandy.png` + 金点 |
| `events/vending.png` | 原版 CoT 背景紫调 + `soul.png` + 手绘机器 |
| `events/cart.png` | 原版 CoT 背景暖调 + `gluttonoushusk.png` + 车轮 |
| `events/archive.png` | 原版 CoT 背景 + `mistake.png` + 手绘书架 |
| `events/spire.png` | 原版 CoT 背景 + 光束 + `watcher.png` |
| `events/trial.png` | 原版 CoT 背景红调 + `great.png` |

为了让 pck 不至于太大，12 张合成图存成 256 色索引 PNG（每张约 0.7-0.8MB，肉眼看不出差别）；`gorgeous.png` 保持原样。

**数量对比**：原版第一幕 `AllEvents` 是 **13 个**；本幕现在 **13 个自建**，一比一。想再加量就继续往 `Acts/Tearscity.cs` 的 `AllEvents` 里加（每加一个要配 `Events/<name>.cs` + `zhs/events.json` 的文案 + 一张 1920×1080 图）。

## 泪水之城的敌人（完整）

按空洞骑士「泪水之城」的原版敌人表，本幕现在是**完全自建**的敌人池：16 种会进池的敌人 + 1 种事件专属（`GORGEOUS`），共 **17 种**。原版第一幕（Overgrowth）的对比基数是「27 个 MonsterModel / 约 20 个敌人概念（史莱姆 4 变体算 1、盗贼 5 变体算 1）」，所以数量级是对齐的。数值都写成 `普通 / 苦难`，苦难用 `AscensionLevel.ToughEnemies`（血量）和 `DeadlyEnemies`（伤害）。

| 怪物 | 类型 | HP | 出招 | 机制一句话 |
|---|---|---|---|---|
| `HUNKSENTRY` 躯壳哨兵 | 普通 | 原来就有 | `BASIC_ATTACK` / `HEAVY_ATTACK` | 基础剑士 |
| `MISTAKE` 错误 | 弱怪 | 20-24 / 24-28 | `BUMP` 4/5、`OOZE` 2×2/3 + 格挡 4/5、`MELT` +2/3 力量 + 格挡 5/7 + 自伤 2 | 融化自己换力量，拖不得 |
| `COWARDLY` 胆小躯壳 | 弱怪 | 24-28 / 28-32 | `SCURRY` 格挡 7/9、`FLURRY` 3×2/4×2、`PANIC` 5/6 + 虚弱 1/2、`CORNERED` 8/10 + 力量 2/3 | 半血前只会逃和乱抓，半血后"拼命"一次并换攻击池 |
| `GLUTTONOUS` 贪食躯壳 | 普通 | 42-48 / 46-52 | `SLAP` 8/9 + 回血 3/4、`GORGE` 4/5 + 格挡 6/8 + 回血 6/7、`BELLY_FLOP` 12/14 + 回血 2 | 打中你就回血，必须一口气打死 |
| `BELFLY` 钟蝇 | 普通 | 14-18 / 18-22 | `BUZZ` 3/4 → `FUSE` 引信（自伤 2、不出手）→ `BOOM` 9/11 后**自爆死亡** | 三回合定时炸弹：多给一回合拆弹，爆得更轻但总量不变 |
| `WINGED` 飞翼哨兵 | 普通 | 30-34 / 34-38 | `JAB` 4×2/5×2、`HOVER` 格挡 8/10 + 力量 1/2、`DIVE` 10/12 | 盘旋攒力量后俯冲 |
| `LANCE` 长矛哨兵 | 普通 | 36-40 / 40-44 | 固定循环 `PROD` 6/7 → `BRACE` 格挡 10/12 + 力量 2/3 → `LUNGE` 12/14 | 节奏固定，考格挡规划 |
| `HUSK_DANDY` 花花公子躯壳 | 普通 | 30-34 / 34-38 | `CANE_SWIPE` 3×2/4×2、`STRIKE_A_POSE` 格挡 8/10 + 力量 2/3、`BOW` 5/6 + 虚弱 1/2 | 摆姿势越摆越强，得在它摆完前压住 |
| `SOUL_WARRIOR` 灵魂战士 | 精英 | 82-90 / 90-98 | `SLASH` 10/12、`DOWN_SLASH` 6×2/7×2、`BULWARK` 格挡 12/15 + 力量 1/2、`SOUL_BALL` 4×2/5×2 + 虚弱 1/2 | 近战 + 灵魂弹，蓄魂后很硬 |
| `SOUL` 灵魂扭曲者 | 精英 | 原来就有 | — | — |
| `GREAT` 大型躯壳哨兵 | 精英 | 原来就有 | — | — |
| `HEAVY` 重型哨兵 | 精英 | 原来就有 | — | — |
| `MASTER` 灵魂大师 | Boss | 原来就有 | — | 两条血 + 空窗复活 |
| `WATCHER` 守望者骑士 | Boss | 原来就有 | — | 三只同屏 |
| `COLLECTOR` 收藏家 | Boss | 原来就有 | — | 开罐放小怪 |
| `GORGEOUS` 华美躯壳 | 事件专属 | 70-78 / 80-90 | `GILDED_SLAP` 11/13 + **偷 10 金币**、`ADORN` 格挡 14/16 + 力量 2/3、`SPARKLE` 4×2/5×2 + 虚弱 1/2 | 打你就顺走你的钱，越拖越破财 |

对应遭遇（`Scripts/encounters.cs`）：

| 遭遇 | 池 | 说明 |
|---|---|---|
| `FollyEncounter` / `MistakeEncounter` / `CowardlyHuskEncounter` | 弱怪 | 各单只 |
| `FollyAndMistakeWeakEncounter` | 弱怪 | 愚蠢 + 错误（`WeakPairEncounter`） |
| `HunksentryEncounter` / `GluttonousHuskEncounter` / `WingedSentryEncounter` / `LanceSentryEncounter` / `HuskDandyEncounter` | 普通 | 各单只 |
| `HunkSentryPairEncounter` | 普通 | 躯壳哨兵 ×2 |
| `SentryDuoEncounter` | 普通 | 飞翼哨兵 + 长矛哨兵（空陆混编） |
| `HuskDuoEncounter` | 普通 | 贪食躯壳 + 胆小躯壳 |
| `DandyAndFollyEncounter` | 普通 | 花花公子躯壳 + 愚蠢 |
| `MistakeGroupEncounter` | 普通 | 错误 ×2 |
| `BelflySwarmEncounter` | 普通 | `GroupEncounter<BELFLY>`，**同时三只**（全炸 ≈27-33，但第 3 回合才炸，够时间拆两只） |
| `SoulEncounter` / `SoulWarriorEncounter` / `GreatEncounter` / `HeavyEncounter` | 精英 | 各单只 |
| `MasterEncounter` / `WatcherEncounter` / `CollectorEncounter` | Boss | — |
| `GorgeousEventEncounter` | **不进池** | `IsValidForAct => false`，只给「华美的躯壳」事件用 |

`GenerateAllEncounters` 现在只有这一份列表（原来的 `TestEncountersOnly` 开关已删除），本幕不再混入原版遭遇；`GroupEncounter<TMonster>` 是普通房间多体遭遇基类（`RoomType.Monster`、`Count` 默认 2）。

**美术来源**：全部从本机 `ActsOfHallownestBeta` pck 里的**空洞骑士原版贴图**派生（`.ctex` → `load()` → `get_image()` → 按 alpha 列切帧 → 去掉 idle 残影 → PNG）：

| 贴图 | 做法 |
|---|---|
| `mistake` / `cowardlyhusk` / `gluttonoushusk` / `belfly` / `soulwarrior` | 原版 idle 帧直接用 |
| `wingedsentry` | 原版 `HuskSentry` 帧 + 手绘一对翅膀 |
| `lancesentry` | 用户提供的图（长矛哨兵） |
| `huskdandy` | 用户提供的图（尖帽躯壳） |
| `gorgeoushusk` | 用户提供的图（金色圆躯壳） |

`wingedsentry / lancesentry / huskdandy / gorgeoushusk` 四张现在是**用户提供的原画**（webp 转 PNG，脚本按“从边缘 flood fill 白底”抠透明，保留内部的白色脸/金色高光）。替换图时要同步改场景里的 scale/Bounds，当前值：winged 215×208×1.442、lance 326×205×1.463、dandy 89×211×1.043、gorgeous 183×234×1.282。

**飞行怪的位置**：`wingedsentry` 整体上浮 70 单位（脚底 y≈-70）、`belfly` 上浮 90，Bounds 同步改成“悬空”范围（底部不再贴地）。

## 测试时怎么稳定遇到自己的怪

**方式一：遭遇池已经全是自己的**

`GenerateAllEncounters` 现在返回的就是本幕完整列表（3 弱怪 / 6 普通 / 4 精英 / 3 Boss），不再有"和原版混池"的开关；RitsuLib 还会把 `[RegisterActEncounter]` 的遭遇再并一次（按 `Id` 去重，不会重复）。

- 弱怪正好 3 个（`NumberOfWeakEncounters = 3`，池子不能少于这个数）；
- 普通 6 个，地图要 12 个普通房间，`GrabBag` 抽空后重填，所以一局里会重复遇到；
- 精英 4 个、Boss 3 个，两者都不能空；
- **Boss 必须留**：`AllBossEncounters = AllEncounters.Where(RoomType == Boss)`，Boss 池空了
  `BossEncounter` 就选不出来，读档时 RitsuLib 会报 **"boss 未设置"**（`RepairBoss` 找不到候选）；
- **Boss 实际出谁由 `BossDiscoveryOrder` 决定**：`RunManager.GenerateRooms` 先随机塞一个 Boss 进
  `_rooms`，随后 `ActModel.ApplyDiscoveryOrderModifications` 会用 `BossDiscoveryOrder` 里
  **第一个还没见过的（`UnlockState.HasSeenEncounter`）** 覆盖它。所以新档第一次必是 `Master`，
  之后依次 `Watcher` → `Collector`，三个都见过之后才开始在 Boss 池里随机。
  想改成纯随机就把这个属性返回空数组。

**方式二：控制台精确指定（`fight` 命令）**

1. `Scripts/Entry.cs` 里 `EnableFullConsole = true`（默认已开）——它把 `settings.save` 的
   `FullConsole` 打开，`fight` 这类 debug 命令才会注册；
2. **重启一次游戏**（控制台在 mod 载入之前就建好了，所以开关要下一次启动才生效）；
3. 开局后按 `` ` ``（反引号/波浪键，`*`、`'`、`^` 也行）呼出控制台；
4. 输入：

```
fight TEARSCITY_ENCOUNTER_HUNKSENTRY_ENCOUNTER         # hunksentry
fight TEARSCITY_ENCOUNTER_FOLLY_ENCOUNTER         # folly
fight TEARSCITY_ENCOUNTER_SOUL_ENCOUNTER          # soul（精英）
fight TEARSCITY_ENCOUNTER_SOUL_WARRIOR_ENCOUNTER  # 灵魂战士（精英）
fight TEARSCITY_ENCOUNTER_GREAT_ENCOUNTER         # great（精英）
fight TEARSCITY_ENCOUNTER_HEAVY_ENCOUNTER         # heavy（精英）
fight TEARSCITY_ENCOUNTER_MASTER_ENCOUNTER        # master（Boss，两条血）
fight TEARSCITY_ENCOUNTER_WATCHER_ENCOUNTER       # watcher（Boss，三只）
fight TEARSCITY_ENCOUNTER_COLLECTOR_ENCOUNTER     # collector（Boss，开罐放小怪）
fight TEARSCITY_ENCOUNTER_MISTAKE_ENCOUNTER       # 错误（弱怪）
fight TEARSCITY_ENCOUNTER_COWARDLY_HUSK_ENCOUNTER # 胆小躯壳（弱怪）
fight TEARSCITY_ENCOUNTER_GLUTTONOUS_HUSK_ENCOUNTER # 贪食躯壳
fight TEARSCITY_ENCOUNTER_BELFLY_SWARM_ENCOUNTER  # 钟蝇群（两只，会自爆）
fight TEARSCITY_ENCOUNTER_WINGED_SENTRY_ENCOUNTER # 飞翼哨兵
fight TEARSCITY_ENCOUNTER_LANCE_SENTRY_ENCOUNTER  # 长矛哨兵
fight TEARSCITY_ENCOUNTER_HUSK_DANDY_ENCOUNTER    # 花花公子躯壳
fight TEARSCITY_ENCOUNTER_FOLLY_ENCOUNTER 12345   # 可选：指定 rng 种子
```

华美躯壳不在池子里，只能用事件测试：`event TEARSCITY_EVENT_GORGEOUS_EVENT`（第二个选项开打）。

遭遇 ID 就是模型 ID（`<MODID>_ENCOUNTER_<类名>`），控制台里 `fight ` 之后按 Tab 可以补全。

## 音乐与环境声（自己播，不走 FMOD bank）

原版一幕的音乐是 FMOD bank（`res://banks/desktop/act1_a1.bank` + `event:/music/act1_a1_v1`），
要自己做就得装 FMOD Studio、建工程、编 event、导 bank —— 改一次音乐就要重导一次，而且 bank 是二进制，出问题很难查。
所以这里换了条路：**用 Godot 自己的 `AudioStreamPlayer` 直接播 mod 目录下的音频文件，把原版 FMOD 音乐绕过去。**

### 三条路线，按投入从低到高

| 路线 | 怎么做 | 适合 |
|---|---|---|
| 用现成素材 | 找 CC0 / 署名即可的现成循环（Pixabay、Freesound、OpenGameArt），Audacity 裁成 1–2 分钟的循环 | 只想尽快有声音、不挑风格 |
| AI 生成 | Suno / Udio / 本地 ACE-Step 生 1–2 分钟纯音乐，Audacity 把首尾接成无缝循环 | 想要「像样」的氛围，又不想学乐理 |
| 程序生成（本仓库已带） | `tools/gen_music.py` 改参数重新生成 | 想随时微调、零版权、完全可控 |

三条路线的成品都走下面这一条播放链路，**换文件即换音乐**，不用改代码。

### 文件放哪、叫什么

```
mods/Tearscity/assets/music/
├─ tearscity_calm.wav       地图 / 休息处 / 商店 / 事件
├─ tearscity_combat.wav     普通战斗 / 精英 / Boss
└─ tearscity_ambience.wav   常驻环境声（雨声），和音乐同时播
```

- 文件名不能改（代码按名字找），**扩展名随便**：优先 `.ogg`，没有就 `.mp3`，再没有才 `.wav`；
- 换音乐 = 直接覆盖文件，**不用重新编译 dll、不用重导 pck**（`dotnet build` 只是把 `assets\` 复制过去）；
- 必须是**能循环**的素材：播放器设了 loop，接缝有爆音那就是素材本身没做无缝。

### 为什么默认是 WAV，体积有点大

机器上没有 ogg 编码器（Godot 只能解码不能编码），所以默认给 44.1kHz/16bit 立体声 WAV，三首共约 28MB。
想要小体积就转 ogg（音质几乎不变，体积约 1/10），转完把 `.wav` 删掉，代码会自动优先读 `.ogg`：

```powershell
ffmpeg -i tearscity_calm.wav -c:a libvorbis -q:a 5 tearscity_calm.ogg
# 没有 ffmpeg 就用 Audacity：打开 wav → 文件 → 导出 → 导出为 Ogg Vorbis
```

**Suno / ACE-Step 这类 AI 生成的 mp3 可以直接改名丢进来**（`tearscity_calm.mp3` 等）。

### 怎么接进游戏

`Scripts/Acts/Tearscity.cs` 把原版三件套清空：

```csharp
public override string[] BgMusicOptions => Array.Empty<string>();   // ResolveMusic 直接返回 null，不加载 bank
public override string[] MusicBankPaths  => Array.Empty<string>();
public override string AmbientSfx        => string.Empty;
```

`Scripts/Audio/TearscityMusic.cs` 建三个 `AudioStreamPlayer`（load 文件、设循环、交叉淡入淡出、同步音量）；
`Scripts/Patches/TearscityMusicPatch.cs` 用五个 Harmony 补丁把流程接上：

| 补丁 | 干什么 |
|---|---|
| `NRunMusicController.UpdateMusic`（postfix） | 进幕/读档：是泪水之城就开播，不是就淡出（第二幕起交还原版） |
| `NRunMusicController.UpdateAmbience`（prefix） | 本幕跳过原版环境声，用自己的雨声 |
| `NRunMusicController.UpdateTrack(RoomType)`（postfix） | 换房间：`Monster/Elite/Boss` → 战斗曲，其余 → 平静曲（1.6s 交叉淡入） |
| `NRunMusicController.StopMusic` / `NAudioManager.PlayMusic`（prefix） | 结束一局 / 回主菜单：淡出 |
| `NAudioManager.SetBgmVol`（postfix） | 把设置里的音乐音量抄一份，让滑条对我们自己的播放器也生效 |

> 自己播的音频不走 FMOD 总线，所以设置里的「音乐音量」不会自动生效，必须靠最后那条补丁同步。

**总开关**：把 `TearscityMusic.EnableCustomMusic` 改成 `false` 就完全不动原版流程
（配合上面三个空数组 = 本幕没音乐，适合排查问题时快速排除音乐的影响）。

### 音乐本身哪来的：`tools/gen_music.py`

三首都是程序生成的（numpy 合成 + 循环卷积混响，零版权问题），没有用任何采样库：

| 文件 | 编曲 |
|---|---|
| `tearscity_calm` | A 小调 `Am9 – Fmaj7 – Dm9 – E7♭9`，8 秒一个和弦，钢琴动机 + 弦垫 + 低音 + 高音钟声，第二遍加旋律线 |
| `tearscity_combat` | D 小调 `Dm – B♭ – Gm – A`，6 秒一个和弦，八分音符固定音型 + 闷鼓 + 轻打击 + 明亮弦垫 |
| `tearscity_ambience` | 宽带噪声雨幕（缓慢阵风）+ 随机滴水 + 城市低鸣 + 极远钟声 |

改风格不用懂乐理，动文件开头的「可调参数」就行：

```python
CALM_CHORDS = [...]          # 和弦表：(低音 MIDI, 和弦音 MIDI 列表)，换和弦 = 换情绪
CALM_CHORD_SECONDS = 8.0     # 一个和弦几秒（越小越快）
CALM_PIANO_DENSITY = 0.42    # 钢琴密度，0 = 只剩长音铺底
REVERB_SECONDS = 3.2         # 混响长度，越大越「空」
RAIN_LEVEL = 0.21            # 雨声大小
SEED = 20261124              # 换个数字就是另一首
```

```powershell
python tools\gen_music.py --out assets\music             # 三首全生成
python tools\gen_music.py --out assets\music --only calm # 只重做一首
```

产物**天然无缝**：所有音符按环形缓冲写入（越界就绕回开头），混响用 FFT 做循环卷积，
所以首尾接得上，不用自己裁剪。改完参数 `dotnet build` 一次，把新文件复制到 `dist`。

### 用 Suno 生成（替换掉自带的三首）

自带的三首是程序生成的「兜底版本」。想更好听可以用 Suno 生成，流程是**生成 → 转 wav → 裁成无缝循环 → 覆盖同名文件**。

**1. Suno 里生成**：用 Custom 模式，打开 **Instrumental**（不打开会有人声），Style 里堆英文标签，歌词框放结构标签：

```
[Intro]
[Instrumental]
[Main Theme]
[Instrumental Break]
[Outro]
```

三组可复制的 Style（Arvo 提示词里别写具体音乐人名字，Suno 会过滤掉，用描述性标签更稳）：

| 用途 | Style |
|---|---|
| 平静曲 | `dark ambient, solo piano, soft string pad, slow 60 bpm, melancholic, sparse, rain, reverb, no drums, instrumental, loopable` |
| 战斗曲 | `dark orchestral tension, low strings ostinato, sparse percussion, 90 bpm, urgent, restrained, no vocals, instrumental, loopable` |
| 环境声 | `field recording, heavy rain on stone pavement, water dripping into a cistern, distant city rumble, wind gusts. no music, no melody, no instruments, no pads, no voices, no rhythm. 60 seconds, seamless loop` |

> **环境声最容易翻车**：写 `ambient` 会被理解成“氛围音乐”（自动加 pad 和旋律），要写 `field recording` / `soundscape` / `foley`；
> 旋律类的词（piano、strings、bells 作主奏）一个都别留。抽 3–5 版还带音乐，就别跟模型较劲了——
> 直接去 Freesound / Pixabay 找 CC0 雨声，或者用 `gen_music.py` 自带的环境声（`--only ambience`）。

同一组提示词多生几版挑一版；不够长就用 Extend 接长。

**2. 裁成无缝循环**：Suno/Mureka 下的 **mp3 可以直接喂给脚本**（不用先转 wav），但 mp3 解码需要 PyAV，
所以要用 ComfyUI 自带的 python 跑（它装了 `av`）：

```powershell
$py = "C:\Users\ASUS\Downloads\ComfyUI_windows_portable\python_embeded\python.exe"
& $py tools\make_loop.py mureka_calm.mp3 -o assets\music\tearscity_calm.wav --length 60 --fade 2.0 --trim -50
```

wav 输入用普通 python 就行（`python tools\make_loop.py raw.wav -o ...`）。

| 参数 | 作用 |
|---|---|
| `--length 60` | 截多长（Mureka 没有时长设置，时长全靠这里） |
| `--start 30` | 从第 30 秒开始截（跳过旋律化的前奏） |
| `--fade 2.0` | 尾巴叠到开头的长度，越大越柔（环境声用 3.0） |
| `--trim -50` | 自动剪首尾静音 |
| `--lowpass 5000` | 低通，弱化旋律存在感 |
| `--reverb 0.4` | 加混响，把旋律糊进空间里 |

脚本最后会打印「接缝差」，小于 0.01 就基本听不出断点。

**3. 统一响度**：三首之间音量要接近，否则切换时忽大忽小。Audacity 用「效果 → 响度归一化」到
−16 LUFS（音乐）/ −20 LUFS（环境声），或者脚本的 `--peak -1`。

**4. 覆盖 + 构建**：文件名必须是 `tearscity_calm` / `tearscity_combat` / `tearscity_ambience`，
放 `assets\music\`，然后 `dotnet build`。**同名的话要把旧的 `.wav` 删掉**（代码按 `.ogg → .mp3 → .wav` 顺序找，
不删会一直读到旧的）。

### 自测清单

1. 进第一幕：应该听到雨声 + 钢琴（雨声比音乐轻，别盖过音效）；
2. 进一场战斗：1.6 秒内交叉淡到战斗曲；打完回地图淡回平静曲；
3. 设置里拖「音乐」滑条：两首都要跟着变；
4. 回主菜单：本 mod 的音乐淡出，原版主菜单音乐正常；

### 已知限制 / 还能做什么

- Boss / 精英没有单独曲子（现在和普通战斗共用 `combat`）。要分就加 `tearscity_boss.wav`，在 `UpdateTrack` 补丁里把 `Boss` 单独映射过去；
- 没做层叠/动态音乐（比如 Boss 第二形态加一层鼓）。要做就开两个播放器同时播、用音量做「分层」，代码结构已经能支持；
- 目前只有「游戏音乐音量」一条，没有本 mod 自己的音量选项；
- WAV 体积大，发布到创意工坊前建议转 OGG。

## 引擎侧的关键约定（踩过的坑）

1. **类名 = 内容 ID**：RitsuLib 会给 mod 模型加前缀，`Acts.Tearscity` 实际是
   `Id.Entry = TEARSCITY_ACT_TEARSCITY` → `FilePathIdentifier = tearscity_act_tearscity`。
   所以目录、本地化 key（`TEARSCITY_ACT_TEARSCITY.title`）都必须按这个长名来。
2. **mod pck 挂载在 `res://` 根目录**，所以场景保持 `scenes/...`、`images/...` 路径；
   本地化必须在 `res://<manifest.id>/localization/<lang>/<表名>.json`（游戏按 mod id 扫）。
3. **Scripts/.gdignore**：避免 Godot 把 .cs 打进 pck、也避免它要求 sln。
4. **dist/.gdignore + 导出排除**：否则已构建的 dll/pck 会被再次打包进新 pck。
5. 游戏在 `Program Files` 下，普通进程可能写不进；用 `deploy.ps1`（必要时以管理员运行）。
6. **`editor/export/convert_text_resources_to_binary=false`**（`project.godot`）必须保留：
   否则导出会把 `.tscn` 转成 `.scn` 并留一个 `xxx.tscn.remap`，而游戏是用**文件名**拼
   分层背景路径的（`BackgroundAssets`），拿到 `.remap` 就加载失败。原版 pck 里也都是文本 `.tscn`。

## 背景为什么必须在 C# 里拼（`CombatBackgroundPatch`）

原版 `NCombatBackground.Create(BackgroundAssets bg)` 会加载
`res://scenes/backgrounds/<id>/<id>_background.tscn`，并把根节点**强转成 `NCombatBackground`**。

而 mod 的 pck **引用不了游戏自带的 C# 脚本**：导出时 Godot 必须先 `load` 那个
`res://src/Core/Nodes/Rooms/NCombatBackground.cs` 才能建立 `[ext_resource]` 引用，加载不到就直接
`Cannot set object script` 把 script 丢弃（在项目里放同名占位 `.cs` 也没用——会立刻要求 `.sln`）。
运行时根节点退化成普通 `Control`，强转抛出：

```
InvalidCastException: Unable to cast 'Godot.Control' to 'NCombatBackground'
  at NCombatBackground.Create(...) → MoveToMapCoordAction 卡死
```

所以 `CombatBackgroundPatch` 用 Harmony 接管 `Create`：只对
`bg.BackgroundScenePath` 含 `tearscity_act_tearscity` 的情况生效，现场 `new NCombatBackground()`，
按 `BgLayers.Count` 建 `Layer_00/Layer_01/...`（有前景层再加 `Foreground`）全屏容器，
再把 `layers/` 里的分层场景挨个挂进去；其余幕一律放行原版逻辑。

分层目录的硬性规则（`BackgroundAssets` 构造时校验，违反直接抛异常）：
- `layers` 目录必须存在，且**里面不能有子目录**；
- 每个文件名必须含 `_bg_` 或 `_fg_`；
- `_bg_` 文件按 `文件名.Split("_bg_")[1].Split("_")[0]` 分组（`..._bg_00_a.tscn` → 组 `00` → `Layer_00`），
  每组随机选一个；所有 `_fg_` 里随机选一个做前景（一个都没有则无前景，合法）。

## 下一步（等你定）

- [x] 自建 3 个 Boss：`MasterEncounter` / `WatcherEncounter` / `CollectorEncounter`
- [x] 自建遇遭（22 个，见「敌人」一节的表）
- [x] 自建事件（13 个，见「事件一览」一节）
- [x] 全部本地化补齐（含 3 个 Boss 的招式名、 foolish/soul/heavy 的遇遭名）
- [x] 自建先古之民（现用原版 Neow，与原版第一幕一致）
- [x] 正式美术：战斗背景（单张全景 + 动态雨幕）、地图三段羊皮纸、休息处（带石凳，人物可坐）
- [x] 事件大图全部重绘为 AI 生成的雨城场景，统一调色
- [x] 收尾之一：删掉休息处调试补丁与战斗背景/幕 Boss 的诊断日志，manifest 描述补全
- [x] 音乐/环境音（自建三轨，见「音乐与环境声」一节；不再沿用 act1 的 bank）
- [ ] Boss/精英的 Spine 骨骼动画（现在用 idle/stagger 两张图淡入淡出）
- [ ] 收尾之二：关掉 Entry.EnableFullConsole
- [ ] 收尾之三：美术版权（怪物贴图来自空洞骑士官方素材，仅可私下使用，公开发布需换原创图）

## 第二幕：真菌荒地（替换第 2 幕）

代码：Scripts/Acts/Fungalwastes.cs（幕本体）、Scripts/encounters_fungal.cs（17 个遭遇）、
Monsters/fungling.cs 等 10 个怪物文件。注册在 Scripts/Entry.cs：

`csharp
content.RegisterAct(typeof(Acts.Fungalwastes));
content.RegisterActEnterForce<Acts.Fungalwastes>(1, 100, ctx => ctx.EnteringActIndex == 1);
`

内容 ID / 资源目录按类名推导：TEARSCITY_ACT_FUNGALWASTES → 	earscity_act_fungalwastes。

### 音乐 / 事件 / Boss：全部沿用原版第二幕（数值写死，不做运行时转发）

早期版本是 ModelDb.Act<Hive>() 转发给原版幕实例，后来改成把原版数值直接写进代码 ——
少一次运行时查表，也不会因为幕注册顺序出问题。这些值是 IL 从 MegaCrit.Sts2.Core.Models.Acts.Hive 抠出来的：

| 成员 | 值 |
|---|---|
| BgMusicOptions | event:/music/act2_a1_v2、event:/music/act2_a2_v2 |
| MusicBankPaths | 
es://banks/desktop/act2_a1.bank、ct2_a2.bank |
| AmbientSfx | event:/sfx/ambience/act2_ambience |
| ChestOpenSfx / 宝箱 spine | 	reasure_act2 / chest_room_act_2_skel_data.tres（skin ct2 / ct2_stroke） |
| 房间数 / 弱怪数 | BaseNumberOfRooms = 14、NumberOfWeakEncounters = 2 |
| 地图点分布 | NextGaussianInt(6, 1, 6, 7) + StandardRandomUnknownCount |
| 事件（10 个） | Amalgamator / Bugslayer / ColorfulPhilosophers / ColossalFlower / FieldOfManSizedHoles / InfestedAutomaton / LostWisp / SpiritGrafter / TheLanternKey / ZenWeaver |
| 先古之民（3 个） | Orobas / Pael / Tezcatara |
| Boss 顺序 | TheInsatiable → KnowledgeDemon → KaiserCrab（原版顺序） |

地图配色换了紫色的（2A1B36 / 9A85B8 / 1A1420），和真菌荒地一致。

TearscityEventPoolPatch 和 TearscityMusicPatch 都只认 Acts.Tearscity，所以对第二幕不生效 —— 第二幕的音乐/共享事件保持原版行为。

### 离线自检脚本

_verify/check_fungal_content.py 会扫一遍源文件，检查：内容 ID 与本地化 key 是否一一对应（怪物名/招式名/遭遇名/败北文案/幕名）、
怪物镜头场景与贴图是否存在、每个怪物是否被至少一场遭遇引用、HP 与伤害是否落在原版第二幕的数值区间内。
最后一次运行：FAILURES = 0（10 怪 / 17 遭遇 = 4 弱 + 10 普通 + 3 精英 + 3 原版 Boss）。

### 普通怪 / 精英（10 种，数值对标原版第二幕）

原版第二幕参考值（IL 提取）：杂兵 26–49 HP、普通 63–141 HP、精英 165–171 HP；
单击伤害杂兵 6–12、普通 9–16、精英 17–22、Boss 18+。

| 怪物 | 中文名 | HP（普通/进阶） | 招式（伤害） | 机制 |
|---|---|---|---|---|
| FUNGLING | 真菌幼虫 | 30–34 / 28–32 | 啃咬 6、蹦跳 4 | 蹦跳附带 1 层虚弱 |
| SHRUMELING | 小菌菇 | 16–20 / 14–18 | 弹跳 4 | 纯杂兵，成群出现 |
| AMBLOOM | 孢花 | 34–40 / 32–38 | 孢子弹 5、绽放 | 绽放给 6 格挡 + 1 层虚弱 |
| FUNGIFIED_HUSK | 菌化躯壳 | 36–42 / 34–40 | 扑咬 8、膨胀、爆裂 12 | 膨胀加 10 格挡，下一回合爆裂后自尽 |
| FUNGOON | 真菌气囊 | 64–72 / 60–68 | 吐孢 9、孢子云 | 孢子云加 8 格挡 + 2 层虚弱 |
| SPORG | 游孢 | 50–58 / 46–54 | 漂浮、爆裂 7 | 漂浮加 10 格挡 + 1 力量，爆裂附带 1 层脆弱 |
| SHRUMAL_WARRIOR | 菌战士 | 80–90 / 76–86 | 冲锋 12、践踏 6×2、戒备 | 戒备加 12 格挡 + 2 力量；**死亡时放出 2 只小菌菇** |
| MANTIS_YOUTH | 螳螂幼体 | 58–66 / 54–62 | 镰击 5×2、俯冲 11 | 俯冲附带 1 层易伤 |
| SHRUMAL_OGRE | 菌巨魔（精英） | 170–180 / 162–172 | 狂砸 18、吐孢 9×2、蓄势 | 蓄势加 14 格挡 + 3 力量；**死亡时放出 3 只小菌菇** |
| MANTIS_WARRIOR | 螳螂战士（精英） | 165–175 / 158–168 | 横斩 9×2、上挑 20、战舞 | 战舞加 12 格挡 + 2 力量 |

### 遭遇（4 弱 / 10 普通 / 3 精英 + 3 原版 Boss）

弱点：FunglingPairEncounter、ShrumelingSwarmEncounter（3 只）、AmbloomEncounter、FungifiedHuskEncounter
普通：FungoonEncounter、SporgEncounter、ShrumalWarriorEncounter、MantisYouthEncounter、
FunglingPackEncounter（幼虫+游孢）、ShrumalDuoEncounter（2 菌战士）、MantisYouthDuoEncounter、
AmbloomFunglingEncounter、SporgDuoEncounter、FungoonPairEncounter
精英：ShrumalOgreEliteEncounter、MantisWarriorEliteEncounter、MantisPatrolEliteEncounter（幼体+菌战士）

**带召唤的 4 场遭遇自带场景**（scenes/encounters/tearscity_encounter_*_encounter.tscn，根节点 Control + 每个槽位一个 Marker2D）：

| 遭遇 | 场景 | 槽位 |
|---|---|---|
| ShrumalWarriorEncounter | ..._shrumal_warrior_encounter.tscn | warrior / s1 / s2 / s3 |
| ShrumalDuoEncounter | ..._shrumal_duo_encounter.tscn | warrior1 / warrior2 / s1 / s2 / s3 |
| ShrumalOgreEliteEncounter | ..._shrumal_ogre_elite_encounter.tscn | ogre / s1 / s2 / s3 |
| MantisPatrolEliteEncounter | ..._mantis_patrol_elite_encounter.tscn | youth / warrior / s1 / s2 |

死亡召唤走原版召唤流程：AfterDeath → CombatState.Encounter.GetNextSlot(...) → CreatureCmd.Add<Shrumeling>(...)；
槽位空了就自动跳过（不会卡死），参考实现是收藏家（collector.cs）。

### 美术（当前为占位）

| 资源 | 路径 |
|---|---|
| 战斗背景 | Tearscity/images/bg/fungal_placeholder.png（程序生成的蘑菇剪影 + 紫青雾气） |
| 休息处 | Tearscity/images/fungal_rest_site_bg.png |
| 地图三段 | images/packed/map/map_bgs/tearscity_act_fungalwastes/（复用泪水之城羊皮纸并染色） |
| 怪物贴图 | Tearscity/images/monsters/fungal_*.png（从空洞骑士 mod 包提取，私人用途） |

替换美术只要保持同名同路径；怪物镜头场景在 scenes/creature_visuals/tearscity_monster_fungal*.tscn 之外的
	earscity_monster_<key>.tscn（key = fungling / shrumeling / …）。

### 测试

`
act TEARSCITY_ACT_FUNGALWASTES     # 直接换成真菌荒地幕
act 2                              # 或者跳到第 2 幕
fight TEARSCITY_ENCOUNTER_SHRUMAL_OGRE_ELITE_ENCOUNTER
room RestSite
`

Boss 是原版的，Boss 遭遇 ID 用 ight KAISERCRAB_BOSS 之类原版 ID 测试。

### 第二幕相关踩坑与验证（第 3 轮补充）

- **遭遇不会重复**：RitsuLib 的 DynamicActContentPatcher.AllEncountersPostfix → MergeScopedCatalog →
  ContentMergeStrategies.AppendDistinctById，按 ModelId 去重；所以「在 GenerateAllEncounters 里列出 +
  打 [RegisterActEncounter]」两条路都写也不会出现同一场遭遇两份（第一幕同理）。
- **多幕共存**：ModContentRegistry.ActEnterPools 是 Dictionary<Int32, ActEnterPool<...>>（按槽位分池），
  RegisteredActs 是 HashSet，同一 mod 注册 0/1 两个幕互不影响。
- **音效**：本 mod 没有自带 FMOD bank，基类 MonsterModel.AttackSfx 会拼出
  event:/sfx/enemy/enemy_attacks/<entry>/<entry>_attack，我们的怪物没有对应事件 → 日志里刷
  “FMOD event was not found” 且没有攻击音。已把全部 27 个怪物招式里的 .WithAttackerFx(null, AttackSfx)
  去掉（58 处），命中音仍由游戏自己的 enemy_hurt 音效负责。要真正有攻击音得上 FMOD bank。
- **怪物尺寸**：按原版第二幕 Bounds 分布校准（原版：杂兵 150–168、普通 133–476、精英 333–358、面积大的飞怪 476）。
  当前：幼虫 150、小菌菇 112、孢花 168、菌化躯壳 205、气囊 225（上浮 80）、游孢 150（上浮 90）、
  菌战士 300、螳螂幼体 265、菌巨魔 350、螳螂战士 345。
- **幕内自检日志**：进第二幕时会打一行
  [Tearscity] fungal act pool: total=... weak=... normal=... elite=... boss=... | events=... ancients=... | bossOrder=...，
  用来确认遭遇池构成和 Boss 顺序（实机验证时看这行最快）。

## 已知外部问题：游戏 0.114.0（public-beta）与 RitsuLib 0.6.15 不兼容

2026-11-25 实机日志（%APPDATA%\SlayTheSpire2\logs\godot.log）：

`
[RitsuLib.Loader] Host=v0.114.0; runtime=0.113.0.
[Critical] ritsulib_card_capability_downgrade_lifecycle - Failed ✗ Target method not found: CardModel.DowngradeInternal
[Critical] ritsulib_card_capability_upgrade_lifecycle - Failed ✗ Target method not found: CardModel.UpgradeInternal
[Patcher - framework core] 2 critical patch(es) failed, mod loading blocked
[Patcher - framework core] Critical patch(es) failed, rolling back all patches... (433 patches removed)
Exception thrown when calling mod initializer of type STS2RitsuLib.Loader.Bootstrap
`

- 本机 Steam 的 STS2 在 public-beta 分支（ppmanifest_2868840.acf 里 BetaKey = public-beta），2026-11-25 12:54 更新到 **0.114.0**；
- RitsuLib 0.6.15（工坊 3747602295）的 compat 分支只有 0.107.1 / 0.111.0 / **0.113.0**，它在 0.114 上命中两个关键补丁失败，
  于是主动 “mod loading blocked” 并回滚全部补丁 → 弹窗「模组 STS2-RitsuLib 的程序集 DLL 初始化失败」；
- **我们的模组本身初始化是成功的**（日志里有 Registered act: Fungalwastes (id=TEARSCITY_ACT_FUNGALWASTES)
  和 Tearscity initialized! (replacing act 1 and act 2)），只是框架被禁用后幕替换补丁没生效。

处理办法（二选一）：
1. Steam → 库 → Slay the Spire 2 → 属性 → 测试版 → 选「无」（退出 public-beta），等 Steam 回滚到 0.113.x；回滚后需要
   用 0.113 的游戏程序集**重新构建**一次本 mod（uild.ps1）。
2. 留在 0.114 beta，等 RitsuLib 发布支持 0.114 的 compat 分支（该工坊条目 	imeupdated = 2026-11-20 15:23，暂未更新）。

### 游戏版本与源码 API 的切换（2026-11-25）

用户把 Steam 测试分支从 public-beta 改回 public，游戏回滚到 **v0.111.1**（`release_info.json` 里 commit `cc1f9462`）。
RitsuLib 0.6.15 有 `compat/0.111.0` 分支，可用；但我们的代码是按 0.113.x 的 API 写的，需要改三处才编得过：

| 位置 | 0.113.x 写法 | v0.111.1 写法 |
|---|---|---|
| `AbstractModel.AfterDeath` 重写（collector / master / watcher / shrumalwarrior / shrumalogre） | `(PlayerChoiceContext, Creature, CardModel source, bool, float)` | `(PlayerChoiceContext, Creature, bool wasRemovalPrevented, float)`，去掉 `CardModel source` |
| `CreatureCmd.LoseMaxHp`（fountain / vending） | 第 4 参数 `CardModel`（传 `null`） | 第 4 参数 `bool isFromCard`（传 `false`） |
| `CreatureCmd.Kill`（collector 清罐） | `Kill(creatures, null, true)` | `Kill(creatures, true)` |

清单也把 `min_game_version` 从 `0.113.0` 降到 `0.111.0`，否则 0.111.1 会拒绝加载本 mod。
以后如果游戏再切到 0.113.x / 0.114.x，上面三处要按对应签名改回来（`AfterDeath` 的 `CardModel source` 参数是 0.113.x 才有的）。

#### 当前状态（2026-11-25 13:1x）

- **源码已经改回 0.113.x / 0.114.x 的 API**（`AfterDeath` 带 `CardModel source`、`LoseMaxHp(..., null)`、
  `Kill(creatures, null, true)`、`min_game_version = 0.113.0`）。在当前安装的 **v0.111.1** 上编译会报
  5 个 `CS0115 AfterDeath` 签名错误 —— 这是预期的，不要在这种情况下 `build.ps1`。
- **`dist/` 里保留的是 v0.111.1 的可运行构建**（dll 179712 / pck 42221100 / json min_game_version 0.111.0），
  想在 0.111.1 上玩可以直接用。
- 等 RitsuLib 发布支持 0.114 的版本、游戏切回 0.113.x/0.114.x 之后，直接 `build.ps1` 重新构建即可（源码不用再改）。

#### 两个方向的切换脚本

| 目标 | 命令 | 改的东西 |
|---|---|---|
| 源码 → 0.113.x / 0.114.x API（当前状态） | `python _verify/revert_to_0113_api.py` | `AfterDeath` 加回 `CardModel source`、`LoseMaxHp` 传 `null`、`Kill` 三参、清单 `0.113.0` |
| 源码 → v0.111.x API | `python _verify/adapt_to_0111_api.py` | 反向：去掉 `CardModel source`、`LoseMaxHp` 传 `false`、`Kill` 两参、清单 `0.111.0` |

附：v0.111.1 的 `release_info.json` 是 `commit cc1f9462 / branch v0.111.1`；
游戏 `mods/` 里的清单 `min_game_version` 高于游戏版本时会被直接拒绝加载，所以切版本一定要同步改这个字段。

### 补丁层踩坑：static class 在 IL 里是 abstract+sealed（2026-11-25）

0.111 上 TearscityEventPoolPatch 的目标方法是 3 参 GenerateRooms(Rng, UnlockState, bool)（0.113 是 4 参），
按 4 参精确指定会让 harmony.PatchAll() 抛 ArgumentException: Undefined target method，
**整批补丁失败并把 Entry.Init() 一起拖崩**（幕注册没跑到，表现为「模组初始化失败」）。

修法两件事：

1. Entry.PatchAllSafely：遍历程序集里带 [HarmonyPatch] 的类，逐个 harmony.CreateClassProcessor(type).Patch()，
   失败只记日志跳过。**坑中坑**：C# 的 static class 在 IL 里是 bstract + sealed，
   过滤条件不能写 if (type.IsAbstract) continue;，否则所有补丁类（全是 static）都会被漏掉 —— 第一次修完
   日志里出现「补丁：成功 0 个，跳过 0 个」，结果进战斗房直接卡死（背景补丁没打，原版把我们的 Control 强转
   NCombatBackground 失败）。现在条件是 IsAbstract && !IsSealed 才跳过。
2. 目标方法按名字匹配：[HarmonyPatch(typeof(ActModel), GenerateRooms)]、
   [HarmonyPatch(typeof(NRunMusicController), UpdateTrack)]，这样 0.111 的 3 参 / 0.113 的 4 参、
   以及 UpdateTrack 的各重载都能绑上；UpdateTrack 的 postfix 改成从 object[] __args 里找 RoomType，
   找不到就直接返回。

_devtools/reflectioncheck 是配套的小工具：加载构建出来的 Tearscity.dll，列出所有被识别到的补丁类和目标，
用来在不开游戏的情况下确认「补丁能不能被找到」（当前输出 8 个）。

## 第三幕：感染十字路口（替换第 3 幕）

代码：`Scripts/Acts/Infectedcrossroads.cs`（幕本体）、`Scripts/encounters_xh.cs`（15 个遭遇）、
`Monsters/xh_*.cs`（12 个怪物）。内容 ID：`TEARSCITY_ACT_INFECTEDCROSSROADS`，
资源目录 `tearscity_act_infectedcrossroads`。

### 配置（数值取自原版第三幕 Glory 的 IL）

| 项 | 值 |
|---|---|
| 音乐 / 环境声 | act3_a1/a2 bank、`act3_ambience`（不做自定义音乐） |
| 宝箱 | `chest_room_act_3` + `treasure_act3` |
| 先古之民 | Nonupeipe / Tanx / Vakuu（原版） |
| Boss | QueenBoss / TestSubjectBoss / AeonglassBoss（原版，顺序同原版） |
| 房间 / 弱怪 / 地图点 | 13 / 2 / `NextGaussianInt(5,1,5,6)` |
| 地图配色 | 锈红 infected 调（`2B1712` / `C9863F` / `33201A`） |

### 事件（7 个自建，与原版第三幕数量对齐）

代码在 `Events/xh_*.cs`，注册方式 `[RegisterActEvent(typeof(Acts.Infectedcrossroads))]`，
大图 `Tearscity/images/events/xh_*.png`（1920×1080、AI 生成、统一压暗到原版亮度区间）。

| 事件 | 选项 | 机制 |
|---|---|---|
| 感染之泉 `InfectedSpringEvent` | 饮下泉水 / 装瓶带走 / 清洗伤口 | 回 20 血并塞 1 张伤口；-4 最大生命换药水；60 金回 12 血 |
| 废弃车站 `StagStationEvent` | 敲响老钟 / 搜刮站台 / 歇一会 | 80 金换遗物；得 70 金加 1 张伤口；回 15 血 |
| 蛆虫之父的收藏 `GrubfatherEvent` | 割开丝茧 / 取走钱袋 / 喂它一点血 | -8 血换遗物；得 90 金加 1 张伤口；-5 血回 20 血 |
| 感染矿工的歌声 `MinerSongEvent` | 听下去 / 应和一句 / 哼着走开 | 50% 回 18 血，50% -12 血 + 怀疑；-6 血换药水；得 30 金 |
| 假骑士的残骸 `FalseKnightEvent` | 让守卫再站起来 / 撬开胸甲 / 硬掰关节 | **打精英 `HuskGuardEliteEncounter`**，胜利给 120 金 + 遗物；遗物 + 伤口；-12 血换 2 瓶药水 |
| 感染巢穴 `MawlekNestEvent` | 点燃它 / 刮取浆液 / 先清掉幼体 | -10 血换遗物；得 80 金加 1 张伤口；**打 `HatchlingSwarmEncounter`**，胜利给 60 金 + 药水 |
| 感染石碑 `InfectedTotemEvent` | 伸手触碰 →（献上生命 / 退后拒绝）/ 绕开它 | **二段事件**：-4 最大生命换遗物，或 -8 血；绕开得 40 金 |

其中带战斗的两个事件 `IsShared => true`（事件内开战的硬性要求）。
`_verify/check_event_keys.py` 会扫所有事件的 `InitialOptionKey / ModOptionKey / PageDescription`，
确认 events.json 里每个 title/description 都存在 —— 当前 20 个事件（第一幕 13 + 第三幕 7）全部 OK，
不会再出现“选项缺 key 导致点下去卡死”的老问题。

### 怪物（12 种，全部出自空洞骑士的十字路口/感染十字路口）

原版感染十字路口的敌人只有 4 种（Furious Vengefly / Volatile Gruzzer / Violent Husk / Slobbering Husk），
加上十字路口原生的 Tiktik / Aspid Hunter / Aspid Mother / Aspid Hatchling / Vengefly King / Husk Guard，
凑成 12 个模型。

| 档位 | 怪物 | HP（普通/进阶） | 伤害 | 机制 |
|---|---|---|---|---|
| 弱 | TIKTIK 提克提克 | 64–72 / 72–80 | 疾爬 11 | 蜷缩拿 14 格挡 |
| 弱 | INFECTED_BLOB 感染脓疱 | 58–66 / 66–74 | 感染飞沫 12 + 1 虚弱 | 膨胀后爆裂 15 并自尽 |
| 弱 | FURIOUS_VENGEFLY 暴怒复仇蝇 | 70–78 / 78–86 | 俯冲 13 | 尖啸 8 格挡 + 1 易伤 |
| 弱 | ASPID_HATCHLING 阿斯庇德幼体 | 34–40 / 40–46 | 撕咬 7 | 只会被母体孵化出来 |
| 普通 | VIOLENT_HUSK 暴戾躯壳 | 165–175 / 175–190 | 冲锋 18、连撞 8×2 | 狂怒 10 格挡 + 2 力量 |
| 普通 | SLOBBERING_HUSK 垂涎躯壳 | 175–185 / 185–200 | 扑跃 20、啃咬 9×2 | 呕菌 12 + 2 虚弱 |
| 普通 | ASPID_HUNTER 阿斯庇德猎手 | 150–160 / 160–175 | 三连吐刺 7×3 | 瞄准 1 易伤 |
| 普通 | ASPID_MOTHER 阿斯庇德母体 | 160–170 / 170–185 | 吐刺 14、突刺 16 | 孵化 1 只幼体（最多 2） |
| 普通 | VOLATILE_GRUZZER 易爆格鲁兹 | 145–155 / 155–170 | 爆裂 18 + 1 脆弱 | 飘移 12 格挡 |
| 精英 | VENGEFLY_KING 复仇蝇之王 | 320–336 / 336–356 | 振翅 30、连续俯冲 22×2 | 尖啸 16 格挡 + 2 易伤 |
| 精英 | HUSK_GUARD 躯壳守卫 | 340–356 / 356–376 | 重砸 38、扑压 20 + 1 脆弱 | 戒备 24 格挡 + 3 力量 |
| 精英 | VIOLENT_HUSK_CHARGED 狂暴暴戾躯壳 | 300–316 / 316–336 | 蓄力冲撞 34（自损 6）、狂暴连斩 12×3 | 蓄势 +2 力量 |

### 遭遇（4 弱 / 8 普通 / 3 精英 + 3 原版 Boss）

弱：`TiktikPairEncounter`、`InfectedBlobSwarmEncounter`（3 只）、`FuriousVengeflyDuoEncounter`、`HatchlingSwarmEncounter`（3 只）
普通：`ViolentHuskEncounter`、`SlobberingHuskEncounter`、`AspidHunterEncounter`、`AspidMotherEncounter`（自带槽位场景）、
`VolatileGruzzerEncounter`、`FuriousVengeflyPackEncounter`、`HusksPairEncounter`、`TiktikBlobPairEncounter`
精英：`VengeflyKingEliteEncounter`、`HuskGuardEliteEncounter`、`ViolentHuskChargedEliteEncounter`

### 测试

```
act 3                                      # 跳到第三幕（会被强制替换成感染十字路口）
act TEARSCITY_ACT_INFECTEDCROSSROADS       # 或直接换幕
fight TEARSCITY_ENCOUNTER_HUSK_GUARD_ELITE_ENCOUNTER
```

进幕日志：`[Tearscity] crossroads act pool: total=18 weak=4 normal=8 elite=3 boss=3 | events=7 ancients=3 | bossOrder=...`

## 难度上调（2026-11-26）

按「现在太简单了」的要求，对第一、二幕做了统一乘法（脚本 `_verify/buff_difficulty.py`，已打标记避免重复应用）：

| 幕 | HP 倍率 | 伤害倍率 | 格挡倍率 | 结果示例 |
|---|---|---|---|---|
| 第一幕 | ×1.30 | ×1.25 | ×1.20 | 灵魂大师 130–140 → **169–182**；躯壳哨兵 17–23 → **22–30**；belfly 14–18 → **18–23**、自爆 9 → **14** |
| 第二幕 | ×1.22 | ×1.20 | ×1.15 | 菌巨魔（精英）170–180 → **207–220**、狂砸 18 → **23**；螳螂战士 → **201–214**、上挑 20 → **26** |

第三幕本身按更高基线设计（普通 145–185 HP、精英 300–356 HP / 32–40 伤害），所以三幕曲线现在是
第一幕 ≈ 170 / 第二幕精英 ≈ 210 / 第三幕精英 ≈ 330。

自检脚本 `_verify/check_fungal_content.py` 的数值区间也同步放宽到上调后的范围（弱 ≤18、普通 ≤28、精英 ≤34 伤害）。

### 第三幕收尾验证（2026-11-26）

```
构建：0 个错误（Tearscity.dll 238080 B）
verify_pck：FAILED_CHECKS=0
  [xh] 背景 / 休息处 / 7 张事件大图 / 母体遭遇场景 -> 全 true
  [xh] 12 个怪物镜头场景 -> 全 true
check_event_keys.py：20 个事件，0 缺 key
check_fungal_content.py：FAILURES=0
Tearscity.pck 60982584 B
```

## 第二幕第一个 Boss：螳螂领主（两阶段，2026-11-27）

替换掉原版第二幕的第一个 Boss（`TheInsatiableBoss`），另两个原版 Boss（`KnowledgeDemonBoss` / `KaiserCrabBoss`）保留。

**代码与资源**

| 项 | 位置 |
|---|---|
| 一阶段怪物 | `Monsters/mantislord.cs`（`MANTIS_LORD`） |
| 二阶段怪物 | `Monsters/mantislordtwin.cs`（`MANTIS_LORD_TWIN`） |
| Boss 遭遇 | `Scripts/encounters_fungal.cs` → `MantisLordsBossEncounter`（`RoomType.Boss`，槽位 lord/twin1/twin2） |
| 遭遇场景 | `scenes/encounters/tearscity_encounter_mantislords_boss_encounter.tscn`（三个 Marker2D） |
| 镜头场景 | `scenes/creature_visuals/tearscity_monster_mantislord{,_twin}.tscn`（同一张 png，高 520 / 470） |
| 贴图 | `Tearscity/images/monsters/mantislord.png`（取自 `BossMantisLord_Idle` 帧条最大帧） |
| 地图/历史图标 | `Tearscity/images/map/tearscity_encounter_mantislords_boss_encounter_{nodeicon,icon}{,_outline}.png`（352×352） |
| 幕接线 | `Acts/Fungalwastes.cs`：`BossDiscoveryOrder` 首位 = 本 Boss；Boss 池 = 本 Boss + 2 原版 |

**阶段机制**

- 一阶段：只有一只 `MANTIS_LORD`；招式 `WIND_SCYTHE 16+1脆弱` / `DASH 24` / `DIVE 26+1易伤` / `BOW 16格挡+2力量`（BOW 用 `UseOnlyOnce`，整场一次）。
- 它死亡时在 `AfterDeath` 里通过遭遇槽位召唤 **2 只 `MANTIS_LORD_TWIN`**（`GetNextSlot` + `CreatureCmd.Add`）；
  召唤期间 `ShouldStopCombatFromEnding` 返回 true，避免"一阶段死完就结算"；两只都放不出时允许战斗正常结束（兜底防软锁）。
- 二阶段：两只各自独立随机出 `TWIN_SLASH 9×2` / `SCYTHE_VOLLEY 7×3` / `CEILING_DROP 15+1易伤` / `GUARD_STANCE 12格挡`，
  合起来每回合 20–45 点压力。

**数值（与当前上调后的难度对齐）**

| 目标 | HP（普通/进阶） | 单体最高伤害 |
|---|---|---|
| MANTIS_LORD | 190–210 / 200–220 | 26（DIVE，进阶 27） |
| MANTIS_LORD_TWIN（每只） | 150–165 / 158–173 | 15（进阶 16） |
| 合计 | 490–540 | — |

对照：第二幕精英 201–220 HP / 23–26 伤害；原版第二幕 Boss KnowledgeDemon 399 HP。

**验证**

```
构建：0 个错误（Tearscity.dll 248832 B）
verify_pck：FAILED_CHECKS=0
  [boss] 4 张图标 / 两个镜头场景 / 遭遇场景三个槽位 -> 全 true
check_fungal_content.py：FAILURES=0
  act pool: ours=18 (weak=4 normal=10 elite=3 boss=1) + vanilla bosses=[KaiserCrabBoss, KnowledgeDemonBoss]
check_event_keys.py：20 个事件 0 缺 key
```

**测试**

```
act 2                                        # 第二幕第一个 Boss 就是螳螂领主
fight TEARSCITY_ENCOUNTER_MANTIS_LORDS_BOSS_ENCOUNTER
```

进幕日志 `bossOrder=` 的首项应为 `TEARSCITY_ENCOUNTER_MANTIS_LORDS_BOSS_ENCOUNTER`。

### 第二幕第二个 Boss：胡长老（Elder Hu，2026-11-27）

第二幕收成**双 Boss 池**：螳螂领主 + 胡长老，原版 KnowledgeDemonBoss / KaiserCrabBoss 从池子与发现顺序里一起删除。

| 项 | 位置 |
|---|---|
| 怪物 | Monsters/elderhu.cs（ELDER_HU），HP 380–400 / 进阶 396–416 |
| 遭遇 | Scripts/encounters_fungal.cs → ElderHuBossEncounter（RoomType.Boss，无槽位场景） |
| 镜头场景 | scenes/creature_visuals/tearscity_monster_elderhu.tscn（高 500，Bounds ±274） |
| 贴图 | Tearscity/images/monsters/elderhu.png（monsterElderHu_Idle 最大帧） |
| 图标 | Tearscity/images/map/tearscity_encounter_elder_hu_boss_encounter_{nodeicon,icon}{,_outline}.png |

招式：环刃 RING_BARRAGE 7×4（进阶 8×4）、镇地 SLAM 30 + 1 脆弱（进阶 32）、瞬闪 FLASH 25 格挡 + 2 力量（进阶 28）、
冥想 MEDITATE 16 格挡 + 1 虚弱（整场一次）。单回合最高压力 30/28，与螳螂领主二阶段同档。

关于「第二幕只有两个 Boss」的机制依据（IL 确认）：RunManager.GenerateRooms 只有在**最后一幕且 Ascension ≥ 10** 时
才会把 AllBossEncounters 里排除当前 Boss 后的某个塞进 RoomSet.SecondBoss；普通难度每幕只有一个 Boss 房，
所以「只有两个 Boss」= Boss 池里只放两个，并靠 BossDiscoveryOrder 决定先出场的是螳螂领主、第二个是胡长老。
旧存档若 Boss 指向已删除的原版 Boss，ActModel.ValidateRoomsAfterLoad 会自动从新池重新随机，不需要额外代码。

验证：ct pool: ours=19 (weak=4 normal=10 elite=3 boss=2) + vanilla bosses=[]，
erify_pck FAILED_CHECKS=0（含胡长老镜头场景与 4 张图标）。
