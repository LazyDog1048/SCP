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

- Assets/Scenes/Playground.unity：唯一的构建场景（联机测试场地）。Unity 模板的 SampleScene、Readme、TutorialInfo 已于 2026-10-28 删除。
- Assets/Settings：渲染管线/模板设置。
- Assets/TutorialInfo：模板说明；仅发现 Readme.cs（ScriptableObject）和 Editor/ReadmeEditor.cs（编辑器脚本）。
- 目录：Assets/Scripts（运行时代码，命名空间 ScpParty）、Editor（编辑器脚本）、Scenes、Prefabs（含 Weapons/ 武器模型）、Models（Tripo 生成的模型，每个模型一个子目录，含 FBX、贴图、材质、prefab）、Materials、Data/Weapons（武器参数）、Resources（WeaponDatabase、FX 材质）、Settings（URP 配置，其中 SampleSceneProfile 是渲染管线引用的后处理配置，不是模板残留）。未使用 asmdef，编译进默认程序集。没有单元测试。
- 场景、玩家/木桩/拾取物 prefab、武器数据与模型由 Assets/Editor/PlaygroundBuilder(.Combat/.Rooms).cs 生成（菜单 SCP/…）。尚无场景加载、存档、玩法规则系统。
- 代码目录：Scripts/Core（出生点、服务器生成器、输入开关）、Network（LanSession）、Player（移动、推人、表现、复活、HUD）、Combat（Health、木桩、飘字）、Weapons（武器定义/拾取/攻击/子弹/特效）、Interaction（可交互基类、门、玩家交互）、DevTools（CombatSelfTest）。
- Playground 布局：室内房间 A（出生点、木桩、武器工作台）与北侧房间 B（工作间），门在 (0,0,11.2)。道具 prefab 在 Prefabs/Props。
- 自动化验证：Builds/Windows/SCP.exe -batchmode -nographics -host -combatTest -quitAfter 75，再开一个 -client 127.0.0.1，查看日志中的 [CombatTest]。

## 验证、工具与未知项

- 已通过静态读取版本、依赖、构建场景及代表性脚本建立基线。
- 2026-10-28 已在 6000.3.27f1 batchmode 编译、Windows 构建，并用无窗口主机+2 客户端验证连接与玩家生成；有画面的手感与推人尚未实测。
- 当前会话无可用 Unity MCP 工具；manifest 未发现 Unity MCP 依赖。其他成员的本机工具需重新检查。
- 目标平台、游戏设计、多人联机需求、测试策略均待用户确认。
- 修改时保留 Assets 的 .meta；不提交生成缓存；依赖和 Unity 版本未经任务需要不升级。

<!-- unity-onboarding:generated:end -->
- 已知警告：日志中 “Missing types referenced from component UniversalRenderPipelineGlobalSettings … PathTracing” 来自 6000.4 保存的 URP 全局设置，6000.3 无此类型，不影响运行。

## 2026-10-30 角色素材更新
- Assets/ThirdParty：KenneyProtagonists（模型、4 皮肤、Idle/Run/Jump 源动画）、QuaterniusMonsters（50 模型与 atlas）、UniversalAnimations（Unity Standard 与 RM 两个 FBX）。附 CC0 许可。
- Assets/Models/Characters：玩家视觉、50 个分类前缀怪物 prefab、URP 材质、Idle/Run Animator Controller 与映射后的 .anim。
- Player.prefab 已接 Kenney 外观；PlayerVisuals 使用水平位移驱动 Idle/Run，仍保留自身隐藏/倒地。跳跃素材已导入但未接 Animator；暂无攻击动画。
- 菜单 SCP/模型/导入玩家与怪物 可重复设置；PlaygroundBuilder 重建玩家后调用 ApplyPlayerVisual 保持外观。原场景不重建。
