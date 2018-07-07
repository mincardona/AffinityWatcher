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
            var configs = new List<WatchedProcessConfig> {
                new WatchedProcessConfig("notepad.exe", 0x1)
            };

            var watcher = new ProcessWatcher(configs);
            watcher.Start();

            while (!Console.KeyAvailable) {
                System.Threading.Thread.Sleep(100);
            }

            watcher.Stop();
        }

    }
}
