# AGENTS.md — RimMind-Personality

人格系统，LLM评估小人状态 → 注入人格Thought(最多3槽位)影响心情与行为。

## 项目定位

每日定时(含确定性抖动)或事件触发(受伤/技能/事件/死亡) → ContextEngine(RequestStructured, SchemaRegistry.PersonalityOutput) → AI评估 → `PersonalityThoughtMapper.Apply` 解析 → 写入 `PersonalityProfile`(narrative+identity) → 生成 `Thought_AIPersonality`(最多3槽位) → `MoodOffsetCalculator` 查表影响心情。含玩家塑造投票(强化/抑制/忽略)、AgentIdentity注册、Bio页人格按钮、WorldComponent定期清理无效Profile。

依赖: Core(编译期)，Personality上下文被Advisor/Dialogue通过Core系统自动消费。翻译自包含(事件触发key已迁入自身语言文件)。

## 构建

| 项 | 值 |
|----|-----|
| Target | net48, C#9.0, Nullable enable |
| Output | `../1.6/Assemblies/` |
| Assembly | RimMindPersonality |
| Harmony ID | mcocdaa.RimMindPersonality |
| 依赖 | RimMindCore.dll, Krafs.Rimworld.Ref, Lib.Harmony.Ref, Newtonsoft.Json 13.0 |

## 源码结构

```
Source/
├── RimMindPersonalityMod.cs              Mod入口(注册Provider/Identity/SettingsTab/Cooldown)
├── Personality/
│   ├── PersonalityThoughtMapper.cs       核心: AI响应→Thought映射+塑造投票+EvaluationSchema
│   ├── PersonalityResultDto.cs           JSON DTO(无RimWorld依赖)
│   ├── Thought_AIPersonality.cs          自定义Thought(重写Label/MoodOffset/DurationTicks)
│   └── MoodOffsetCalculator.cs           强度→心情偏移查表(-3~+3)
├── Settings/AIPersonalitySettings.cs     17项设置(含4项时间参数)
├── Data/
│   ├── PersonalityProfile.cs             人格档案(IExposable) + AIPersonalityWorldComponent(含定期清理)
│   └── ShapingRecord.cs                  玩家塑造记录
├── Comps/CompAIPersonality.cs            ThingComp(Tick触发/事件触发/TriggerEventType枚举)
├── UI/BioTabPersonalityPatch.cs + Dialog_PersonalityProfile.cs
├── Patches/
│   ├── AddCompToHumanlikePatch.cs        ThingDef.ResolveReferences Postfix注入Comp
│   ├── Patch_PersonalityInjury.cs        HediffSet.AddDirect Postfix(isBad+Severity>=0.2f+IsFreeNonSlaveColonist过滤)
│   ├── Patch_PersonalityDeath.cs         Pawn.Kill Postfix(检查DirectRelations)
│   ├── Patch_PersonalityIncident.cs      IncidentWorker.TryExecuteWorker Postfix(ThreatBig/ThreatSmall过滤)
│   └── Patch_PersonalitySkill.cs         SkillRecord.Learn Prefix+Postfix(对象引用作键+GetSkill引用比较)
└── Debug/PersonalityDebugActions.cs      8个Debug动作
```

## 触发机制

CompAIPersonality.CompTick: `DailyInterval` + `JitterRange`(基于thingIDNumber确定性抖动) + 事件触发(eventCooldownTicks冷却)。

TriggerEventType枚举: `Injury`(enableInjuryTrigger) / `Skill`(enableSkillTrigger) / `Incident`(enableIncidentTrigger) / `Death`(enableDeathTrigger)

请求参数: `Scenario=Personality, ExcludeKeys=["personality_state"], MaxTokens=600, Temperature=0.8f`

### 事件触发Patch过滤逻辑

| Patch | 过滤条件 |
|-------|---------|
| Injury | `hediff.def.isBad != false` + `hediff.Severity >= 0.2f` + `pawn.IsFreeNonSlaveColonist` |
| Skill | `__instance.levelInt > preLevel`(通过GetSkill引用比较找Pawn) |
| Incident | `def.category.defName == "ThreatBig" \|\| "ThreatSmall"` |
| Death | `pawn.relations.DirectRelations` 包含 killedPawn |

## 心情偏移查表

| intensity | -3 | -2 | -1 | 0 | +1 | +2 | +3 |
|-----------|---:|----|----|---|----|----|----|
| MoodOffset | -10 | -3 | -1 | 0 | +1 | +3 | +10 |

`MoodOffsetCalculator.CalcMoodOffset` 自动clamp到[-3,+3]后查表。

## PersonalityResultDto

```csharp
thoughts[]: {type, label, description, intensity(-3~+3), duration_hours?}
narrative: string
identity?: {motivations[], traits[], core_values[]}
```

## 上下文注入

| Provider | 层级 | 内容 |
|----------|------|------|
| personality_profile | L3_State(0.25) | 人格档案(描述+工作倾向+社交倾向+AI叙事) |
| personality_state | L3_State(0.20) | 当前活跃Thought列表(Slot_0/1/2) |
| personality_shaping | L3_State(0.15) | 玩家塑造历史记录 |
| personality_task | L0_Static(0.95) | TaskInstruction(仅Personality场景) |
| AgentIdentity | Core注册 | identity→motivations/traits/core_values |

## Profile清理

`AIPersonalityWorldComponent.WorldComponentTick()` 每60000 tick执行：
1. 收集所有地图+WorldPawns的存活Pawn ID
2. 移除_profiles中不存在于存活集合的条目

## 代码约定

- Thought槽位最多3个(`SlotDefNames[3]`)
- `aiDescription` 存档key为 `"aiDesc"`(非 `"aiDescription"`，修改需向后兼容)
- `AIDecides` 模式: `duration_hours` clamp到[1,24]
- 翻译键前缀: `RimMind.Personality.*`
- 事件触发翻译key虽含 `RimMind.Memory.*` / `RimMind.Storyteller.*` 命名空间，但已定义在自身语言文件中
- Injury Patch使用 `IsFreeNonSlaveColonist`，与CompAIPersonality.IsEligible()一致
- Skill Patch使用 `Dictionary<object, int>` 以对象引用为键，避免哈希碰撞
- Skill Patch使用 try/finally 确保 PreLevels 条目在异常时也能清理
- `_compat1` 字段仅消费旧存档 `rimTalkSynced` key，值不使用

## 操作边界

### ✅ 必须做
- 新触发类型在 `TriggerEventType` 添加值 + `CompAIPersonality.TriggerEvent` 添加分支
- 新设置项在 `ExposeData` + UI + 翻译键三处同步

### ⚠️ 先询问
- 修改 `MoodTable` 心情偏移值
- 修改每日抖动算法
- 修改Patch触发过滤(如Injury调整严重度阈值)
- 修改 `_compat1` 序列化(影响旧存档兼容)

### 🚫 绝对禁止
- 后台线程调用 `ThoughtMaker.MakeThought`/`TryGainMemory`
- 修改 `"aiDesc"` 存档key(破坏向后兼容)
- 向Core注册Provider用旧API(用 `ContextKeyRegistry.Register`)
- Skill Patch 中用 `GetHashCode()` 作字典键(已改用对象引用)
