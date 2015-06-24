using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SimpleHttpServer.Common;

namespace SimpleHttpServer.HttpServerEngine
{
    public class HttpProcessor
    {
        private const string HTTP_VERSION = "HTTP/1.1";
        private HttpServer _HttpSrv;

        private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB
        private const int BUF_SIZE = 4096;

        /// <summary>
        /// 클라이언트가 보낸 접속 요청 데이터
        /// </summary>
        private Stream _InputStream;
        private NetworkStream _OutputStream;

        #region Propertys
        public Hashtable httpHeaders { get; private set; }
        public TcpClient Client { get; private set; }
        public String http_method { get; private set; }
        public String http_url { get; private set; }
        public String http_protocol_versionstring { get; private set; }
        public string Body { get; set; }
        public FileStream fs { get; set; }
        #endregion  // Propertys

        public HttpProcessor(TcpClient s, HttpServer srv)
        {
            Client = s;
            _HttpSrv = srv;
            httpHeaders = new Hashtable();
        }

        private string streamReadLine(Stream inputStream)
        {
            int next_char;
            string data = "";
            while (true)
            {
                next_char = inputStream.ReadByte();
                if (next_char == '\n') { break; }
                if (next_char == '\r') { continue; }
                if (next_char == -1) { Thread.Sleep(1); continue; };
                data += Convert.ToChar(next_char);
            }
            return data;
        }

        /// <summary>
        /// 클라이언트 요청 처리
        /// </summary>
        public void process()
        {
            // we can't use a StreamReader for input, because it buffers up extra data on us inside it's
            // "processed" view of the world, and we want the data raw after the headers
            _InputStream = new BufferedStream(Client.GetStream());

            // we probably shouldn't be using a streamwriter for all output from handlers either
            _OutputStream = Client.GetStream();
            try
            {
                parseRequest();
                readHeaders();
                if (http_method.Equals("GET"))
                {
                    this.handleGETRequest();
                }
                else if (http_method.Equals("POST"))
                {
                    this.handlePOSTRequest();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
                this.Write(RequestStatus.Not_Found);
            }
            finally
            {
                _InputStream = null;
                _OutputStream.Flush();
                _OutputStream.Close();
                _OutputStream = null;
                Client.Close();

                Console.WriteLine("Client Socket Close");
                Console.WriteLine("");
                Console.WriteLine("");
            }
        }

        private void parseRequest()
        {
            String request = streamReadLine(_InputStream);
            string[] tokens = request.Split(' ');
            if (tokens.Length != 3)
            {
                throw new Exception("invalid http request line");
            }
            http_method = tokens[0].ToUpper();
            http_url = tokens[1];
            http_protocol_versionstring = tokens[2];

            Console.WriteLine("starting: " + request);
        }

        private void readHeaders()
        {
            Console.WriteLine("readHeaders()");
            String line;
            while ((line = streamReadLine(_InputStream)) != null)
            {
                if (line.Equals(""))
                {
                    Console.WriteLine("got headers");
                    return;
                }

                int separator = line.IndexOf(':');
                if (separator == -1)
                {
                    throw new Exception("invalid http header line: " + line);
                }
                String name = line.Substring(0, separator);
                int pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' '))
                {
                    pos++; // strip any spaces
                }

                string value = line.Substring(pos, line.Length - pos);
                Console.WriteLine("header: {0}:{1}", name, value);
                httpHeaders[name] = value;
            }
        }

        public void Write(RequestStatus Status, string LastModified = null, string content_type = "text/html")
        {
            string StrStatus = GEI.GetParameter<RequestStatus>(Status, 0).ToString();

            StringBuilder SB_Header = new StringBuilder();
            SB_Header.Append(HTTP_VERSION);
            SB_Header.AppendLine(string.Format(" {0}", StrStatus));
            SB_Header.AppendLine(string.Format("Content-Type: {0}", content_type));
            // 클라이언트 브라우저 캐시 사용, 컨텐츠의 마지막 수정 날짜를 헤더로 클라이언트에게 보냄
            if(LastModified != null)
                SB_Header.AppendLine(string.Format("Last-Modified: {0}", LastModified));
            SB_Header.AppendLine(string.Format("Date: {0}", DateTime.Now.ToString("r")));
            SB_Header.AppendLine(string.Format("Accept-Ranges: {0}", "bytes"));
            SB_Header.AppendLine(string.Format("Server: {0}", "SimpleHttpServer"));
            SB_Header.AppendLine(string.Format("Connection: {0}", "close"));
            SB_Header.AppendLine("");

            // Send headers	
            byte[] bHeadersString = Encoding.UTF8.GetBytes(SB_Header.ToString());
            _OutputStream.Write(bHeadersString, 0, bHeadersString.Length);

            if ((int)Status < 400)
            {
                // Send body
                if (string.IsNullOrWhiteSpace(Body) == false)
                {
                    byte[] bBodyString = Encoding.UTF8.GetBytes(Body);
                    _OutputStream.Write(bBodyString, 0, bBodyString.Length);
                    Body = null;
                }
                else if (fs != null)
                {
                    using (fs)
                    {
                        byte[] b = new byte[Client.SendBufferSize];
                        int bytesRead;
                        while ((bytesRead = fs.Read(b, 0, b.Length)) > 0)
                        {
                            _OutputStream.Write(b, 0, bytesRead);
                        }

                        fs.Close();
                    }
                    fs = null;
                }
            }
        }

        private void handleGETRequest()
        {
            _HttpSrv.handleGETRequest(this);
        }

        private void handlePOSTRequest()
        {
            // this post data processing just reads everything into a memory stream.
            // this is fine for smallish things, but for large stuff we should really
            // hand an input stream to the request processor. However, the input stream 
            // we hand him needs to let him see the "end of the stream" at this content 
            // length, because otherwise he won't know when he's seen it all! 

            Console.WriteLine("get post data start");
            int content_len = 0;
            MemoryStream ms = new MemoryStream();
            if (httpHeaders.ContainsKey("Content-Length"))
            {
                content_len = Convert.ToInt32(httpHeaders["Content-Length"]);
                if (content_len > MAX_POST_SIZE)
                {
                    throw new Exception(
                        String.Format("POST Content-Length({0}) too big for this simple server",
                          content_len));
                }
                byte[] buf = new byte[BUF_SIZE];
                int to_read = content_len;
                while (to_read > 0)
                {
                    Console.WriteLine("starting Read, to_read={0}", to_read);

                    int numread = _InputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
                    Console.WriteLine("read finished, numread={0}", numread);
                    if (numread == 0)
                    {
                        if (to_read == 0)
                        {
                            break;
                        }
                        else
                        {
                            throw new Exception("client disconnected during post");
                        }
                    }
                    to_read -= numread;
                    ms.Write(buf, 0, numread);
                }
                ms.Seek(0, SeekOrigin.Begin);
            }
            Console.WriteLine("get post data end");
            _HttpSrv.handlePOSTRequest(this, new StreamReader(ms));
        }
    }
}
