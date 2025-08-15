using Autodesk.Revit.UI;
using DeepBIM.RibbonConfigs;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace DeepBIM.RibbonConfigs
{
    internal static class RibbonHelpers
    {
        // Đọc JSON từ Resource: Resources/Jsons/DeepBIM.ribbon.json
        public static RibbonConfig LoadConfig(string resourcePath = "Resources/Jsons/DeepBIM.ribbon.json")
        {
            // 1) Thử WPF Resource (Build Action = Resource)
            try
            {
                var asmName = typeof(DeepBIM.Application).Assembly.GetName().Name; // tên assembly thực tế
                var packUri = new Uri($"pack://application:,,,/{asmName};component/{resourcePath}", UriKind.Absolute);

                var sri = System.Windows.Application.GetResourceStream(packUri);
                if (sri?.Stream != null)
                {
                    using var reader = new StreamReader(sri.Stream);
                    var json = reader.ReadToEnd();
                    var cfg = System.Text.Json.JsonSerializer.Deserialize<RibbonConfig>(json, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    });
                    if (cfg != null && !string.IsNullOrWhiteSpace(cfg.Tab)) return cfg;
                }
            }
            catch { /* tiếp tục thử cách khác */ }

            // 2) Thử Embedded Resource (Build Action = Embedded Resource)
            try
            {
                var asm = typeof(DeepBIM.Application).Assembly;
                // manifest name thường là "<assembly>.<folders.dot>DeepBIM.ribbon.json"
                var resName = asm.GetManifestResourceNames()
                                 .FirstOrDefault(n => n.EndsWith(".DeepBIM.ribbon.json", StringComparison.OrdinalIgnoreCase));
                if (resName != null)
                {
                    using var s = asm.GetManifestResourceStream(resName);
                    if (s != null)
                    {
                        using var r = new StreamReader(s);
                        var json = r.ReadToEnd();
                        var cfg = System.Text.Json.JsonSerializer.Deserialize<RibbonConfig>(json, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (cfg != null && !string.IsNullOrWhiteSpace(cfg.Tab)) return cfg;
                    }
                }
            }
            catch { /* tiếp tục thử cách khác */ }

            // 3) Thử đọc file ngoài (nếu bạn để Content + Copy to Output hoặc tự đặt cạnh DLL)
            var asmDir = Path.GetDirectoryName(typeof(DeepBIM.Application).Assembly.Location)!;
            var diskPath = Path.Combine(asmDir, resourcePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(diskPath))
            {
                var json = File.ReadAllText(diskPath);
                var cfg = System.Text.Json.JsonSerializer.Deserialize<RibbonConfig>(json, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (cfg != null && !string.IsNullOrWhiteSpace(cfg.Tab)) return cfg;
            }

            throw new FileNotFoundException($"Cannot locate ribbon JSON via Resource/Embedded/File. Checked path: {resourcePath}");
        }

        // Load hình từ Resource bằng pack URI
        public static BitmapImage LoadImage(string relativeResourcePath)
        {
            if (string.IsNullOrWhiteSpace(relativeResourcePath)) return null;

            var pack = $"pack://application:,,,/DeepBIM;component/{relativeResourcePath}";
            return new BitmapImage(new Uri(pack, UriKind.Absolute));
        }

        public static void SafeSetImages(object item, string small, string large)
        {
            try
            {
                var sm = string.IsNullOrWhiteSpace(small) ? null : LoadImage(small);
                var lg = string.IsNullOrWhiteSpace(large) ? null : LoadImage(large);

                switch (item)
                {
                    case Autodesk.Revit.UI.PushButton pb:
                        if (sm != null) pb.Image = sm;
                        if (lg != null) pb.LargeImage = lg;
                        break;

                    case Autodesk.Revit.UI.PulldownButton pd:
                        if (sm != null) pd.Image = sm;
                        if (lg != null) pd.LargeImage = lg;
                        break;

                    default:
                        break; // các loại khác không hỗ trợ icon
                }
            }
            catch
            {
                // Bỏ qua lỗi icon để không chặn tạo Ribbon
            }
        }

        public static string SanitizeName(string label)
        {
            var baseName = new string((label ?? "").Where(char.IsLetterOrDigit).ToArray());
            return string.IsNullOrEmpty(baseName) ? Guid.NewGuid().ToString("N") : baseName;
        }
    }
}
