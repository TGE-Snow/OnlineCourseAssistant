using Newtonsoft.Json.Linq;
using OnlineCourseAssistant.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Titanium.Web.Proxy;
using System.Collections.Concurrent;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Http;
using System.Web;

namespace OnlineCourseAssistant
{
    public partial class OnlineCourseAssistant : Form
    {
        public OnlineCourseAssistant()
        {
            InitializeComponent();
        }

        private string header_url = "";

        private string overlayKey = "";

        private string overlayIv = "";

        private string path = @"D:\课程下载";

        private bool showffmpeg = false;

        private List<Task> tasksList = new List<Task>();

        private void btn_dowm_Click(object sender, EventArgs e)
        {
            if (proxyServer == null)
            {
                btn_dowm.Text = "关闭监听";
                LisentHttp();
            }
            else
            {
                btn_dowm.Text = "开启监听";
                LesinHttpStop();
            }
        }

        public Task StartDownM3u8(string btn_down_m3u8url, string name, string body)
        {
            Task.Run(() =>
            {
                MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
                DialogResult dr = MessageBox.Show("监听到课程 '" + name + "',是否下载", "监听提示", messButton, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);

                if (dr == DialogResult.OK)//如果点击“确定”按钮
                {
                    int datagridindex = dataGridView1.Rows.Count - 1;
                    dataGridView1.Rows.Add();
                    try
                    {
                        char[] illegalcharacter = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|', ' ' };

                        foreach (var item in illegalcharacter)
                        {
                            name = name.Replace(item, '_');
                        }

                        dataGridView1.Rows[datagridindex].Cells[0].Value = name;

                        string nowpath = path + "/" + name;

                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"创建/格式化'{name}'文件夹";
                        if (Directory.Exists(nowpath))
                        {
                            DelectDir(nowpath);
                        }
                        else
                        {
                            Directory.CreateDirectory(nowpath);
                        };

                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"'{name}文件夹创建/格式化完成";

                        GetHeaderUrl(btn_down_m3u8url);

                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"获取M3U8的URL(选择最高画质)";

                        string m3u8_ts_url = GetM3u8TsUrl(body);

                        string tsStr = HttpPostNew(m3u8_ts_url);
                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"生成key.key文件";
                        tsStr = GetM3u8Key(tsStr, nowpath);
                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"生成ts文件";
                        tsStr = GetM3u8Ts(tsStr, nowpath, datagridindex);
                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"生成index.m3u8文件";
                        GetM3u8Index(tsStr, nowpath);
                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"合并生成mp4文件";

                        if (ConvertVideo(path, name))
                        {
                            dataGridView1.Rows[datagridindex].Cells[1].Value = $"视频合并成功";
                            DelectDir(nowpath);
                            //Directory.Delete(nowpath);
                        }
                        else
                        {
                            dataGridView1.Rows[datagridindex].Cells[1].Value = $"视频合并失败";
                        }
                    }
                    catch (Exception ex)
                    {
                        dataGridView1.Rows[datagridindex].Cells[1].Value = $"错误提示:{ex}";
                    }
                }
            });

            return null;
        }

        public void DelectDir(string srcPath)
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
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public bool ConvertVideo(string path, string name)
        {
            string mp4path = path + "\\" + name + ".mp4";

            File.Delete(mp4path);

            string strArg = @"-allowed_extensions ALL -i " + path + "/" + name + "/index.m3u8 -c copy " + path + "/" + name + ".mp4";

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

        private void GetM3u8Index(string tsStr, string nowpath)
        {
            using (StreamWriter sw = new StreamWriter(nowpath + @"\index.m3u8"))
            {
                sw.WriteLine(tsStr);
            }
        }

        private string GetM3u8Ts(string tsStr, string nowpath, int datagridindex)
        {
            string[] ts_list = tsStr.Split('\n');
            List<string> tsdata = new List<string>();

            int ts_Count = 0;

            for (int i = 0; i < ts_list.Length - 1; i++)
            {
                if (ts_list[i].Contains("ts"))
                {
                    tsdata.Add(header_url + '/' + ts_list[i]);
                    ts_list[i] = ts_Count + ".ts";
                    ts_Count++;
                }
            }

            tasksList.Add(Task.Factory.StartNew(() => { BeforDownload(tsdata, nowpath, datagridindex); }));

            Task.WaitAll(tasksList.ToArray());

            return string.Join("\n", ts_list);
        }

        private void BeforDownload(List<string> tsdata, string nowpath, int datagridindex)
        {
            List<Task> listThread = new List<Task>();

            for (int i = 0; i < tsdata.Count(); i++)
            {
                listThread.Add(DownloadFile(tsdata[i], i, nowpath));
            }

            int overcount = 0;

            while (overcount != tsdata.Count())
            {
                overcount = listThread.Where(x => x.Status == TaskStatus.RanToCompletion).Count();

                dataGridView1.Rows[datagridindex].Cells[1].Value = overcount + "/" + tsdata.Count();
            }

            Task.WaitAll(listThread.ToArray());
        }

        private Task DownloadFile(string URL, int name, string nowpath)
        {
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    HttpWebRequest Myrq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(URL);
                    HttpWebResponse myrp = (System.Net.HttpWebResponse)Myrq.GetResponse();
                    Stream st = myrp.GetResponseStream();

                    Stream so = new System.IO.FileStream(nowpath + @"\" + name + ".ts", System.IO.FileMode.Create);
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
                    await DownloadFile(URL, name, nowpath);
                }
                finally
                {
                }
            });
        }

        /// <summary>
        /// 获取m3u8 key
        /// </summary>
        /// <param name="ts_list"></param>
        private string GetM3u8Key(string ts_list, string nowpath)
        {
            string regexStr = "(?<=URI=\").*(?=\")";

            string key_url = Regex.Matches(ts_list, regexStr)[0].ToString();

            StringBuilder sb = new StringBuilder(ts_list);
            sb.Replace(key_url, "key.key");

            var byte_key = HttpPostKey(key_url);

            Byte[] keylist = new Byte[16];
            Byte[] ivlist = new Byte[16];

            for (int h = 0; h < 16; h++)
            {
                int aa = 2 * h;

                string f = overlayKey.Substring(aa, 2);
                string g = overlayIv.Substring(2 * h, 2);

                keylist[h] = (byte)Convert.ToInt32(f, 16);
                ivlist[h] = (byte)Convert.ToInt32(g, 16);
            }

            RijndaelManaged rijndaelCipher = new RijndaelManaged();
            rijndaelCipher.Key = keylist;
            rijndaelCipher.IV = ivlist;
            rijndaelCipher.Mode = CipherMode.CBC;
            rijndaelCipher.Padding = PaddingMode.Zeros;

            ICryptoTransform transform = rijndaelCipher.CreateDecryptor();
            byte[] plainText = transform.TransformFinalBlock(byte_key, 0, byte_key.Length);

            string path1 = nowpath + "\\key.key";
            using (FileStream fs = new FileStream(path1, FileMode.Create, FileAccess.Write))
            {
                foreach (var item in plainText)
                {
                    fs.WriteByte(item);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取头部URL
        /// </summary>
        /// <param name="btn_down_m3u8url"></param>
        private void GetHeaderUrl(string btn_down_m3u8url)
        {
            List<string> header_list = btn_down_m3u8url.Split('/').ToList();

            header_list.RemoveAt(header_list.Count() - 1);

            header_url = string.Join("/", header_list);
        }

        /// <summary>
        /// 获取M3U8 ts 的URL
        /// </summary>
        /// <param name="m3u8urllist"></param>
        /// <returns></returns>
        private string GetM3u8TsUrl(string m3u8urllist)
        {
            List<string> list = m3u8urllist.Split('\n').ToList();

            list.Reverse();

            foreach (string item in list)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    GetTokenStr(item);
                    return header_url + '/' + item;
                }
            }
            return null;
        }

        private void GetTokenStr(string token)
        {
            string regexStr = "(?<=~).*(?=~)";

            string str = Regex.Matches(token, regexStr)[0].ToString();

            var base64Str = Base64Decode(str);
            JObject base64Object = JObject.Parse(base64Str);
            overlayKey = base64Object["overlayKey"].ToString();
            overlayIv = base64Object["overlayIv"].ToString();
        }

        private string HttpPostNew(string url)
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

            return retString;
        }

        private byte[] HttpPostKey(string url)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            request.ContentType = "application/json";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            Stream myResponseStream = response.GetResponseStream();

            byte[] btArray = new byte[16];
            myResponseStream.Read(btArray, 0, btArray.Length);

            myResponseStream.Close();

            return btArray;
        }

        /// <summary>
        /// Base64解密
        /// </summary>
        /// <param name="encodeType">解密采用的编码方式，注意和加密时采用的方式一致</param>
        /// <param name="result">待解密的密文</param>
        /// <returns>解密后的字符串</returns>
        public static string Base64Decode(string result)
        {
            Encoding encodeType = Encoding.UTF8;
            string decode = string.Empty;
            result = HttpUtility.UrlDecode(result);
            var bytes = Convert.FromBase64String(result);
            try
            {
                decode = encodeType.GetString(bytes);
            }
            catch
            {
                decode = result;
            }
            return decode;
        }

        private void OnlineCourseAssistant_Load(object sender, EventArgs e)
        {
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            label_dog.Text = path;
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

        private string ClassName = "";

        // Modify response
        public async Task OnResponse(object sender, SessionEventArgs e)
        {
            // read response headers
            var responseHeaders = e.HttpClient.Response.Headers;
            //if (!e.ProxySession.Request.Host.Equals("medeczane.sgk.gov.tr")) return;
            if (e.HttpClient.Request.Method == "GET" && e.HttpClient.Request.Url.Contains("m3u8") && e.HttpClient.Request.Url.Contains("adp"))
            {
                if (e.HttpClient.Response.StatusCode == 200)
                {
                    if (e.HttpClient.Response.ContentType != null)
                    {
                        byte[] bodyBytes = await e.GetResponseBody();
                        e.SetResponseBody(bodyBytes);
                        string body = await e.GetResponseBodyAsString();

                        e.SetResponseBodyString(body);

                        await Task.Run(() =>
                        {
                            StartDownM3u8(e.HttpClient.Request.Url, ClassName, body);
                        });
                    }
                }
            }

            if (e.HttpClient.Request.Method == "POST" && e.HttpClient.Request.Url.Contains("asyn.huke88.com/video/video-play"))
            {
                if (e.HttpClient.Response.StatusCode == 200)
                {
                    if (e.HttpClient.Response.ContentType != null)
                    {
                        byte[] bodyBytes = await e.GetResponseBody();
                        e.SetResponseBody(bodyBytes);
                        string body = await e.GetResponseBodyAsString();

                        e.SetResponseBodyString(body);
                        ClassName = JObject.Parse(body)["catalogHeaderTitle"].ToString();
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
            path = folderBrowserDialog1.SelectedPath;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            showffmpeg = checkBox1.Checked;
        }
    }
}