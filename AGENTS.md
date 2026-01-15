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
构建可独立运行的发布版本（无需目标机器安装 .NET 运行时）：

```powershell
# 构建自包含单文件可执行程序
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish

# 拷贝图标文件到发布目录
Copy-Item -Path Idle.png, Recording.png, Replaying.png -Destination publish\
```

发布后 `publish\` 目录包含：
- `RecPlay.exe` - 自包含可执行文件（约 158MB，包含 .NET 运行时）
- `Idle.png`, `Recording.png`, `Replaying.png` - 托盘图标文件

将整个 `publish\` 目录拷贝到目标机器即可直接运行。

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
