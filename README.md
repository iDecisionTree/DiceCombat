# DiceCombat 开发者 README

## 1. 项目定位

DiceCombat 是一个基于 Godot 4.6.2 + C# 的 3D 回合制骰子战斗原型。当前项目把核心玩法拆成三层：

1. 战斗状态机负责回合、选骰、结算与胜负判定。
2. 卡牌与骰子资源负责战斗数据、视觉外观与资源组合。
3. 展示层负责镜头、UI、动画与结算表现。

如果你是第一次接手这个仓库，建议先按下面顺序理解项目：

1. 阅读 `project.godot`，确认引擎版本、主场景和渲染配置。
2. 阅读 `game_scene.tscn`，确认主场景节点 wiring。
3. 阅读 `scripts/state_machine/CombatStateMachine.cs`，理解完整战斗流程。
4. 阅读 `scripts/card/Card.cs`、`scripts/card/CardData.cs`、`scripts/dice/DiceManager.cs`，理解数据与行为的衔接。
5. 最后再看 `scripts/card_skill` 和 UI、特效目录，理解扩展点。

## 2. 技术栈与运行基线

### 2.1 当前项目配置

- 引擎版本：Godot 4.6.2
- 脚本语言：C#
- 桌面目标框架：.NET 8.0
- Android 条件目标框架：.NET 9.0
- 物理引擎：Jolt Physics
- Windows 渲染驱动：D3D12
- 渲染方式：Mobile Renderer

这些信息来自以下配置：

- `project.godot`
- `DiceCombat.csproj`

### 2.2 建议开发环境

建议至少准备以下环境：

1. Godot 4.6.2 的 .NET 版本。
2. .NET SDK 8.x。
3. Git。
4. 一种 C# IDE：
   - VS Code + C# 扩展 + Godot Tools
   - JetBrains Rider

### 2.3 VS Code 本地配置

仓库中的 `.vscode/settings.json` 当前示例把 Godot 路径指向本机的：

```json
{
    "godotTools.editorPath.godot4": "c:\\Users\\14779\\Desktop\\Godot\\Godot_v4.6.2-stable_mono_win64\\Godot_v4.6.2-stable_mono_win64.exe"
}
```

这个路径是本地环境示例，不应假定其他开发机器上也存在。新开发者接手时通常需要改成自己的 Godot 安装路径。

## 3. 快速开始

### 3.1 获取代码

```powershell
git clone <your-repo-url>
cd dice-combat
```

### 3.2 首次打开项目

1. 用 Godot 4.6.2 .NET 版本打开仓库根目录。
2. 等待 Godot 完成资源导入。
3. 让 Godot 生成或刷新 `.godot/` 中的缓存与 C# 相关中间文件。
4. 打开 `DiceCombat.sln` 或直接在仓库根目录使用 `dotnet build`。

### 3.3 构建项目

当前仓库已经验证可以在根目录直接执行：

```powershell
dotnet build
```

本次验证结果为构建成功，产物位于：

```text
.godot/mono/temp/bin/Debug/DiceCombat.dll
```

也可以显式构建解决方案：

```powershell
dotnet build DiceCombat.sln
```

### 3.4 运行项目

最直接的方式是用 Godot 编辑器运行主场景。项目配置中的主场景是：

```text
res://game_scene.tscn
```

如果 Godot CLI 已配置到环境变量，也可以使用类似方式启动编辑器或运行项目，但不同机器上的 Godot 可执行文件路径差异很大，因此本 README 只把 Godot 编辑器运行作为标准开发流程。

## 4. 当前仓库结构

下面只列开发中最重要的目录和职责，不展开第三方插件内部实现。

```text
.
├─ project.godot                 # Godot 项目配置，主场景、渲染、插件启用状态
├─ DiceCombat.csproj             # C# 项目文件
├─ DiceCombat.sln                # 解决方案文件
├─ game_scene.tscn               # 当前主场景，包含战斗系统 wiring
├─ addons/                       # 第三方插件
├─ animations/                   # 动画资源库
├─ materials/                    # 材质资源
├─ models/                       # 3D 模型资源
├─ resourses/                    # 运行时场景/资源。注意：目录名当前就是 resourses
│  ├─ card/                      # 卡牌场景资源
│  ├─ card_data/                 # 卡牌数据资源
│  ├─ card_skill/                # 卡牌技能资源
│  ├─ dice/                      # 骰子场景资源
│  └─ coin.tscn                  # 其他资源场景
├─ scripts/
│  ├─ card/                      # 卡牌节点与卡牌数据定义
│  ├─ card_skill/                # 技能基类、上下文、运行时状态、具体技能实现
│  ├─ dice/                      # 骰子实体、骰子管理器、选骰 UI
│  ├─ state_machine/             # 战斗状态机与表现导演
│  ├─ ui/                        # 面板与图标切换动画
│  └─ NodeSearch.cs              # 场景树节点查找工具
├─ shaders/                      # 自定义 shader
├─ textures/                     # 贴图资源
└─ themes/                       # UI 主题
```

### 4.1 关于 `resourses` 目录名

请注意，仓库里资源目录当前拼写为 `resourses`，不是常见的 `resources`。这是一个已经进入项目路径系统的既有命名，场景和资源都已经大量引用它。

不要因为看到拼写问题就直接重命名目录。

如果一定要改，需要一次性处理：

1. 所有 `.tscn`、`.tres`、`.gdshader` 中的资源路径。
2. Godot 导入缓存。
3. 可能受影响的 UID 引用。

在没有明确迁移计划之前，默认保持现状。

## 5. 主场景与启动流程

### 5.1 启动入口

项目启动由 `project.godot` 指向 `game_scene.tscn`。没有复杂的多场景启动器，也没有额外的 autoload 游戏框架层。

这意味着：

- 运行时大部分系统 wiring 都直接发生在 `game_scene.tscn`。
- 调试主流程时，优先从 `game_scene.tscn` 的导出引用是否完整开始排查。

### 5.2 `game_scene.tscn` 中的核心节点

主场景里最重要的几个节点如下：

- `CombatManager`
  - 挂载 `scripts/state_machine/CombatStateMachine.cs`
  - 负责整个战斗状态流
- `DiceManager`
  - 挂载 `scripts/dice/DiceManager.cs`
  - 负责骰子生成、投掷、重投、选择动画与选中状态维护
- `Coin`
  - 当前实例为 `res://resourses/coin.tscn`
  - 战斗开始前抛硬币决定先后手，正面为玩家先手，反面为敌人先手
- `PlayerCard`
  - 当前实例为 `res://resourses/card/card_0003.tscn`
- `EnemyCard`
  - 当前实例为 `res://resourses/card/card_0005.tscn`
- `CombatCameraDirector3D`
  - 镜头表现层
- `CombatEffectDirector3D`
  - 结算遮罩、揭示动画、受击动画
- `CanvasLayer`
  - 2D UI 层，包含玩家/敌方面板、确认按钮、重投按钮、结算遮罩
- `Env/Board/UI/RichText3D_DiceSelect`
  - 棋盘上的 3D 文本，用于显示选骰总点数与加成

### 5.3 状态机在场景中的依赖绑定

`CombatStateMachine` 通过导出字段绑定场景节点：

- `PlayerCard`
- `EnemyCard`
- `DiceManager`
- `DiceSelectUI`
- `DiceRerollUI`
- `TurnOrderCoin`
- `TurnIconSwapAnimator`
- `CameraDirector`
- `EffectDirector`
- `PlayerInfoPanel`
- `EnemyInfoPanel`

因此任何一个节点改名、替换或从主场景删除后，都需要检查 `CombatManager` 上的导出引用是否仍然有效。

## 6. 核心玩法与运行时架构

## 6.1 战斗主循环

战斗流程由 `CombatStateMachine` 驱动，主要状态定义在 `scripts/state_machine/CombatState.cs`：

- `PlayerRollAllDice`
- `PlayerChoose`
- `PlayerConfirm`
- `EnemyRollAllDice`
- `EnemyChoose`
- `ResolveDamage`
- `CheckEnd`
- `SwitchTurn`
- `Victory`
- `Defeat`

### 6.2 实际流程顺序

完整流程如下：

1. `StartBattle()` 初始化战斗。
2. 先播放一次抛硬币动画决定首回合：正面是玩家先手，反面是敌人先手。
3. 先手方掷出自己卡牌对应骰池中的所有骰子。
4. 若当前是玩家行动，则进入玩家选骰确认阶段；玩家可选择指定数量的骰子，并可以在限制次数内重投当前选中的骰子。
5. 若当前是敌人行动，则敌方按当前规则自动选择最高点数组合。
6. 被动方随后执行自己的掷骰与选骰流程。
7. 双方进入伤害结算。
8. 检查是否产生胜负。
9. 若未结束，则切换攻防回合并继续下一轮。

### 6.3 攻防角色与选骰数量

项目里攻击和防御不是“固定玩家攻击、敌人防御”，而是随 `CombatTurn` 变化：

- 当前回合主动方的选骰数量取自卡牌的 `Attack`
- 当前回合被动方的选骰数量取自卡牌的 `Defense`

对应逻辑在 `CombatStateMachine.GetRequiredSelectionCount()`。

### 6.4 敌方选骰逻辑

敌方没有复杂 AI，当前规则非常明确：

1. 根据当前攻防角色，得到应选骰数量。
2. 对已掷出的骰子按点数降序排序。
3. 取前 N 个。

也就是说，当前敌方行为是一个“选择最高点数组合”的基线策略。

如果以后需要：

- 技能驱动的 AI 选骰
- 按骰子类型权重选择
- 风险控制或保留高面值重投策略

优先改造的位置就是 `CombatStateMachine.ExecuteEnemyChooseDice()`。

## 6.5 伤害公式

当前伤害结算逻辑可以概括为：

```text
基础伤害 = max(攻击方已选骰子总和 - 防御方已选骰子总和, 0)
最终伤害 = max(基础伤害 + 攻击方待结算加成 - 防御方待结算减伤, 0)
```

之后再进入技能钩子：

1. 构造 `DamageResolutionSkillContext`
2. 对攻击方调用 `ApplyBeforeDamageResolved`
3. 对防御方调用 `ApplyBeforeDamageResolved`
4. 读取上下文中的最新伤害值
5. 播放表现层效果
6. 对目标卡牌真正扣血
7. 触发双方 `ApplyAfterDamageResolved`

### 6.6 技能结算时机

技能系统目前有三类主要时机：

1. 选骰预览阶段：
   - `GetSelectionDamagePreviewBonus`
   - 用于更新 UI 上的预估加成
2. 选骰确认后：
   - `OnAfterDiceSelected`
   - 适合把加成/减伤记入 `CardSkillRuntimeState`
3. 伤害结算前后：
   - `OnBeforeDamageResolved`
   - `OnAfterDamageResolved`

当前示例技能 `AddDamageAfterSelectState` 的逻辑是：

- 如果当前是攻击选骰，则提供固定伤害加成预览。
- 在真正结算前，把固定加成应用到伤害上下文里。

## 7. 数据模型与职责分层

### 7.1 `CardData` 负责什么

`scripts/card/CardData.cs` 是卡牌数据资源定义，主要字段包括：

- `CardId`
- `CardName`
- `Description`
- `CardBackground`
- `CardAvatar`
- `InfoAvatar`
- `Attack`
- `Defense`
- `MaxHealth`
- `MaxReroll`
- `DiceGroups`
- `Skills`

可以把 `CardData` 理解为“纯内容配置”。

### 7.2 `Card` 负责什么

`scripts/card/Card.cs` 是场景中的卡牌节点逻辑，负责：

1. 把 `CardData` 渲染到 3D 卡面和信息文本上。
2. 维护战斗中的 `CurrentHealth`。
3. 根据 `DiceGroups` 生成本回合可掷出的骰子列表。
4. 分发技能钩子。
5. 维护每张卡自己的 `CardSkillRuntimeState`。

很重要的一点是：

- `RollAllDice()` 只会生成 `DiceData` 列表，初始点数为 `0`
- 真正的点数结果由 `Dice` 物理模拟结束后写回 `DiceData.Num`

### 7.3 `DiceSet` 与骰子池

`scripts/dice/DiceSet.cs` 很简单，只描述两件事：

- `DiceType`
- `Count`

当前支持的骰子种类由 `scripts/dice/DiceType.cs` 定义：

- `Dice4`
- `Dice6`
- `Dice8`
- `Dice12`

每张卡牌的骰池就是若干个 `DiceSet` 的数组组合。

### 7.4 `Dice` 与 `DiceManager` 的分工

#### `Dice`

`scripts/dice/Dice.cs` 是单个物理骰子实体，负责：

- 监听物理稳定状态
- 解析当前朝上的面
- 响应鼠标点击选择
- 控制描边与选中状态

#### `DiceManager`

`scripts/dice/DiceManager.cs` 负责：

- 按 `DiceType` 实例化不同骰子场景
- 投掷动画与力学参数控制
- 重投逻辑
- 选中骰子统计
- 敌方选骰展示动画
- 向 UI 发出选择数量变化信号

如果你要调整“骰子手感”，主要就在 `DiceManager` 中改这些导出参数：

- `DiceSpacing`
- `SpawnHeight`
- `RollUpImpulse`
- `RollHorizontalImpulse`
- `TorqueImpulseStrength`
- `StableTimeRequired`
- `MaxRollTime`

## 7.5 展示层导演

项目把镜头与表现做了轻量抽象：

- `CombatCameraDirector`
- `CombatEffectDirector`

当前 3D 实现分别是：

- `CombatCameraDirector3D`
- `CombatEffectDirector3D`

这样做的好处是：

1. 状态机不直接依赖某一种具体镜头实现。
2. 如果以后做不同战斗表现风格，可以在不改战斗逻辑的前提下替换导演节点。

## 8. UI 与信息展示

### 8.1 玩家确认 UI

`scripts/dice/DiceSelectUI.cs` 负责：

- 打开/关闭确认按钮区域
- 显示 `已选数量 / 需要数量`
- 显示 3D 选骰总点数
- 显示技能带来的预估伤害加成

### 8.2 重投 UI

`scripts/dice/DiceRerollUI.cs` 负责：

- 打开/关闭重投面板
- 更新剩余重投次数
- 响应重投按钮点击

### 8.3 战斗信息面板

`scripts/ui/CombatInfoPanel.cs` 负责把 `Card` 当前状态同步到 2D 面板，包括：

- 描述
- 血量
- 攻防值
- 头像

它支持在编辑器里做预览刷新，这对调面板资源很方便。

### 8.4 回合图标切换

`scripts/ui/TurnIconSwapAnimator.cs` 负责棋盘上攻击/防御图标的位置与旋转互换动画。它不参与伤害逻辑，只是视觉反馈。

## 9. 常见开发任务

## 9.1 新增一张卡牌

推荐流程如下。

### 步骤 1：复制一个现有数据资源

从 `resourses/card_data/` 里复制一个最接近的新卡模板，例如：

- `card_data_0003.tres`

修改以下字段：

- `CardId`
- `CardName`
- `Description`
- `Attack`
- `Defense`
- `MaxHealth`
- `MaxReroll`
- `DiceGroups`
- `Skills`

### 步骤 2：准备卡牌贴图

通常需要至少准备：

- 卡面头像 `CardAvatar`
- 卡面背景 `CardBackground`
- 信息面板头像 `InfoAvatar`

它们通常放在 `textures/card/`。

### 步骤 3：复制一个卡牌场景

从 `resourses/card/` 中复制一份现有 `.tscn`，例如：

- `card_0003.tscn`

然后至少确认两件事：

1. 根节点 `Card` 组件的 `CardData` 指向你的新 `.tres`
2. 卡面动画、材质和 3D 文本节点仍然完整可用

### 步骤 4：接入主场景或测试场景

最简单的方法是先在 `game_scene.tscn` 中把 `PlayerCard` 或 `EnemyCard` 的实例替换成你的新卡牌场景。

### 步骤 5：运行并验证

至少检查：

1. 卡面文本是否刷新正确
2. 血量是否初始化正确
3. 选骰数量是否符合攻防值
4. 技能是否真的生效
5. 信息面板头像与描述是否同步正确

## 9.2 新增一个技能

推荐流程如下。

### 步骤 1：新增 C# 技能类

技能类应继承 `CardSkill`。最小骨架如下：

```csharp
using Godot;
using DiceCombat.scripts.card_skill;

namespace DiceCombat.scripts.card_skill.skill;

[GlobalClass]
[Tool]
public partial class ExampleSkill : CardSkill
{
    [Export] public int Value { get; set; } = 1;

    public override int GetSelectionDamagePreviewBonus(DiceSelectionPreviewContext context)
    {
        return 0;
    }

    public override void OnAfterDiceSelected(DiceSelectionSkillContext context)
    {
    }

    public override void OnBeforeDamageResolved(DamageResolutionSkillContext context)
    {
    }

    public override void OnAfterDamageResolved(DamageResolutionSkillContext context)
    {
    }
}
```

### 步骤 2：决定技能逻辑应该挂在哪个时机

经验规则如下：

- 只影响 UI 预览：改 `GetSelectionDamagePreviewBonus`
- 选完骰子后缓存状态：改 `OnAfterDiceSelected`
- 改动最终伤害：改 `OnBeforeDamageResolved`
- 根据本次实际受伤/造成伤害再触发：改 `OnAfterDamageResolved`

### 步骤 3：需要跨阶段记忆状态时，使用 `CardSkillRuntimeState`

不要把“一次选择后暂存的数据”硬塞进 `CardData`。`CardData` 是静态配置，不应该承载回合内的运行时状态。

应该使用：

- `AddPendingDamageBonus`
- `AddPendingDamageReduction`
- `SetFlag`
- `GetValue`
- `SetValue`

### 步骤 4：创建技能资源

在 `resourses/card_skill/` 下创建对应的 `.tres` 资源，并在卡牌的 `CardData.Skills` 中引用它。

### 步骤 5：验证资源是否被 Godot 识别

如果 Godot 编辑器里没有正确识别你的新技能资源类型，优先检查：

1. 类是否有 `[GlobalClass]`
2. 类是否继承了 `CardSkill`
3. 命名空间和编译是否成功
4. Godot 是否已完成 C# 重新编译

## 9.3 调整骰子表现或物理手感

你通常会同时改两层：

1. 资源层：`resourses/dice/*.tscn`
2. 管理层：`scripts/dice/DiceManager.cs`

### 在资源层常改内容

- 骰子模型
- 碰撞体
- 面标记节点 `Face1` 到 `Face12`
- 描边 mesh

### 在管理层常改内容

- 投掷力度
- 生成高度
- 稳定判定阈值
- 展示阶段动画速度

如果你改了骰子模型或面标记，请特别注意 `Dice.ResolveFaceFromWorldUp()` 是否还能正确得到面值。对于四面骰，当前逻辑和其他骰子不同，使用的是最低面而不是最高面。

## 9.4 调整镜头和结算特效

镜头与特效主要由两个导演类控制：

- `CombatCameraDirector3D`
- `CombatEffectDirector3D`

如果只是微调表现，优先改导出参数；如果需要彻底换表现方案，再考虑新增导演实现。

`CombatEffectDirector3D` 当前主要负责：

- 黑幕遮罩淡入淡出
- 结算卡牌揭示动画
- 受击动画播放
- 控制哪张卡在结算阶段可见

## 9.5 调整信息面板样式

优先看：

- `themes/theme.tres`
- `game_scene.tscn` 中 `CanvasLayer/PlayerInfo`
- `game_scene.tscn` 中 `CanvasLayer/EnemyInfo`
- `scripts/ui/CombatInfoPanel.cs`

样式问题通常是主题资源或节点布局问题；数据不同步问题通常是 `BoundCard` 未绑定或 `RefreshFromCard()` 没有被调用。

## 10. 代码约定与工程注意事项

### 10.1 C# 代码风格

`.editorconfig` 当前要求：

- UTF-8
- CRLF
- 文件结尾有换行
- C# 使用 Tab 缩进

请尽量保持一致，避免无意义格式噪音。

### 10.2 Godot 导出引用优先于硬编码路径

这个项目的大部分核心对象都通过 Godot Inspector 的导出字段绑定，而不是在代码里用绝对场景路径硬编码查找。

这意味着：

- 场景 wiring 的正确性非常重要
- 改节点名时必须同步检查导出引用
- Null 引用问题往往不是逻辑代码本身，而是主场景绑定断了

### 10.3 `NodeSearch` 的定位

`scripts/NodeSearch.cs` 是辅助查找工具，主要被 UI 脚本用于兜底查找按钮、文本节点。它适合做场景内部容错，但不建议把核心系统都做成“运行时盲找节点”。

核心依赖仍然应该优先靠导出引用显式绑定。

### 10.4 当前没有自动化测试

仓库中当前没有发现单元测试或集成测试目录。现阶段验证手段主要是：

1. `dotnet build`
2. 在 Godot 中实际运行主场景
3. 观察 Godot 输出日志与场景表现

如果后续要补测试，建议先从纯 C# 逻辑层切起，例如：

- 伤害计算辅助逻辑
- 技能上下文逻辑
- 运行时状态对象

## 11. 常见排查思路

### 11.1 打开项目后 C# 不生效

优先检查：

1. 打开的是否是 Godot .NET 版本
2. 本机是否安装了 .NET 8 SDK
3. `dotnet build` 是否通过
4. Godot 是否完成了 C# 项目刷新

### 11.2 战斗一开始就报空引用

优先看 `game_scene.tscn` 中 `CombatManager` 的导出依赖是否都还在：

- `PlayerCard`
- `EnemyCard`
- `DiceManager`
- `DiceSelectUI`
- `DiceRerollUI`
- 展示导演
- 信息面板

### 11.3 骰子投出后点数不对

优先检查：

1. 骰子场景里的 `Face1` 到 `FaceN` 是否仍然正确放置
2. 模型替换后坐标系是否改变
3. `DiceType` 是否与场景实际面数一致
4. `ResolveFaceFromWorldUp()` 对新模型是否仍然成立

### 11.4 技能资源看得见但不生效

优先检查：

1. `CardData.Skills` 是否真的挂上了资源
2. 触发时机是否选对
3. 你的逻辑是改预览、改运行时缓存，还是改结算伤害
4. 是否遗漏了对攻击/防御角色的判断

### 11.5 结算时卡牌没有动画

优先检查：

1. 卡牌场景下是否存在 `AnimationPlayer`
2. 请求播放的动画名是否存在
3. `CombatEffectDirector3D` 的导出动画名是否与资源一致

## 12. 推荐阅读顺序

如果你准备开始改功能，而不是只想跑起来，建议按这个顺序阅读代码：

1. `scripts/state_machine/CombatStateMachine.cs`
2. `scripts/card/Card.cs`
3. `scripts/card/CardData.cs`
4. `scripts/dice/DiceManager.cs`
5. `scripts/dice/Dice.cs`
6. `scripts/card_skill/CardSkill.cs`
7. `scripts/card_skill/CardSkillRuntimeState.cs`
8. `scripts/ui/CombatInfoPanel.cs`
9. `game_scene.tscn`

## 13. 当前开发基线结论

截至本 README 编写时，可以确认以下事实：

1. 项目主场景是 `game_scene.tscn`
2. 核心流程由 `CombatStateMachine` 统一驱动
3. 卡牌数据与技能资源已经分离
4. 骰子点数来源于物理模拟结果，而不是纯随机文本生成
5. 当前仓库可在根目录执行 `dotnet build` 并成功构建
6. 当前仓库尚未建立自动化测试体系

如果你准备在此基础上继续开发，最稳妥的策略是：

1. 保持主场景 wiring 清晰
2. 把新玩法优先落在 `CardData + CardSkill` 扩展点上
3. 尽量不要把临时状态塞进静态资源
4. 每次改完都至少执行一次 `dotnet build` 和一次主场景手动验证
