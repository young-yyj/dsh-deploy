using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace dsh_deploy.Services
{
    /// <summary>
    /// 系统托盘服务
    /// </summary>
    public class TrayService : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private readonly LogService _logService;
        private Window? _mainWindow;
        private bool _disposed = false;

        public TrayService(LogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// 初始化系统托盘
        /// </summary>
        /// <param name="mainWindow">主窗口</param>
        public void Initialize(Window mainWindow)
        {
            _mainWindow = mainWindow;

            try
            {
                _notifyIcon = new NotifyIcon
                {
                    Text = "DSH Deploy Manager",
                    Visible = true
                };

                // 设置图标
                SetIcon();

                // 创建右键菜单
                CreateContextMenu();

                // 绑定事件
                _notifyIcon.DoubleClick += OnTrayDoubleClick;
                _notifyIcon.MouseClick += OnTrayMouseClick;

                // 监听窗口状态变化
                _mainWindow.StateChanged += OnMainWindowStateChanged;
                _mainWindow.Closing += OnMainWindowClosing;

                _logService.Info("系统托盘初始化完成");
            }
            catch (Exception ex)
            {
                _logService.Error($"系统托盘初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置图标
        /// </summary>
        private void SetIcon()
        {
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "dsh-favicon.ico");
                
                if (File.Exists(iconPath))
                {
                    _notifyIcon!.Icon = new Icon(iconPath);
                }
                else
                {
                    // 使用应用程序图标
                    _notifyIcon!.Icon = SystemIcons.Application;
                    _logService.Warn($"图标文件不存在: {iconPath}，使用默认图标");
                }
            }
            catch (Exception ex)
            {
                _logService.Warn($"设置图标失败: {ex.Message}，使用默认图标");
                _notifyIcon!.Icon = SystemIcons.Application;
            }
        }

        /// <summary>
        /// 创建右键菜单
        /// </summary>
        private void CreateContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            // 显示主窗口
            var showItem = new ToolStripMenuItem("显示主窗口");
            showItem.Click += (s, e) => ShowMainWindow();
            contextMenu.Items.Add(showItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // 服务状态
            var statusItem = new ToolStripMenuItem("服务状态");
            statusItem.Enabled = false;
            contextMenu.Items.Add(statusItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // 打开Web界面
            var openWebItem = new ToolStripMenuItem("打开Web界面");
            openWebItem.Click += (s, e) => OpenWebInterface();
            contextMenu.Items.Add(openWebItem);

            // 查看日志
            var showLogsItem = new ToolStripMenuItem("查看日志");
            showLogsItem.Click += (s, e) => ShowMainWindow();
            contextMenu.Items.Add(showLogsItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // 退出
            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _notifyIcon!.ContextMenuStrip = contextMenu;
        }

        /// <summary>
        /// 更新托盘提示文本
        /// </summary>
        /// <param name="text">提示文本</param>
        public void UpdateTooltip(string text)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = text.Length > 63 ? text.Substring(0, 60) + "..." : text;
            }
        }

        /// <summary>
        /// 显示气泡通知
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="text">内容</param>
        /// <param name="icon">图标类型</param>
        public void ShowNotification(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, text, icon);
        }

        /// <summary>
        /// 显示主窗口
        /// </summary>
        public void ShowMainWindow()
        {
            if (_mainWindow != null)
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
            }
        }

        /// <summary>
        /// 隐藏主窗口（最小化到托盘）
        /// </summary>
        public void HideMainWindow()
        {
            if (_mainWindow != null)
            {
                _mainWindow.Hide();
                ShowNotification("DSH Deploy Manager", "程序已最小化到系统托盘");
            }
        }

        /// <summary>
        /// 打开Web界面
        /// </summary>
        private void OpenWebInterface()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "http://127.0.0.1:3080",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logService.Error($"打开Web界面失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void ExitApplication()
        {
            _logService.Info("用户请求退出应用程序");
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 托盘双击事件
        /// </summary>
        private void OnTrayDoubleClick(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        /// <summary>
        /// 托盘鼠标点击事件
        /// </summary>
        private void OnTrayMouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowMainWindow();
            }
        }

        /// <summary>
        /// 主窗口状态变化事件
        /// </summary>
        private void OnMainWindowStateChanged(object? sender, EventArgs e)
        {
            if (_mainWindow?.WindowState == WindowState.Minimized)
            {
                _mainWindow.Hide();
            }
        }

        /// <summary>
        /// 主窗口关闭事件
        /// </summary>
        private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 最小化到托盘而不是关闭
            e.Cancel = true;
            _mainWindow?.Hide();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.Visible = false;
                        _notifyIcon.Dispose();
                        _notifyIcon = null;
                    }
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~TrayService()
        {
            Dispose(false);
        }
    }
}
