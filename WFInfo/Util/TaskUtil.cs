using System;
using System.Threading.Tasks;

namespace WFInfo.Util
{
    public static class TaskUtil
    {
        public static T[] MapTasksResult<T>(Task<T>[] tasks)
        {
            if(tasks != null)
            {
                T[] output = new T[tasks.Length];
                for (int i = 0; i < tasks.Length; i++)
                {
                    output[i] = tasks[i].Result;
                }

                return output;
            }

            return Array.Empty<T>();
        }
    }
}