

using Newtonsoft.Json.Linq;

namespace OnlineCourseAssistant
{
    static class ClassFunc
    {

        public static string getClassType(string url)
        {

            if (url.Contains("huke88.com/video/video-play"))
            {
                return "huke";
            }
            return null;
        }


        public static ClassFuncGetM3u8 GetM3u8Url(string type, string body)
        {

            ClassFuncGetM3u8 classFuncGetM3U8 = new ClassFuncGetM3u8();


            switch (type)
            {
                case "huke":
                    JObject bodyJobject = JObject.Parse(body);
                    classFuncGetM3U8.url = (string)bodyJobject.GetValue("video_url");
                    classFuncGetM3U8.name = (string)bodyJobject.GetValue("catalogHeaderTitle");
                    classFuncGetM3U8.tsHeadUrl = "https://m3u8.huke88.com";
                    break;
            }

            return classFuncGetM3U8;

            //JObject bodyJobject = JObject.Parse(body);

            //JToken bodyResult = bodyJobject.GetValue("result");

            //JToken recVideoInfo = bodyResult.Value<JToken>("rec_video_info");

            ////string dk = recVideoInfo.Value<string>("dk");

            //char[] illegalcharacter = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|', ' ' };

            //foreach (var item in illegalcharacter)
            //{
            //    strname = strname.Replace(item, '_');
            //}

            //dataGridView1.Rows[datagridindex].Cells[0].Value = strname;

            //string nowpath = path + "/" + strname;

            //dataGridView1.Rows[datagridindex].Cells[1].Value = $"创建/格式化'{strname}'文件夹";
            //if (Directory.Exists(nowpath))
            //{
            //    DelectDir(nowpath, false);
            //}
            //else
            //{
            //    Directory.CreateDirectory(nowpath);
            //};

            //dataGridView1.Rows[datagridindex].Cells[1].Value = $"'{strname}文件夹创建/格式化完成";

            //JToken tsInfo = recVideoInfo.Value<JArray>("infos").OrderByDescending(v => v.Value<long>("height")).ToArray()[0];
            //string urlHead = GetHeaderUrl(tsInfo.Value<string>("url"));

            //dataGridView1.Rows[datagridindex].Cells[1].Value = $"获取M3U8的URL(选择最高画质)";

            //string m3u8_ts_url = tsInfo.Value<string>("url");

        }


    }

    public class ClassFuncGetM3u8
    {
        public string name { get; set; } = "";
        public string url { get; set; } = "";
        public string tsHeadUrl { get; set; } = "";
    }
}
