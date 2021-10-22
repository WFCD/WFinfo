using System;
using System.Collections.Generic;
using System.Drawing;

namespace WFInfo.Util
{
    public static class Util
    {
        public static Rectangle CombineBounds(Rectangle r1, Rectangle r2)
        {
            int left = Math.Min(r1.Left, r2.Left);
            int top = Math.Min(r1.Top, r2.Top);
            int right = Math.Max(r1.Right, r2.Right);
            int bot = Math.Max(r1.Bottom, r2.Bottom);

            return new Rectangle(left, top, right - left, bot - top);
        }
    }
}