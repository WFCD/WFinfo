using System;
using System.Text.RegularExpressions;

namespace WFInfo.WFInfoUtil
{
    public static class StringUtil
    {
        private static Regex MatchIllegalPartChars = new Regex("[^a-z가-힣\\ ]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex MatchMultipleSpaces = new Regex("(\\ )\\1+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex MatchAllButNumbers = new Regex("[^0-9]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string CorrectPartName(string name)
        {
            string correctedName = MatchIllegalPartChars.Replace(name, "");
            correctedName = MatchMultipleSpaces.Replace(correctedName, " ");
            return correctedName.TrimEnd();
        }

        public static int GetPartEqmtLevel(string name, System.Globalization.CultureInfo culture)
        {
            //TODO take regex instad and isolate numbers
            if (name != null && name.Length > 2)
            {
                string substring = name.Substring(name.Length - 2);
                substring = MatchAllButNumbers.Replace(substring, string.Empty);

                if (string.IsNullOrEmpty(substring))
                    return -1;

                try
                {
                    int result = int.Parse(substring, culture);
                    return WFInfoUtil.Util.Clamp(result, 0, 30);
                }
                catch (Exception ex)
                {
                    return -1;
                }
            }

            return -1;

        }
    }
}