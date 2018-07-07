using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AffinityWatcher
{
    class ProcessWatcher
    {
        Dictionary<string, WatchedProcessConfig> processes;
        ManagementEventWatcher watcher;

        public ProcessWatcher(List<WatchedProcessConfig> procConfigs)
        {
            watcher = null;
            processes = new Dictionary<string, WatchedProcessConfig>();
            foreach (var config in procConfigs) {
                processes[config.ProcessName] = config;
            }
        }

        public void Start()
        {
            if (watcher != null) {
                Stop();
            }

            // these could throw
            watcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace")
            );
            watcher.EventArrived += new EventArrivedEventHandler(ProcessStartHandler);
            watcher.Start();
        }

        public void Stop()
        {
            watcher.Stop();
            watcher = null;
        }

        private void ProcessStartHandler(object sender, EventArrivedEventArgs eventArgs)
        {
            PropertyDataCollection properties = eventArgs.NewEvent.Properties;
            var name = (string)properties["ProcessName"].Value;

            var timestamp = (UInt64)properties["TIME_CREATED"].Value;
            // The WMI timestamp is a UInt64, but this factory method requires a long.
            // Just cast it
            var timeOf = DateTime.FromFileTime((long)timestamp);

            var pid = (UInt32)properties["ProcessID"].Value;

            //Console.WriteLine("Noticed process with name \"{0}\" and PID {1}", name, pid);

            if (processes.TryGetValue(name, out WatchedProcessConfig processConfig)) {
                Console.WriteLine("Process detected with name \"{0}\" and PID {1}", name, pid);

                Process ps = null;
                try {
                    // The WMI PID returned is a UInt32, but this factory method requires an int.
                    // Just cast it
                    ps = Process.GetProcessById((int)pid);
                } catch (Exception e) {
                    Console.WriteLine("Unable to get process with PID {0}: {1}", pid, e.Message);
                    return;
                }

                try {
                    ChangeProcessAffinity(ps, processConfig.TargetAffinity);
                    var now = DateTime.Now;
                    TimeSpan ts = new TimeSpan(0, 0, 0);
                    if (now > timeOf) {
                        ts = now.Subtract(timeOf);
                    }
                    Console.WriteLine("Changed affinity of process with PID {0} (delay {1}ms)", pid, ts.TotalMilliseconds);
                } catch (Exception e) {
                    Console.WriteLine("Unable to change affinity of process with PID {0}: {1}", pid, e.Message);
                    return;
                }
            }
        }

        private static void ChangeProcessAffinity(Process ps, long affinityMask)
        {
            var oldMask = (long)ps.ProcessorAffinity;
            ps.ProcessorAffinity = (IntPtr)(oldMask & affinityMask);
        }
    }
}
