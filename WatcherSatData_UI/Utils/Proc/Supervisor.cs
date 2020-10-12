using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using NLog;

namespace WatcherSatData_UI.Utils.Proc
{
    internal class Supervisor : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly ProcessStartInfo _info;
        private readonly Job job = new Job();
        private Thread observerThread;
        private int? processId;

        public Supervisor(ProcessStartInfo info)
        {
            _info = info;
        }

        public Process Process { get; private set; }

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

        public event EventHandler<SupervisorStateChangedEventArgs> StateChanged;

        private void StartObserver()
        {
            logger.Debug("Запускаю поток-наблюдатель...");
            observerThread = new Thread(s => ObserveProcess((SynchronizationContext) s))
            {
                Name = "ProcessObserver",
                IsBackground = true
            };
            observerThread.Start(SynchronizationContext.Current);
            if (processId != null)
            {
                var proc = Process.GetProcessById((int) processId);
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
                    Process.GetProcessById((int) processId);
                }
                catch (ArgumentException)
                {
                    logger.Debug($"Процесс PID={processId} умер, перезапуск ...");
                    context.Post(_s =>
                    {
                        if (Process == null || !Process.HasExited)
                            // примечание: если условие выше не выполнятеся это означает, что дочерний процесс
                            // либо завершился по нормальному (а значит событие StateChanged было вызвано в Process_Exited), 
                            // либо что произошла какая-то дичь (скорее первое)
                            StateChanged?.Invoke(this, new SupervisorStateChangedEventArgs {IsAlive = false});
                        Restart();
                    }, null);
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

        public void Start()
        {
            logger.Debug($"Запуск дочернего процесса {_info.FileName} ...");

            try
            {
                Process = Process.Start(_info);
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode == 1223)
                    StateChanged?.Invoke(this, new SupervisorStateChangedEventArgs { IsRejectedByUser = true });
                logger.Error(e.ToString());
                return;
            }
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
            StateChanged?.Invoke(this, new SupervisorStateChangedEventArgs {IsAlive = true});
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
            StateChanged?.Invoke(this, new SupervisorStateChangedEventArgs {IsAlive = false});
            UnsubscribeFromEvents();
        }
    }
}