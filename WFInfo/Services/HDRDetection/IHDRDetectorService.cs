namespace WFInfo.Services.HDRDetection
{
    /// <summary>
    /// Detects whether the user is using HDR
    /// </summary>
    public interface IHDRDetectorService
    {
        /// <summary>
        /// Whether the user is using HDR in Warframe
        /// </summary>
        bool IsHDR { get; }
    }
}