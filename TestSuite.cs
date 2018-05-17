using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SocketPoll
{
    public class TestSuite : IDisposable
    {
        const string IpAddress = "127.0.0.1";
        const int Port = 1514;
        const int ConnectionCheckTimeout = 15000000;

        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Stopwatch _stopwatch;
        private readonly UdpClient _udpServer;

        public TestSuite(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _stopwatch = new Stopwatch();
            _udpServer = new UdpClient(Port);
            Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var remoteEP = new IPEndPoint(IPAddress.Any, Port);
                    var received = _udpServer.Receive(ref remoteEP);
                    _testOutputHelper.WriteLine(Encoding.ASCII.GetString(received));
                }
            });
        }

        [Fact]
        public void ImmediateConnectionCheckAndOneMessageSend()
        {
            using (var udp = new UdpClient(IpAddress, Port))
            {
                CheckConnectionAssertCheckWasInstantaneousSend(udp, 1);
            }
        }

        [Fact]
        public void ImmediateConnectionCheckAndTwoMessageSend()
        {
            using (var udp = new UdpClient(IpAddress, Port))
            {
                CheckConnectionAssertCheckWasInstantaneousSend(udp, 1);
                CheckConnectionAssertCheckWasInstantaneousSend(udp, 2);
            }
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _udpServer.Dispose();
        }

        private void CheckConnectionAssertCheckWasInstantaneousSend(UdpClient udp, int i)
        {
            _stopwatch.Restart();
            var isConnected = IsConnected(udp?.Client, ConnectionCheckTimeout);
            _stopwatch.Stop();
            Assert.True(isConnected);
            Assert.Equal(0, _stopwatch.ElapsedMilliseconds / 1000);
            var bytes = new ASCIIEncoding().GetBytes($"my message {i}");
            udp.Send(bytes, bytes.Length);
        }

        private bool IsConnected(Socket socket, int timeout)
        {
            if (socket == null)
                return true;

            if (timeout <= 0)
                return true;

            var isDisconnected = socket?.Poll(timeout, SelectMode.SelectRead) == true && socket?.Available == 0;
            return !isDisconnected;
        }
    }
}