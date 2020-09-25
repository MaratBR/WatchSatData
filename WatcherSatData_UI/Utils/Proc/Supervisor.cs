﻿using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace WatcherSatData_UI.Utils.Proc
{
    class Supervisor : IDisposable
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public Process Process { get; private set; }
        private ProcessStartInfo _info;
        private Thread observerThread;
        private int? processId;
        private Job job = new Job();

        public Supervisor(ProcessStartInfo info)
        {
            _info = info;

            Start();
        }

        private void StartObserver()
        {
            logger.Debug("Запускаю поток-наблюдатель...");
            observerThread = new Thread((object s) => ObserveProcess((SynchronizationContext)s))
            {
                Name = "ProcessObserver",
                IsBackground = true,
            };
            observerThread.Start(SynchronizationContext.Current);
            if (processId != null)
            {
                var proc = Process.GetProcessById((int)processId);
                job.AddProcess(proc.Handle);
            }
        }

        private void ObserveProcess(SynchronizationContext context)
        {
            logger.Debug($"Наблюдаю за дочерним процессом PID={processId}");
            while (processId != null)
            {
                try
                {
                    Process.GetProcessById((int)processId);
                }
                catch(ArgumentException)
                {
                    logger.Debug($"Процесс PID={processId} умер, перезапуск ...");
                    context.Post(_s => Restart(), null);
                }
                Thread.Sleep(10000);
            }
        }

        public void Restart()
        {
            if (Process != null)
            {
                if (!Process.HasExited)
                    Process.Kill();
                Process = null;
            }
            Start();
        }

        private void Start()
        {
            logger.Debug($"Запуск дочернего процесса {_info.FileName} ...");

            Process = Process.Start(_info);
            if (processId == null)
            {
                processId = Process.Id;
                StartObserver();
            }
            else
            {
                processId = Process.Id;
            }

            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            Process.Exited += Process_Exited;
        }
        private void UnsubscribeFromEvents()
        {
            Process.Exited -= Process_Exited;
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            UnsubscribeFromEvents();
        }

        public void Dispose()
        {
            if (Process != null)
            {
                UnsubscribeFromEvents();
                if (!Process.HasExited)
                    Process.Kill();
                Process.Dispose();
            }
        }
    }
}
