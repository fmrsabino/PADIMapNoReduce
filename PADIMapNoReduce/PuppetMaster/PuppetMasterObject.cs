using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            try
            {
                workers.Add(id, serviceURL);
            } catch (Exception e)
            {
                // Same id already exists. Not relaunching.
                return false;
            }

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = workerExecutablePath;
            p.StartInfo.Arguments = "-u " + serviceURL;
            return p.Start();
        }

    }
}
