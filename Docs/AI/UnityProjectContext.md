# Unity 项目上下文

<!-- unity-onboarding:generated:start -->

- 分析日期：2026-10-27（Asia/Hong_Kong）。分析基线：首次 Git 提交之前的工程；之后参考 Git 历史。
- 项目名：SCP。用户已说明类型为多人联机派对游戏；目前仍为初始模板，具体玩法、目标平台及完成标准尚未确定。
- Unity：6000.3.27f1，revision 672d562a42a7（ProjectSettings/ProjectVersion.txt）。2026-10-27 由 6000.4.7f1 降级，见 Decisions.md。
- 渲染：URP 17.3.0（6000.3 内置版本）；GraphicsSettings.asset 已指定渲染管线资源，Assets/Settings 保存渲染设置。
- 输入：Input System 1.19.0；ProjectSettings.asset 的 activeInputHandler=1，Assets/InputSystem_Actions.inputactions 保存输入动作。
- 相关依赖：AI Navigation 2.0.12、Timeline 1.8.12、uGUI 2.0.0、Visual Scripting 1.9.11、Unity Test Framework 1.6.0。来源：Packages/manifest.json、packages-lock.json。
- 联机：Netcode for GameObjects 2.13.3（局域网，Unity Transport），尚未接入 Relay/Lobby。

## 目录、程序集及启动

- Assets/Game/Scenes/Playground.unity：首个启用的构建场景（联机测试场地）。Assets/Scenes/SampleScene.unity 保留但在构建中禁用。
- Assets/Settings：渲染管线/模板设置。
- Assets/TutorialInfo：模板说明；仅发现 Readme.cs（ScriptableObject）和 Editor/ReadmeEditor.cs（编辑器脚本）。
- 自有代码在 Assets/Game（命名空间 ScpParty），未使用 asmdef，编译进默认程序集；Assets/Game/Editor 为编辑器脚本。没有自动化测试。
- 场景与玩家 prefab 由 Assets/Game/Editor/PlaygroundBuilder.cs 生成（菜单 SCP/…）。尚无场景加载、存档、玩法规则系统。

## 验证、工具与未知项

- 已通过静态读取版本、依赖、构建场景及代表性脚本建立基线。
- 2026-10-28 已在 6000.3.27f1 batchmode 编译、Windows 构建，并用无窗口主机+2 客户端验证连接与玩家生成；有画面的手感与推人尚未实测。
- 当前会话无可用 Unity MCP 工具；manifest 未发现 Unity MCP 依赖。其他成员的本机工具需重新检查。
- 目标平台、游戏设计、多人联机需求、测试策略均待用户确认。
- 修改时保留 Assets 的 .meta；不提交生成缓存；依赖和 Unity 版本未经任务需要不升级。

<!-- unity-onboarding:generated:end -->
