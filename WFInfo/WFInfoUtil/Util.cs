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
            if (jObj == null ||path == null || path.Length == 0)
                return ifNil;

            string key = path[0];
            if (jObj.ContainsKey(key))
            {
                if(path.Length == 1)
                {
                    try
                    {
                        return jObj[key].ToObject<T>();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Something went wrong while getting prop {key}");
                        return ifNil;
                    }
                }
                else
                {
                    try
                    {
                        JObject newJObj = jObj[key].ToObject<JObject>();
                        return newJObj.PathOr<T>(ifNil, path.DropFirst());
                    }
                    catch (Exception ex)
                    {
                        return ifNil;
                    }
                }
            }
            else
            {
                return ifNil;
            }
        }

        public static T[] DropFirst<T>(this T[] arr)
        {
            if (arr == null || arr.Length == 0)
                return Array.Empty<T>();

            T[] newArr = new T[arr.Length - 1];
            for(int i = 0; i < newArr.Length; i++)
            {
                newArr[i] = arr[i+1];
            }

            return newArr;
        }
    }
}