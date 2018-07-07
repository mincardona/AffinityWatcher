using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace AffinityWatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            var configs = new List<WatchedProcessConfig>();
            try {
                ParseConfig(@"..\..\awconfig.xml", configs);
            } catch (Exception e) {
                Console.WriteLine("Error parsing configuration: {0}", e.Message);
                return;
            }

            PrintConfigStatus(configs);

            var watcher = new ProcessWatcher(configs);
            watcher.Start();

            while (!Console.KeyAvailable) {
                System.Threading.Thread.Sleep(100);
            }

            watcher.Stop();
        }

        static void PrintConfigStatus(List<WatchedProcessConfig> configs)
        {
            Console.WriteLine("{0} visible logical processors detected", Environment.ProcessorCount);
            Console.WriteLine("Read data for {0} process(es):", configs.Count);
            foreach (var config in configs) {
                Console.WriteLine("\t{0} => {1}", config.ProcessName, config.TargetAffinity);
            }
        }

        static void ParseConfig(string filePath, List<WatchedProcessConfig> list)
        {
            list.Clear();

            // XmlException
            XElement tree = XElement.Load(filePath);
            // get all process elements
            XElement xmlProcListElement = tree.Element("processes");
            if (xmlProcListElement == null) {
                throw new Exception("process list not found");
            }

            foreach (XElement xmlProc in xmlProcListElement.Elements().Where(el => el.Name == "process")) {
                string name = xmlProc.Attribute("name")?.Value;
                string affinityStr = xmlProc.Attribute("affinity")?.Value;

                if (name == null || affinityStr == null) {
                    throw new Exception("Invalid entry in process list (missing attributes)");
                }

                if (!long.TryParse(affinityStr, out long affinity)) {
                    throw new Exception(string.Format("Invalid affinity value in process list (name = \"{0}\")", name));
                }

                list.Add(new WatchedProcessConfig(name, affinity));
            }
        }
    }
}
