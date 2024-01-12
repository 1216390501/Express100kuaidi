using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace Express100
{
    class Log
    {
        private static readonly object lockObj = new object();
        public static void WriteAsync(object obj, string filePath = "Vlog", bool isAppend = true)
        {
        }

        public static void Write(object obj, string filePath = "Vlog", bool isAppend = true)
        {
            try
            {
                lock (lockObj)
                {
                    filePath = filePath + "\\ZSJwebapi" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                    filePath = AppDomain.CurrentDomain.BaseDirectory + filePath;
                    if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    }
                    bool flag = File.Exists(filePath);
                    if (!flag)
                    {
                        File.Create(filePath).Close();
                    }
                    using (StreamWriter streamWriter = new StreamWriter(filePath, isAppend))
                    {
                        if (flag && isAppend)
                        {
                            streamWriter.WriteLine();
                        }
                        object arg = (obj is string) ? obj : JsonConvert.SerializeObject(obj);
                        streamWriter.WriteLine($"{DateTime.Now} {arg}");
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
