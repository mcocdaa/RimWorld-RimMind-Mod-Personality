# RimMind - Personality

AI 驱动的人格系统，每日通过 LLM 评估殖民者状态，注入动态人格 Thought，影响心情与行为。

## RimMind 是什么

RimMind 是一套 AI 驱动的 RimWorld 模组套件，通过接入大语言模型（LLM），让殖民者拥有人格、记忆、对话和自主决策能力。

## 子模组列表与依赖关系

| 模组 | 职责 | 依赖 | GitHub |
|------|------|------|--------|
| RimMind-Core | API 客户端、请求调度、上下文打包 | Harmony | [链接](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Core) |
| RimMind-Actions | AI 控制小人的动作执行库 | Core | [链接](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Actions) |
| RimMind-Advisor | AI 扮演小人做出工作决策 | Core, Actions | [链接](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Advisor) |
| RimMind-Dialogue | AI 驱动的对话系统 | Core | [链接](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Dialogue) |
| RimMind-Memory | 记忆采集与上下文注入 | Core | [链接](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Memory) |
| **RimMind-Personality** | **AI 生成人格与想法** | Core | [链接](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Personality) |
| RimMind-Storyteller | AI 叙事者，智能选择事件 | Core | [链接](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Storyteller) |

```
Core ── Actions ── Advisor
  ├── Dialogue
  ├── Memory
  ├── Personality
  └── Storyteller
```

## 安装步骤

### 从源码安装

**Linux/macOS:**
```bash
git clone git@github.com:RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Personality.git
cd RimWorld-RimMind-Mod-Personality
./script/deploy-single.sh <your RimWorld path>
```

**Windows:**
```powershell
git clone git@github.com:RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Personality.git
cd RimWorld-RimMind-Mod-Personality
./script/deploy-single.ps1 <your RimWorld path>
```

### 从 Steam 安装

1. 安装 [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) 前置模组
2. 安装 RimMind-Core
3. 安装 RimMind-Personality
4. 在模组管理器中确保加载顺序：Harmony → Core → Personality

## 快速开始

### 填写 API Key

1. 启动游戏，进入主菜单
2. 点击 **选项 → 模组设置 → RimMind-Core**
3. 填写你的 **API Key** 和 **API 端点**
4. 填写 **模型名称**（如 `gpt-4o-mini`）
5. 点击 **测试连接**，确认显示"连接成功"

### 查看人格档案

1. 进入游戏，选择一个殖民者
2. 打开 Bio 页面，点击 **"人格"** 按钮
3. 查看 AI 生成的人格评估，编辑人格描述和工作/社交倾向

## 核心功能

### AI 人格评估

每日向 LLM 发送殖民者状态，AI 返回：

- **人格 Thought**：1-3 个动态心情状态，含标签、描述、强度、持续时长
- **叙事摘要**：50 字内描述小人近期心理变化
- **身份信息**：动机、特质、价值观（供其他 RimMind 模组使用）

Thought 通过独立槽位注入，在心情面板独立显示，互不叠加。

### 触发机制

| 触发类型 | 设置开关 | 过滤条件 | 说明 |
|---------|---------|---------|------|
| 每日定时 | enableDailyEval | — | 每游戏天评估一次（含随机抖动避免同时触发） |
| 受伤/患病 | enableInjuryTrigger | isBad + Severity≥0.2 | 殖民者受伤或患病时触发，轻微伤不触发 |
| 技能里程碑 | enableSkillTrigger | levelInt > preLevel | 技能等级提升时触发 |
| 威胁事件 | enableIncidentTrigger | ThreatBig/ThreatSmall | 仅威胁类事件触发，访客/贸易不触发 |
| 亲近者死亡 | enableDeathTrigger | 有社交关系 | 有社交关系的殖民者死亡时触发 |

事件触发有冷却期（默认 1200 tick），防止连锁触发。每种触发方式可独立开关。

### 人格档案

每个殖民者拥有持久化的人格档案：

- **玩家可编辑**：人格描述、工作倾向、社交倾向
- **AI 生成**：近期心理状态叙事（每日更新）
- **塑造投票**：玩家可对 AI 的人格评估投票，影响未来评估

### 心情影响

| AI 强度 | 心情偏移 |
|---------|---------|
| -3 | -10 |
| -2 | -3 |
| -1 | -1 |
| 0 | 0 |
| +1 | +1 |
| +2 | +3 |
| +3 | +10 |

### 上下文注入

人格档案和当前 Thought 自动注入 AI Prompt，供 Advisor、Dialogue 等模块参考。

## 设置项

| 设置 | 默认值 | 说明 |
|------|--------|------|
| 启用 AI 人格系统 | 开启 | 总开关 |
| 每日定时评估 | 开启 | 每游戏天评估一次 |
| 受伤触发 | 开启 | 受伤或患病时触发（过滤轻微伤） |
| 技能升级触发 | 开启 | 技能提升时触发 |
| 事件触发 | 开启 | 威胁类事件时触发 |
| 死亡触发 | 开启 | 亲近者死亡时触发 |
| Thought 持续时长模式 | AI 决定 | 固定 / AI 决定 |
| 固定时长 | 24 游戏小时 | 固定模式下的时长（1~24 小时） |
| 显示通知 | 开启 | 人格更新时右下角提示 |
| 显示 [RimMind] 前缀 | 开启 | 在心情面板区分 AI 生成和原版 Thought |
| 启用塑造投票 | 开启 | 玩家可对 AI 评估投票 |
| 请求过期时间 | 0.50 游戏天 | 塑造投票超时自动取消 |
| 塑造历史保留数量 | 20 | 保留最近 N 次投票记录供 AI 参考 |
| 每日评估间隔 | 24 游戏小时 | 自动评估的时间间隔 |
| 抖动范围 | 1.2 游戏小时 | 防止所有殖民者同时被评估 |
| 事件冷却 | 0.48 游戏小时 | 事件触发之间的最小间隔 |
| 请求超时 | 24 游戏小时 | 超时的待处理 AI 请求将被取消 |

## 常见问题

**Q: AI 会给殖民者什么 Thought？**
A: AI 根据殖民者的状态、经历和关系生成符合情境的 Thought。例如，长期受伤的殖民者可能获得"对康复失去信心"，刚建立关系的殖民者可能获得"对新朋友感到期待"。

**Q: 可以编辑人格档案吗？**
A: 可以。在 Bio 页面的"人格"面板中，你可以编辑人格描述、工作倾向和社交倾向。AI 评估时会参考你填写的内容。

**Q: 塑造投票有什么用？**
A: 你可以对 AI 的人格评估投票（强化/抑制/忽略），投票历史会注入后续 AI 请求的上下文，让 AI 逐渐学习你的偏好。选择"忽略"不会写入记录。

**Q: Thought 持续多久？**
A: 默认模式为"AI 决定"，AI 根据情境设定 1-24 小时不等的时长。也可切换为"固定"模式，统一使用设定的小时数（默认 24 游戏小时）。

**Q: 触发开关有什么用？**
A: 每种触发方式（每日定时、受伤、技能升级、事件、死亡）都有独立开关。关闭后该类型的触发将不再发起人格评估。

**Q: 配合 Memory 和 Advisor 效果更好吗？**
A: 是的。Memory 提供历史记忆，Personality 提供人格档案，Advisor 综合这些信息做出更符合角色的决策。

## 致谢

本项目开发过程中参考了以下优秀的 RimWorld 模组：

- [RimTalk](https://github.com/jlibrary/RimTalk.git) - 对话系统参考
- [RimTalk-ExpandActions](https://github.com/sanguodxj-byte/RimTalk-ExpandActions.git) - 动作扩展参考
- [NewRatkin](https://github.com/solaris0115/NewRatkin.git) - 种族模组架构参考
- [VanillaExpandedFramework](https://github.com/Vanilla-Expanded/VanillaExpandedFramework.git) - 框架设计参考

## 贡献

欢迎提交 Issue 和 Pull Request！如果你有任何建议或发现 Bug，请通过 GitHub Issues 反馈。


---

# RimMind - Personality (English)

An AI-driven personality system that evaluates colonist state daily via LLM, injecting dynamic personality Thoughts that affect mood and behavior.

## What is RimMind

RimMind is an AI-driven RimWorld mod suite that connects to Large Language Models (LLMs), giving colonists personality, memory, dialogue, and autonomous decision-making.

## Sub-Modules & Dependencies

| Module | Role | Depends On | GitHub |
|--------|------|------------|--------|
| RimMind-Core | API client, request dispatch, context packaging | Harmony | [Link](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Core) |
| RimMind-Actions | AI-controlled pawn action execution | Core | [Link](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Actions) |
| RimMind-Advisor | AI role-plays colonists for work decisions | Core, Actions | [Link](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Advisor) |
| RimMind-Dialogue | AI-driven dialogue system | Core | [Link](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Dialogue) |
| RimMind-Memory | Memory collection & context injection | Core | [Link](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Memory) |
| **RimMind-Personality** | **AI-generated personality & thoughts** | Core | [Link](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Personality) |
| RimMind-Storyteller | AI storyteller, smart event selection | Core | [Link](https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Storyteller) |

## Installation

### Install from Source

**Linux/macOS:**
```bash
git clone git@github.com:RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Personality.git
cd RimWorld-RimMind-Mod-Personality
./script/deploy-single.sh <your RimWorld path>
```

**Windows:**
```powershell
git clone git@github.com:RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Personality.git
cd RimWorld-RimMind-Mod-Personality
./script/deploy-single.ps1 <your RimWorld path>
```

### Install from Steam

1. Install [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
2. Install RimMind-Core
3. Install RimMind-Personality
4. Ensure load order: Harmony → Core → Personality

## Quick Start

### API Key Setup

1. Launch the game, go to main menu
2. Click **Options → Mod Settings → RimMind-Core**
3. Enter your **API Key** and **API Endpoint**
4. Enter your **Model Name** (e.g., `gpt-4o-mini`)
5. Click **Test Connection** to confirm

### View Personality Profile

1. In-game, select a colonist
2. Open the Bio tab, click the **"Personality"** button
3. View AI-generated personality assessment, edit description and tendencies

## Key Features

- **AI Personality Assessment**: Daily LLM evaluation generates 1-3 dynamic mood Thoughts + narrative summary + identity (motivations, traits, core values)
- **Multiple Triggers**: Daily timer, injury, skill milestone, threat incidents, death of loved ones - each with independent toggle and smart filtering
- **Editable Profile**: Players can edit personality description, work tendencies, and social tendencies
- **Shaping Vote**: Players can vote (reinforce/suppress/ignore) on AI assessments, influencing future evaluations
- **Context Injection**: Personality profiles and current Thoughts are automatically injected into AI prompts

## Trigger Mechanism

| Trigger Type | Setting Switch | Filter | Description |
|-------------|---------------|--------|-------------|
| Daily timer | enableDailyEval | — | Evaluate once per game day (with random jitter) |
| Injury/Illness | enableInjuryTrigger | isBad + Severity≥0.2 | Trigger on significant injuries, not minor scratches |
| Skill milestone | enableSkillTrigger | levelInt > preLevel | Trigger on skill level up |
| Threat incident | enableIncidentTrigger | ThreatBig/ThreatSmall | Only threat events trigger, not visitors/traders |
| Death of loved one | enableDeathTrigger | Has social relation | Trigger when a colonist with social relation dies |

Event triggers have a cooldown period (default 1200 ticks) to prevent chain triggering.

## Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Enable AI Personality System | On | Master switch |
| Daily Evaluation | On | Evaluate once per game day |
| Injury Trigger | On | Trigger on significant injuries (filters minor) |
| Skill Level Up Trigger | On | Trigger on skill improvement |
| Incident Trigger | On | Trigger on threat events only |
| Death Trigger | On | Trigger when loved ones die |
| Thought Duration Mode | AI Decides | Fixed / AI Decides |
| Fixed Duration | 24 game hours | Duration in Fixed mode (1~24 hours) |
| Show Notifications | On | Display notification on personality updates |
| Show [RimMind] Prefix | On | Distinguish AI-generated Thoughts from vanilla in mood panel |
| Enable Shaping Vote | On | Players can vote on AI assessments |
| Request Expiry | 0.50 game days | Auto-cancel shaping votes after timeout |
| Shaping History Limit | 20 | Keep last N vote records for AI reference |
| Daily Evaluation Interval | 24 game hours | Time between automatic evaluations |
| Jitter Range | 1.2 game hours | Prevents all colonists from being evaluated simultaneously |
| Event Cooldown | 0.48 game hours | Minimum time between event-triggered evaluations |
| Request Timeout | 24 game hours | Cancel pending AI requests that exceed this duration |

## FAQ

**Q: What kind of Thoughts does AI generate?**
A: AI generates context-appropriate Thoughts based on colonist state. For example, a long-injured colonist might get "losing faith in recovery", while a newly befriended colonist might get "excited about new friends".

**Q: Can I edit the personality profile?**
A: Yes. In the Bio tab's "Personality" panel, you can edit description, work tendencies, and social tendencies. AI evaluations reference your input.

**Q: What does shaping vote do?**
A: You can vote (reinforce/suppress/ignore) on AI personality assessments. Choosing "reinforce" or "suppress" writes a record; "ignore" does not. Vote history is injected into future AI requests, helping AI learn your preferences.

**Q: How long do Thoughts last?**
A: Default mode is "AI Decides", where AI sets 1-24 hour durations based on context. You can also switch to "Fixed" mode for a uniform duration (default 24 game hours).

**Q: What do the trigger toggles do?**
A: Each trigger type (daily timer, injury, skill level up, incident, death) has an independent toggle. When disabled, that trigger type will no longer initiate personality evaluations.

**Q: Does it work better with Memory and Advisor?**
A: Yes. Memory provides history, Personality provides character profiles, and Advisor combines these for more character-appropriate decisions.

## Acknowledgments

This project references the following excellent RimWorld mods:

- [RimTalk](https://github.com/jlibrary/RimTalk.git) - Dialogue system reference
- [RimTalk-ExpandActions](https://github.com/sanguodxj-byte/RimTalk-ExpandActions.git) - Action expansion reference
- [NewRatkin](https://github.com/solaris0115/NewRatkin.git) - Race mod architecture reference
- [VanillaExpandedFramework](https://github.com/Vanilla-Expanded/VanillaExpandedFramework.git) - Framework design reference

## Contributing

Issues and Pull Requests are welcome! If you have any suggestions or find bugs, please feedback via GitHub Issues.
