# Repository Guidelines

本仓库是一个 Windows 托盘应用，用于录制与回放鼠标操作（键盘录制在后续规划中）。请按以下约定保持修改清晰、可维护、易审查。

## Project Structure & Module Organization
- 程序入口：`Program.cs` 启动 `TrayAppContext`。
- UI：`SettingsForm.cs` 为设置窗口，`TrayAppContext.cs` 负责托盘行为。
- 输入链路：
  - `Hotkeys/`：热键解析、捕获控件、注册逻辑。
  - `Native/`：Windows API 的 P/Invoke 封装。
  - `Services/`：录制、回放、设置与存储。
  - `Models/`：数据模型。
- 构建产物：`bin/`、`obj/`。
- 本地数据：`%LOCALAPPDATA%\\RecPlay\\settings.json` 与 `recording.json`。

## Build, Test, and Development Commands
- `dotnet build`：构建 WinForms 应用。
- `dotnet run`：本地启动托盘应用进行手动验证。
- `dotnet test`：若后续加入测试项目，用于运行测试。

### 自包含发布（Publish Self-Contained）
根目录 `build.ps1` 是本仓库的标准打包入口，负责 Release 构建、自包含单文件发布、托盘图标拷贝与 zip 产物生成。

```powershell
.\build.ps1 -Version 1.0.0
```

如果当前环境无法 restore 自包含 runtime pack，可先生成依赖目标机器 .NET Desktop Runtime 的测试包：

```powershell
.\build.ps1 -Version 1.0.0 -FrameworkDependent
```

GitHub Actions 入口为 `.github/workflows/package.yml`：
- `workflow_dispatch`：手动输入版本号、runtime，并选择是否生成 framework-dependent 包。
- `push` `v*` 标签：自动打包并创建或更新 GitHub Release。

发布后 `release\` 目录包含：
- `release\RecPlay\RecPlay.exe` - 自包含可执行文件（包含 .NET 运行时）
- `release\RecPlay\Idle.png`, `Recording.png`, `Replaying.png` - 托盘图标文件
- `release\RecPlay-v<version>-win-x64.zip` - 可直接分发的压缩包

将整个 `release\RecPlay\` 目录或对应 zip 拷贝到目标机器即可直接运行。

## Coding Style & Naming Conventions
- 语言：C#（.NET 8 WinForms），已启用 Nullable 与 Implicit Usings。
- 缩进：4 空格；尽量保持 ASCII 编码。
- 命名：类型/方法 PascalCase，局部变量/字段 camelCase，文件名与类型名一致。
- 修改范围：保持最小变更，避免引入重量级依赖。

## Testing Guidelines
- 当前无自动化测试。
- 如需添加测试，建议路径：`tests/RecPlay.Tests`，框架使用 xUnit。
- 命名：`*Tests.cs`，使用 Arrange-Act-Assert，优先覆盖回放时序和边界场景。

## Commit & Pull Request Guidelines
- 本工作区无 Git 历史，暂无既定提交规范。
- 建议使用 Conventional Commits（如 `feat: add hotkey editor`）或简洁祈使句。
- PR 建议包含：变更摘要、手动验证步骤、UI 变更截图。

## Security & Configuration Tips
- 全局钩子会捕获输入，避免记录敏感信息。
- 如需重置状态，删除 `%LOCALAPPDATA%\\RecPlay` 后重启应用。
