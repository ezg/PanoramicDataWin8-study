using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicDataWin8.controller.data
{
    public abstract class Job
    {
        public event EventHandler<JobEventArgs> JobUpdate;
        public event EventHandler<JobEventArgs> JobCompleted;
        public event EventHandler<JobEventArgs> JobStopped;

        public abstract void Start();
        public abstract void Stop();

        protected void FireJobUpdated(JobEventArgs jobEventArgs)
        {
            if (JobUpdate != null)
            {
                JobUpdate(this, jobEventArgs);
            }
        }

        protected void FireJobCompleted(JobEventArgs jobEventArgs)
        {
            if (JobCompleted != null)
            {
                JobCompleted(this, jobEventArgs);
            }
        }

        protected void FireJobStopped(JobEventArgs jobEventArgs)
        {
            if (JobStopped != null)
            {
                JobStopped(this, jobEventArgs);
            }
        }
    }
}
