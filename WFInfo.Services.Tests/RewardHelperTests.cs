using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Tesseract;
using WFInfo.Services.OCR;
using Xunit;

namespace WFInfo.Services.Tests
{
    public class RewardHelperTests
    {
        public int LevenshteinDistanceDefault(string s, string t)
        {
            // Levenshtein Distance determines how many character changes it takes to form a known result
            // For example: Nuvo Prime is closer to Nova Prime (2) then Ash Prime (4)
            // For more info see: https://en.wikipedia.org/wiki/Levenshtein_distance
            s = s.ToLower(CultureInfo.CurrentCulture);
            t = t.ToLower(CultureInfo.CurrentCulture);
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
        
            if (n == 0 || m == 0)
                return n + m;
        
            d[0, 0] = 0;
        
            int count = 0;
            for (int i = 1; i <= n; i++)
                d[i, 0] = (s[i - 1] == ' ' ? count : ++count);
        
            count = 0;
            for (int j = 1; j <= m; j++)
                d[0, j] = (t[j - 1] == ' ' ? count : ++count);
        
            for (int i = 1; i <= n; i++)
            for (int j = 1; j <= m; j++)
            {
                // deletion of s
                int opt1 = d[i - 1, j];
                if (s[i - 1] != ' ')
                    opt1++;
        
                // deletion of t
                int opt2 = d[i, j - 1];
                if (t[j - 1] != ' ')
                    opt2++;
        
                // swapping s to t
                int opt3 = d[i - 1, j - 1];
                if (t[j - 1] != s[i - 1])
                    opt3++;
                d[i, j] = Math.Min(Math.Min(opt1, opt2), opt3);
            }
        
        
        
            return d[n, m];
        }
        [Fact]
        public async Task Test()
        {
            var filepath = "https://i.imgur.com/9l6Q0mK.png";
            try
            {
                AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);
            }
            catch(Exception){}
            
            Bitmap bitmap;
            using (var webClient = new System.Net.Http.HttpClient())
            {
                await using (Stream stream = await webClient.GetStreamAsync(filepath))
                {
                    bitmap = new Bitmap(stream);
                }
            }

            Directory.CreateDirectory(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Debug"));
            var parts = RewardHelpers.ExtractPartBoxAutomatically(out var scaling,
                out var theme,
                bitmap,
                CultureInfo.CurrentCulture,
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                CultureInfo.CurrentCulture,
                "",
                (b => { }),
                (s, i) => { },
                s => { },
                bitmap.Height / 1080f,
                bitmap.Width,
                bitmap.Height
            );
            var tempfolder = CreateUniqueTempDirectory();
            Directory.CreateDirectory(Path.Combine(tempfolder, "tessdatas"));
            var dataPath = tempfolder + @"\tessdata";
            // getLocaleTessdata("en", dataPath);
            TesseractEngine CreateEngine() => 
                new TesseractEngine(dataPath, "en")
                {
                    DefaultPageSegMode = PageSegMode.SingleBlock
                };           
            
            var firstChecks = new string[parts.Count];
            
            Task[] tasks = new Task[parts.Count];
            for (int i = 0; i < parts.Count; i++)
            {
                int tempI = i;
                tasks[i] = Task.Factory.StartNew(() => { firstChecks[tempI] = RewardHelpers.GetTextFromImage(parts[tempI], CreateEngine());});
            }
            Task.WaitAll(tasks);
            Assert.True(LevenshteinDistanceDefault(firstChecks[0], "FormaBlueprint") < 3);
            Assert.True(LevenshteinDistanceDefault(firstChecks[1], "MesaPrimeChassisBlueprint") < 3);
            Assert.True(LevenshteinDistanceDefault(firstChecks[2], "IvaraPrimeNeuropticsBlueprint") < 3);
            Assert.True(LevenshteinDistanceDefault(firstChecks[3], "CarrierPrimeCerebrum") < 3);
        }
        private void getLocaleTessdata(string Locale, string AppdataTessdataFolder)
        {
            string traineddata_hotlink_prefix = "https://raw.githubusercontent.com/WFCD/WFinfo/libs/tessdata/";
            JObject traineddata_checksums = new JObject
            {
                {"en", "7af2ad02d11702c7092a5f8dd044d52f"},
                {"ko", "c776744205668b7e76b190cc648765da"}
            };
        
            // get trainned data
            string traineddata_hotlink = traineddata_hotlink_prefix + Locale + ".traineddata";
            string app_data_traineddata_path = AppdataTessdataFolder + @"\" + Locale + ".traineddata";
        
            WebClient webClient = new WebClient();
        
            if (!File.Exists(app_data_traineddata_path))
            {
                try
                {
                    webClient.DownloadFile(traineddata_hotlink, app_data_traineddata_path);
                }
                catch (Exception) { }
            }
        }
        public string CreateUniqueTempDirectory()
        {
            var uniqueTempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            Directory.CreateDirectory(uniqueTempDir);
            return uniqueTempDir;
        }
 
    }
}