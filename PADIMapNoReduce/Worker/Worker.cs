﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Worker
{
    class Worker : MarshalByRefObject, WorkerApi, JobTrackerApi
    {

        private List<string> workers = new List<string>();

        /**** WorkerImpl ****/
        public void registerWork(int[] splits)
        {
            throw new NotImplementedException();
        }

        public void registerSplitData(string data)
        {
            throw new NotImplementedException();
        }


        /**** JobTrackerImpl ****/
        public void registerJob(string inputFilePath, int nSplits, string outputResultPath)
        {
            throw new NotImplementedException();
        }

        public void hello(string src)
        {
            System.Console.WriteLine("Hello from " + src);
        }
    }
}
