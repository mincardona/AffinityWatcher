using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AffinityWatcher
{
    class WatchedProcessConfig
    {
        public string ProcessName { get; private set; }
        public long TargetAffinity { get; private set; }

        public WatchedProcessConfig(string processName, long targetAffinity)
        {
            ProcessName = processName;
            TargetAffinity = targetAffinity;
        }
    }
}
