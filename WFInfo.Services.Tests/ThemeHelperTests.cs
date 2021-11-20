using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WFInfo.Services.OCR;
using Xunit;

namespace WFInfo.Services.Tests
{
    public class ThemeHelperTests
    {
        [Fact]
        public async Task TestDarkLotus()
        {
            var filepath = "https://i.imgur.com/9PYYS9c.png";
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
 
            // var bitmap = new Bitmap(@"D:\WFinfo\Images\darklotus_part4_720p_50_50\SSCLEAN-193.png");
            var theme = ThemeHelpers.GetThemeWeighted(out var closestThresh, 1, s => { }, CultureInfo.CurrentCulture, bitmap);
            Assert.Equal(WFtheme.DARK_LOTUS, theme);
        }
    }
}