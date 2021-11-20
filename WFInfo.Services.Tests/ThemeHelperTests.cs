using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using WFInfo.Services.OCR;
using Xunit;

namespace WFInfo.Services.Tests
{
    public class ThemeHelperTests
    {
        [Fact]
        public void TestDarkLotus()
        {
            try
            {
                AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);
            }
            catch(Exception){}
            var bitmap = new Bitmap(@"D:\WFinfo\Images\darklotus_part4_720p_50_50\SSCLEAN-193.png");
            var theme = ThemeHelpers.GetThemeWeighted(out var closestThresh, 1, s => { }, CultureInfo.CurrentCulture, bitmap);
            Assert.Equal(WFtheme.DARK_LOTUS, theme);
        }
    }
}