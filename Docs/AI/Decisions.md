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

## 2026-10-28：战斗与武器方案

- 已确认需求：地上捡武器（同时只拿一把，空手左键=推人）；玩家 100 血，归零倒地 3 秒后复活；第一版 4 把武器（棒球棍、平底锅、手枪、水枪）。
- 伤害由服务器结算（Health 只允许服务器写），木桩、玩家、推人共用 Health；击退通过 IKnockbackReceiver 转给玩家拥有者执行（因为移动是拥有者权威）。
- 命中判定在服务器：近战为头部前方扇形范围，手枪为服务器射线，水弹由服务器模拟一颗不联网的子弹结算伤害，其他端只模拟表现。没有延迟补偿，局域网可接受。
- 武器参数放 ScriptableObject（Assets/Data/Weapons），武器表放 Resources/WeaponDatabase；网络只同步下标，所以调整武器表顺序后联机各方必须用同一版本。
- 武器、木桩、拾取物的 prefab 也由 PlaygroundBuilder 生成；手调参数请改 Weapons/*.asset（生成器会覆盖数值），或者同步改生成器。
- 输入：Interact 去掉模板自带的 Hold 交互（拾取需要按下即触发），新增 Drop、Reload 动作。
- 自动化自测 CombatSelfTest 只在 -combatTest 参数下运行，正式游戏不受影响。

## 2026-10-28：Assets 目录扁平化

- 已确认需求：用户不希望多一层 Assets/Game，并要求删除 Unity 模板自带的无用内容。
- 做法：Assets/Game 下的 Scripts、Editor、Prefabs、Materials、Resources、Scenes 移到 Assets 根目录，武器参数放 Assets/Data/Weapons；删除 Readme.asset、TutorialInfo/、Scenes/SampleScene.unity 及其构建项。移动时保留 .meta，GUID 不变。
- 保留：Settings/ 全部（URP 管线、渲染器、手机画质档、全局设置、后处理配置 SampleSceneProfile 均被引用）、InputSystem_Actions、DefaultNetworkPrefabs。
- 约定：以后导入的第三方插件保持其自带文件夹，不要混进 Scripts/Prefabs 等自有目录。

## 2026-10-28：用 Tripo 生成模型

- 已确认需求：用户用 Tripo 按需生成 lowpoly 模型替换占位外观；没有用户指令不得擅自生成（消耗积分）。用户后来授权 AI 直接在其 Chrome 里操作 Tripo 网页版，不必每步询问。
- 生成的模型放 Assets/Models/<名称>/，每个模型配一个可重复运行的编辑器导入脚本（如 ExplorerModelSetup），负责导入参数、材质、比例与朝向，并输出 prefab；不要在 Inspector 里手调后不写回脚本。
- 朝向：Tripo 网页版导出的 FBX（Blender 预设）在 Unity 里是 Z 轴朝上、趴着的，需绕 X 转 -90°。以后的模型也先渲染预览图确认朝向。
- 尺寸：角色按玩家 CharacterController 高度 1.8 米缩放，脚底对原点。
- 材质：URP Lit，只用 BaseColor + Normal，金属度 0，保持派对游戏的哑光平涂感；Tripo 给的 metallic/roughness 图不使用。
- 限制：Tripo 免费网页账号只能导出 H2.5 模型（15 次），不能导出骨骼。P2.0 低多边形质量更好，但需要订阅或 API 积分才能拿到文件。选择 H2.5 是成本取舍，不是画质取舍。

## 2026-10-28：房间、门与交互

- 已确认需求：房间 A、B 由门连接，玩家交互开门后可去 B；现有木桩等道具留在 A；房间里放工作台一类的物品。
- 场地改为室内，原露天小岛删除（推断：用户要的是房间式场景；如需保留户外区域可在生成器中加回）。
- 屋顶 shadowCastingMode=Off，让平行光照进室内，再加暖色点光源；这是风格化取舍，若改为真实室内光照需要关闭此设置并调整灯光。
- 交互采用通用 Interactable + PlayerInteractor：客户端射线选目标，服务器校验距离后执行；被墙挡住时不可交互。以后的开关、箱子等继承 Interactable 即可。E 优先交给可交互物，其次才是拾取武器。
- 门是服务器生成的网络物体（与木桩一样由 ServerSpawner 生成），不使用场景内放置的 NetworkObject，避免 batchmode 生成场景时的哈希问题。
- 道具是普通（非联网）静态 prefab，放在 Prefabs/Props，可以在编辑器里自由摆放；注意重新运行生成器会重建整个 Playground 场景。

## 2026-10-30：角色素材接入
- 已确认：用户选择 Kenney 玩家包，跳过 Universal Base Characters，并授权导入玩家、怪物、Standard 动画库开始制作。
- 实现取舍：玩家先用 skaterMaleA 皮肤与原包 Idle/Run，保持第一人称相机、碰撞和网络权威；通过同步后的水平位移驱动表现，不增加网络消息。暂不对贴图整身染色，当前玩家外观相同，后续可接四套皮肤。
- Generic 动画源文件省略模型容器，生成 .anim 时将路径加 Root/ 前缀，保留骨架原始轨道，避免覆盖模型容器姿势和比例。原始文件不改。
- 怪物按 Big/Blob/Flying 前缀保存 50 个视觉 prefab，统一 1.8 米作为预览起点；尚无敌人 AI、碰撞、伤害或生成接入，具体行为待用户确认。Universal Animation Library 暂作导入备用，未接重定向。
- 素材与 CC0 原许可放 Assets/ThirdParty，生成器为 CharacterAssetSetup；怪物许可文本标题与包名不一致的原文保留。

## 2026-10-30：静止怪物第一版
- 已确认需求：用户选择会受伤、死亡的静止怪物。
- 实现参数（可后续调整）：先选 Big_Cactoro，100 血，放房间 B (3,0.05,19)，不追逐、不攻击、不自动复活；死亡保留尸体并关闭命中碰撞。
- 技术：复用服务器 Health，受击 Rpc 驱动 HitReact，IsDead 同步驱动死亡 Animator 与碰撞；晚加入客户端按当前死亡状态初始化。不增加 NetworkTransform，不允许客户端结算伤害。
- 生成器定点修改现有场景，追加生成组与网络 prefab，BuildAll 同步调用 InstallEnemy；保留场景其余布局。

## 2026-10-30：用家具模型重搭房间
- 已确认：用户要求用下载的 Kenney 家具模型重新搭建场景。
- 陈设方案（实现选择）：A 房休息区/装备台/训练区，B 房电脑工位/柜台/储物；使用 29 种原包 FBX、保留 CC0，转换为 URP 材质，场景用独立家具 prefab 实例与简单盒碰撞。
- 保留双房间壳体、联网门、武器生成点、木桩和怪物；移除旧 Level 的高台/坡道/方块障碍与旧道具实例、旧训练垫，未更改网络规则。墙与地板为结构几何，家具使用新模型。
- PlaygroundBuilder.Furniture 的 RebuildFurniture 定点重搭陈设并保存；BuildAll 最后调用 FurnishRooms，避免旧生成器恢复占位陈设。原家具源文件与生成 prefab 分别放 ThirdParty/KenneyFurniture、Prefabs/Furniture。
