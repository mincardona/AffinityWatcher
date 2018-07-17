using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AffinityWatcher
{
    /**
     * Watches for processes and modifies their processor affinities.
     */
    class ProcessWatcher
    {
        // maps process names to their configurations
        Dictionary<string, WatchedProcessConfig> processes;
        // WMI object that listens for new processes
        /** @invariant watcher is null if and only if this object is stopped */
        ManagementEventWatcher watcher;

        /**
         * Creates a new ProcessWatcher with the supplied configuration.
         * 
         * Start() must be called to begin monitoring processes.
         * 
         * @param procConfigs the set of process configurations
         */
        public ProcessWatcher(List<WatchedProcessConfig> procConfigs)
        {
            watcher = null;
            // store the configurations in a dictionary for easy lookup by name
            processes = new Dictionary<string, WatchedProcessConfig>();
            foreach (var config in procConfigs) {
                processes[config.ProcessName] = config;
            }
        }

        /**
         * Starts or restarts watching for new processes.
         * 
         * If this watcher is currently stopped, it is started. If it is
         * currently started, it is stopped and then started again.
         * 
         * @throws Exception if the watcher cannot be started
         */
        public void Start()
        {
            if (watcher != null) {
                Stop();
            }

            // try to create a new watcher
            try {
                // these could throw
                watcher = new ManagementEventWatcher(
                    new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace")
                );
                watcher.EventArrived += new EventArrivedEventHandler(ProcessStartHandler);
                watcher.Start();
            } catch {
                watcher = null;
                throw;
            }
        }

        /**
         * Stops watching for processes.
         * 
         * @throws Exception if an error is signalled while stopping
         * @post this object is stopped, even if an exception is thrown
         */
        public void Stop()
        {
            try {
                watcher.Stop();
            } catch {
                throw;
            } finally {
                watcher = null;
            }
        }

        /**
         * Event handler when a new process is noticed.
         */
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

            // apply any matching process config we have
            if (processes.TryGetValue(name, out WatchedProcessConfig processConfig)) {
                Console.WriteLine("Process detected with name \"{0}\" and PID {1}", name, pid);
                
                // get the process
                Process ps = null;
                try {
                    // The WMI PID returned is a UInt32, but this factory method requires an int.
                    // Just cast it
                    ps = Process.GetProcessById((int)pid);
                } catch (Exception e) {
                    Console.WriteLine("Unable to get process with PID {0}: {1}", pid, e.Message);
                    return;
                }

                // change the affinity
                try {
                    ChangeProcessAffinity(ps, processConfig.TargetAffinity);
                    Console.WriteLine("Changed affinity of process with PID {0} (delay {1}ms)", pid, TimeSince(timeOf).TotalMilliseconds);
                } catch (Exception e) {
                    Console.WriteLine("Unable to change affinity of process with PID {0}: {1}", pid, e.Message);
                    return;
                }
            }
        }

        /**
         * Gets the time elapsed since some other time, clamped to be at least 0.
         * 
         * @param then the past time
         * @return time elapsed since then, or a zero TimeSpan if then is now or in the future
         */
        private static TimeSpan TimeSince(DateTime then)
        {
            var now = DateTime.Now;
            if (now > then) {
                return now.Subtract(then);
            } else {
                return new TimeSpan(0, 0, 0);
            }
        }

        /**
         * Changes the affinity of a process.
         * 
         * @param ps the process to modify
         * @param affinityMask mask for the new affinity
         * @throws Exception if the affinity could not be changed
         */
        private static void ChangeProcessAffinity(Process ps, long affinityMask)
        {
            var oldMask = (long)ps.ProcessorAffinity;
            ps.ProcessorAffinity = (IntPtr)(oldMask & affinityMask);
        }
    }
}
