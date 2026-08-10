# OnlyR 系统托盘集成计划

## 1. 目标

在保留 OnlyR 现有录音引擎、设置、输出路径、错误处理和便携版打包方式的前提下，为其增加可靠的 Windows 系统托盘运行能力。

首个版本应支持：

1. 点击最小化按钮时执行普通最小化，主窗口仍保留在任务栏中。
2. 在设置页增加“点击关闭按钮时仅在托盘显示”开关；启用后，点击 X 只隐藏主窗口和任务栏按钮，程序继续运行并保留托盘图标。
3. 托盘图标反映当前录音状态；托盘右键菜单中的命令文字和可用状态随录音状态变化。
4. 通过托盘菜单开始、停止、暂停和继续录音。
5. 双击托盘图标恢复主窗口。
6. 通过托盘菜单打开录音文件夹。
7. 托盘菜单提供“退出”命令，用于明确请求真正退出程序；当 `CloseToTray` 关闭时，点击 X 也按原有逻辑请求关闭。

## 2. 基线

- 上游仓库：`AntonyCorbett/OnlyR`
- 上游默认分支：`master`
- 本计划对应的基线提交：`481fb62cf9d23fca4f81dea2cf54dd46245f8cbe`
- 应用程序：WPF、.NET 10、Windows x86
- 托盘 API：`System.Windows.Forms.NotifyIcon`
- 不引入第三方托盘库

实施分支为 `feature/tray-integration`，由 Fork 仓库的 `master` 分支创建。

## 3. 范围边界

以下代码不在本阶段范围内，不得重新设计或替换：

- `OnlyR.Core/Recorder/AudioRecorder.cs` 及其余录音数据通路
- NAudio/WASAPI 采集、重采样、声道匹配、缓冲、混音和编解码
- 录音文件命名、保存位置选择、磁盘空间检查和错误处理
- 除在现有设置页增加一个开关外，不重写现有 WPF 页面和视觉布局
- 现有便携版和安装版的交付方式

托盘菜单必须执行现有录音命令；不得直接调用 `AudioRecorder`，也不得复制 `RecordingPageViewModel` 中的录音逻辑。

本阶段也不包括全局快捷键、转录、开机自启、通知，以及新建设置页面。

## 4. 必要行为

### 4.1 窗口生命周期

- 正常启动时，主窗口仍正常显示。
- 点击最小化按钮时只执行普通最小化：窗口缩小到任务栏，但任务栏按钮仍保留。最小化不得调用 `Hide()`，不得设置 `ShowInTaskbar = false`。
- 在现有设置页增加布尔选项 `CloseToTray`，界面文字为“点击关闭按钮时仅在托盘显示”，默认启用。
- 本文统一使用“仅在托盘显示”这一种表述，其含义固定为：主窗口隐藏、任务栏按钮消失，但程序继续运行并保留托盘图标。
- `CloseToTray` 启用时，点击窗口关闭按钮 X 后仅在托盘显示；不得调用 `MainViewModel.Closing()`，不得停止或释放正在进行的录音。
- `CloseToTray` 禁用时，点击 X 继续走 OnlyR 原有关闭路径，并遵守 `AllowCloseWhenRecording` 的现有行为。
- 双击托盘图标时，应恢复、激活并聚焦主窗口。
- 托盘中的**退出**先设置明确的退出意图，然后走现有真正关闭路径。
- 真正退出时，必须继续遵守 OnlyR 当前的 `AllowCloseWhenRecording` 行为和清理流程。
- 现有“启动时最小化”选项仍表示普通最小化：启动后窗口保留在任务栏中，不进入“仅在托盘显示”状态。

### 4.2 托盘图标和命令状态

| 录音状态 | 托盘图标 | 开始 | 停止 | 暂停 / 继续 |
| --- | --- | --- | --- | --- |
| 未录音 | 灰色 / 空闲 | 当 `CanRecord` 为真时启用 | 禁用 | 禁用 |
| 录音中 | 红色 / 录音中 | 禁用 | 启用 | 显示“暂停”；当 `CanPause` 为真时启用 |
| 已暂停 | 黄色 / 已暂停 | 禁用 | 启用 | 显示“继续”并启用 |
| 已请求停止 | 红色 / 正在停止 | 禁用 | 禁用 | 禁用 |

当 `RecordingPageViewModel.RecordingStatus` 改变时，图标和提示文本必须同步更新。

可选增强：可以在托盘图标的提示文本，或托盘右键菜单顶部的只读文字中显示“未录音”“录音中”“已暂停”等状态。如果不能稳定实现，不影响本阶段验收。

### 4.3 托盘菜单

本文所称“托盘菜单”，是指右键点击 Windows 通知区域中的 OnlyR 托盘图标后弹出的菜单。

第一版菜单只包含：

- 开始录音
- 停止录音
- 暂停 / 继续
- 显示窗口
- 打开录音文件夹
- 退出

录音相关菜单项分别调用：

- `StartRecordingCommand`
- `StopRecordingCommand`
- `PauseResumeRecordingCommand`
- `ShowRecordingsCommand`

菜单项的可用状态必须遵循现有 ViewModel 状态；托盘不得维护第二套录音状态机。

## 5. 设计与文件改动

### 5.1 新增文件

- `OnlyR/Services/Tray/ITrayIconService.cs`
  - 定义初始化、状态刷新、显示窗口和释放资源等职责。
- `OnlyR/Services/Tray/TrayIconService.cs`
  - 持有 `NotifyIcon` 和 `ContextMenuStrip`。
  - 执行现有 ViewModel 命令。
  - 订阅 ViewModel 属性变化。
  - 将 UI 操作调度到 WPF Dispatcher。
- `OnlyR/Services/Tray/TrayPresentationState.cs`
  - 将 OnlyR 录音属性纯映射为图标和菜单状态，使其无需真实通知区域即可进行单元测试。
- 空闲、录音中、暂停三种状态的托盘图标资源。
- `OnlyR.Tests/TestTrayPresentationState.cs`
  - 覆盖状态到图标和菜单的映射。

### 5.2 需要修改的现有文件

- `OnlyR/App.xaml.cs`
  - 在依赖注入配置完成后注册并初始化托盘服务。
  - 在真正退出程序时释放托盘服务。
- `OnlyR/MainWindow.xaml` 与 `OnlyR/MainWindow.xaml.cs`
  - 保持最小化按钮的普通最小化行为。
  - 根据 `CloseToTray` 判断点击 X 后是“仅在托盘显示”还是走原有关闭路径。
  - 将明确的退出意图与“仅在托盘显示”请求区分开。
  - 恢复已有主窗口，不创建第二个窗口。
- `OnlyR/Services/Options/Options.cs`
  - 新增可持久化的 `CloseToTray` 布尔选项，默认值为 `true`。
- `OnlyR/ViewModel/SettingsPageViewModel.cs` 与 `OnlyR/Pages/SettingsPage.xaml`
  - 在现有设置页加入“点击关闭按钮时仅在托盘显示”开关。
- `OnlyR/ViewModel/MainViewModel.cs`
  - 为托盘服务提供到现有 `RecordingPageViewModel` 命令和状态的窄只读访问入口。
  - 保持页面所有权和现有录音流程不变。
- `OnlyR.Tests/TestMainViewModel.cs` 或新增窗口生命周期测试辅助类
  - 在不依赖 Windows Explorer 的前提下，尽可能覆盖普通最小化、“仅在托盘显示”和真正退出三种行为的区别。
- `OnlyR.Tests/TestOptions.cs` 及设置页相关测试
  - 验证 `CloseToTray` 默认值、保存与重新加载行为，以及设置页绑定。

不计划修改 `OnlyR.Core` 或 `OnlyR/Services/Audio` 下的任何文件。

## 6. 实施顺序

### 阶段 1：建立干净基线

1. Fork 上游仓库。
2. 记录本计划使用的上游提交。
3. 在 Release 配置下构建未修改的工程。
4. 运行未修改工程的测试套件，并记录准确的通过/失败数量。
5. 从 `master` 创建 `feature/tray-integration`。

完成标准：加入托盘代码前，基线构建和测试均通过。

### 阶段 2：加入可测试的托盘状态映射

1. 新增 `TrayPresentationState`。
2. 将 NotRecording、Recording、Paused、StopRequested 映射到图标、提示文本、菜单文本和启用状态。
3. 为每个状态及 `CanRecord` / `CanPause` 边界情况补充单元测试。

完成标准：所有托盘状态决策均可在不构造 `NotifyIcon` 的情况下测试。

### 阶段 3：加入 `NotifyIcon` 并复用现有命令

1. 使用 `System.Windows.Forms.NotifyIcon` 实现 `TrayIconService`。
2. 构造固定的第一版菜单。
3. 将菜单动作绑定到现有 `RelayCommand` 实例。
4. 订阅录音属性变化并刷新托盘显示。
5. 双击托盘图标恢复 WPF 主窗口。

完成标准：托盘命令走与主界面完全相同的录音流程，且不调用 `AudioRecorder`。

### 阶段 4：区分普通最小化、“仅在托盘显示”和真正退出

1. 保持最小化按钮和“启动时最小化”的普通最小化行为。
2. 新增 `CloseToTray` 配置项及设置页开关。
3. `CloseToTray` 启用时，拦截 X 并让主窗口仅在托盘显示。
4. `CloseToTray` 禁用时，让 X 继续走原有关闭路径。
5. 为“托盘 → 退出”增加明确的退出意图，并走现有 `MainViewModel.Closing()` 路径。
6. 保留 `AllowCloseWhenRecording` 启用时异步停止后关闭的行为。

完成标准：最小化始终是普通最小化；点击 X 的行为由 `CloseToTray` 控制；“托盘 → 退出”执行现有清理流程。

### 阶段 5：验证与打包

1. 运行格式化和静态分析。
2. 构建并运行完整自动化测试套件。
3. 执行 Windows 手工音频测试。
4. 构建便携版，并确认托盘图标被正确包含。
5. 合并前提交一份简短测试报告，记录具体测试结果。

完成标准：自动化测试和手工验收均通过，且便携版与开发版本行为一致。

## 7. 验证

### 7.1 自动化命令

```powershell
dotnet build OnlyR.slnx --configuration Release
dotnet test --project OnlyR.Tests/OnlyR.Tests.csproj --configuration Release
```

最终测试报告必须给出准确的执行、通过、失败和跳过数量；只写“全部测试通过”不够。

### 7.2 Windows 手工验收项

1. 正常启动：主窗口和空闲托盘图标都存在。
2. 空闲时最小化：窗口缩小到任务栏，任务栏按钮仍存在，托盘图标也仍存在。
3. 启用 `CloseToTray` 后点击 X：主窗口和任务栏按钮消失，进程及托盘图标保留。
4. 禁用 `CloseToTray` 后点击 X：程序走原有关闭路径。
5. 修改 `CloseToTray` 后重启程序：确认设置值被保存。
6. 从托盘开始麦克风录音：确认文件输出，以及红色/录音中图标。
7. 从托盘开始系统回环录音：确认系统声音被写入输出文件。
8. 从托盘开始麦克风加回环录音：确认输出文件为混音结果。
9. 在每种录音模式下最小化：确认窗口普通最小化且录音持续进行。
10. 启用 `CloseToTray` 后，在每种录音模式下点击 X：确认主窗口仅在托盘显示且录音持续进行。
11. 从托盘暂停和继续：确认图标、菜单文字、计时行为和文件连续性正确。
12. 从托盘停止：确认最终文件移动、命名和空闲状态正确。
13. 从托盘打开录音文件夹。
14. 双击托盘图标：确认恢复并聚焦的是同一个主窗口。
15. 使用“启动时最小化”启动：确认窗口普通最小化到任务栏，且托盘图标可用。
16. 空闲时从托盘退出：确认进程退出，托盘图标消失。
17. 在 `CloseToTray` 和 `AllowCloseWhenRecording` 的不同组合下，于录音时点击 X 或从托盘退出：确认两项设置的职责没有混淆，原有关闭策略被保留。
18. 退出后重新启动：确认不存在残留图标、锁定文件或孤儿进程。

## 8. 完成定义

仅当同时满足以下条件，功能才算完成：

- 所要求的七项托盘行为均在 Windows 便携版中可用；
- 最小化始终是普通最小化，窗口仍保留在任务栏中；
- 点击 X 是否“仅在托盘显示”由 `CloseToTray` 设置控制，且设置值可持久化；
- “托盘 → 退出”始终请求真正关闭；`CloseToTray` 禁用时，点击 X 也走原有真正关闭路径；
- 托盘操作复用现有录音命令；
- 不修改录音引擎或编解码代码；
- 现有完整测试套件和新增托盘测试均通过；
- 具体的自动化和手工测试结果已提交到仓库。
