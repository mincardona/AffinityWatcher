using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AffinityWatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            int processResultCode = MonitorProcess();
            // breakpoint statement to view output
            int nop = 0;
        }

        static int MonitorProcess()
        {
            ManagementEventWatcher processStartWatcher = null;
            try
            {
                processStartWatcher = new ManagementEventWatcher(
                    new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace")
                );
                processStartWatcher.EventArrived += new EventArrivedEventHandler(ProcessStartHandler);
                processStartWatcher.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to create process listener (are you elevated?): {0}", e.Message);
                return -1;
            }

            Console.WriteLine("Press any key to exit...");
            while (!Console.KeyAvailable)
            {
                System.Threading.Thread.Sleep(100);
            }

            processStartWatcher.Stop();

            return 0;
        }

        static void ProcessStartHandler(object sender, EventArrivedEventArgs eventArgs)
        {
            PropertyDataCollection properties = eventArgs.NewEvent.Properties;
            var name = (string)properties["ProcessName"].Value;
            //var timestamp = (UInt64)e.NewEvent.Properties["TIME_CREATED"].Value;
            var pid = (UInt32)properties["ProcessID"].Value;

            Console.WriteLine("Noticed process with name \"{0}\" and PID {1}", name, pid);

            if (ProcessNameEquals(name, "notepad.exe"))
            {

                Console.WriteLine("Process detected with PID {0}", pid);

                Process ps = null;
                try
                {
                    // WMI docs say the PID returned is a uint32, but the Process factory method requires an int
                    // just cast it
                    ps = Process.GetProcessById((int)pid);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to get process with PID {0}: {1}", pid, e.Message);
                    return;
                }

                try
                {
                    ChangeProcessAffinity(ps, 0x1);
                    Console.WriteLine("Changed affinity of process with PID {0}", pid);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to change affinity of process with PID {0}: {1}", pid, e.Message);
                    return;
                }
            }
        }

        static bool ProcessNameEquals(string lhs, string rhs)
        {
            return lhs == rhs;
        }

        static void ChangeProcessAffinity(Process ps, long affinityMask)
        {
            var oldMask = (long)ps.ProcessorAffinity;
            ps.ProcessorAffinity = (IntPtr)(oldMask & affinityMask);
        }
        
    }
}
