using System;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WFInfo.Services.OCR
{
    public static class OcrConstants
    {
        
        /// Pixel measurements for reward screen @ 1920 x 1080 with 100% scale https://docs.google.com/drawings/d/1Qgs7FU2w1qzezMK-G1u9gMTsQZnDKYTEU36UPakNRJQ/edit
        public const int pixleRewardWidth = 968;
        public const int pixleRewardHeight = 235;
        public const int pixleRewardYDisplay = 316;
        public const int pixelRewardLineHeight = 48;
        public const int SCALING_LIMIT = 100;

        /// Colors for the top left "profile bar"
        public static readonly Color[] ThemePrimary = new Color[] {  Color.FromArgb(190, 169, 102),		//VITRUVIAN		
            Color.FromArgb(153,  31,  35), 	    //STALKER		
            Color.FromArgb(238, 193, 105),  	//BARUUK		
            Color.FromArgb( 35, 201, 245),  	//CORPUS		
            Color.FromArgb( 57, 105, 192),  	//FORTUNA		
            Color.FromArgb(255, 189, 102),  	//GRINEER		
            Color.FromArgb( 36, 184, 242),  	//LOTUS			
            Color.FromArgb(140,  38,  92),  	//NIDUS			
            Color.FromArgb( 20,  41,  29),  	//OROKIN		
            Color.FromArgb(  9,  78, 106),  	//TENNO			
            Color.FromArgb(  2, 127, 217),  	//HIGH_CONTRAST	
            Color.FromArgb(255, 255, 255),  	//LEGACY		
            Color.FromArgb(158, 159, 167),  	//EQUINOX		
            Color.FromArgb(140, 119, 147),      //DARK_LOTUS
            Color.FromArgb(253, 132,   2), };   //ZEPHER

        /// highlight colors from selected items
        public static readonly Color[] ThemeSecondary = new Color[] {    Color.FromArgb(245, 227, 173),		//VITRUVIAN		
            Color.FromArgb(255,  61,  51), 	//STALKER		
            Color.FromArgb(236, 211, 162),  	//BARUUK		
            Color.FromArgb(111, 229, 253),  	//CORPUS		
            Color.FromArgb(255, 115, 230),  	//FORTUNA		
            Color.FromArgb(255, 224, 153),  	//GRINEER		
            Color.FromArgb(255, 241, 191),  	//LOTUS			
            Color.FromArgb(245,  73,  93),  	//NIDUS			
            Color.FromArgb(178, 125,   5),  	//OROKIN		
            Color.FromArgb(  6, 106,  74),  	//TENNO			
            Color.FromArgb(255, 255,   0),  	//HIGH_CONTRAST	
            Color.FromArgb(232, 213,  93),  	//LEGACY		
            Color.FromArgb(232, 227, 227),  	//EQUINOX		
            Color.FromArgb(189, 169, 237),      //DARK_LOTUS	
            Color.FromArgb(255,  53,   0) };    //ZEPHER	

        public const NumberStyles styles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent;
        public static readonly Regex RE = new Regex("[^a-z가-힣]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly string applicationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo";
    }
}