#region license
// Copyright 2016 Christoph Müller
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
	// HiPerfTimer
    public class HpTimer
    {
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

	    #region private
	    private long startTime, stopTime;
	    private readonly long freq;

	    [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

	    [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(
            out long lpFrequency);
	    #endregion
    }
}
