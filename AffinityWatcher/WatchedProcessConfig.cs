using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AffinityWatcher
{
    /**
     * Configuration associated with a watched process.
     */
    class WatchedProcessConfig
    {
        /** The name of the process (e.g. "notepad.exe") */
        public string ProcessName { get; private set; }
        /** The affinity mask to apply */
        public long TargetAffinity { get; private set; }

        /**
         * Constructs a new configuration object.
         * 
         * @param processName the name of the process
         * @param targetAffinity the affinity mask
         */
        public WatchedProcessConfig(string processName, long targetAffinity)
        {
            ProcessName = processName;
            TargetAffinity = targetAffinity;
        }
    }
}
