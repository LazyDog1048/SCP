# 进度与交接

## 当前交接（2026-10-27）

- 用户目标：将项目上传到自己的 Git，并允许另一位成员与其 AI 从共享上下文继续开发。
- 用户已创建 GitHub 仓库：https://github.com/LazyDog1048/SCP 。远端 origin 已配置，main 跟踪 origin/main。
- 已准备：Unity 忽略规则、文件属性、跨 AI 入口、项目事实与决策记录、团队分支工作流。
- 游戏开发状态：3D lowpoly 多人联机派对游戏。已有局域网联机底子、血量与伤害、4 把武器、训练木桩、两个由门连接的房间（见下方各节），尚无具体玩法规则。
- Unity 版本：已改为 6000.3.27f1 LTS（URP 17.3.0），详见 Decisions.md。
- 验证：2026-10-27 用 6000.3.27f1 以 batchmode（-batchmode -quit）打开项目，成功完成导入与脚本编译，日志无编译错误，退出码 0。未在编辑器 GUI 中打开场景，未运行测试或构建。
- 本地首次提交：c6f2df1，main 分支；包含 68 个文件，缓存排除已检查。新增文档通过 Git 空白检查；原有 Unity 模板序列化文件保留原样。
- 上传已完成：用户创建仓库，并通过 GitHub 设备验证完成本机 Git 认证，工程及 AI 上下文已推送到 main。此前浏览器创建/认证阻塞已解决。
- 待完成：由所有者邀请协作者；对方克隆、使用 Unity 6000.3.27f1 首次导入验证，并让其 AI 按 AGENTS.md 读取交接。
- 下一步开发前：请用户实测联机底子并提供首个派对玩法及验收条件，不要自行推断 SCP 具体设定。

## 房间与门（2026-10-28 / Claude / main）

- 用户需求：生成房间并放置物品（工作台那类）；房间 A 与房间 B 由一道门连接，玩家与门交互打开后可以去房间 B；木桩等现有道具保留在房间 A。
- 已完成：
  - 场地由露天小岛改为室内：房间 A（内部 x -13~13，z -11~11）放出生点、木桩、武器、箱子、高台、矮墙；房间 B（x -8~8，z 11.4~25.4）是工作间。两房间共用 A 的北墙，门在 (0, 0, 11.2)。层高 5 米，屋顶不投影（阳光照进室内），每间 2 盏暖色吊灯。小岛地面、树及其材质已删除。
  - 交互 Assets/Scripts/Interaction：Interactable（基类，提示文字 + 服务器交互）、Door（服务器切换 IsOpen，门板绕铰链转动，碰撞体随门板）、PlayerInteractor（视线射线找可交互物，E 发给服务器，服务器校验距离）。看着门时 E 不会拾取武器；HUD 显示“E 开门/关门”。
  - 道具 prefab Assets/Prefabs/Props：Workbench、WallWorkbench（带工具挂板）、Shelf、Crate、Lamp（带点光源）。武器改放在房间 A 中间两张工作台上（台面高 0.95）。
  - ServerSpawner 改为通用生成组（prefab + 位置组），现在生成木桩和门；门 prefab Assets/Prefabs/Door.prefab 由服务器生成。
  - 生成器新增 Assets/Editor/PlaygroundBuilder.Rooms.cs。
- 验证（实际执行）：batchmode 编译无错误；生成器成功，4 个网络 prefab 已登记；Windows 构建成功；主机(-combatTest)+客户端自测 ALL PASSED，新增门测试：关门挡住门洞、开门后通畅、玩家站进房间 B (0, 0.04, 14.2)、从 B 侧关门后再次挡住，客户端收到开关同步。原有武器/拾取/PvP 测试仍全部通过。
- 未验证：有画面的室内光照效果与道具外观、真人按 E 开门手感、门关上时夹到玩家的表现（目前不做处理）。

## 木桩与武器（2026-10-28 / Claude / main）

- 用户需求：做假人木桩测试交互和伤害；玩家可以有近战道具或枪械。已确认：武器在地上捡（E 拾取、可丢弃）；玩家有血量，打空倒地后复活；第一版武器为棒球棍、平底锅、手枪（即时命中）、水枪（实体水弹）。
- 已完成：
  - 伤害系统 Assets/Scripts/Combat：Health（服务器扣血、NetworkVariable 同步、受击 Rpc 广播、死亡事件）、DamageInfo/IKnockbackReceiver、DamagePopup（伤害飘字）、TrainingDummy（不死、停手 3 秒回满、受击摇晃、头顶显示 HP/上次伤害/DPS）。推人也改为走 Health（推 0 伤害，冲刺扑倒 5 伤害）。
  - 武器 Assets/Scripts/Weapons：WeaponDefinition（ScriptableObject 参数）、WeaponDatabase（Resources/WeaponDatabase，网络只同步下标）、WeaponPickup（地上武器，丢下的 30 秒后消失）、WeaponSpawnPoint、PlayerWeapons（拾取/丢弃/换弹/攻击，服务器判定）、Projectile（服务器那颗结算，其他端表现）、CombatFx（弹道线、命中碎块）。
  - 玩家：PlayerLife（死亡丢武器，3 秒后满血复活回出生点）、PlayerHud（准星、命中标记、血条、武器子弹、拾取提示、受伤闪红、死亡倒计时）。
  - 场景：训练区在场地 +X 侧（3 个木桩），武器架在出生圈内 z=-2.6（从左到右：棒球棍、平底锅、手枪、水枪），被捡走 8 秒后刷新。ServerSpawner 在服务器启动时生成木桩与武器。
  - 输入：InputSystem_Actions 新增 Drop（G / 手柄十字键下）、Reload（R / 手柄右肩键），Interact 去掉 Hold。
  - 自测：DevTools/CombatSelfTest，主机带 -combatTest 启动时自动跑战斗测试。
- 武器参数（Assets/Data/Weapons/*.asset 可调）：棒球棍 25 伤/0.55s；平底锅 15 伤并扑倒/0.75s；手枪 15 伤/0.25s/8 发/换弹 1.2s；水枪 8 伤/0.15s 连发/24 发/换弹 1.5s。
- 验证（实际执行）：batchmode 编译无错误无警告；生成器成功，3 个网络 prefab 哈希非 0 且已登记；Windows 构建成功；无窗口主机(-combatTest)+1 客户端：四把武器各打木桩 3 次均造成伤害（75/45/45/24），地上拾取成功，手枪 7 发打倒客户端玩家、3 秒后满血复活，客户端收到全部 19 次受击广播与死亡/复活状态，ALL PASSED。
- 未验证：有画面的手感（挥动/后坐动画、弹道、飘字、HUD 中文、木桩文字）、真人键鼠输入下的拾取/丢弃/换弹、两台电脑局域网。
- 已知限制：命中判定在服务器用同步后的位置（局域网延迟小可接受，无延迟补偿）；第一人称武器模型直接挂在相机前，可能穿进墙里；没有音效与角色动画。

## 联机底子（2026-10-28 / Claude / main）

- 用户需求：3D lowpoly 多人联机派对游戏，先搭底子。已确认：局域网联机（一人当主机，其他人填 IP 加入）、第一人称视角、移动+跳跃、冲刺、推/扑倒其他玩家。验收条件未单独给出。
- 已完成：
  - 依赖：Netcode for GameObjects 2.13.3（带入 Unity Transport 2.6.0）。
  - 脚本 Assets/Scripts：Player/PlayerController（第一人称移动/跳跃/冲刺/视角、击退、被扑倒状态）、Player/PlayerPush（左键推人，冲刺中左键扑倒，服务器校验）、Player/PlayerVisuals（按玩家编号上色、隐藏自己身体、倒地表现）、Network/LanSession（创建主机/按 IP 加入、连接审批与出生点、IMGUI 菜单、命令行参数）、Core/InputGate、CameraHome、SpawnPoints。
  - 生成器 Assets/Editor/PlaygroundBuilder.cs：菜单「SCP/重新生成 Playground 场景与玩家 Prefab」生成 Materials、Prefabs/Player.prefab、Scenes/Playground.unity，并把 Playground 设为唯一构建场景；菜单「SCP/构建 Windows 测试包」输出 Builds/Windows/SCP.exe。
  - Assets/DefaultNetworkPrefabs.asset 为 Netcode 自动生成。
- 操作：WASD/左摇杆移动，鼠标/右摇杆视角，空格/A 跳，左 Shift/按下左摇杆冲刺，左键/手柄 X 推人，冲刺中推=扑倒（对方倒地 1.6 秒）；Esc 释放鼠标，点击画面恢复；掉出平台自动回出生点。最多 8 人，端口默认 7777。
- 验证（实际执行）：
  - 6000.3.27f1 batchmode 编译无错误，生成器成功，玩家 prefab GlobalObjectIdHash 非 0。
  - Windows Development 构建成功。
  - 本机 1 主机 + 2 客户端无窗口进程（-batchmode -nographics -host / -client 127.0.0.1 -port 7790 -quitAfter N）：3 个进程均连接成功，各自生成 3 个玩家，位置均在出生点，日志无异常。期间发现并修复：客户端 CharacterController 首次 Move 把玩家拉回原点（生成后先 Physics.SyncTransforms）。
- 未验证：推人/扑倒、跳跃冲刺手感、第一人称相机、IMGUI 菜单中文显示均未在有画面、有真人输入的情况下测试；未在两台真实电脑的局域网中测试（Windows 防火墙首次会弹窗，需允许）；没有自动化测试。
- 已知限制：移动为拥有者权威（客户端可作弊，派对游戏暂可接受）；菜单是临时 IMGUI；没有局域网自动发现、角色动画、音效、玩法规则。
- 未提交：ProjectSettings/UnityConnectSettings.asset 的 m_Enabled 0→1，来源不明（非本任务需要），保留在工作区由用户决定。
- 下一步：用户在编辑器打开 Assets/Scenes/Playground.unity，用 Builds/Windows/SCP.exe 或 Multiplayer Play Mode 开两个实例实测手感；确认首个派对玩法与验收条件。

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
- 2026-10-28 / Claude / main：新增训练木桩、血量/死亡复活、四把可拾取武器与 HUD、自动化战斗自测，详见“木桩与武器”。
- 2026-10-28 / Claude / main：Assets 扁平化（去掉 Assets/Game 一层），删除模板 Readme、TutorialInfo、SampleScene；生成器路径同步更新。验证：batchmode 导入与 Windows 构建成功，主机+客户端 -combatTest ALL PASSED，生成器在新路径运行成功（场景重生成的 ID 变动已丢弃，保留移动后的原场景）。
- 2026-10-28 / Claude / main：场地改为室内房间 A/B，新增可交互的联网门与工作台等道具，详见“房间与门”。
