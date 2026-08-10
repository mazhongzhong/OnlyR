# OnlyR 系统托盘集成计划

## 1. 目标

在保留 OnlyR 现有录音引擎、设置、输出路径、错误处理和便携版打包方式的前提下，为其增加可靠的 Windows 系统托盘运行能力。

首个版本应支持：

1. 将主窗口最小化到通知区域。
2. 点击窗口关闭按钮时隐藏到托盘。
3. 在托盘图标和菜单中显示当前录音状态。
4. 通过托盘菜单开始、停止、暂停和继续录音。
5. 双击托盘图标恢复主窗口。
6. 通过托盘菜单打开录音文件夹。
7. 仅通过托盘菜单的“退出”命令真正退出程序。

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
- 现有 WPF 页面与视觉布局
- 现有便携版和安装版的交付方式

托盘菜单必须执行现有录音命令；不得直接调用 `AudioRecorder`，也不得复制 `RecordingPageViewModel` 中的录音逻辑。

本阶段也不包括全局快捷键、转录、开机自启、通知，以及新的设置页面。

## 4. 必要行为

### 4.1 窗口生命周期

- 正常启动时，主窗口仍正常显示。
- 最小化时隐藏主窗口，并从任务栏移除。
- 点击窗口关闭按钮时，同样隐藏主窗口并从任务栏移除。
- 隐藏窗口不得调用 `MainViewModel.Closing()`，不得停止或释放正在进行的录音。
- 双击托盘图标时，应恢复、激活并聚焦主窗口。
- 托盘中的**退出**先设置明确的退出意图，然后走现有真正关闭路径。
- 真正退出时，必须继续遵守 OnlyR 当前的 `AllowCloseWhenRecording` 行为和清理流程。
- 现有“启动时最小化”选项应表现为：主窗口隐藏，但托盘图标可见且可用。

### 4.2 托盘状态

| 录音状态 | 托盘状态 | 开始 | 停止 | 暂停 / 继续 |
| --- | --- | --- | --- | --- |
| 未录音 | 空闲 | 当 `CanRecord` 为真时启用 | 禁用 | 禁用 |
| 录音中 | 录音中 | 禁用 | 启用 | 显示“暂停”；当 `CanPause` 为真时启用 |
| 已暂停 | 已暂停 | 禁用 | 启用 | 显示“继续”并启用 |
| 已请求停止 | 录音中 / 正在停止 | 禁用 | 禁用 | 禁用 |

当 `RecordingPageViewModel.RecordingStatus` 改变时，图标和提示文本必须同步更新。

### 4.3 托盘菜单

第一版只包含：

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
  - 处理最小化和隐藏到托盘。
  - 将明确的退出意图与普通窗口隐藏请求区分开。
  - 恢复已有主窗口，不创建第二个窗口。
- `OnlyR/ViewModel/MainViewModel.cs`
  - 为托盘服务提供到现有 `RecordingPageViewModel` 命令和状态的窄只读访问入口。
  - 保持页面所有权和现有录音流程不变。
- `OnlyR.Tests/TestMainViewModel.cs` 或新增窗口生命周期测试辅助类
  - 在不依赖 Windows Explorer 的前提下，尽可能覆盖“隐藏请求”和“真正退出请求”的区别。

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

### 阶段 4：分离“隐藏”和“真正退出”

1. 拦截最小化并隐藏主窗口。
2. 拦截关闭按钮并隐藏主窗口。
3. 为“托盘 → 退出”增加明确的退出意图。
4. 仅让明确退出走现有 `MainViewModel.Closing()` 路径。
5. 保留 `AllowCloseWhenRecording` 启用时异步停止后关闭的行为。
6. 验证现有“启动时最小化”选项。

完成标准：最小化和点击关闭都保持录音继续；“托盘 → 退出”执行现有清理流程。

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
2. 空闲时最小化：窗口和任务栏按钮消失，托盘图标仍存在。
3. 空闲时点击 X：结果与最小化相同，进程仍在运行。
4. 从托盘开始麦克风录音：确认文件输出，以及红色/录音中状态。
5. 从托盘开始系统回环录音：确认系统声音被写入输出文件。
6. 从托盘开始麦克风加回环录音：确认输出文件为混音结果。
7. 在每种录音模式下最小化和点击 X：确认录音持续进行。
8. 从托盘暂停和继续：确认图标、菜单文字、计时行为和文件连续性正确。
9. 从托盘停止：确认最终文件移动、命名和空闲状态正确。
10. 从托盘打开录音文件夹。
11. 双击托盘图标：确认恢复并聚焦的是同一个主窗口。
12. 使用“启动时最小化”启动：确认没有任务栏按钮，且托盘图标可用。
13. 空闲时从托盘退出：确认进程退出，托盘图标消失。
14. 在 `AllowCloseWhenRecording` 两种取值下，于录音时从托盘退出：确认原有策略被保留。
15. 退出后重新启动：确认不存在残留图标、锁定文件或孤儿进程。

## 8. 完成定义

仅当同时满足以下条件，功能才算完成：

- 所要求的七项托盘行为均在 Windows 便携版中可用；
- 最小化和点击 X 从不进入真正关闭路径；
- 只有“托盘 → 退出”请求真正关闭；
- 托盘操作复用现有录音命令；
- 不修改录音引擎或编解码代码；
- 现有完整测试套件和新增托盘测试均通过；
- 具体的自动化和手工测试结果已提交到仓库。
