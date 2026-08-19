using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using dsh_deploy.Models;

namespace dsh_deploy.Services
{
    /// <summary>
    /// pnpm 升级命令执行结果
    /// </summary>
    public class WebUiUpdateResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// 是否被取消
        /// </summary>
        public bool Cancelled { get; init; }

        /// <summary>
        /// 进程退出码
        /// </summary>
        public int ExitCode { get; init; }

        /// <summary>
        /// 结果消息
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// 完整输出
        /// </summary>
        public string Output { get; init; } = string.Empty;
    }

    /// <summary>
    /// dsh-web-ui 升级服务 - 负责插件全家桶的版本检测与 pnpm 升级
    /// </summary>
    public class WebUiUpdateService
    {
        private readonly LogService _logService;
        private readonly ConfigService _configService;

        // registry 域名白名单
        private static readonly string[] AllowedRegistryHosts =
        {
            "registry.npmmirror.com",
            "registry.npmjs.org"
        };

        // 包名校验（支持 @scope/name 与 name 两种形式）
        private static readonly Regex PackageNamePattern = new(
            @"^(@[a-z0-9][a-z0-9._-]*/)?[a-z0-9][a-z0-9._-]*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public WebUiUpdateService(LogService logService, ConfigService configService)
        {
            _logService = logService;
            _configService = configService;
        }

        /// <summary>
        /// 升级配置（从应用配置读取）
        /// </summary>
        public WebUiUpdateConfig Config => _configService.Current.WebUiUpdate;

        /// <summary>
        /// dsh-web-ui 安装路径（%USERPROFILE%\.dsh\profiles\web）
        /// </summary>
        public string ProfilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dsh", "profiles", "web");

        /// <summary>
        /// 检查 dsh-web-ui 是否已安装
        /// </summary>
        public Task<bool> IsInstalledAsync() => Task.Run(() =>
            Directory.Exists(ProfilePath) && File.Exists(GetPackageJsonPath()));

        /// <summary>
        /// 读取当前安装版本
        /// </summary>
        public async Task<string?> GetInstalledVersionAsync()
        {
            try
            {
                var packageJsonPath = GetPackageJsonPath();
                if (!File.Exists(packageJsonPath))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(packageJsonPath, Encoding.UTF8);
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty("version", out var version)
                    ? version.GetString()
                    : null;
            }
            catch (Exception ex)
            {
                _logService.Warn($"读取 dsh-web-ui 版本失败: {SecurityService.SanitizeLogMessage(ex.Message)}");
                return null;
            }
        }

        /// <summary>
        /// 获取 npm registry 上的最新版本（复用 npm CLI，与升级命令走同一网络栈）
        /// </summary>
        public async Task<string?> GetLatestVersionAsync()
        {
            if (!IsRegistryUrlSafe(Config.RegistryUrl) || !IsPackageNameSafe(Config.PackageName))
            {
                _logService.Warn("获取最新版本失败: 包名或镜像地址未通过安全校验");
                return null;
            }

            var npmCommand = ProcessService.ResolveCommandPath("npm");
            if (!SecurityService.IsCommandSafe(npmCommand))
            {
                _logService.Warn("获取最新版本失败: 不安全的命令");
                return null;
            }

            var arguments = $"view {Config.PackageName} version --registry {Config.RegistryUrl}";
            if (!SecurityService.AreArgumentsSafe(arguments))
            {
                _logService.Warn("获取最新版本失败: 参数未通过安全校验");
                return null;
            }

            try
            {
                var result = await RunProcessAsync(
                    npmCommand, arguments,
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    TimeSpan.FromSeconds(30), null, CancellationToken.None);

                if (result.ExitCode != 0)
                {
                    var detail = string.IsNullOrWhiteSpace(result.Output) ? result.Message : result.Output;
                    _logService.Warn($"获取 dsh-web-ui 最新版本失败: npm 命令执行失败: {SecurityService.SanitizeLogMessage(detail.Trim())}");
                    return null;
                }

                var version = result.Output.Trim();
                return string.IsNullOrWhiteSpace(version) ? null : version;
            }
            catch (Exception ex)
            {
                _logService.Warn($"获取 dsh-web-ui 最新版本失败: {SecurityService.SanitizeLogMessage(ex.Message)}");
                return null;
            }
        }

        /// <summary>
        /// 检查 pnpm 是否可用
        /// </summary>
        public async Task<bool> IsPnpmAvailableAsync()
        {
            // Windows 下 npm/pnpm 是 .cmd 垫片：.NET 以裸名启动时会破坏其内部 %~dp0 解析，
            // 必须先解析出完整路径再启动
            var pnpmCommand = ProcessService.ResolveCommandPath("pnpm");
            if (!SecurityService.IsCommandSafe(pnpmCommand))
            {
                return false;
            }

            try
            {
                var result = await RunProcessAsync(
                    pnpmCommand, "--version", ProfilePath,
                    TimeSpan.FromSeconds(15), null, CancellationToken.None);
                return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 执行 dsh-web-ui 升级（pnpm update --latest，使用镜像源）
        /// </summary>
        /// <param name="progress">实时输出回调</param>
        /// <param name="cancellationToken">取消令牌（触发时终止进程树）</param>
        public async Task<WebUiUpdateResult> UpdateAsync(
            IProgress<string>? progress,
            CancellationToken cancellationToken)
        {
            var pnpmCommand = ProcessService.ResolveCommandPath("pnpm");
            if (!SecurityService.IsCommandSafe(pnpmCommand))
            {
                return new WebUiUpdateResult { Success = false, Message = "不安全的命令: pnpm" };
            }

            if (!IsPackageNameSafe(Config.PackageName))
            {
                return new WebUiUpdateResult { Success = false, Message = $"不安全的包名: {Config.PackageName}" };
            }

            if (!IsRegistryUrlSafe(Config.RegistryUrl))
            {
                return new WebUiUpdateResult { Success = false, Message = $"不安全的镜像地址: {Config.RegistryUrl}" };
            }

            if (!Directory.Exists(ProfilePath))
            {
                return new WebUiUpdateResult
                {
                    Success = false,
                    Message = $"dsh-web-ui 未安装，路径不存在: {ProfilePath}"
                };
            }

            var arguments = $"update {Config.PackageName} --latest --registry {Config.RegistryUrl}";
            if (!SecurityService.AreArgumentsSafe(arguments))
            {
                return new WebUiUpdateResult { Success = false, Message = "升级参数未通过安全校验" };
            }

            _logService.Info($"开始升级 dsh-web-ui: pnpm {arguments}");

            var output = new StringBuilder();
            void OnOutput(string line)
            {
                lock (output)
                {
                    output.AppendLine(line);
                }
                progress?.Report(line);
            }

            try
            {
                var result = await RunProcessAsync(
                    pnpmCommand, arguments, ProfilePath,
                    TimeSpan.FromMinutes(10), OnOutput, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return new WebUiUpdateResult
                    {
                        Success = false,
                        Cancelled = true,
                        Message = "升级已取消",
                        Output = output.ToString()
                    };
                }

                if (result.ExitCode == 0)
                {
                    _logService.Info("dsh-web-ui 升级成功");
                    return new WebUiUpdateResult
                    {
                        Success = true,
                        ExitCode = 0,
                        Message = "升级成功",
                        Output = output.ToString()
                    };
                }

                var message = string.IsNullOrWhiteSpace(result.Output) ? result.Message : result.Output;
                _logService.Error($"dsh-web-ui 升级失败: {SecurityService.SanitizeLogMessage(message)}");
                return new WebUiUpdateResult
                {
                    Success = false,
                    ExitCode = result.ExitCode,
                    Message = message,
                    Output = output.ToString()
                };
            }
            catch (OperationCanceledException)
            {
                return new WebUiUpdateResult
                {
                    Success = false,
                    Cancelled = true,
                    Message = "升级已取消",
                    Output = output.ToString()
                };
            }
            catch (Exception ex)
            {
                _logService.Error($"dsh-web-ui 升级失败: {SecurityService.SanitizeLogMessage(ex.Message)}");
                return new WebUiUpdateResult
                {
                    Success = false,
                    Message = ex.Message,
                    Output = output.ToString()
                };
            }
        }

        /// <summary>
        /// 校验 registry URL（https + 域名白名单）
        /// </summary>
        private static bool IsRegistryUrlSafe(string url)
        {
            if (string.IsNullOrWhiteSpace(url) ||
                !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            foreach (var host in AllowedRegistryHosts)
            {
                if (uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 校验包名格式
        /// </summary>
        private static bool IsPackageNameSafe(string packageName)
        {
            return !string.IsNullOrWhiteSpace(packageName) && PackageNamePattern.IsMatch(packageName);
        }

        /// <summary>
        /// 获取安装包的 package.json 路径
        /// </summary>
        private string GetPackageJsonPath()
        {
            // @linxin666/dsh-web-ui-all → node_modules\@linxin666\dsh-web-ui-all\package.json
            var relativePath = Config.PackageName.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(ProfilePath, "node_modules", relativePath, "package.json");
        }

        /// <summary>
        /// 运行进程并逐行读取输出
        /// </summary>
        private async Task<WebUiUpdateResult> RunProcessAsync(
            string fileName,
            string arguments,
            string workingDirectory,
            TimeSpan timeout,
            Action<string>? onOutput,
            CancellationToken cancellationToken)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                },
                // Exited 事件仅在 EnableRaisingEvents 为 true 时触发
                EnableRaisingEvents = true
            };

            var exitTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var output = new StringBuilder();

            void HandleLine(string line)
            {
                lock (output)
                {
                    output.AppendLine(line);
                }
                onOutput?.Invoke(line);
            }

            process.Exited += (_, _) =>
            {
                try
                {
                    exitTcs.TrySetResult(process.ExitCode);
                }
                catch
                {
                    exitTcs.TrySetResult(-1);
                }
            };

            if (!process.Start())
            {
                return new WebUiUpdateResult { Success = false, Message = "无法启动 pnpm 进程" };
            }

            // 取消时终止整个进程树
            using var cancellationRegistration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // 进程已退出，忽略
                }
            });

            var stdoutTask = ReadLinesAsync(process.StandardOutput, HandleLine);
            var stderrTask = ReadLinesAsync(process.StandardError, HandleLine);

            var completedTask = await Task.WhenAny(exitTcs.Task, Task.Delay(timeout, CancellationToken.None));

            if (completedTask != exitTcs.Task)
            {
                // 超时，终止进程树
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // 进程已退出，忽略
                }

                return new WebUiUpdateResult
                {
                    Success = false,
                    Message = $"命令执行超时（{(int)timeout.TotalSeconds}秒）",
                    Output = output.ToString()
                };
            }

            var exitCode = await exitTcs.Task;
            await Task.WhenAll(stdoutTask, stderrTask);

            return new WebUiUpdateResult
            {
                Success = exitCode == 0,
                ExitCode = exitCode,
                Output = output.ToString()
            };
        }

        /// <summary>
        /// 逐行读取输出流
        /// </summary>
        private static async Task ReadLinesAsync(StreamReader reader, Action<string> onLine)
        {
            try
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    onLine(line);
                }
            }
            catch
            {
                // 进程被终止时读取流会抛异常，忽略
            }
        }
    }
}
