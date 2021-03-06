﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;
using static Jvedio.GlobalVariable;

namespace Jvedio
{
    public static class FileProcess
    {
        /// <summary>
        /// 判断拖入的是文件夹还是文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsFile(string path)
        {
            try
            {
                FileAttributes attr = File.GetAttributes(path);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    return false;
                else
                    return true;
            }
            catch
            {
                return true;
            }

        }


        public static Movie GetInfoFromNfo(string path)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(path);
            }
            catch { return null; }
            XmlNode rootNode = doc.SelectSingleNode("movie");
            if (rootNode == null) return null;
            Movie movie = new Movie();
            foreach (XmlNode node in rootNode.ChildNodes)
            {
                try
                {
                    switch (node.Name)
                    {
                        case "id": movie.id = node.InnerText.ToUpper(); break;
                        case "num": movie.id = node.InnerText.ToUpper(); break;
                        case "title": movie.title = node.InnerText; break;
                        case "release": movie.releasedate = node.InnerText; break;
                        case "releasedate": movie.releasedate = node.InnerText; break;
                        case "director": movie.director = node.InnerText; break;
                        case "studio": movie.studio = node.InnerText; break;
                        case "rating": movie.rating = node.InnerText == "" ? 0 : float.Parse(node.InnerText); break;
                        case "plot": movie.plot = node.InnerText; break;
                        case "outline": movie.outline = node.InnerText; break;
                        case "year": movie.year = node.InnerText == "" ? 1970 : int.Parse(node.InnerText); break;
                        case "runtime": movie.runtime = node.InnerText == "" ? 0 : int.Parse(node.InnerText); break;
                        case "country": movie.country = node.InnerText; break;
                        case "source": movie.sourceurl = node.InnerText; break;
                        default: break;

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }
            if (movie.id == "") { return null; }
            //视频类型

            movie.vediotype = (int)Identify.GetVedioType(movie.id);

            //扫描视频获得文件大小
            if (File.Exists(path))
            {
                string fatherpath = new FileInfo(path).DirectoryName;
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(fatherpath, "*.*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception e)
                {
                    Logger.LogE(e);
                }

                if (files != null)
                {

                    var movielist = Scan.FirstFilter(files.ToList(), movie.id);
                    if (movielist.Count == 1)
                    {
                        movie.filepath = movielist[0];
                    }
                    else if (movielist.Count > 1)
                    {
                        //分段视频
                        movie.filepath = movielist[0];
                        string subsection = "";
                        movielist.ForEach(arg => { subsection += arg + ";"; });
                        movie.subsection = subsection;
                    }
                }



            }

            //tag
            XmlNodeList tagNodes = doc.SelectNodes("/movie/tag");
            if (tagNodes != null)
            {
                string tags = "";
                foreach (XmlNode item in tagNodes)
                {
                    if (item.InnerText != "") { tags += item.InnerText + " "; }

                }
                if (tags.Length > 0)
                {

                    if (movie.id.IndexOf("FC2") >= 0)
                    {
                        movie.genre = tags.Substring(0, tags.Length - 1);
                    }
                    else
                    {
                        movie.tag = tags.Substring(0, tags.Length - 1);
                    }


                }
            }

            //genre
            XmlNodeList genreNodes = doc.SelectNodes("/movie/genre");
            if (genreNodes != null)
            {
                string genres = "";
                foreach (XmlNode item in genreNodes)
                {
                    if (item.InnerText != "") { genres += item.InnerText + " "; }

                }
                if (genres.Length > 0) { movie.genre = genres.Substring(0, genres.Length - 1); }
            }

            //actor
            XmlNodeList actorNodes = doc.SelectNodes("/movie/actor/name");
            if (actorNodes != null)
            {
                string actors = "";
                foreach (XmlNode item in actorNodes)
                {
                    if (item.InnerText != "") { actors += item.InnerText + " "; }
                }
                if (actors.Length > 0) { movie.actor = actors.Substring(0, actors.Length - 1); }
            }

            //fanart
            XmlNodeList fanartNodes = doc.SelectNodes("/movie/fanart/thumb");
            if (fanartNodes != null)
            {
                string extraimageurl = "";
                foreach (XmlNode item in fanartNodes)
                {
                    if (item.InnerText != "") { extraimageurl += item.InnerText + ";"; }
                }
                if (extraimageurl.Length > 0) { movie.extraimageurl = extraimageurl.Substring(0, extraimageurl.Length - 1); }
            }


            return movie;
        }

        public static List<string> LabelToList(string label)
        {

            List<string> result = new List<string>();
            if (string.IsNullOrEmpty(label)) return result;
            if (label.IndexOf(' ') > 0)
            {
                foreach (var item in label.Split(' '))
                {
                    if (item.Length > 0)
                        if (!result.Contains(item)) result.Add(item);
                }
            }
            else { if (label.Length > 0) result.Add(label.Replace(" ", "")); }
            return result;
        }

        public static void ByteArrayToFile(byte[] byteArray, string fileName)
        {
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                }
            }
            catch (Exception ex)
            {
                Logger.LogF(ex);
            }
        }


        public static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString().ToLower();
        }

        public static string GetFileMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }


        /// <summary>
        /// 加载 321 识别码与网站转换规则，多 30M 内存
        /// </summary>
        public static void InitJav321IDConverter()
        {
            //var jav321 = Resource_IDData.jav321;//来自于JavGo
            var jav321 = 123;
            Stream jav321_stream = new MemoryStream(jav321);
            string str = "";
            using (var zip = new ZipArchive(jav321_stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry zipArchiveEntry = zip.Entries[0];
                using (StreamReader sr = new StreamReader(zipArchiveEntry.Open()))
                {
                    str = sr.ReadToEnd();
                }
            }
            Jav321IDDict = new Dictionary<string, string>();
            str = str.Replace("\r\n", "\n").ToUpper();
            foreach (var item in str.Split('\n'))
            {

                if (item.IndexOf(",") > 0)
                {
                    if (Jav321IDDict.ContainsKey(item.Split(',')[1]))
                    {
                        Jav321IDDict[item.Split(',')[1]] = item.Split(',')[0];
                    }
                    else
                    {
                        Jav321IDDict.Add(item.Split(',')[1], item.Split(',')[0]);
                    }
                }
            }


        }

        public static void addTag(ref Movie movie)
        {
            //添加标签戳
            if (movie == null) return;
            if (Identify.IsHDV(movie.filepath) || movie.genre?.IndexOf("高清") >= 0 || movie.tag?.IndexOf("高清") >= 0 || movie.label?.IndexOf("高清") >= 0) movie.tagstamps += "高清";
            if (Identify.IsCHS(movie.filepath) || movie.genre?.IndexOf("中文") >= 0 || movie.tag?.IndexOf("中文") >= 0 || movie.label?.IndexOf("中文") >= 0) movie.tagstamps += "中文";
            if (Identify.IsFlowOut(movie.filepath) || movie.genre?.IndexOf("流出") >= 0 || movie.tag?.IndexOf("流出") >= 0 || movie.label?.IndexOf("流出") >= 0) movie.tagstamps += "流出";
        }

        public static void addTag(ref DetailMovie movie)
        {
            //添加标签戳
            if (Identify.IsHDV(movie.filepath) || movie.genre?.IndexOf("高清") >= 0 || movie.tag?.IndexOf("高清") >= 0 || movie.label?.IndexOf("高清") >= 0) movie.tagstamps += "高清";
            if (Identify.IsCHS(movie.filepath) || movie.genre?.IndexOf("中文") >= 0 || movie.tag?.IndexOf("中文") >= 0 || movie.label?.IndexOf("中文") >= 0) movie.tagstamps += "中文";
            if (Identify.IsFlowOut(movie.filepath) || movie.genre?.IndexOf("流出") >= 0 || movie.tag?.IndexOf("流出") >= 0 || movie.label?.IndexOf("流出") >= 0) movie.tagstamps += "流出";
        }


        #region "配置xml"

        /// <summary>
        /// 读取原有的 config.ini到 xml
        /// </summary>
        public static void SaveScanPathToXml()
        {
            if (!File.Exists("DataBase\\Config.ini")) return;
            Dictionary<string, StringCollection> DataBases = new Dictionary<string, StringCollection>();
            using (StreamReader sr = new StreamReader(DataBaseConfigPath))
            {
                try
                {
                    string content = sr.ReadToEnd();
                    List<string> info = content.Split('\n').ToList();
                    info.ForEach(arg => {
                        string name = arg.Split('?')[0];
                        StringCollection stringCollection = new StringCollection();
                        arg.Split('?')[1].Split('*').ToList().ForEach(path => { if (!string.IsNullOrEmpty(path)) stringCollection.Add(path); });
                        if (!DataBases.ContainsKey(name)) DataBases.Add(name, stringCollection);
                    });
                }
                catch { }
            }
            foreach (var item in DataBases)
            {
                SaveScanPathToConfig(item.Key, item.Value.Cast<string>().ToList());
            }
            File.Delete("DataBase\\Config.ini");
        }

        public static StringCollection ReadScanPathFromConfig(string name)
        {
            return new ScanPathConfig(name).Read();
        }


        public static void SaveScanPathToConfig(string name, List<string> paths)
        {
            ScanPathConfig scanPathConfig = new ScanPathConfig(name);
            scanPathConfig.Save(paths);
        }


        /// <summary>
        /// 读取原有的 config.ini到 xml
        /// </summary>
        public static void SaveServersToXml()
        {
            if (!File.Exists("ServersConfig.ini")) return;
            Dictionary<WebSite, Dictionary<string, string>> Servers = new Dictionary<WebSite, Dictionary<string, string>>();
            using (StreamReader sr = new StreamReader("ServersConfig.ini"))
            {
                try
                {
                    string content = sr.ReadToEnd();
                    List<string> rows = content.Split('\n').ToList();
                    rows.ForEach(arg => {
                        string name = arg.Split('?')[0];
                        WebSite webSite = WebSite.None;
                        Enum.TryParse<WebSite>(name, out webSite);
                        string[] infos = arg.Split('?')[1].Split('*');
                        Dictionary<string, string> info = new Dictionary<string, string>
                        {
                            { "Url", infos[0] },
                            { "ServerName", infos[1] },
                            { "LastRefreshDate", infos[2] }
                        };
                        if (!Servers.ContainsKey(webSite)) Servers.Add(webSite, info);
                    });
                }
                catch { }
            }
            foreach (var item in Servers)
            {
                SaveServersInfoToConfig(item.Key, item.Value.Values.ToList<string>());
            }
            File.Delete("ServersConfig.ini");
        }

        public static void SaveServersInfoToConfig(WebSite webSite, List<string> infos)
        {
            Dictionary<string, string> info = new Dictionary<string, string>
            {
                { "Url", infos[0] },
                { "ServerName", infos[1] },
                { "LastRefreshDate", infos[2] }
            };

            ServerConfig serverConfig = new ServerConfig(webSite.ToString());
            serverConfig.Save(info);
        }

        public static void DeleteServerInfoFromConfig(WebSite webSite)
        {

            if (!File.Exists("ServersConfig.ini")) return;
            ServerConfig serverConfig = new ServerConfig(webSite.ToString());
            serverConfig.Delete();
        }

        public static List<string> ReadServerInfoFromConfig(WebSite webSite)
        {

            if (!File.Exists("ServersConfig")) return new List<string>() { webSite.ToString(), "", "" };


            List<string> result = new ServerConfig(webSite.ToString()).Read();
            if (result.Count < 3) result = new List<string>() { webSite.ToString(), "", "" };
            return result;
        }



        //最近观看

        public static void SaveRecentWatchedToXml()
        {
            if (!File.Exists("RecentWatch.ini")) return;
            Dictionary<string, List<string>> RecentWatchedes = new Dictionary<string, List<string>>();
            using (StreamReader sr = new StreamReader("RecentWatch.ini"))
            {
                try
                {
                    string content = sr.ReadToEnd();
                    List<string> rows = content.Split('\n').ToList();
                    rows.ForEach(arg => {
                        string date = arg.Split(':')[0];
                        var IDs = arg.Split(':')[1].Split(',').ToList();
                        if (!RecentWatchedes.ContainsKey(date)) RecentWatchedes.Add(date, IDs);
                    });
                }
                catch { }
            }
            foreach (var item in RecentWatchedes)
            {
                SaveRecentWatchedToConfig(item.Key, item.Value);
            }
            File.Delete("RecentWatch.ini");
        }

        public static void SaveRecentWatchedToConfig(string date, List<string> IDs)
        {
            RecentWatchedConfig recentWatchedConfig = new RecentWatchedConfig(date);
            recentWatchedConfig.Save(IDs);
        }

        public static void ReadRecentWatchedFromConfig()
        {
            if (!File.Exists("RecentWatch")) return;
            RecentWatched = new RecentWatchedConfig("").Read();
        }


        public static void SaveRecentWatched()
        {
            foreach (var keyValuePair in RecentWatched)
            {
                if (keyValuePair.Key <= DateTime.Now && keyValuePair.Key >= DateTime.Now.AddDays(-1 * Properties.Settings.Default.RecentDays))
                {
                    if (keyValuePair.Value.Count > 0)
                    {
                        List<string> IDs = keyValuePair.Value.Where(arg => !string.IsNullOrEmpty(arg)).ToList();

                        string date = keyValuePair.Key.Date.ToString("yyyy-MM-dd");
                        RecentWatchedConfig recentWatchedConfig = new RecentWatchedConfig(date);
                        recentWatchedConfig.Save(IDs);
                    }
                }
            }






        }


        #endregion

    }
}
