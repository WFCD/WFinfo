using System.Collections.Generic;
using WFInfo.Services.HDRDetection.Schemes;

namespace WFInfo.Services.HDRDetection
{
    public class SchemeHDRDetector : IHDRDetectorService
    {
        private readonly List<IHDRDetectionScheme> _schemes = new List<IHDRDetectionScheme>
        {
            new GameSettingsHDRDetectionScheme()
        };

        public bool IsHDR
        {
            get
            {
                // Only return guaranteed results
                foreach (var scheme in _schemes) 
                {
                    var result = scheme.Detect();
                    if (result.IsGuaranteed) return result.IsDetected;
                }

                return false;
            }
        }
    }
}
