using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DeepBIM.Utils
{
    public static class SystemLoadManager
    {
        /// <summary>
        /// Load Xceed.Wpf.Toolkit.dll từ các vị trí quen thuộc.
        /// Truyền thêm hintDir nếu muốn ưu tiên một thư mục cụ thể (vd: "B:\\C# Tool Revit\\DeepBIM\\DeepBIM\\bin\\Debug R25").
        /// </summary>
        public static void LoadXceedToolkit(string hintDir = null)
        {
            TryLoadAssembly(
                fileName: "Xceed.Wpf.Toolkit.dll",
                hintDirs: BuildProbeDirs(hintDir)
            );
        }

        /// <summary>
        /// Load các assembly MaterialDesign (Themes + Colors).
        /// Truyền thêm hintDir nếu muốn ưu tiên một thư mục cụ thể (vd: "B:\\...\\bin\\Debug R25").
        /// </summary>
        public static void LoadMaterialDesign(string hintDir = null)
        {
            var hintDirs = BuildProbeDirs(hintDir);

            // Load lần lượt
            TryLoadAssembly("MaterialDesignThemes.Wpf.dll", hintDirs);
            TryLoadAssembly("MaterialDesignColors.dll", hintDirs);
        }

        /// <summary>
        /// Load Microsoft.Xaml.Behaviors.dll (nếu bạn dùng behaviors trong XAML).
        /// </summary>
        public static void LoadBehaviors(string hintDir = null)
        {
            TryLoadAssembly(
                fileName: "Microsoft.Xaml.Behaviors.dll",
                hintDirs: BuildProbeDirs(hintDir)
            );
        }

        // ===== Helpers =====

        private static List<string> BuildProbeDirs(string hintDir)
        {
            var dirs = new List<string>();

            // 1) Thư mục cạnh assembly hiện tại
            try
            {
                var asmPath = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(asmPath))
                {
                    var dir = Path.GetDirectoryName(asmPath);
                    if (!string.IsNullOrEmpty(dir)) dirs.Add(dir);
                }
            }
            catch { }

            // 2) CodeBase (nếu Location trống)
            try
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                if (!string.IsNullOrEmpty(codeBase))
                {
                    var local = new Uri(codeBase).LocalPath;
                    var dir = Path.GetDirectoryName(local);
                    if (!string.IsNullOrEmpty(dir)) dirs.Add(dir);
                }
            }
            catch { }

            // 3) AppDomain base
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (!string.IsNullOrEmpty(baseDir)) dirs.Add(baseDir);
            }
            catch { }

            // 4) Thư mục gợi ý (ví dụ: bin\Debug R25)
            if (!string.IsNullOrWhiteSpace(hintDir))
                dirs.Add(hintDir);

            // Loại trùng + rỗng
            var uniq = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in dirs)
            {
                if (string.IsNullOrWhiteSpace(d)) continue;
                if (seen.Add(d)) uniq.Add(d);
            }
            return uniq;
        }

        private static void TryLoadAssembly(string fileName, List<string> hintDirs)
        {
            var tried = new List<string>();

            foreach (var dir in hintDirs)
            {
                var full = Path.Combine(dir, fileName);
                tried.Add(full);

                if (!File.Exists(full)) continue;

                // LoadFrom giúp CLR resolve tiếp các dependency trong cùng thư mục
                Assembly.LoadFrom(full);
                return; // OK
            }

            // Nếu tới đây là không tìm thấy
            var message = $"Could not find '{fileName}'. Searched:\n - " + string.Join("\n - ", tried);
            throw new FileNotFoundException(message);
        }
    }
}
