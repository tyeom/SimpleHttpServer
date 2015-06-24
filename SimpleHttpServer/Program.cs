using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;
using Microsoft.Win32;
using SimpleHttpServer.Common;
using SimpleHttpServer.Config;
using SimpleHttpServer.HttpServerEngine;

namespace SimpleHttpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // 환경설정 로드
            int Port = int.Parse(ConfigMgr.Default.GetValueByEnum(ParentCode.Base, ChildCode.DefaultPort).ToString());

            //ThreadPool.QueueUserWorkItem(new WaitCallback(HttpServerStarting), new Object[] { Port });
            //Console.ReadLine();

            Thread thread = new Thread(new ParameterizedThreadStart(HttpServerStarting));
            thread.Start(new Object[] { Port });
        }

        static void HttpServerStarting(object Params)
        {
            object[] ObjParams = (object[])Params;
            int Port = (int)ObjParams[0];

            HttpServer Server = new MyHttpServer(Port);
            Server.Listener();
        }
    }

    public class MyHttpServer : HttpServer
    {
        private static object _lockObject = new object();
        string _rootDirectory;
        object[] _DefaultDocuments;

        public MyHttpServer(int Port)
            : base(Port)
        {
            _rootDirectory = ConfigMgr.Default.GetValueByEnum(ParentCode.Base, ChildCode.DefaultRootDirectory).ToString();
            _DefaultDocuments = (object[])ConfigMgr.Default.GetValueByEnum(ParentCode.Document, ChildCode.DefaultDocument);
        }

        public override void handleGETRequest(HttpProcessor p)
        {
            this.OnResponse(p);
        }

        public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
        {
            string PostData = inputData.ReadToEnd();
            Console.WriteLine("POST request: {0}", p.http_url);
            Console.WriteLine("POST Data: {0}", PostData);

            this.OnResponse(p);
        }

        private void OnResponse(HttpProcessor p)
        {
            string UrlPath;
            if (string.Compare(p.http_url, "/") == 0)
            {
                UrlPath = _rootDirectory + "\\";
                foreach (object DefaultDocument in _DefaultDocuments)
                {
                    UrlPath = string.Concat(UrlPath, DefaultDocument);
                    if (File.Exists(UrlPath))
                    {
                        break;
                    }
                }
            }
            else
            {
                UrlPath = _rootDirectory + HttpUtility.UrlDecode(p.http_url).Replace("/", "\\");
            }

            if (File.Exists(UrlPath))
            {
                FileInfo fi = new FileInfo(UrlPath);
                if (p.httpHeaders.ContainsKey("If-Modified-Since") && p.httpHeaders["If-Modified-Since"] != null && p.httpHeaders["If-Modified-Since"].ToString() == fi.LastWriteTimeUtc.ToString("r"))  // 클라이언트 브라우저의 캐시 날짜를 읽는다.
                {
                    // 클라이언트 캐시상에 컨텐츠와 서버상의 컨텐츠가 수정된 내용이 없으면(파일의 마지막 수정날짜가 같다면) 304응답 리턴
                    p.Write(RequestStatus.Not_Modified);
                }
                else
                {
                    lock (_lockObject)
                    {
                        string MimeType = GetMimeType(UrlPath);
                        p.fs = new FileStream(UrlPath, FileMode.Open);
                        p.Write(RequestStatus.Ok, fi.LastWriteTimeUtc.ToString("r"), MimeType);
                    }
                }
                fi = null;
            }
            else
            {
                p.Body = "<html><body><h1>File not found!!</h1></html>";
                p.Write(RequestStatus.Ok);
            }
        }

        private string GetMimeType(string UrlPath)
        {
            RegistryKey rk = Registry.ClassesRoot.OpenSubKey(Path.GetExtension(UrlPath), true);
            string StrMimeTypes = (String)rk.GetValue("Content Type");

            return StrMimeTypes;

            // WinAPI 이용
            //return MimeTypes.GetContentType(UrlPath);
        }
    }
}
