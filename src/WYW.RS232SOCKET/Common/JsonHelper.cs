using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace WYW.RS232SOCKET
{
    public class JsonHelper
    {
        public static T ReadJson<T>(string fileName)
        {
            var json = File.ReadAllText(fileName, Encoding.UTF8);
            return JsonConvert.DeserializeObject<T>(json);
        }
        public static void SaveJson(object obj,string fileName)
        {
            var json = ObjectToJson(obj);
            var fi = new FileInfo(fileName);
            if (!Directory.Exists(fi.DirectoryName))
            {
                Directory.CreateDirectory(fi.DirectoryName);
            }
            var bytes = Encoding.UTF8.GetBytes(json);
            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(bytes, 0, bytes.Length);
            }
        }
        public static string ObjectToJson(object obj)
        {
            var jSetting = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include }; //包括值为null的属性
            var json = JsonConvert.SerializeObject(obj, jSetting);
            return json;
        }
        public static T JsonToObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

    }
}
