# RecPlay

RecPlay 是一个 Windows 托盘应用，用于录制与回放鼠标操作。支持全局快捷键、回放次数/间隔/速度配置，适合重复操作的自动化需求。

## 功能
- 鼠标移动/点击/滚轮录制与回放
- 全局热键开始/停止录制与回放
- 回放次数、间隔、速度可配置
- 托盘菜单管理（开始/停止录制、回放、设置、退出）
- 本地持久化设置与录制

## 快速开始
- 运行环境：Windows + .NET 8 SDK
- 构建：`dotnet build`
- 运行：`dotnet run`
- 打包：`.\build.ps1 -Version 1.0.0`

## 打包发布
根目录 `build.ps1` 是本仓库的标准打包入口，会执行 Release 构建、自包含单文件发布，并生成可交付 zip。

```powershell
.\build.ps1 -Version 1.0.0
```

默认输出：
- 应用目录：`release\RecPlay\`
- 可执行文件：`release\RecPlay\RecPlay.exe`
- 压缩包：`release\RecPlay-v1.0.0-win-x64.zip`

可选参数：
- `-Version`：版本号，支持 `1.0.0` 或 `v1.0.0` 输入。
- `-Runtime`：运行时标识，默认 `win-x64`。
- `-Configuration`：构建配置，默认 `Release`。
- `-FrameworkDependent`：生成依赖目标机器 .NET Desktop Runtime 的包；当自包含 runtime pack 暂时无法 restore 时用于本地测试。
- `-NoZip`：只生成发布目录，不生成 zip。

### GitHub Actions
`.github/workflows/package.yml` 可在 GitHub 上触发同一套打包流程：
- 手动触发：Actions -> Package RecPlay -> Run workflow，输入版本号与 runtime。
- 标签触发：推送 `v*` 标签，例如 `v1.0.0`，会生成 zip artifact 并创建或更新 GitHub Release。

## 使用方式
1) 启动后托盘显示 RecPlay 图标。
2) 使用默认热键 `Ctrl+Alt+R` 开始/停止录制。
3) 使用默认热键 `Ctrl+Alt+P` 开始/停止回放。
4) 托盘菜单打开 Settings 调整热键、次数、间隔与速度。

## 数据与存储
- 设置：`%LOCALAPPDATA%\\RecPlay\\settings.json`
- 录制：`%LOCALAPPDATA%\\RecPlay\\recording.json`

## 注意事项
- 当前仅录制鼠标移动、点击与滚轮，不记录键盘输入。
- 录制数据明文存储在本机用户目录，避免在敏感场景开启录制。

## 截图
![RecPlay](RecPlay.png)
