using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace dsh_deploy.Services
{
    /// <summary>
    /// 安全服务 - 提供输入验证和安全检查
    /// </summary>
    public class SecurityService
    {
        // 允许的命令白名单
        private static readonly HashSet<string> AllowedCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "dsh",
            "node",
            "npm",
            "npx"
        };

        // 允许的参数模式
        private static readonly Regex SafeArgumentPattern = new(@"^[a-zA-Z0-9\-_\.\/\:\@\=\?\&\%\+\ ]+$", RegexOptions.Compiled);

        // 危险字符
        private static readonly char[] DangerousChars = { ';', '|', '&', '$', '`', '(', ')', '{', '}', '<', '>', '\n', '\r' };

        // 允许的端口范围
        private const int MinPort = 1;
        private const int MaxPort = 65535;

        /// <summary>
        /// 验证命令是否安全
        /// </summary>
        /// <param name="command">命令</param>
        /// <returns>是否安全</returns>
        public static bool IsCommandSafe(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return false;

            // 检查是否在白名单中
            var commandName = Path.GetFileNameWithoutExtension(command);
            return AllowedCommands.Contains(commandName);
        }

        /// <summary>
        /// 验证参数是否安全
        /// </summary>
        /// <param name="args">参数</param>
        /// <returns>是否安全</returns>
        public static bool AreArgumentsSafe(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
                return true;

            // 检查危险字符
            if (args.IndexOfAny(DangerousChars) >= 0)
                return false;

            // 检查参数模式
            return SafeArgumentPattern.IsMatch(args);
        }

        /// <summary>
        /// 验证端口是否有效
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>是否有效</returns>
        public static bool IsPortValid(int port)
        {
            return port >= MinPort && port <= MaxPort;
        }

        /// <summary>
        /// 验证URL是否安全
        /// </summary>
        /// <param name="url">URL</param>
        /// <returns>是否安全</returns>
        public static bool IsUrlSafe(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            // 只允许本地地址
            return url.StartsWith("http://127.0.0.1:") ||
                   url.StartsWith("https://127.0.0.1:") ||
                   url.StartsWith("http://localhost:") ||
                   url.StartsWith("https://localhost:");
        }

        /// <summary>
        /// 验证文件路径是否安全（防止路径遍历）
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="allowedDirectory">允许的目录</param>
        /// <returns>是否安全</returns>
        public static bool IsPathSafe(string path, string allowedDirectory)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                // 获取完整路径
                var fullPath = Path.GetFullPath(path);
                var allowedPath = Path.GetFullPath(allowedDirectory);

                // 检查是否在允许的目录下
                return fullPath.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 清理文件名（移除危险字符）
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>清理后的文件名</returns>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "unnamed";

            // 移除路径分隔符和危险字符
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

            // 限制长度
            if (sanitized.Length > 255)
                sanitized = sanitized.Substring(0, 255);

            return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
        }

        /// <summary>
        /// 验证进程ID是否有效
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <returns>是否有效</returns>
        public static bool IsProcessIdValid(int processId)
        {
            return processId > 0 && processId < 100000;
        }

        /// <summary>
        /// 清理日志消息（移除敏感信息）
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>清理后的消息</returns>
        public static string SanitizeLogMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return string.Empty;

            // 移除可能的敏感信息模式
            var patterns = new[]
            {
                @"password\s*[:=]\s*\S+",
                @"token\s*[:=]\s*\S+",
                @"secret\s*[:=]\s*\S+",
                @"api[_-]?key\s*[:=]\s*\S+"
            };

            var sanitized = message;
            foreach (var pattern in patterns)
            {
                sanitized = Regex.Replace(sanitized, pattern, "[REDACTED]", RegexOptions.IgnoreCase);
            }

            return sanitized;
        }
    }
}
