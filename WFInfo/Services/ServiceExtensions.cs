using Microsoft.Extensions.DependencyInjection;
using WFInfo.Services.Screenshot;
using WFInfo.Services.WarframeProcess;
using WFInfo.Services.WindowInfo;

namespace WFInfo.Services
{
    // TODO: Convert classes that use these services into services
    public static class ServiceExtensions
    {
        public static void AddGDIScreenshots(this IServiceCollection services)
        {
            services.AddSingleton<IScreenshotService, GdiScreenshotService>();
        }

        public static void AddWindowsCaptureScreenshots(this IServiceCollection services)
        {
            services.AddSingleton<IScreenshotService, WindowsCaptureScreenshotService>();
        }

        /// <summary>
        /// Registers <see cref="ImageScreenshotService"/> service for providing image data from files.
        /// With <paramref name="primaryProvider"/> <see langword="false"/> this adds a standalone instance that is not tied to <see cref="IScreenshotService"/>.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="primaryProvider">Whether to use this as the primary image source</param>
        public static void AddImageScreenshots(this IServiceCollection services, bool primaryProvider = false)
        {
            if (primaryProvider) services.AddSingleton<IScreenshotService, ImageScreenshotService>();
            else services.AddSingleton<ImageScreenshotService>();
        }

        public static void AddWin32WindowInfo(this IServiceCollection services)
        {
            services.AddSingleton<IWindowInfoService, Win32WindowInfoService>();
        }

        public static void AddProcessFinder(this IServiceCollection services)
        {
            services.AddSingleton<IProcessFinder, WarframeProcessFinder>();
        }

        // TODO: Convert old services
    }
}
