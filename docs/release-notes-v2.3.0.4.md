# OnlyR Tray Edition v2.3.0.4

这是 [AntonyCorbett/OnlyR](https://github.com/AntonyCorbett/OnlyR) 的非官方
Fork 版本，由 [mazhongzhong/OnlyR](https://github.com/mazhongzhong/OnlyR)
构建和发布，不代表原项目维护者的官方发布或认可。

## 本版本新增

- Windows 系统托盘图标。
- 灰色、红色和黄色图标分别表示未录音、录音中和暂停。
- 托盘菜单支持开始、停止、暂停或继续、显示窗口、打开录音文件夹和退出。
- 双击托盘图标恢复主窗口。
- 新增“点击关闭按钮时仅在托盘显示”设置，默认启用并可持久化。
- 最小化保持普通最小化，任务栏按钮仍然存在。
- 托盘退出继续遵守原有录音关闭策略和资源清理流程。

## 下载与校验

- 下载 `OnlyR-portable-win-x86-v2.3.0.4.zip`，解压后运行 `OnlyR.exe`。
- `OnlyR-portable-win-x86-v2.3.0.4.zip.sha256` 用于校验 ZIP 完整性。
- 便携包中包含主项目、Fork 和第三方依赖的许可证及来源说明：
  `LICENSE.txt`、`FORK-NOTICE.txt`、`THIRD-PARTY-NOTICES.txt` 和
  `LAME-COPYING.txt`。
- 便携包同时包含 `lame-3.100-source.tar.gz`，与随程序分发的 LAME 原生库
  对应，方便长期获取、检查和重新构建。

## 验证

- Windows Release 构建：0 个警告，0 个错误。
- 自动化测试：总计 373，执行 373，通过 373，失败 0，跳过 0。
- 已在 Windows 便携版中验证托盘图标、录音命令、窗口恢复、打开目录、退出、
  多麦克风选择和关闭按钮行为。
- 仅系统声音录音、麦克风与系统声音混录和“启动时最小化”未执行手工验收，
  发布者已接受相应剩余风险。

## 已知事项

当前可执行文件没有代码签名。首次运行时 Windows Defender SmartScreen 可能显示
“未知发布者”；这不是程序到期或文件失效。

## 许可证

OnlyR 及本 Fork 依据 MIT License 分发。原作者版权和许可证声明均已保留。
MP3 编码所用的 LAME 3.100 原生库依据 GNU Library General Public License
version 2 或更高版本分发；完整许可文本和对应源码已随便携包提供。
