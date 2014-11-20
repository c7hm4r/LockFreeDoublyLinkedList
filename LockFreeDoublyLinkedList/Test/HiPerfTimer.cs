using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace HiPerfTimer
{
    public class HpTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        private long startTime, stopTime;
        private long freq;

        // Constructor

        public static long Ticks
        {
            get
            {
                long result;
                QueryPerformanceCounter(out result);
                return result;
            }
        }

        public static long Frequency
        {
            get
            {
                long result;
                QueryPerformanceFrequency(out result);
                return result;
            }
        }

        public HpTimer()
        {
            startTime = 0;
            stopTime = 0;

            if (QueryPerformanceFrequency(out freq) == false)
            {
                // high-performance counter not supported

                throw new Win32Exception();
            }
        }

        // Start the timer

        public void Start()
        {
            // lets do the waiting threads there work

            Thread.Sleep(0);

            QueryPerformanceCounter(out startTime);
        }

        // Stop the timer

        public void Stop()
        {
            QueryPerformanceCounter(out stopTime);
        }

        /// <summary>
        /// Time passed since start in seconds.
        /// </summary>
        public double TimeSinceStart
        {
            get
            {
                long time;
                QueryPerformanceCounter(out time);
                return (double)(time - startTime) / (double)freq;
            }
        }

        // Returns the duration of the timer (in seconds)

        public double Duration
        {
            get
            {
                return (double)(stopTime - startTime) / (double)freq;
            }
        }
    }
}
