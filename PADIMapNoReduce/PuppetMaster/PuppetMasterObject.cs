using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PuppetMaster
{
    class PuppetMaster : MarshalByRefObject, PADIMapNoReduce.IPuppetMaster
    {
        private Dictionary<int, string> workers;
        private string workerExecutablePath;

        public PuppetMaster (string _workerExecutablePath)
        {
            workers = new Dictionary<int, string>();
            workerExecutablePath = _workerExecutablePath;
        }

        public bool startWorker(int id, string serviceURL, string entryURL)
        {
            Uri serviceURLparsed;
            try
            {
                serviceURLparsed = new Uri(serviceURL);
                workers.Add(id, "tcp://" + System.Environment.MachineName + ":" + serviceURLparsed.Port + "/W");
            }
            catch (Exception e)
            {
                // Same id already exists. Not relaunching.
                // Or serviceURL is not a valid URL
                return false;
            }

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = workerExecutablePath;
            p.StartInfo.Arguments = "-p " + serviceURLparsed.Port + (serviceURL != entryURL ? " " + entryURL : "");
            return p.Start();
        }

        public void printStatus()
        {
            foreach(KeyValuePair<int, string> worker in workers)
            {

                PADIMapNoReduce.IJobTracker w = (PADIMapNoReduce.IJobTracker)Activator.GetObject(
        typeof(PADIMapNoReduce.IJobTracker), worker.Value);
                w.printStatus();
            }
        }
    }
}
