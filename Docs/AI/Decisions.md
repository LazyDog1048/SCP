# 需求与决策

## 2026-10-27：使用仓库文件共享 AI 上下文

- 已确认需求：用户需要将工程上传到自己的 Git，并让其他成员的 AI 理解进度并接续。
- 做法：AGENTS.md 为通用入口，CLAUDE.md 引用该入口，Docs/AI 维护项目事实、当前交接和决策。
- 原因：这些文件随 Git 提交与拉取同步，不依赖某一聊天账号或 AI 的私有记忆。
- 限制：聊天历史不自动同步；各 AI 需遵循并更新文档。未拉取的提交和未推送的本地进度无法在另一台电脑看到。
- 协作流程：成员各自使用功能分支，通过 Pull Request 合并；场景与 prefab 协调编辑负责人。
- 未确认：玩法、平台、美术方向、联机需求；项目名 SCP 不构成这些需求的依据。

## 2026-10-27：Unity 版本改为 6000.3.27f1 LTS

- 已确认需求：用户要求项目改用其本机安装的 Unity 6000.3.27f1（LTS）打开。
- 做法：ProjectVersion.txt 改为 6000.3.27f1 (672d562a42a7)；URP 从 17.4.0 降到 6000.3 内置的 17.3.0，其余注册表包版本不变。packages-lock.json、EditorBuildSettings.asset、ShaderGraphSettings.asset 的改动由 Unity 6000.3 打开项目时自动生成。
- 原因：URP/Core/ShaderGraph 与编辑器版本绑定，17.4.0 无法在 6000.3 中解析。
- 影响：所有成员需使用 6000.3.27f1；不要再用 6000.4 打开并提交，否则会把版本和包升级回去。

## 2026-10-27：游戏类型

- 已确认需求：用户说明该项目是 3D lowpoly 多人联机派对游戏；第一版为局域网联机、第一人称视角，动作为移动、跳跃、冲刺、推/扑倒（2026-10-28 确认）。
- 未确认：具体玩法规则、玩家人数上限（暂定 8）、目标平台、是否需要在线联机（Relay/Lobby）。

## 2026-10-28：联机底子技术方案

- 网络库：Netcode for GameObjects 2.13.3 + Unity Transport。原因：Unity 官方、适合小规模派对游戏，后续可接 Relay/Lobby 做在线联机。3.x 要求 Unity 6000.7，不适用。
- 权威模式：角色移动由拥有者客户端计算（NetworkTransform AuthorityMode=Owner），保证第一人称手感；推人由发起方请求、服务器校验冷却与距离后，通知被推者拥有者施加击退。代价：客户端可作弊，派对局域网场景可接受；若以后做公开在线对战需改为服务器权威或预测。
- 场景/prefab 由编辑器脚本 PlaygroundBuilder 生成，便于 AI 在 batchmode 下复现与审查；重新生成会覆盖 Playground.unity 和 Player.prefab，手工在编辑器中改过后不要再运行生成器，或先把改动写回生成器。
- 菜单用 IMGUI 临时实现，正式 UI 待定。
- 输入使用项目级 InputSystem_Actions 的 Player 动作表（Move/Look/Jump/Sprint/Attack），未新增动作。
- PlayerSettings.runInBackground=1：联机游戏失焦时必须继续运行网络与逻辑。
