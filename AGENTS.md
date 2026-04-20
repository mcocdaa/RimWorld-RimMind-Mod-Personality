# AGENTS.md — RimMind-Personality

本文件供 AI 编码助手阅读，描述 RimMind-Personality 的架构、代码约定和扩展模式。

## 项目定位

RimMind-Personality 是 RimMind AI 模组套件的人格系统模块。职责：

1. **人格评估**：定时或事件触发时，调用 AI 评估小人人格状态
2. **Thought 生成**：将 AI 评估结果转化为游戏内 Thought（最多 3 个槽位）
3. **心情影响**：Thought 通过 `MoodOffsetCalculator` 影响小人心情
4. **叙事更新**：AI 生成的叙事文本写入 `PersonalityProfile`
5. **玩家塑造**：玩家可通过悬浮窗投票"强化/抑制"人格 Thought
6. **上下文注入**：将人格档案、状态、塑造历史注入 AI Prompt

**依赖关系**：
- 依赖 RimMind-Core 提供的 API 和上下文构建
- 被 RimMind-Advisor 和 RimMind-Dialogue 消费（人格上下文影响决策和对话风格）

## 源码结构

```
Source/
├── RimMindPersonalityMod.cs        Mod 入口：注册 Harmony、ContextProvider、SettingsTab、ModCooldown
├── Personality/
│   ├── PersonalityThoughtMapper.cs  核心：AI 响应 → Thought 映射 + 塑造投票注册
│   ├── PersonalityContextBuilder.cs 构建 AI 请求的 User Prompt（排除 personality_state，传递 enableShapingVote）
│   ├── PersonalityResultDto.cs      AI 响应 JSON DTO（无 RimWorld 依赖）
│   ├── EvaluationInstructionHelper.cs 评估指令构建（Poisson 抽样 + JSON 格式模板 + DiversityHint + PromptSanitizer）
│   ├── Thought_AIPersonality.cs     自定义 Thought 类型（重写 Label/Description/MoodOffset/DurationTicks）
│   └── MoodOffsetCalculator.cs     强度→心情偏移查表（无 RimWorld 依赖）
├── Settings/
│   └── AIPersonalitySettings.cs     模组设置（触发源、持续时间、塑造等）
├── Data/
│   ├── PersonalityProfile.cs        人格档案 + AIPersonalityWorldComponent
│   └── ShapingRecord.cs             玩家塑造记录
├── Comps/
│   └── CompAIPersonality.cs         ThingComp + CompProperties + TriggerEventType 枚举
├── UI/
│   ├── BioTabPersonalityPatch.cs    Transpiler 注入"人格"按钮到 Bio 页
│   └── Dialog_PersonalityProfile.cs 人格档案编辑窗口
├── Patches/
│   └── AddCompToHumanlikePatch.cs   Harmony Postfix 为人形种族注入 Comp
└── Debug/
    └── PersonalityDebugActions.cs   Dev 菜单调试动作（5 个）

Tests/  (xUnit，无 RimWorld 依赖)
├── MoodOffsetTests.cs               心情偏移查表测试
├── PersonalityResultParseTests.cs   JSON 解析测试（使用 Core 的 JsonTagExtractor）
├── EvaluationInstructionBuilderTests.cs  评估指令构建测试
└── PersonalityPipelineTests.cs      端到端纯逻辑流水线测试

Defs/ThoughtDefs/AIPersonalityThoughts.xml   3 个 ThoughtDef 槽位定义
Patches/AddAIPersonalityComp.xml             空文件（仅注释说明为何不用 XML Patch）
Languages/{ChineseSimplified,English}/Keyed/RimMind_Personality.xml  翻译
Languages/ChineseSimplified/DefInjected/ThoughtDef/  ThoughtDef 中译
About/About.xml                               模组元数据
```

## 关键类与 API

### CompAIPersonality

挂载到每个殖民者的 ThingComp，负责触发评估：

```csharp
private const int DailyInterval = 60000;       // 1 游戏天
private const int JitterRange = 3000;          // 每日抖动范围（±3000 ticks）
private const int EventCooldownTicks = 1200;   // 0.02 天事件冷却

// 状态字段
private bool   _hasPendingRequest;             // 防止重复请求
private int    _lastEventTick = -EventCooldownTicks; // 上次事件触发 tick
private string? _pendingEventContext;           // 待处理事件上下文
private int    _dailyJitter = -1;              // 基于 thingIDNumber 的确定性抖动

// 核心方法
override void CompTick()     // 检测：每日定时（含抖动，受 enableDailyEval 控制）或 事件触发
void TriggerEvent(string context, TriggerEventType eventType = TriggerEventType.Incident)
                             // 外部 Patch 调用，先检查 enablePersonality，再根据 eventType 检查对应触发开关
bool IsEligible()            // 自由非奴隶殖民者、未死亡、在地图上、有 mood

// System Prompt 构建
static string BuildSystemPrompt()  // 使用 StructuredPromptBuilder.FromKeyPrefix("RimMind.Personality.Prompt.System")
```

**触发条件**：
1. 总开关 `enablePersonality` 开启
2. API 已配置（`RimMindAPI.IsConfigured()`）
3. 小人符合资格（`IsEligible()`）
4. 无待处理请求（`!_hasPendingRequest`）
5. 每日定时触发（需 `enableDailyEval` 开启，含确定性抖动避免同时触发）
6. 或事件触发（受伤/技能/事件/死亡），需对应触发开关开启，带 1200 tick 冷却

**TriggerEventType 枚举**：

| 枚举值 | 对应设置开关 | 说明 |
|--------|------------|------|
| `Injury` | `enableInjuryTrigger` | 受伤/患病触发 |
| `Skill` | `enableSkillTrigger` | 技能升级触发 |
| `Incident` | `enableIncidentTrigger` | 重大事件触发（默认值） |
| `Death` | `enableDeathTrigger` | 亲近者死亡触发 |

**AIRequest 参数**：`MaxTokens=300, Temperature=0.8f, ModId="Personality", Priority=Low`

### PersonalityThoughtMapper

核心映射逻辑，将 AI 响应转化为游戏内 Thought：

```csharp
static void Apply(AIResponse response, Pawn pawn)
// 1. 检查 response.Success
// 2. JsonConvert.DeserializeObject<PersonalityResultDto>(response.Content)（try-catch，失败时 log 并 return）
// 3. 写入 narrative → PersonalityProfile（更新 lastNarrativeUpdateTick）
// 4. RemoveAllAIPersonalityThoughts（清除旧 Thought）
// 5. 遍历 result.thoughts，创建 Thought_AIPersonality（最多 SlotDefNames.Length 个）
// 6. 若 enableShapingVote，为每个 Thought 注册 RimMindAPI.RegisterPendingRequest
//    选项：强化/抑制/忽略，选择"忽略"时不写入 ShapingRecord
// 7. 若 showNotifications，发 Messages.Message

private static int CalcDurationTicks(ThoughtEntryDto entry, AIPersonalitySettings? settings)
// AIDecides 模式：Math.Clamp(entry.duration_hours.Value, 1, 24) * 2500
// Fixed 模式：Math.Max(1, (int)(settings.thoughtDurationHours * 2500))
// settings 为 null 时兜底返回 2500（1 小时）

static readonly string[] SlotDefNames = {
    "AIPersonality_Slot_0",
    "AIPersonality_Slot_1",
    "AIPersonality_Slot_2"
};

static void RemoveAllAIPersonalityThoughts(Pawn pawn)
static bool IsAIPersonalityDef(string defName)
```

### Thought_AIPersonality

自定义 Thought 类型，挂到 Pawn 的记忆 Thought 列表：

```csharp
public class Thought_AIPersonality : Thought_Memory
{
    public string aiLabel = string.Empty;           // AI 生成的标签（<=8字）
    public string aiDescription = string.Empty;     // AI 生成的描述（<=20字）
    public int aiIntensity;                          // 强度 -3~+3（超出由 MoodOffsetCalculator clamp）
    public int customDurationTicks = -1;             // 自定义持续时间（-1=使用 def 默认值）

    override int DurationTicks       // customDurationTicks > 0 时使用，否则 base.DurationTicks
    override string LabelCap         // showLabelPrefix ? "[RimMind] {aiLabel}" : aiLabel
    override string Description      // aiDescription ?? base.Description
    override float MoodOffset()      // 委托 MoodOffsetCalculator.CalcMoodOffset(aiIntensity)
    override void ExposeData()       // 序列化：aiLabel, aiDesc(注意key), aiIntensity, customDurationTicks
}
```

**序列化 key 注意**：`aiDescription` 的存档 key 是 `"aiDesc"` 而非 `"aiDescription"`。

### PersonalityContextBuilder

```csharp
static string BuildEvaluationPrompt(Pawn pawn, string? eventContext = null, int targetCount = 2)
// 1. RimMindAPI.BuildFullPawnPrompt(exclude: ["personality_state"])
// 2. 从 Settings 读取 durationMode → aiDecidesDuration
// 3. 从 Settings 读取 enableShapingVote
// 4. 调用 EvaluationInstructionHelper.Append(basePrompt, eventContext, targetCount, aiDecidesDuration, enableShapingVote)
```

### MoodOffsetCalculator

强度到心情偏移的查表映射（无 RimWorld 依赖，可直接单元测试）：

```csharp
static class MoodOffsetCalculator
{
    static readonly float[] MoodTable = { -10f, -3f, -1f, 0f, +1f, +3f, +10f };
    // intensity: -3  -2  -1  0  +1  +2  +3

    static float CalcMoodOffset(int intensity);  // 自动 clamp 到 [-3, +3] 后查表
}
```

### PersonalityResultDto（JSON DTO）

```csharp
public class PersonalityResultDto
{
    public ThoughtEntryDto[] thoughts { get; set; } = Array.Empty<ThoughtEntryDto>();
    public string narrative { get; set; } = string.Empty;
}

public class ThoughtEntryDto
{
    public string type { get; set; } = "state";    // "state"（影响心情）或 "behavior"（纯行为标志）
    public string label { get; set; } = string.Empty;          // <=8 字
    public string description { get; set; } = string.Empty;    // <=20 字，第三人称
    public int intensity { get; set; }                         // -3 ~ +3
    public int? duration_hours { get; set; } = null;           // 可选，仅 AIDecides 模式，1~24
}
```

### PersonalityProfile

```csharp
public class PersonalityProfile : IExposable
{
    // 玩家可编辑
    public string description = string.Empty;       // 人格描述
    public string workTendencies = string.Empty;    // 工作倾向
    public string socialTendencies = string.Empty;  // 社交倾向

    // AI 生成（玩家可查看/覆盖）
    public string aiNarrative = string.Empty;       // AI 叙事文本（每日更新）
    // 元数据
    public bool rimTalkSynced;                       // RimTalk 同步标记（预留）
    public int lastNarrativeUpdateTick;              // 上次叙事更新的 tick

    // 人格塑造投票
    public List<ShapingRecord> playerShapingHistory = new List<ShapingRecord>();

    // 方法
    bool IsEmpty { get; }  // description、workTendencies、aiNarrative 均 NullOrEmpty 时为 true
                           // 注意：不检查 socialTendencies
    void AddShapingRecord(ShapingRecord record, int maxCount);  // 超出 maxCount（且 maxCount>0）时移除最旧
    void ExposeData();
}
```

### AIPersonalityWorldComponent

```csharp
public class AIPersonalityWorldComponent : WorldComponent
{
    static AIPersonalityWorldComponent? Instance;  // 单例引用，构造时赋值

    PersonalityProfile GetOrCreate(Pawn pawn);     // 以 thingIDNumber 为键
    bool TryGet(Pawn pawn, out PersonalityProfile? profile);
    void Remove(Pawn pawn);
    override void ExposeData();                    // Scribe_Collections.Look(_profiles, LookMode.Value, LookMode.Deep)
    // playerShapingHistory 使用 Scribe_Collections.Look(ref list, key, LookMode.Deep)
}
```

### ShapingRecord

```csharp
public class ShapingRecord : IExposable
{
    public string label = string.Empty;   // Thought 标签（注意：不是 thoughtLabel）
    public string action = string.Empty;  // "reinforce" / "suppress"（"ignored" 不写入记录）
    public int tick;                      // 时间戳
}
```

## AI Prompt 结构

### System Prompt

使用 `StructuredPromptBuilder.FromKeyPrefix("RimMind.Personality.Prompt.System")` 构建，7 个段落均来自翻译 key：

| 段落 | 翻译 Key | 内容摘要 |
|------|----------|----------|
| Role | `.System.Role` | 你是殖民者人格评估师 |
| Goal | `.System.Goal` | 根据状态/档案/触发原因生成 Thought 和叙事 |
| Process | `.System.Process` | 5 步流程：读状态→参考档案→优先事件→生成 Thought→生成叙事 |
| Constraint | `.System.Constraint` | label<=8字, description<=20字, intensity -3~+3, 叙事<=50字 |
| Example | `.System.Example` | 输入输出示例 JSON |
| Output | `.System.Output` | 只返回 JSON 格式 |
| Fallback | `.System.Fallback` | 无法评估时返回空 thoughts |

### User Prompt

```
{RimMindAPI.BuildFullPawnPrompt(pawn, excludeProviders: ["personality_state"])}
{EvaluationInstructionHelper.Append(basePrompt, eventContext, targetCount, aiDecidesDuration, enableShapingVote)}
```

`PersonalityContextBuilder.BuildEvaluationPrompt` 从 `RimMindPersonalityMod.Settings` 读取 `durationMode` 和 `enableShapingVote`，传递给 `EvaluationInstructionHelper.Append`。

`EvaluationInstructionHelper.Append` 追加：
- 触发原因行（`[触发原因] {eventContext}`），仅 eventContext 非空时
- 评估指令（"恰好生成 N 个 thought"）
- 若 `enableShapingVote` 为 true，追加多样化提示（DiversityHint），避免重复生成同类 Thought
- JSON 格式模板（AIDecides 模式含 duration_hours，Fixed 模式不含）
- 指令部分经 `PromptSanitizer.Sanitize()` 清洗

### EvaluationInstructionHelper

```csharp
static int SampleThoughtCount(float mu)
// mu <= 0 时固定返回 1
// 否则标准 Poisson 采样，结果 clamp 到 [1, 3]

static string Append(string basePrompt, string? eventContext = null, int targetCount = 2,
                     bool aiDecidesDuration = false, bool enableShapingVote = false)
// 1. 追加触发原因行（仅 eventContext 非空时）
// 2. 追加评估指令（"恰好生成 N 个 thought"）
// 3. 若 enableShapingVote，追加 DiversityHint
// 4. 追加 JSON 格式模板（AIDecides 含 duration_hours，Fixed 不含）
// 5. 指令部分经 PromptSanitizer.Sanitize() 清洗
```

### Poisson 抽样

`EvaluationInstructionHelper.SampleThoughtCount(float mu)` 详见上方 API 签名。

## 玩家塑造系统

1. AI 评估后，每个 Thought 通过 `RimMindAPI.RegisterPendingRequest` 注册投票请求
2. 悬浮窗显示选项："强化" / "抑制" / "忽略"
3. 选择"强化"或"抑制"后写入 `ShapingRecord`，追加到 `PersonalityProfile.playerShapingHistory`
4. 选择"忽略"不写入记录
5. 塑造历史通过 `personality_shaping` Provider 注入下次评估的 Prompt

## 上下文注入

Personality 向 Core 注册三个 Provider（在 `RimMindPersonalityMod.RegisterContextProviders` 中）：

| Provider | 内容 | 备注 |
|----------|------|------|
| personality_profile | 人格档案（描述+工作倾向+社交倾向+AI叙事） | 含翻译 key 格式化 |
| personality_state | 当前活跃的人格/对话 Thought 列表 | 检查 defName 为 `AIPersonality_State`、`AIPersonality_BehaviorFlag`、`AIDialogue_Thought` |
| personality_shaping | 玩家塑造历史记录 | 取最近 maxCount 条（Settings 为 null 时兜底 50），格式为翻译后标签 |

`BuildFullPawnPrompt` 时排除 `personality_state`（避免评估时看到自己的当前状态）。

## Mod 入口注册

`RimMindPersonalityMod` 构造函数中完成：

1. `GetSettings<AIPersonalitySettings>()` — 加载设置
2. `new Harmony("mcocdaa.RimMindPersonality").PatchAll()` — 注册 Harmony 补丁
3. `RegisterContextProviders()` — 注册 3 个 ContextProvider
4. `RimMindAPI.RegisterSettingsTab("personality", ...)` — 注册设置标签页
5. `RimMindAPI.RegisterModCooldown("Personality", () => 36000)` — 注册模组冷却（36000 ticks = 0.6 天）

## 数据流

```
CompAIPersonality.CompTick()
    │
    ├── 检查触发条件（每日含抖动 / 事件含冷却）
    │       ▼
    ├── EvaluationInstructionHelper.SampleThoughtCount(mu) → targetCount
    │       ▼
    ├── PersonalityContextBuilder.BuildEvaluationPrompt(pawn, eventCtx, targetCount)
    │       ├── RimMindAPI.BuildFullPawnPrompt(exclude: ["personality_state"])
    │       └── EvaluationInstructionHelper.Append(basePrompt, eventCtx, targetCount, aiDecidesDuration, enableShapingVote)
    │       ▼
    ├── RimMindAPI.RequestAsync(request, callback)
    │       request: { SystemPrompt, UserPrompt, MaxTokens=300, Temperature=0.8, ModId="Personality", Priority=Low }
    │       ▼
    ├── AI 生成响应
    │       ▼
    ├── PersonalityThoughtMapper.Apply(response, pawn)
    │       ├── JsonConvert.DeserializeObject<PersonalityResultDto>(response.Content)
    │       ├── profile.aiNarrative = result.narrative
    │       ├── RemoveAllAIPersonalityThoughts(pawn)
    │       ├── foreach thought → ThoughtMaker.MakeThought → TryGainMemory
    │       │   └── 若 enableShapingVote → RegisterPendingRequest（强化/抑制/忽略）
    │       └── 若 showNotifications → Messages.Message
    │       ▼
    └── Thought_AIPersonality 挂到 Pawn
            └── MoodOffsetCalculator.CalcMoodOffset(aiIntensity) → 影响心情
```

## UI 层

### BioTabPersonalityPatch

- 使用 **Transpiler**（非 Postfix）挂载到 `CharacterCardUtility.DoTopStack`
- 锚点方法：`QuestUtility.AppendInspectStringsFromQuestParts`
- 在锚点调用后插入 `AddPersonalityButton(pawn)` 调用
- 按钮显示"人格"标签，hover 显示 AI 叙事，点击打开 `Dialog_PersonalityProfile`

### Dialog_PersonalityProfile

- 520x480 窗口，可拖拽
- 编辑区：人格描述、工作倾向、社交倾向（TextArea）
- 只读区：AI 近期叙事（ScrollView）
- 保存/取消按钮

## 设置项

| 设置 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| enablePersonality | bool | true | 总开关 |
| showNotifications | bool | true | 显示通知 |
| enableDailyEval | bool | true | 每日定时评估 |
| enableInjuryTrigger | bool | true | 受伤触发 |
| enableSkillTrigger | bool | true | 技能升级触发 |
| enableIncidentTrigger | bool | true | 事件触发 |
| enableDeathTrigger | bool | true | 死亡触发 |
| thoughtCountMu | float | **1.0** | Poisson 抽样参数（0→固定1个，1.5→平均1.5个，结果[1,3]） |
| thoughtDurationHours | float | 24 | 固定持续时间（小时），仅 Fixed 模式 |
| durationMode | enum | **AIDecides** | 持续时间模式（Fixed/AIDecides） |
| showLabelPrefix | bool | true | [RimMind] 前缀 |
| enableShapingVote | bool | true | 玩家塑造投票 |
| requestExpireTicks | int | 30000 | 请求过期（ticks） |
| shapingHistoryMaxCount | int | **20** | 塑造历史上限 |

## 代码约定

### 命名空间

- `RimMind.Personality` — 核心逻辑（Mod 入口、Thought 映射、Prompt、DTO、心情计算）
- `RimMind.Personality.Data` — 数据模型（Profile、WorldComponent、ShapingRecord）
- `RimMind.Personality.Comps` — ThingComp
- `RimMind.Personality.UI` — 界面（BioTab 补丁、对话框）
- `RimMind.Personality.Patches` — Harmony 补丁
- `RimMind.Personality.Debug` — 调试动作

### ThoughtDef 定义

在 `Defs/ThoughtDefs/AIPersonalityThoughts.xml` 中定义 3 个槽位：

```xml
<ThoughtDef>
  <defName>AIPersonality_Slot_0</defName>
  <thoughtClass>RimMind.Personality.Thought_AIPersonality</thoughtClass>
  <durationDays>1</durationDays>
  <stackLimit>1</stackLimit>
  <stages>
    <li>
      <label>AI personality state</label>
      <description>(AI generated)</description>
      <baseMoodEffect>0</baseMoodEffect>
    </li>
  </stages>
</ThoughtDef>
<!-- Slot_1, Slot_2 同理 -->
```

运行时通过 `Thought_AIPersonality` 覆盖 Label/Description/MoodOffset/DurationTicks。

### 序列化

```csharp
// ThingComp
public override void PostExposeData()
{
    base.PostExposeData();
    Scribe_Values.Look(ref _lastEventTick, "lastEventTick", -EventCooldownTicks);
    Scribe_Values.Look(ref _dailyJitter, "dailyJitter", -1);
}

// WorldComponent
public override void ExposeData()
{
    base.ExposeData();
    Scribe_Collections.Look(ref _profiles, "profiles", LookMode.Value, LookMode.Deep);
    _profiles ??= new Dictionary<int, PersonalityProfile>();
}

// Thought_AIPersonality — 注意 aiDescription 的存档 key 是 "aiDesc"
Scribe_Values.Look(ref aiLabel,            "aiLabel",            string.Empty);
Scribe_Values.Look(ref aiDescription,      "aiDesc",             string.Empty);
Scribe_Values.Look(ref aiIntensity,        "aiIntensity",        0);
Scribe_Values.Look(ref customDurationTicks, "customDurationTicks", -1);
```

### Harmony

- Harmony ID：`mcocdaa.RimMindPersonality`
- `AddCompToHumanlikePatch`：Postfix on `ThingDef.ResolveReferences`，检查 `race.intelligence == Humanlike` 后注入 Comp
- `BioTabPersonalityPatch`：Transpiler on `CharacterCardUtility.DoTopStack`，锚点 `QuestUtility.AppendInspectStringsFromQuestParts`
- Comp 注入不使用 XML PatchOperation（因为 XML Patch 在继承解析前运行，无法按 race/intelligence 过滤）

### 构建

- 目标框架：`net48`
- C# 语言版本：9.0
- Nullable：enable
- RimWorld 版本：1.6
- 输出路径：`../1.6/Assemblies/`
- 依赖：Krafs.Rimworld.Ref、Lib.Harmony.Ref、Newtonsoft.Json 13.0
- 编译期引用 RimMindCore.dll（Private=false，运行时由 RimWorld 加载）

### 测试

- 框架：xUnit
- 位置：`Tests/` 目录
- 无 RimWorld 依赖（纯逻辑测试）
- 使用 Core 的 `JsonTagExtractor.Extract<T>` 解析带标签的 JSON
- 测试覆盖：MoodOffset 查表、JSON 解析、评估指令构建、端到端流水线

### 翻译

- 中英双语，key 前缀 `RimMind.Personality.`
- System Prompt 7 段落通过翻译 key 定义（`RimMind.Personality.Prompt.System.*`）
- 评估指令模板通过翻译 key 定义（`RimMind.Personality.Prompt.*`）

## 调试

Dev 菜单（需开启开发模式）→ RimMind Personality：

- **Force Evaluate Selected Pawn** — 强制对选中 Pawn 发起 AI 评估（使用 `RequestImmediate`，ExpireAtTicks=36000）
- **Show Personality State (selected)** — 输出人格状态到日志（含 Profile、Thought 详情）
- **Clear Personality Thoughts (selected)** — 清除选中 Pawn 的人格 Thought
- **List Personality-Enabled Pawns** — 列出启用人格系统的殖民者
- **Reset Personality Profile (selected)** — 重置选中 Pawn 的人格档案

## 注意事项

1. **Thought 槽位限制**：最多 3 个，超出时忽略（不覆盖最早的，直接 break）
2. **每日抖动**：基于 `thingIDNumber ^ 0x3C3C3C3C` 的确定性随机，避免所有殖民者同一 tick 触发
3. **Poisson 抽样**：`thoughtCountMu` 控制平均 Thought 数量，结果 clamp 到 [1,3]；mu<=0 时固定 1
4. **AIDecides 模式**：AI 可为每个 Thought 指定不同持续时间（1~24 小时，超出 clamp）
5. **塑造历史**：`shapingHistoryMaxCount` 控制历史长度，超出时 `RemoveRange` 移除最旧记录
6. **排除 personality_state**：评估时排除当前状态，避免 AI 简单复制已有 Thought
7. **CalcDurationTicks null safety**：settings 参数为 nullable，null 时兜底返回 2500（1 小时）
8. **personality_state Provider**：同时检查 `AIPersonality_State`、`AIPersonality_BehaviorFlag`、`AIDialogue_Thought` 三个 defName
9. **IsEmpty 不检查 socialTendencies**：`PersonalityProfile.IsEmpty` 只检查 description、workTendencies、aiNarrative
10. **序列化 key 不一致**：`aiDescription` 字段的存档 key 是 `"aiDesc"`，修改时需注意向后兼容
11. **触发开关生效机制**：`enableDailyEval` 在 CompTick 中检查；事件触发开关通过 `TriggerEventType` 枚举在 TriggerEvent 中检查，外部 Patch 需传入正确的 eventType
12. **TriggerEvent 默认值**：`eventType` 默认为 `TriggerEventType.Incident`，未传入 eventType 的旧调用默认检查 `enableIncidentTrigger`
13. **DiversityHint**：`enableShapingVote` 为 true 时，`EvaluationInstructionHelper.Append` 在评估指令后追加多样化提示翻译 key `RimMind.Personality.Prompt.DiversityHint`，避免 AI 重复生成同类 Thought
14. **PromptSanitizer**：`EvaluationInstructionHelper.Append` 对指令部分调用 `PromptSanitizer.Sanitize()` 清洗，基础 prompt 不清洗
