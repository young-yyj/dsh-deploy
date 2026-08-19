# DSH Deploy Manager - WPF版本

## 项目概述

基于WPF重构的DSH部署管理工具，采用MVVM架构，提供现代化的图形用户界面。

## 技术栈

- **框架**：.NET 8.0 WPF
- **架构**：MVVM (Model-View-ViewModel)
- **语言**：C# 12
- **UI框架**：WPF (Windows Presentation Foundation)

## 项目结构

```
dsh-deploy/
├── Models/                    # 数据模型
│   ├── AppConfig.cs          # 应用程序配置
│   ├── LogEntry.cs           # 日志条目
│   ├── PortInfo.cs           # 端口信息
│   ├── ProcessInfo.cs        # 进程信息
│   └── ServiceStatus.cs      # 服务状态
├── Services/                  # 服务层
│   ├── ConfigService.cs      # 配置服务
│   ├── DshService.cs         # DSH核心服务
│   ├── LogService.cs         # 日志服务
│   ├── PortService.cs        # 端口服务
│   └── ProcessService.cs     # 进程服务
├── ViewModels/                # 视图模型
│   ├── AsyncRelayCommand.cs  # 异步命令
│   ├── MainViewModel.cs      # 主窗口ViewModel
│   ├── RelayCommand.cs       # 同步命令
│   └── ViewModelBase.cs      # ViewModel基类
├── Converters/                # 转换器
│   └── StringToVisibilityConverter.cs
├── Resources/                 # 资源文件
│   └── dsh-favicon.ico       # 应用图标
├── MainWindow.xaml            # 主窗口XAML
├── MainWindow.xaml.cs         # 主窗口代码
├── App.xaml                   # 应用程序XAML
└── App.xaml.cs                # 应用程序代码
```

## 核心功能

### 1. 服务管理
- ✅ 启动DSH服务
- ✅ 停止DSH服务
- ✅ 重启DSH服务
- ✅ 状态实时监控

### 2. 端口管理
- ✅ 端口占用检测
- ✅ 端口冲突清理
- ✅ 可用端口查找

### 3. 进程管理
- ✅ DSH进程检测
- ✅ 进程信息获取
- ✅ 进程强制终止

### 4. 配置管理
- ✅ 配置文件读写
- ✅ 配置备份恢复
- ✅ 默认配置管理

### 5. 日志系统
- ✅ 多级别日志
- ✅ 文件日志记录
- ✅ UI日志显示
- ✅ 日志导出

### 6. 用户界面
- ✅ 现代化UI设计
- ✅ 状态实时更新
- ✅ 操作反馈提示
- ✅ 响应式布局

## 架构设计

### MVVM架构

```
View (MainWindow.xaml)
    ↓ Binding
ViewModel (MainViewModel.cs)
    ↓ 调用
Service (DshService.cs)
    ↓ 使用
Model (ServiceStatus, PortInfo, etc.)
```

### 服务层设计

```
DshService (核心服务)
    ├── PortService (端口服务)
    ├── ProcessService (进程服务)
    ├── ConfigService (配置服务)
    └── LogService (日志服务)
```

## 性能优化

### 1. 状态缓存
- 5秒缓存机制，避免频繁WMI查询
- 快速端口检查模式
- 后台定时更新

### 2. 异步操作
- 所有IO操作异步执行
- 不阻塞UI线程
- 响应式用户界面

### 3. 资源管理
- 及时释放进程资源
- 限制日志条目数量
- 优化内存使用

## 使用说明

### 启动应用

```powershell
# 方式1：使用dotnet run
cd dsh-deploy\dsh-deploy
dotnet run

# 方式2：直接运行可执行文件
.\bin\Debug\net8.0-windows\dsh-deploy.exe
```

### 界面操作

1. **启动服务**：点击"启动服务"按钮
2. **停止服务**：点击"停止服务"按钮
3. **重启服务**：点击"重启服务"按钮
4. **打开Web**：点击"打开Web界面"按钮
5. **查看日志**：在日志区域查看实时日志

### 配置文件

配置文件位置：`%USERPROFILE%\.dsh\wpf-config.json`

```json
{
  "webUrl": "http://127.0.0.1:3080",
  "port": 3080,
  "autoStart": true,
  "notifications": true,
  "soundEnabled": true,
  "logLevel": "INFO",
  "statusCheckInterval": 30
}
```

## 代码规范

### 命名规范
- 类名：PascalCase (如 `MainViewModel`)
- 方法名：PascalCase (如 `StartServiceAsync`)
- 私有字段：_camelCase (如 `_dshService`)
- 公共属性：PascalCase (如 `StatusText`)

### 注释规范
- 所有公共API必须有XML文档注释
- 复杂逻辑必须有行内注释
- 异常处理必须记录日志

### 异步规范
- 异步方法以Async结尾
- 使用async/await而非Task.Wait()
- 异常处理使用try-catch

## 扩展性

### 添加新功能

1. **添加新服务**：
   - 在Services文件夹创建新服务类
   - 在DshService中集成新服务
   - 在MainViewModel中暴露功能

2. **添加新UI**：
   - 在MainWindow.xaml添加UI元素
   - 在MainViewModel添加属性和命令
   - 使用绑定连接UI和ViewModel

3. **添加新配置**：
   - 在AppConfig添加新属性
   - 在ConfigService处理新配置
   - 在UI中添加配置界面

## 依赖项

- .NET 8.0 SDK
- Windows 10/11
- WPF运行时

## 构建和发布

```powershell
# 调试版本
dotnet build

# 发布版本
dotnet publish -c Release -r win-x64 --self-contained

# 单文件发布
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## 故障排除

### 问题1：端口被占用
- 使用"清理端口"功能
- 或手动关闭占用端口的程序

### 问题2：服务启动失败
- 检查DSH是否正确安装
- 检查端口是否可用
- 查看日志获取详细错误信息

### 问题3：配置文件损坏
- 删除配置文件，应用会自动创建默认配置
- 或使用备份配置文件恢复

## 版本历史

### v1.0.0 (2026-08-19)
- 初始版本
- 实现核心功能
- WPF现代化界面

## 许可证

MIT License

## 联系方式

- GitHub: https://github.com/young-yyj/dsh-deploy
