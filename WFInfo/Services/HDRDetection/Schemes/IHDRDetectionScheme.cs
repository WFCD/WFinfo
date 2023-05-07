namespace WFInfo.Services.HDRDetection.Schemes
{
    /// <summary>
    /// Result of a HDR detection scheme
    /// </summary>
    public class HDRDetectionSchemeResult
    {
        public HDRDetectionSchemeResult(bool isDetected, bool isGuaranteed)
        {
            IsDetected = isDetected;
            IsGuaranteed = isGuaranteed;
        }

        /// <summary>
        /// Whether the scheme detected a possibility of HDR being enabled
        /// </summary>
        public bool IsDetected { get; private set; }

        /// <summary>
        /// Whether the scheme guarantees that <see cref="IsDetected"/> is the true value. E.g. if a user has disabled HDR in warframe they can still have Auto HDR on.
        /// </summary>
        public bool IsGuaranteed { get; private set; }
    }

    /// <summary>
    /// Determines whether HDR could be enabled from a single source
    /// </summary>
    public interface IHDRDetectionScheme
    {
        HDRDetectionSchemeResult Detect();
    }
}
