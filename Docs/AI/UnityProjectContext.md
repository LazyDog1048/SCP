# Unity 项目上下文

<!-- unity-onboarding:generated:start -->

- 分析日期：2026-10-27（Asia/Hong_Kong）。分析基线：首次 Git 提交之前的工程；之后参考 Git 历史。
- 项目名：SCP。已确认目前为初始模板；游戏玩法、目标平台及完成标准尚未确定。
- Unity：6000.4.7f1，revision f3c3c4248748（ProjectSettings/ProjectVersion.txt）。
- 渲染：URP 17.4.0；GraphicsSettings.asset 已指定渲染管线资源，Assets/Settings 保存渲染设置。
- 输入：Input System 1.19.0；ProjectSettings.asset 的 activeInputHandler=1，Assets/InputSystem_Actions.inputactions 保存输入动作。
- 相关依赖：AI Navigation 2.0.12、Timeline 1.8.12、uGUI 2.0.0、Visual Scripting 1.9.11、Unity Test Framework 1.6.0。来源：Packages/manifest.json、packages-lock.json。
- Multiplayer Center 存在，但没有游戏网络代码证据；不可据此认定游戏已实现多人联机。

## 目录、程序集及启动

- Assets/Scenes/SampleScene.unity：EditorBuildSettings.asset 中唯一启用场景，也是当前启动候选。
- Assets/Settings：渲染管线/模板设置。
- Assets/TutorialInfo：模板说明；仅发现 Readme.cs（ScriptableObject）和 Editor/ReadmeEditor.cs（编辑器脚本）。
- 未发现自有游戏代码、asmdef 或专用测试。现有脚本使用默认程序集，Editor 目录由 Unity 隔离。
- 尚未建立游戏架构、场景加载系统、存档系统或统一代码规范；不可从模板脚本推断正式规范。

## 验证、工具与未知项

- 已通过静态读取版本、依赖、构建场景及代表性脚本建立基线。
- 未运行 Unity 编译、EditMode/PlayMode 测试或构建；没有已验证可玩的功能。
- 当前会话无可用 Unity MCP 工具；manifest 未发现 Unity MCP 依赖。其他成员的本机工具需重新检查。
- 目标平台、游戏设计、多人联机需求、测试策略均待用户确认。
- 修改时保留 Assets 的 .meta；不提交生成缓存；依赖和 Unity 版本未经任务需要不升级。

<!-- unity-onboarding:generated:end -->
