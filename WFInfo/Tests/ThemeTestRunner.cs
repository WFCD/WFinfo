using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WFInfo.Settings;
using WFInfo.Services.WindowInfo;

namespace WFInfo.Tests
{
    public static class ThemeTestRunner
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        private static readonly Dictionary<string, WFtheme> ThemeNames = new Dictionary<string, WFtheme>(StringComparer.OrdinalIgnoreCase)
        {
            ["vitruvian"] = WFtheme.VITRUVIAN,
            ["stalker"] = WFtheme.STALKER,
            ["baruuk"] = WFtheme.BARUUK,
            ["corpus"] = WFtheme.CORPUS,
            ["fortuna"] = WFtheme.FORTUNA,
            ["grineer"] = WFtheme.GRINEER,
            ["lotus"] = WFtheme.LOTUS,
            ["nidus"] = WFtheme.NIDUS,
            ["orokin"] = WFtheme.OROKIN,
            ["tenno"] = WFtheme.TENNO,
            ["high_contrast"] = WFtheme.HIGH_CONTRAST,
            ["legacy"] = WFtheme.LEGACY,
            ["equinox"] = WFtheme.EQUINOX,
            ["dark_lotus"] = WFtheme.DARK_LOTUS,
            ["zephyr"] = WFtheme.ZEPHYR,
            ["conquera"] = WFtheme.CONQUERA,
            ["deadlock"] = WFtheme.DEADLOCK,
            ["lunar_renewal"] = WFtheme.LUNAR_RENEWAL,
            ["pom_2"] = WFtheme.POM_2,
        };

        public static int Run(string folderPath, double overrideUiScale = -1)
        {
            var log = new StringBuilder();

            try { TryAttachConsole(); } catch { }

            if (!Directory.Exists(folderPath))
            {
                string msg = $"ERROR: Folder not found: {folderPath}";
                log.AppendLine(msg);
                File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "theme_test_results.txt"), log.ToString());
                return 2;
            }

            log.AppendLine($"Folder: {folderPath}");

            try
            {
                var settings = ApplicationSettings.GlobalSettings;
                settings.Debug = true;

                var windowService = new SettableWindowService();
                OCR.InitThemeTest(ApplicationSettings.GlobalReadonlySettings, windowService);

                if (overrideUiScale > 0)
                    OCR.uiScaling = overrideUiScale;

                var files = Directory.GetFiles(folderPath, "*.png").OrderBy(f => f).ToArray();
                if (files.Length == 0)
                {
                    log.AppendLine("No PNG files found");
                    WriteLog(folderPath, log);
                    return 2;
                }

                log.AppendLine($"Testing {files.Length} file(s)");
                log.AppendLine();

                int passed = 0, failed = 0, skipped = 0;
                var failures = new List<string>();

                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    WFtheme? expected = ParseThemeFromFilename(fileName);
                    if (expected == null)
                    {
                        log.AppendLine($"SKIP: {fileName}{Path.GetExtension(file)} - no theme name found");
                        skipped++;
                        continue;
                    }

                    try
                    {
                        using (var image = new Bitmap(file))
                        {
                            windowService.SetDimensions(image.Width, image.Height);
                            WFtheme detected = OCR.GetThemeWeighted(out double thresh, image);

                            if (detected == expected)
                            {
                                var probe = OCR.GetProbeColors(image);
                                log.AppendLine($"PASS: {fileName}{Path.GetExtension(file)} -> {detected} [{image.Width}x{image.Height}]");
                                log.AppendLine($"     Probe: ({probe.x},{probe.y1}-{probe.y2})  Top RGB({probe.top.R},{probe.top.G},{probe.top.B})  Bot RGB({probe.bot.R},{probe.bot.G},{probe.bot.B})  scale={windowService.ScreenScaling:F2}");
                                passed++;
                            }
                            else
                            {
                                var breakdown = OCR.GetThemeWeightBreakdown(image);
                                var topCands = GetTopCandidates(breakdown, 5);
                                var probe = OCR.GetProbeColors(image);
                                log.AppendLine($"FAIL: {fileName}{Path.GetExtension(file)}");
                                log.AppendLine($"     Expected: {expected}");
                                log.AppendLine($"     Detected: {detected} (weight={thresh:F2})");
                                log.AppendLine($"     Probe: ({probe.x},{probe.y1}-{probe.y2})  Top RGB({probe.top.R},{probe.top.G},{probe.top.B})  Bot RGB({probe.bot.R},{probe.bot.G},{probe.bot.B})  scale={windowService.ScreenScaling:F2}");
                                log.AppendLine($"     Size: {image.Width}x{image.Height}, dpi={windowService.ScreenScaling:F2}");
                                log.AppendLine("     Top-5 candidates:");
                                for (int i = 0; i < topCands.Length; i++)
                                    log.AppendLine($"       {i + 1}. {(WFtheme)topCands[i].Index} weight={topCands[i].Weight:F2}");
                                failures.Add($"{fileName}: expected={expected} detected={detected} weight={thresh:F2} probe=({probe.top.R},{probe.top.G},{probe.top.B})/({probe.bot.R},{probe.bot.G},{probe.bot.B})");
                                failed++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.AppendLine($"ERROR: {fileName}{Path.GetExtension(file)}: {ex.Message}");
                        failed++;
                    }
                }

                log.AppendLine();
                log.AppendLine($"=== Results: {passed} passed, {failed} failed, {skipped} skipped ===");
                if (failures.Count > 0)
                {
                    log.AppendLine("Failures:");
                    foreach (var f in failures)
                        log.AppendLine($"  {f}");
                }

                WriteLog(folderPath, log);

                return failed > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                log.AppendLine($"FATAL: {ex}");
                WriteLog(folderPath, log);
                return 2;
            }
        }

        private static void WriteLog(string folderPath, StringBuilder log)
        {
            string output = log.ToString();

            try
            {
                Console.Out.Flush();
                Console.Error.Flush();
            }
            catch { }

            try
            {
                File.WriteAllText(Path.Combine(folderPath, "theme_test_results.txt"), output);
            }
            catch { }

            try
            {
                File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "theme_test_results.txt"), output);
            }
            catch { }
        }

        private static void TryAttachConsole()
        {
            if (!AttachConsole(0xFFFFFFFF))
                AllocConsole();
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        }

        private static WFtheme? ParseThemeFromFilename(string name)
        {
            foreach (var kvp in ThemeNames.OrderByDescending(k => k.Key.Length))
            {
                if (name.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return kvp.Value;
            }
            return null;
        }

        private static (int Index, double Weight)[] GetTopCandidates(double[] weights, int count)
        {
            if (weights == null || weights.Length == 0)
                return new (int, double)[0];

            var indexed = new (int Index, double Weight)[weights.Length];
            for (int i = 0; i < weights.Length; i++)
                indexed[i] = (i, weights[i]);

            Array.Sort(indexed, (a, b) => b.Weight.CompareTo(a.Weight));
            int resultCount = Math.Min(count, indexed.Length);
            var result = new (int Index, double Weight)[resultCount];
            Array.Copy(indexed, result, resultCount);
            return result;
        }
    }

    internal class SettableWindowService : IWindowInfoService
    {
        public double DpiScaling => 1.0;
        public double ScreenScaling => Math.Max(Window.Width / 1920.0, Window.Height / 1080.0);
        public Rectangle Window { get; private set; }
        public Point Center => new Point(Window.X + Window.Width / 2, Window.Y + Window.Height / 2);
        public Screen Screen => System.Windows.Forms.Screen.PrimaryScreen;

        public void SetDimensions(int width, int height)
        {
            Window = new Rectangle(0, 0, width, height);
        }

        public void UseImage(Bitmap bitmap) => SetDimensions(bitmap.Width, bitmap.Height);

        public void UpdateWindow() { }
    }
}
