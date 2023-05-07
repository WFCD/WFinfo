using System;
using System.IO;

namespace WFInfo.Services.HDRDetection.Schemes
{
    public class GameSettingsHDRDetectionScheme : IHDRDetectionScheme
    {
        private string ConfigurationFile
        {
            get
            {
                var localAppdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localAppdata, "Warframe", "EE.cfg");
            }
        }

        public HDRDetectionSchemeResult Detect()
        {
            if (File.Exists(ConfigurationFile))
            {
                var contents = File.ReadAllText(ConfigurationFile);
                var containsEnable = contents.Contains("Graphics.HDROutput=1");

                if (containsEnable) return new HDRDetectionSchemeResult(containsEnable, true); // 100% HDR
                else return new HDRDetectionSchemeResult(containsEnable, false); // Could still be Auto HDR
            }

            // Could still be Auto HDR with old engine?
            return new HDRDetectionSchemeResult(false, false);
        }
    }
}
