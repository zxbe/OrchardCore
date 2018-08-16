using System;
using NCrontab;

namespace OrchardCore.BackgroundTasks
{
    public class BackgroundTaskScheduler
    {
        public BackgroundTaskScheduler(string tenant, string name, DateTime referenceTime)
        {
            Name = name;
            Tenant = tenant;
            ReferenceTime = referenceTime;
            Settings = new BackgroundTaskSettings() { Name = name };
            State = new BackgroundTaskState() { Name = name };
        }

        public string Name { get; }
        public string Tenant { get; }
        public DateTime ReferenceTime { get; set; }
        public BackgroundTaskSettings Settings { get; set; }
        public BackgroundTaskState State { get; set; }
        public bool Released { get; set; }
        public bool Updated { get; set; }

        public DateTime GetNextStartTime()
        {
            return CrontabSchedule.Parse(Settings.Schedule).GetNextOccurrence(ReferenceTime);
        }

        public bool CanRun()
        {
            State.NextStartTime = GetNextStartTime();

            if (DateTime.UtcNow >= State.NextStartTime)
            {
                if (Settings.Enable && !Released && State.Status == BackgroundTaskStatus.Idle)
                {
                    return true;
                }

                ReferenceTime = DateTime.UtcNow;
                State.NextStartTime = GetNextStartTime();
            }

            return false;
        }

        public void Run()
        {
            State.Status = BackgroundTaskStatus.Running;
            State.LastStartTime = ReferenceTime = DateTime.UtcNow;
            State.NextStartTime = GetNextStartTime();
            State.StartCount += 1;
        }

        public void Idle()
        {
            if (State.Status == BackgroundTaskStatus.Running)
            {
                State.Status = BackgroundTaskStatus.Idle;
            }
        }

        public void Fault(Exception exception)
        {
            Idle();
            State.FaultMessage = DateTime.UtcNow.ToString() + ' ' + exception.Message;
        }

        public BackgroundTaskScheduler Clone()
        {
            return new BackgroundTaskScheduler(Tenant, State.Name, ReferenceTime)
            {
                Settings = Settings.Clone(),
                State = State.Clone()
            };
        }

        public BackgroundTaskScheduler Clone(Action<BackgroundTaskScheduler> action)
        {
            var scheduler = Clone();
            action(scheduler);
            return scheduler;
        }
    }
}
