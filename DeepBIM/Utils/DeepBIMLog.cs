using System;
using System.IO;
using System.Reflection;

namespace DeepBIM.Utils
{
    /// <summary>
    /// Lớp ghi log đơn giản cho plugin Revit DeepBIM.
    /// Lưu log vào: [Đường dẫn dự án]\Logs\log-deepbim.log
    /// </summary>
    public static class DeepBIMLog
    {
        // Thư mục Logs trong dự án
        private static readonly string ProjectDirectory = Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location);

        private static readonly string LogDirectory = Path.Combine(ProjectDirectory, "Logs");

        private static readonly string LogFilePath = Path.Combine(LogDirectory, "log-deepbim.log");

        static DeepBIMLog()
        {
            // Tạo thư mục Logs nếu chưa tồn tại
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }

        /// <summary>
        /// Ghi một thông báo log bình thường
        /// </summary>
        public static void Log(string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // Không làm gì cả – tránh crash plugin
            }
        }

        /// <summary>
        /// Ghi thông báo lỗi (có thể kèm Exception)
        /// </summary>
        public static void LogError(string message, Exception ex = null)
        {
            string fullMessage = $"[ERROR] {message}";
            if (ex != null)
            {
                fullMessage += $" | Exception: {ex.Message}";
            }
            Log(fullMessage);
        }

        /// <summary>
        /// Ghi thông báo thông tin
        /// </summary>
        public static void LogInfo(string message)
        {
            Log($"[INFO] {message}");
        }

        /// <summary>
        /// Ghi log debug (chỉ hoạt động trong chế độ DEBUG)
        /// </summary>
        public static void LogDebug(string message)
        {
#if DEBUG
            Log($"[DEBUG] {message}");
#endif
        }
    }
}