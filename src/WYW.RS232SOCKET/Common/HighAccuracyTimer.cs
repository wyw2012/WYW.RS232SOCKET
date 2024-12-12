using System.Diagnostics;
using System.Threading;

namespace WYW.RS232SOCKET
{
    public class HighAccuracyTimer
    {
        public static void Sleep(int millisecondsTimeout)
        {
            if (millisecondsTimeout <= 0)
                return;
            var stopWatch = Stopwatch.StartNew();
            while (stopWatch.ElapsedMilliseconds < millisecondsTimeout)
            {

            }
            stopWatch.Stop();
        }
        public static void MixedSleep(int milliseconds)
        {
            // 前n-2ms用Thread计时，后2ms用CPU计时
            var stopWatch = Stopwatch.StartNew();
            if (milliseconds <= 0)
                return;
            if (milliseconds > 2)
            {
                Thread.Sleep(milliseconds - 2);
            }
            while (stopWatch.ElapsedMilliseconds < milliseconds)
            {

            }
            stopWatch.Stop();

        }
    }
}
