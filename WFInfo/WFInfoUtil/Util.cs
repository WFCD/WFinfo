using System;
using System.Diagnostics;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace WFInfo.WFInfoUtil
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

        public static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public static T PathOr<T>(this JObject jObj, T ifNil, string[] path)
        {
            if (jObj == null || path == null)
                return ifNil;

            JObject tempObj = jObj;
            for (int i = 0; i < path.Length - 1; i++) //iterate over path except for last element
            {
                string key = path[i];
                if (tempObj.ContainsKey(key))
                {
                    try
                    {
                        if (i == path.Length - 1) 
                        {
                            //return if key is last key in path
                            return jObj[key].ToObject<T>();
                        }
                        else 
                        {
                            tempObj = tempObj[key].ToObject<JObject>();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Something went wrong while getting prop {key}");
                        return ifNil;
                    }
                }
                else
                {
                    return ifNil;
                }
            }

            return ifNil;
        }
    }
}