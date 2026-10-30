# 进度与交接

## 当前交接（2026-10-30）

- 最新任务：两房间已用 Kenney 家具重新陈设，旧占位障碍已移除；武器、门、木桩和静止怪物保留并通过联机回归。具体见末尾家具场景记录；玩家跑步实际姿势仍待排查。

### 早期项目交接（2026-10-27）

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

## 角色模型：探险小人（2026-10-28 / Claude / main）

- 用户需求：用 Tripo 生成一个类似 PEAK 风格的 lowpoly 小人（原创设计），并绑骨。用户要求没有指令不许擅自生成；之后授权 AI 在其 Chrome（Claude in Chrome 插件）里直接操作 Tripo 网页版。
- Tripo 账户情况：API 余额 0（`tripo balance`），网页版 Studio 有 200 免费积分（2026-11-01 到期）。免费账号的导出限制：只能导出 H2.5 模型（共 15 次），P2.0/H3.x 模型导出和「导出骨骼」都要订阅。
- 网页版实际操作与扣费（网页积分 200 → 110）：
  - P2.0 智能网格小人（三角面 5000）：生成用免费试用 0 积分，贴图 20，绑骨 20。造型最好，但免费账号导不出来，留在 Tripo 账号里（任务 c7e4f915-39e8-45ec-866f-79dca1da262a）。
  - H2.5 小人（2K 贴图、三角面 5000、T 姿势、提示词要求眼睛无高光）：生成 30，绑骨 20。不带骨骼导出成功；绑骨后再导出，FBX 里没有骨骼与蒙皮（已检查无 LimbNode/Deformer），绑骨的 20 积分没有产出。任务 dd33797e-29fe-45bc-8471-a83251df4643。
  - 剩余：网页积分 110，H2.5 免费导出 13 次，P2.0 免费试用 1 次。
- 已完成：
  - Assets/Models/Explorer：Explorer.fbx（H2.5 不带骨骼，5000 三角面）、Textures/（BaseColor 2K、Normal）、Explorer.mat（URP Lit，金属度 0、光滑度 0.15）、Explorer.prefab（脚底在原点、脸朝 +Z、身高 1.8 米与玩家胶囊一致，宽 1.92 米为 T 姿势两臂）。
  - 编辑器脚本 Assets/Editor/ExplorerModelSetup.cs（菜单 SCP/模型/导入探险小人 Explorer）：设置导入参数、建材质、生成 prefab，并渲染正面/侧面预览图到 Logs/。Tripo 的 FBX 是 Z 轴朝上、趴着的，脚本里绕 X 转 -90° 修正。
- 验证（实际执行）：batchmode 运行导入脚本成功、无编译错误；预览图确认站立、脸朝 +Z、背包在背后、比例正常。没有替换玩家外观、没有改 PlaygroundBuilder、没有重新构建或跑联机自测（本次没有改动运行时代码与场景）。
- 未完成 / 下一步：
  1. 绑骨：模型目前是静态的。可选做法：Mixamo（免费，需用户登录 Adobe）、在 Unity 里用脚本按距离算权重自动绑、安装 Blender 用自动权重绑、给 Tripo API 充值后 `tripo anim rig --spec unity`（约 25 积分）。用户还没决定。
  2. 绑骨后：替换 Player.prefab 外观（需同步改 PlaygroundBuilder，避免重新生成时丢失；保持 CharacterController 不变；PlayerVisuals 目前按玩家编号上色、需改为对贴图材质染色或另行区分玩家），接 Animator（待机/走/跑），然后构建并跑 -combatTest 自测。
  3. T 姿势两臂张开，作为静态外观直接替换玩家会很怪，建议等绑骨后再替换。

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
- 2026-10-28 / Claude / main：用 Tripo 网页版生成探险小人，导入为 Assets/Models/Explorer（静态、未绑骨、未替换玩家外观），详见“角色模型：探险小人”。

## 素材下载（2026-10-30 / Codex）
- 范围：按用户要求下载免费 lowpoly 模型，优先 Kenney Furniture Kit；保存来源与许可，暂不导入或修改场景。
- 状态：进行中。
- 实际结果：官网已核实 Furniture Kit（140 项、CC0），但下载端返回 HTTP 403，未获得有效模型包；未导入资源、未运行 Unity 测试。
- 下一步：通过浏览器手动下载 https://kenney.nl/assets/furniture-kit 后再导入；本次未完成素材下载。

## 本地素材整理（2026-10-30 / Codex / codex/download-model-assets）
- 用户已下载 D:\Download\kenney_furniture-kit.zip；范围为检查并解压待选素材，保留许可，暂不导入 Unity。
- 已完成：解压到 Downloads/ModelAssets/FurnitureKit，共 1546 个文件；含 FBX、OBJ、DAE、GLTF、STL 格式和预览图。License.txt 确认 Furniture Kit 2.0 为 CC0，可商用，无强制署名。
- 原始待选包目录已加入忽略规则，避免重复格式与预览图全部提交；正式选用资源后放入 Assets 独立第三方目录并保留许可和 meta。
- 验证：ZIP 解压成功、检查许可和模型格式；未运行 Unity 导入、材质或场景验证。本次无代码修改。
- 下一步：挑选家具后导入 FBX 与所需贴图，检查 URP 材质、比例及碰撞；不自动替换场景。

## 角色素材下载核验（2026-10-30 / Codex）
- 用户选择：玩家只选 Kenney Animated Characters Protagonists，跳过 Universal Base Characters；已下载敌人包和 Standard 动画库。
- 已核验 D:\Download：玩家包含 4 个 FBX，Ultimate Monsters 含 50 个 FBX 和 50 个 Blend，Universal Animation Library Standard 含 2 个 FBX 和 2 个 GLB。
- 验证：三个 ZIP 的所有非空条目均成功解压读取到空输出流，无读取异常；附带 License.txt 均标注 CC0。怪物包许可文件标题写 Ultimate Platformer Pack，与包名不一致，保留此原文差异。
- 尚未导入 Unity，未验证骨骼、动画片段数、材质或运行效果；本次没有代码或资源修改。下一步按需选择 FBX 导入并验证。

## 角色导入与玩家外观（2026-10-30 / Codex）
- 用户授权：导入已下载素材并开始制作。范围：导入 FBX/必要贴图/许可，生成 URP 模型 prefab，接入 Kenney 玩家外观及基础动画；怪物仅模型 prefab，行为待确认；不重建场景、不改联机和碰撞规则。
- 状态：进行中。
- 已完成：三套角色素材导入 Assets/ThirdParty 并保留 CC0 许可；Assets/Models/Characters 生成 51 个视觉 prefab、URP 材质和待机/跑步控制器，Player.prefab 替换外观，PlayerVisuals 按水平位移切换动画；生成器同步保持接入。
- 修复：正式动画片段排除 __preview__；原包动画比模型少容器层，生成副本映射到 Root 骨架，解决缩放/躺倒/T 姿势。预览确认站立、皮肤、大小与待机姿势。
- 尚未完成：敌人 AI/生成/受伤死亡接入，跳跃/攻击动画，按玩家区分皮肤，Universal 动画重定向；家具本次未导入。敌人具体行为已询问，尚未收到回复。
- 验证：Unity 6000.3.27f1 batchmode Setup 完成退出 0；初版 Windows 构建成功且主机+客户端 CombatSelfTest ALL PASSED；最终动画修正后再次构建中。未实测真人输入与两台电脑局域网；素材预览不等于完整动画手感验证。
- 最终验证：动画重映射后的 Windows 构建 Succeeded（CharacterBuild.log，退出 0）；最终主机与客户端 CharacterHostFinal/CharacterClientFinal 回归 ALL PASSED，未发现运行异常。所有新增资源 .meta 完整；git diff --check 无空白错误。
- 视觉限制：待机、材质、站立比例已确认；静态跑步采样预览尚未呈现明显不同于待机的姿势，不能把跑步实际播放效果标为通过，后续必须在 Play Mode 验证或继续排查曲线。控制器与位移切换逻辑已接入。
- 预览：Logs/CharacterPreview.png；Windows 测试包 Builds/Windows/SCP.exe。日志、构建、原始 ZIP 不提交。已有 UnityConnectSettings 修改保留、不纳入提交。
- 下一步：打开 Playground 创建主机、用第二实例观察玩家移动动画，确认跑步曲线表现后再补跳跃/攻击与皮肤区分；用户确认敌人行为后接怪物逻辑。

## 静止怪物（2026-10-30 / Codex）
- 用户已确认：第一版敌人为会受伤、死亡的静止怪物。范围：复用 Health 和武器命中，制作网络怪物 prefab、房间 B 放置生成点、同步受击/死亡表现，验证主机客户端；不做追逐攻击或自动复活。
- 状态：进行中。
- 已完成：StationaryEnemy.cs 复用 Health 与网络同步，制作 StationaryEnemy.prefab 和 Idle/Hit/Death 控制器；房间 B (3,0.05,19) 服务器生成一只 Big_Cactoro，100 血、受击反应、死亡保留尸体并禁用碰撞。
- 生成：PlaygroundBuilder.Enemies.cs 定点更新场景与生成组，网络 prefab 已登记，主生成器同步接入；未重建整个房间。
- 验证：Unity SetupEnemy/Windows 构建成功退出 0；EnemyHost.log 原有测试与新增六项怪物测试 ALL PASSED；EnemyClient.log 收到 death applied server=False collider=False；EnemyLateClient.log 晚加入看到 health=0 dead=True、碰撞关闭。EnemyDeathPreview.png 已视觉核对死亡姿势正常。git diff --check 通过。
- 未验证：真人手动四把武器打怪及有画面的联机表现、两台电脑局域网；玩家跑步动画的既有待查项仍未解决。
- 下一步：打开 Playground 建主机，穿门进入 B 测试怪物；重开主机会重新生成满血怪物。可在 Health 调 maxHealth，生成器也需同步调整。已有 UnityConnectSettings 修改不提交。
- 状态：静止怪物需求已实现并通过上述自动验证。

## 家具场景重搭（2026-10-30 / Codex）
- 用户要求：用已下载 Kenney 家具模型重新搭建场景；用户已保存关闭 Unity。范围：导入所需家具，重搭 A 休息/装备/训练区与 B 工作间，移除旧占位障碍和道具，保留武器生成点、门、木桩、怪物及联机系统；更新可重复生成脚本并验证。
- 状态：进行中。
- 实际修改：导入 29 种 Kenney 家具与许可，创建 29 个家具 prefab 和共享 URP 材质；A 改为沙发茶几/书柜/双装备桌/训练区，B 改为电脑工位/柜台水槽/冰箱/储物区；更新地板墙面配色与灯具。旧占位箱体/桌架/坡道高台与训练垫已移除，门/武器点/木桩/怪物保留。
- 生成器：PlaygroundBuilder.Furniture.cs 可重复构建陈设，BuildAll 最后同步调用；修改场景通过 Unity Editor API 完成。
- 已验证：Unity RebuildFurniture 成功退出 0，Windows 构建 Succeeded；两个房间渲染图已检查比例/材质/家具摆放，新增资源 .meta 完整。联机回归进行中。
- 未验证：真人有画面走动/家具碰撞手感、两台电脑局域网；玩家跑步动画的旧待查项仍存在。
- 最终验证：FurnitureHost.log 原有与怪物测试 ALL PASSED，FurnitureClient.log 同步怪物死亡/关闭碰撞。未改动网络玩法代码。git diff --check 仅报告 Unity 自动序列化的两处空 m_Name 行尾空白，保留引擎输出；其余无空白错误。
- 完成与下一步：重新打开 Playground 可看到新家具布局，或运行 Builds/Windows/SCP.exe；请真人走一遍休息区、装备台、门口和 B 房测试家具碰撞。场景预览/日志/构建不进 Git，已有 UnityConnectSettings 修改不提交。
