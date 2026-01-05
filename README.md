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

## 使用方式
1) 启动后托盘显示 RecPlay 图标。
2) 使用默认热键 `Ctrl+Alt+R` 开始/停止录制。
3) 使用默认热键 `Ctrl+Alt+P` 开始/停止回放。
4) 托盘菜单打开 Settings 调整热键、次数、间隔与速度。

## 数据与存储
- 设置：`%LOCALAPPDATA%\\RecPlay\\settings.json`
- 录制：`%LOCALAPPDATA%\\RecPlay\\recording.json`

## 注意事项
- 当前仅支持鼠标事件，键盘录制计划中。
- 录制为全局鼠标钩子，避免在敏感场景开启。

## 截图
![RecPlay](RecPlay.png)
