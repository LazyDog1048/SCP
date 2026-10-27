# 进度与交接

## 当前交接（2026-10-27）

- 用户目标：将项目上传到自己的 Git，并允许另一位成员与其 AI 从共享上下文继续开发。
- 用户已创建 GitHub 仓库：https://github.com/LazyDog1048/SCP 。远端 origin 已配置，main 跟踪 origin/main。
- 已准备：Unity 忽略规则、文件属性、跨 AI 入口、项目事实与决策记录、团队分支工作流。
- 游戏开发状态：3D lowpoly 多人联机派对游戏。已有局域网联机底子（见下方“联机底子”），尚无具体玩法规则。
- Unity 版本：已改为 6000.3.27f1 LTS（URP 17.3.0），详见 Decisions.md。
- 验证：2026-10-27 用 6000.3.27f1 以 batchmode（-batchmode -quit）打开项目，成功完成导入与脚本编译，日志无编译错误，退出码 0。未在编辑器 GUI 中打开场景，未运行测试或构建。
- 本地首次提交：c6f2df1，main 分支；包含 68 个文件，缓存排除已检查。新增文档通过 Git 空白检查；原有 Unity 模板序列化文件保留原样。
- 上传已完成：用户创建仓库，并通过 GitHub 设备验证完成本机 Git 认证，工程及 AI 上下文已推送到 main。此前浏览器创建/认证阻塞已解决。
- 待完成：由所有者邀请协作者；对方克隆、使用 Unity 6000.3.27f1 首次导入验证，并让其 AI 按 AGENTS.md 读取交接。
- 下一步开发前：请用户实测联机底子并提供首个派对玩法及验收条件，不要自行推断 SCP 具体设定。

## 联机底子（2026-10-28 / Claude / main）

- 用户需求：3D lowpoly 多人联机派对游戏，先搭底子。已确认：局域网联机（一人当主机，其他人填 IP 加入）、第一人称视角、移动+跳跃、冲刺、推/扑倒其他玩家。验收条件未单独给出。
- 已完成：
  - 依赖：Netcode for GameObjects 2.13.3（带入 Unity Transport 2.6.0）。
  - 脚本 Assets/Game/Scripts：Player/PlayerController（第一人称移动/跳跃/冲刺/视角、击退、被扑倒状态）、Player/PlayerPush（左键推人，冲刺中左键扑倒，服务器校验）、Player/PlayerVisuals（按玩家编号上色、隐藏自己身体、倒地表现）、Network/LanSession（创建主机/按 IP 加入、连接审批与出生点、IMGUI 菜单、命令行参数）、Core/InputGate、CameraHome、SpawnPoints。
  - 生成器 Assets/Game/Editor/PlaygroundBuilder.cs：菜单「SCP/重新生成 Playground 场景与玩家 Prefab」生成 Materials、Prefabs/Player.prefab、Scenes/Playground.unity，并把 Playground 设为首个构建场景（SampleScene 保留但禁用）；菜单「SCP/构建 Windows 测试包」输出 Builds/Windows/SCP.exe。
  - Assets/DefaultNetworkPrefabs.asset 为 Netcode 自动生成。
- 操作：WASD/左摇杆移动，鼠标/右摇杆视角，空格/A 跳，左 Shift/按下左摇杆冲刺，左键/手柄 X 推人，冲刺中推=扑倒（对方倒地 1.6 秒）；Esc 释放鼠标，点击画面恢复；掉出平台自动回出生点。最多 8 人，端口默认 7777。
- 验证（实际执行）：
  - 6000.3.27f1 batchmode 编译无错误，生成器成功，玩家 prefab GlobalObjectIdHash 非 0。
  - Windows Development 构建成功。
  - 本机 1 主机 + 2 客户端无窗口进程（-batchmode -nographics -host / -client 127.0.0.1 -port 7790 -quitAfter N）：3 个进程均连接成功，各自生成 3 个玩家，位置均在出生点，日志无异常。期间发现并修复：客户端 CharacterController 首次 Move 把玩家拉回原点（生成后先 Physics.SyncTransforms）。
- 未验证：推人/扑倒、跳跃冲刺手感、第一人称相机、IMGUI 菜单中文显示均未在有画面、有真人输入的情况下测试；未在两台真实电脑的局域网中测试（Windows 防火墙首次会弹窗，需允许）；没有自动化测试。
- 已知限制：移动为拥有者权威（客户端可作弊，派对游戏暂可接受）；菜单是临时 IMGUI；没有局域网自动发现、角色动画、音效、玩法规则。
- 未提交：ProjectSettings/UnityConnectSettings.asset 的 m_Enabled 0→1，来源不明（非本任务需要），保留在工作区由用户决定。
- 下一步：用户在编辑器打开 Assets/Game/Scenes/Playground.unity，用 Builds/Windows/SCP.exe 或 Multiplayer Play Mode 开两个实例实测手感；确认首个派对玩法与验收条件。

## 后续每次任务更新格式

- 日期 / 执行者 / 分支：
- 用户需求及验收条件：
- 已完成（附关键文件）：
- 决策及原因（重要项写入 Decisions.md）：
- 验证命令或 Unity 操作 / 实际结果：
- 未验证项 / 已知问题 / 阻塞：
- 下一位执行者的具体步骤：

每次任务结束更新当前交接，追加必要的历史记录，并与代码一起提交。不要把未执行或未完成的工作标记为完成。

## 历史记录

- 2026-10-27 / Claude / main：按用户要求将 Unity 版本从 6000.4.7f1 改为 6000.3.27f1，URP 17.4.0→17.3.0；batchmode 导入编译通过；记录游戏类型为多人联机派对游戏。改动文件：ProjectSettings/ProjectVersion.txt、Packages/manifest.json、Packages/packages-lock.json、ProjectSettings/EditorBuildSettings.asset、ProjectSettings/ShaderGraphSettings.asset、README.md、Docs/AI/*。
- 2026-10-28 / Claude / main：参考 TowerDefend 项目的 .gitignore 合并整理本项目 .gitignore，补充 AssetStoreTools、Rider 插件、ExportedObj、.consulo、*.unityproj/*.userprefs/*.booproj/*.svd/*.tmp、*.pdb.meta 等、sysinfo.txt、fmod_editor.log、*.apk/*.aab/*.unitypackage，以及 Assets 下 .meta 永不忽略规则。保留 UserSettings 忽略（TowerDefend 中该行被注释，但 AGENTS.md 要求不提交）。验证：git ls-files -ci --exclude-standard 为空，无已跟踪文件被误忽略。
- 2026-10-28 / Claude / main：搭建局域网联机底子（Netcode for GameObjects 2.13.3、第一人称角色、推人/扑倒、Playground 场景与生成器），详见“联机底子”。
- 2026-10-28 / Claude / main：PlayerSettings 开启 Run In Background（联机时失焦窗口不能暂停，否则主机停止同步），已重新构建 Windows 包。
