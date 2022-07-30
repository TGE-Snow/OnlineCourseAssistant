using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Microsoft.VisualBasic;

namespace OnlineCourseAssistant
{
    public partial class OnlineCourseAssistant : Form
    {
        public OnlineCourseAssistant()
        {
            InitializeComponent();
            //List<FileInfo> a = getath(@"C:\Users\TGE\Desktop\课程\C#.NET P6黄埔架构班");
            //foreach (FileInfo item in a)
            //{
            //    string ppath = item.DirectoryName.Replace(" ", "");
            //    string pname = item.Name.Replace(" ", "");
            //    Directory.CreateDirectory(ppath);
            //    File.Copy(item.FullName, ppath + "//" + pname);
            //}
        }

        private void StartUp()
        {
            List<FileInfo> a = getath(@"C:\Users\TGE\Desktop\课程\C#.NETP6黄埔架构班");

            int a1 = a.FindIndex(v => v.Name.Contains("课程正式启动"));

            StreamReader sr = new StreamReader(a[a1].FullName);
            string text = sr.ReadToEnd();
            sr.Close();

            List<string> reslist = text.Split('\n').ToList();

            HttpWebRequest request = WebRequest.Create(reslist[0]) as HttpWebRequest;
            request.Method = "GET";
            request.ContentType = "application/json";

            for (int i = 1; i < reslist.Count - 1; i++)
            {
                List<string> headstr = reslist[i].Split(':').ToList();

                string key = headstr[0];
                headstr.RemoveAt(0);
                string value = string.Join(":", headstr);

                switch (key)
                {
                    case "Host":
                        request.Host = value;
                        break;
                    case "Connection":
                        request.KeepAlive = true;
                        break;
                    case "User-Agent":
                        request.UserAgent = value;
                        break;
                    case "Accept":
                        request.Accept = value;
                        break;
                    case "Referer":
                        request.Referer = value;
                        break;
                    default:
                        request.Headers.Set(key, value);
                        break;
                }
            }


            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            Task.Run(() =>
           {
               StartDownM3u8(retString, a[a1].Name.Replace(".txt", ""), a[a1].DirectoryName, reslist[reslist.Count - 1]);
           });

            var b = 12;
        }

        private List<FileInfo> getath(string fpath)
        {
            DirectoryInfo dir = new DirectoryInfo(fpath);
            FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();

            List<FileInfo> filesys = dir.GetFiles().ToList();

            foreach (FileSystemInfo item in fileinfo)
            {
                if (item is DirectoryInfo)            //判断是否文件夹
                {
                    List<FileInfo> npath = getath(item.FullName).ToList();
                    filesys.AddRange(npath);
                }
                else
                {
                    if (filesys.FindIndex(v => v.FullName == item.FullName) == -1)
                    {
                        filesys.Add((FileInfo)item);
                    }
                }

            }
            return filesys;
        }


        private int dowmNum = 10;

        //private string path = @"D:\课程下载";

        private bool showffmpeg = false;

        private List<Task> tasksList = new List<Task>();

        private void btn_dowm_Click(object sender, EventArgs e)
        {
            StartUp();
            //if (proxyServer == null)
            //{
            //    btn_dowm.Text = "关闭监听";
            //    LisentHttp();
            //}
            //else
            //{
            //    btn_dowm.Text = "开启监听";
            //    LesinHttpStop();
            //}
        }

        public Task StartDownM3u8(string body, string name, string path, string dk)
        {
            Task.Run(() =>
            {
                string strname = name;
                if (string.IsNullOrEmpty(name))
                {
                    strname = Interaction.InputBox("监听到课程", "命名课程", "在这里输入", -1, -1);
                }

                if (strname.Length > 0)//如果点击“确定”按钮
                {
                    int datagridindex = dataGridView1.Rows.Count - 1;
                    dataGridView1.Rows.Add();
                    try
                    {
                        JObject bodyJobject = JObject.Parse(body);

                        JToken bodyResult = bodyJobject.GetValue("result");

                        JToken recVideoInfo = bodyResult.Value<JToken>("rec_video_info");

                        //string dk = recVideoInfo.Value<string>("dk");

                        char[] illegalcharacter = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|', ' ' };

                        foreach (var item in illegalcharacter)
                        {
                            strname = strname.Replace(item, '_');
                        }

                        dataGridView1.Rows[datagridindex].Cells[0].Value = strname;

                        string nowpath = path + "/" + strname;

                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"创建/格式化'{strname}'文件夹";
                        if (Directory.Exists(nowpath))
                        {
                            DelectDir(nowpath, false);
                        }
                        else
                        {
                            Directory.CreateDirectory(nowpath);
                        };

                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"'{strname}文件夹创建/格式化完成";

                        JToken tsInfo = recVideoInfo.Value<JArray>("infos").OrderByDescending(v => v.Value<long>("height")).ToArray()[0];
                        string urlHead = GetHeaderUrl(tsInfo.Value<string>("url"));

                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"获取M3U8的URL(选择最高画质)";

                        string m3u8_ts_url = tsInfo.Value<string>("url");
                        List<string> tsStr = HttpPostNew(m3u8_ts_url);
                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"生成key.key文件";
                        tsStr = GetM3u8Key(dk, nowpath, tsStr);
                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"生成ts文件";
                        tsStr = GetM3u8Ts(tsStr, nowpath, datagridindex, urlHead);
                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"生成index.m3u8文件";
                        GetM3u8Index(tsStr, nowpath);
                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"合并生成mp4文件";

                        if (ConvertVideo(path, strname))
                        {
                            dataGridView1.Rows[datagridindex].Cells[1].Value = $"视频合并成功";
                            DelectDir(nowpath, true);
                            //Directory.Delete(nowpath);
                        }
                        else
                        {
                            dataGridView1.Rows[datagridindex].Cells[1].Value = $"视频合并失败";
                        }
                    }
                    catch (Exception ex)
                    {
                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"错误提示:{ex.Message}";
                    }
                }
            });

            return null;
        }

        public void DelectDir(string srcPath, bool droot)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(srcPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)            //判断是否文件夹
                    {
                        DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                        subdir.Delete(true);          //删除子目录和文件
                    }
                    else
                    {
                        File.Delete(i.FullName);      //删除指定文件
                    }
                }

                if (droot)
                {
                    DirectoryInfo subdir = new DirectoryInfo(srcPath);
                    subdir.Delete(true);
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public bool ConvertVideo(string path, string name)
        {
          string mp4path = path + "\\" + name + ".mp4";

            string m3uppath = path + "\\" + name + "\\index.m3u8";

            File.Delete(mp4path);

            string strArg = @"-allowed_extensions ALL -i " + m3uppath + " -c copy " + path + "/" + name + ".mp4";

            string nowDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(nowDir);
            Process p = new Process();//建立外部调用线程
            p.StartInfo.FileName = @"ffmpeg/bin/ffmpeg.exe";//要调用外部程序的绝对路径
            p.StartInfo.Arguments = strArg;
            p.StartInfo.UseShellExecute = false;//不使用操作系统外壳程序启动线程(一定为FALSE,详细的请看MSDN)
            p.StartInfo.RedirectStandardError = true;//把外部程序错误输出写到StandardError流中(这个一定要注意,FFMPEG的所有输出信息,都为错误输出流,用StandardOutput是捕获不到任何消息的...这是我耗费了2个多月得出来的经验...mencoder就是用standardOutput来捕获的)
            p.StartInfo.CreateNoWindow = true;//不创建进程窗口
            p.ErrorDataReceived += new DataReceivedEventHandler(Output);
            p.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            p.Start();//启动线程
            p.BeginErrorReadLine();//开始异步读取
            p.WaitForExit();//阻塞等待进程结束
            p.Close();//关闭进程
            p.Dispose();//释放资源

            if (File.Exists(mp4path))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Output(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data) && showffmpeg)
            {
                richTextBox1.AppendText(output.Data + "\n");
            }
        }

        private void GetM3u8Index(List<string> tsStr, string nowpath)
        {
            string m3u8str = string.Join("\n", tsStr);
            using (StreamWriter sw = new StreamWriter(nowpath + @"\index.m3u8"))
            {
                sw.WriteLine(m3u8str);
            }
        }

        private List<string> GetM3u8Ts(List<string> tsStr, string nowpath, int datagridindex, string urlHead)
        {
            List<string> tslist = new List<string>();

            int ts_Count = 0;

            for (int i = 0; i < tsStr.Count - 1; i++)
            {
                if (tsStr[i].Contains(".ts"))
                {
                    tslist.Add(urlHead + '/' + tsStr[i]);
                    tsStr[i] = ts_Count + ".ts";
                    ts_Count++;
                }
            }

            //tasksList.Add(Task.Factory.StartNew(() => { BeforDownload(tslist, nowpath, datagridindex); }));

            //Task.WaitAll(tasksList.ToArray());

            return tsStr;
        }

        private void BeforDownload(List<string> tsdata, string nowpath, int datagridindex)
        {
            List<Task> listThread = new List<Task>();
            int i = 0;

            while (i < tsdata.Count)
            {
                while (listThread.Count() < dowmNum)
                {
                    if (i < tsdata.Count)
                    {
                        dataGridView1.Rows[datagridindex].Cells[1].Value = i + "/" + tsdata.Count();
                        int noindex = i++;
                        listThread.Add(Task.Factory.StartNew(() => { DownloadFile(tsdata[noindex], nowpath + @"\" + noindex + ".ts"); }));
                    }
                    else
                    {
                        break;
                    }
                }
                Task.WaitAll(listThread.ToArray());
                listThread = new List<Task>();
            }
            Task.WaitAll(listThread.ToArray());
        }

        private void DownloadFile(string URL, string pathname)
        {
            try
            {
                HttpWebRequest Myrq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(URL);
                HttpWebResponse myrp = (System.Net.HttpWebResponse)Myrq.GetResponse();
                Stream st = myrp.GetResponseStream();

                Stream so = new System.IO.FileStream(pathname, System.IO.FileMode.Create);
                byte[] by = new byte[1024];
                int osize = st.Read(by, 0, (int)by.Length);
                while (osize > 0)
                {
                    so.Write(by, 0, osize);
                    osize = st.Read(by, 0, (int)by.Length);
                }
                so.Close();
                st.Close();
                myrp.Close();
                Myrq.Abort();
            }
            catch (System.Exception e)
            {
                DownloadFile(URL, pathname);
            }
            finally
            {
            }
        }

        /// <summary>
        /// 获取m3u8 key
        /// </summary>
        /// <param name="ts_list"></param>
        private List<string> GetM3u8Key(string dk, string nowpath, List<string> tsStr)
        {
            var Bse64Bys = Convert.FromBase64String(dk);
            byte[] byte_key = new byte[16];

            int i = 0;
            foreach (var item in Bse64Bys)
            {
                byte_key[i++] = Convert.ToByte(item);
            }
            string path1 = nowpath + "\\key.key";
            using (FileStream fs = new FileStream(path1, FileMode.Create, FileAccess.Write))
            {
                foreach (var item in byte_key)
                {
                    fs.WriteByte(item);
                }
            }

            int findex = tsStr.FindIndex(v => v.Contains("#EXT-X-KEY"));

            string regexStr = "(?<=URI=\").*(?=\")";

            string key_url = Regex.Matches(tsStr[findex], regexStr)[0].ToString();
            tsStr[findex] = tsStr[findex].Replace(key_url, "key.key");

            findex += 1;
            do
            {

                if (tsStr[findex].Contains("#EXT-X-KEY"))
                {
                    tsStr.RemoveAt(findex);
                }
                else
                {
                    findex++;
                }
            } while (findex < tsStr.Count);


            for (int keyIndex = findex + 1; keyIndex < tsStr.Count; keyIndex++)
            {

            }
            return tsStr;
        }

        /// <summary>
        /// 获取头部URL
        /// </summary>
        /// <param name="btn_down_m3u8url"></param>
        private string GetHeaderUrl(string btn_down_m3u8url)
        {
            List<string> header_list = btn_down_m3u8url.Split('/').ToList();

            header_list.RemoveAt(header_list.Count() - 1);

            return string.Join("/", header_list);
        }

        private List<string> HttpPostNew(string url)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            request.ContentType = "application/json";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString.Split('\n').ToList();
        }

        private void OnlineCourseAssistant_Load(object sender, EventArgs e)
        {
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            //label_dog.Text = path;
            checkBox1.Checked = showffmpeg;
        }

        private ExplicitProxyEndPoint explicitEndPoint;
        private ProxyServer proxyServer;

        private void LisentHttp()
        {
            proxyServer = new ProxyServer();
            proxyServer.CertificateManager.CertificateEngine = Titanium.Web.Proxy.Network.CertificateEngine.DefaultWindows;
            proxyServer.CertificateManager.EnsureRootCertificate();

            proxyServer.BeforeResponse += OnResponse;

            explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000, true)
            {
            };

            proxyServer.AddEndPoint(explicitEndPoint);
            proxyServer.Start();

            var transparentEndPoint = new TransparentProxyEndPoint(IPAddress.Any, 8001, true)
            {
                GenericCertificateName = "google.com"
            };
            proxyServer.AddEndPoint(transparentEndPoint);
            richTextBox1.AppendText("开始监听端口:");
            foreach (var endPoint in proxyServer.ProxyEndPoints)
                richTextBox1.AppendText($"{endPoint.Port} ");
            richTextBox1.AppendText("\n");
            proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
            proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);
        }

        public async Task OnResponse(object sender, SessionEventArgs e)
        {
            if (e.HttpClient.Request.Method == "GET" && e.HttpClient.Request.Url.Contains("ke.qq.com/cgi-proxy/rec_video/describe_rec_video"))
            {
                if (e.HttpClient.Response.StatusCode == 200)
                {
                    if (e.HttpClient.Response.ContentType != null)
                    {
                        byte[] bodyBytes = await e.GetResponseBody();
                        e.SetResponseBody(bodyBytes);
                        string body = await e.GetResponseBodyAsString();
                        e.SetResponseBodyString(body);

                        //await Task.Run(() =>
                        //{
                        //    StartDownM3u8(body);
                        //});
                    }
                }
            }
        }

        private void OnlineCourseAssistant_FormClosing(object sender, FormClosingEventArgs e)
        {
            LesinHttpStop();
        }

        private void LesinHttpStop()
        {
            if (proxyServer != null)
            {
                proxyServer.BeforeResponse -= OnResponse;
                proxyServer.Stop();
                proxyServer = null;
            }
            richTextBox1.AppendText("关闭监听\n");
        }

        private void btn_dog_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            label_dog.Text = folderBrowserDialog1.SelectedPath;
            //path = folderBrowserDialog1.SelectedPath;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            showffmpeg = checkBox1.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (int.TryParse(textBox1.Text, out int dnum))
            {
                dowmNum = dnum;
            }
            else
            {
                textBox1.Text = dowmNum.ToString();
            }
        }
    }
}