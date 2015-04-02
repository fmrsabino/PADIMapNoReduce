using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMaster
{
    class PuppetMaster : MarshalByRefObject, PADIMapNoReduce.IPuppetMaster
    {
        public bool startWorker(int id, string serviceURL, string entryURL)
        {
            PuppetMasterForm form = (PuppetMasterForm)Application.OpenForms["PuppetMasterForm"];
            // Using Invoke() caused a bug as the object call would block the execution and the communication
            // Used BeginInvoke() instead for async calling
            form.BeginInvoke(form.delegateMockStartWorker, new Object[] { id, serviceURL, entryURL });
            return true;
        }
 
    }
}
