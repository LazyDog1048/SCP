# SCP

Unity URP 多人派对游戏原型，已有局域网联机、玩家移动与战斗、可交互房间及角色素材。最新状态见 Docs/AI/Progress.md。

## 打开项目

1. 克隆仓库，在 Unity Hub 中选择仓库根目录。
2. 使用 **Unity 6000.3.27f1**，等待依赖恢复与资源导入。
3. 打开 `Assets/Scenes/Playground.unity`，点 Play 后在左上角菜单创建主机或加入。

仓库保存 `Assets/`（包含 `.meta`）、`Packages/` 和 `ProjectSettings/`。缓存由各成员本地生成。

## 与其他成员及 AI 接力

把下列指令发给对方的 AI，即使它不自动读取 AGENTS.md，也可以接手：

> 请先读取根目录 AGENTS.md，以及 Docs/AI 下的 UnityProjectContext.md、Progress.md、Decisions.md，检查 Git 状态和近期提交，根据当前交接记录继续工作。完成后更新交接文档并与代码一起提交。

聊天历史不会自动共享。已确认需求、进度、决定和验证结果通过仓库文档共享；AI 能接续的准确程度取决于记录是否及时更新。

每位成员从最新 `main` 建立自己的功能分支，例如：

```sh
git switch main
git pull --ff-only
git switch -c feature/your-task
# 修改代码，并更新 Docs/AI/Progress.md
git add Assets Packages ProjectSettings Docs AGENTS.md CLAUDE.md README.md
git commit -m "Describe the completed change"
git push -u origin feature/your-task
```

随后创建 Pull Request，审核合并。已有分支先确保工作已提交，再拉取远端变更。双方约定场景/prefab 的编辑负责人，减少资源冲突。推送后对方仍需拉取，才会拿到最新进度。

私有仓库需要所有者先在仓库 Settings → Collaborators 中邀请对方；项目文档本身不会授予访问权限。

当前资源很小，未启用 Git LFS；以后添加大型模型、音频或视频时，在提交大文件前共同配置 LFS。
