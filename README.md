# RimMind - Personality

AI 驱动的人格系统，每日通过 LLM 评估殖民者状态，注入动态人格 Thought，影响心情与行为。

## RimMind 是什么

RimMind 是一套 AI 驱动的 RimWorld 模组套件，通过接入大语言模型（LLM），让殖民者拥有人格、记忆、对话和自主决策能力。

## 子模组列表与依赖关系

| 模组 | 职责 | 依赖 |
|------|------|------|
| RimMind-Core | API 客户端、请求调度、上下文打包 | Harmony |
| RimMind-Actions | AI 控制小人的动作执行库 | Core |
| RimMind-Advisor | AI 扮演小人做出工作决策 | Core, Actions |
| RimMind-Dialogue | AI 驱动的对话系统 | Core |
| RimMind-Memory | 记忆采集与上下文注入 | Core |
| **RimMind-Personality** | **AI 生成人格与想法** | Core |
| RimMind-Storyteller | AI 叙事者，智能选择事件 | Core |

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
git clone git@github.com:mcocdaa/RimWorld-RimMind-Mod-Personality.git
cd RimWorld-RimMind-Mod-Personality
./script/deploy-single.sh <your RimWorld path>
```

**Windows:**
```powershell
git clone git@github.com:mcocdaa/RimWorld-RimMind-Mod-Personality.git
cd RimWorld-RimMind-Mod-Personality
./script/deploy-single.ps1 <your RimWorld path>
```

### 从 Steam 安装

1. 安装 [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) 前置模组
2. 安装 RimMind-Core
3. 安装 RimMind-Personality
4. 在模组管理器中确保加载顺序：Harmony → Core → Personality

<!-- ![安装步骤](images/install-steps.png) -->

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

<!-- ![人格档案](images/screenshot-personality-profile.png) -->

## 截图展示

<!-- ![人格Thought](images/screenshot-personality-thought.png) -->
<!-- ![心情面板](images/screenshot-personality-mood.png) -->
<!-- ![人格编辑](images/screenshot-personality-edit.png) -->

## 核心功能

### AI 人格评估

每日（或事件触发）向 LLM 发送殖民者状态，AI 返回：

- **人格 Thought**：1-3 个动态心情状态，含标签、描述、强度、持续时长
- **叙事摘要**：50 字内描述小人近期心理变化

Thought 通过独立槽位注入，在心情面板独立显示，互不叠加。

### 触发机制

| 触发类型 | 说明 |
|---------|------|
| 每日定时 | 每游戏天评估一次 |
| 受伤/患病 | 健康状态剧变时触发 |
| 技能里程碑 | 技能等级提升时触发 |
| 重要事件 | 袭击、收获等事件时触发 |
| 亲近者死亡 | 社交关系对象死亡时触发 |

事件触发有 0.5 天冷却期，防止连锁触发。

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
| 受伤触发 | 开启 | 健康剧变时触发 |
| 技能升级触发 | 开启 | 技能提升时触发 |
| 事件触发 | 开启 | 重要事件时触发 |
| 死亡触发 | 开启 | 亲近者死亡时触发 |
| Thought 数量期望值 | 1.5 | 每次评估生成 1-3 个 Thought |
| Thought 持续时长模式 | 固定 | 固定 / AI 决定 |
| 固定时长 | 24 游戏小时 | 固定模式下的时长 |
| 显示通知 | 开启 | 人格更新时右下角提示 |
| 启用塑造投票 | 开启 | 玩家可对 AI 评估投票 |

## 常见问题

**Q: AI 会给殖民者什么 Thought？**
A: AI 根据殖民者的状态、经历和关系生成符合情境的 Thought。例如，长期受伤的殖民者可能获得"对康复失去信心"，刚建立关系的殖民者可能获得"对新朋友感到期待"。

**Q: 可以编辑人格档案吗？**
A: 可以。在 Bio 页面的"人格"面板中，你可以编辑人格描述、工作倾向和社交倾向。AI 评估时会参考你填写的内容。

**Q: 塑造投票有什么用？**
A: 你可以对 AI 的人格评估投票（赞同/反对），投票历史会注入后续 AI 请求的上下文，让 AI 逐渐学习你的偏好。

**Q: Thought 持续多久？**
A: 默认 24 游戏小时。可以切换为"AI 决定"模式，让 AI 根据情境设定 1-24 小时不等的时长。

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

| Module | Role | Depends On |
|--------|------|------------|
| RimMind-Core | API client, request dispatch, context packaging | Harmony |
| RimMind-Actions | AI-controlled pawn action execution | Core |
| RimMind-Advisor | AI role-plays colonists for work decisions | Core, Actions |
| RimMind-Dialogue | AI-driven dialogue system | Core |
| RimMind-Memory | Memory collection & context injection | Core |
| **RimMind-Personality** | **AI-generated personality & thoughts** | Core |
| RimMind-Storyteller | AI storyteller, smart event selection | Core |

## Installation

### Install from Source

**Linux/macOS:**
```bash
git clone git@github.com:mcocdaa/RimWorld-RimMind-Mod-Personality.git
cd RimWorld-RimMind-Mod-Personality
./script/deploy-single.sh <your RimWorld path>
```

**Windows:**
```powershell
git clone git@github.com:mcocdaa/RimWorld-RimMind-Mod-Personality.git
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

- **AI Personality Assessment**: Daily (or event-triggered) LLM evaluation generates 1-3 dynamic mood Thoughts + narrative summary
- **Multiple Triggers**: Daily timer, injury, skill milestone, incidents, death of loved ones
- **Editable Profile**: Players can edit personality description, work tendencies, and social tendencies
- **Shaping Vote**: Players can vote on AI assessments, influencing future evaluations
- **Context Injection**: Personality profiles and current Thoughts are automatically injected into AI prompts

## FAQ

**Q: What kind of Thoughts does AI generate?**
A: AI generates context-appropriate Thoughts based on colonist state. For example, a long-injured colonist might get "losing faith in recovery", while a newly befriended colonist might get "excited about new friends".

**Q: Can I edit the personality profile?**
A: Yes. In the Bio tab's "Personality" panel, you can edit description, work tendencies, and social tendencies. AI evaluations reference your input.

**Q: What does shaping vote do?**
A: You can vote (approve/disapprove) on AI personality assessments. Vote history is injected into future AI requests, helping AI learn your preferences.

**Q: How long do Thoughts last?**
A: Default is 24 game hours. You can switch to "AI Decides" mode for 1-24 hour durations based on context.

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
