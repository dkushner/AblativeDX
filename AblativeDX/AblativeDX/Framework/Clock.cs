using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using SlimDX;

namespace AblativeDX.Framework
{
    public class Clock
    {
        private bool isRunning;
        private long count;
        private readonly long frequency;

        public Clock()
        {
            frequency = Stopwatch.Frequency;
        }
        public void Start()
        {
            count = Stopwatch.GetTimestamp();
            isRunning = true;
        }
        public float Update()
        {
            float result = 0.0f;
            if (isRunning)
            {
                long lcount = count;
                count = Stopwatch.GetTimestamp();
                result = (float)(count - lcount) / frequency;
            }
            return result;
        }
    }
}
