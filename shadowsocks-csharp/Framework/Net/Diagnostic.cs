using System;
using System.Net.NetworkInformation;


namespace Shadowsocks.Framework.Net
{
	public static class Diagnostic
	{
        private const int PingTo = 1000;




        public delegate bool PingCallback(string host, int echoId, PingReply reply);

        public static void Ping(string host, PingCallback callback, int timeout = PingTo)
        {
            var pingSender = new Ping();
            var echoId = 0;

            while ( 
	            callback(host, ++echoId, pingSender.Send(host, timeout))
	            ) {}
        }




        public static PingReply[] Ping(string host, int echoNum, int timeout = PingTo)
        {
	        if (echoNum <= 0 || echoNum > 100)
	            throw new Exception("bad echoNum: " + echoNum);

	        var result = new PingReply[echoNum];

            var pingSender = new Ping();
            for (var i = 0; i < echoNum; i++)
				result[i] = pingSender.Send(host, timeout);

            return result;
        }




        public static double PingAvg(string host, int echoNum, int timeout = PingTo)
        {
            long totalTime = 0;
            var success = 0;
            var pingSender = new Ping();

            for (var i = 0; i < echoNum; i++)
            {
                var reply = pingSender.Send(host, timeout);
                if (reply?.Status == IPStatus.Success)
                {
                    totalTime += reply.RoundtripTime;
                    ++success;
                }
            }

            return (double)totalTime / success;
        }

    }
}
