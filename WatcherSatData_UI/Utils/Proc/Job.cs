using System;
using System.Runtime.InteropServices;

namespace WatcherSatData_UI.Utils.Proc
{
    public enum JobObjectInfoType
    {
        AssociateCompletionPortInformation = 7,
        BasicLimitInformation = 2,
        BasicUIRestrictions = 4,
        EndOfJobTimeInformation = 6,
        ExtendedLimitInformation = 9,
        SecurityLimitInformation = 5,
        GroupInformation = 11
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public short LimitFlags;
        public uint MinimumWorkingSetSize;
        public uint MaximumWorkingSetSize;
        public short ActiveProcessLimit;
        public long Affinity;
        public short PriorityClass;
        public short SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public uint ProcessMemoryLimit;
        public uint JobMemoryLimit;
        public uint PeakProcessMemoryUsed;
        public uint PeakJobMemoryUsed;
    }

    public class Job : IDisposable
    {
        private bool m_disposed;

        private IntPtr m_handle;

        public Job()
        {
            m_handle = CreateJobObject(IntPtr.Zero, null);

            var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION();
            info.LimitFlags = 0x2000;

            var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            extendedInfo.BasicLimitInformation = info;

            var length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            var extendedInfoPtr = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

            if (!SetInformationJobObject(m_handle, JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr,
                (uint) length))
                throw new Exception(
                    string.Format("Unable to set information.  Error: {0}", Marshal.GetLastWin32Error()));
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject(IntPtr a, string lpName);

        [DllImport("kernel32.dll")]
        private static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoType infoType,
            IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        private void Dispose(bool disposing)
        {
            if (m_disposed)
                return;

            if (disposing)
            {
            }

            Close();
            m_disposed = true;
        }

        public void Close()
        {
            CloseHandle(m_handle);
            m_handle = IntPtr.Zero;
        }

        public bool AddProcess(IntPtr handle)
        {
            return AssignProcessToJobObject(m_handle, handle);
        }
    }
}