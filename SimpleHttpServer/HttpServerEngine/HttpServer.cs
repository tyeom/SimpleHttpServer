using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SimpleHttpServer.HttpServerEngine
{
    public abstract class HttpServer
    {
        protected int _Port;
        TcpListener _Listener;
        bool _IsActive = true;

        public HttpServer(int Port)
        {
            _Port = Port;
        }

        /// <summary>
        /// 클라이언트 접속 대기
        /// </summary>
        public void Listener()
        {
            IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            //_Listener = new TcpListener(ipAddress, _Port);
            // 모든 IP주소에서 접속하는걸 허용
            _Listener = new TcpListener(IPAddress.Any, _Port);
            _Listener.Start();

            var task = this.HandleConnectionsAsync(_Listener);
            task.Wait();
        }

        /// <summary>
        /// GET방식으로 요청시 처리
        /// </summary>
        /// <param name="p"></param>
        public abstract void handleGETRequest(HttpProcessor p);
        /// <summary>
        /// POST방식으로 요청시 처리
        /// </summary>
        /// <param name="p"></param>
        /// <param name="inputData"></param>
        public abstract void handlePOSTRequest(HttpProcessor p, StreamReader inputData);

        /// <summary>
        /// 비동기 방식으로 클라이언트 접속 처리
        /// </summary>
        /// <param name="listener"></param>
        /// <returns></returns>
        private async Task HandleConnectionsAsync(TcpListener listener)
        {
            // 항상 클라이언트 접속요청을 대기 한다.
            while (_IsActive)
            {
                Console.Write("Waiting for async connection...");
                // 비동기로 클라이언트 접속을 받아드린다.
                var client = await listener.AcceptTcpClientAsync();

                // 클라이언트 접속!
                IPEndPoint RemoteIPAddress = ((IPEndPoint)(client.Client.RemoteEndPoint));
                Console.WriteLine("OK # {0}", RemoteIPAddress.Address.ToString());

                // 클라이언트의 요청을 처리 [비동기 방식]
                HttpProcessor processor = new HttpProcessor(client, this);
                var HttpProcessorTask = new Task(() => processor.process());
                HttpProcessorTask.Start();
            }
        }
    }
}
