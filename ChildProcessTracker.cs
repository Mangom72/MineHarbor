using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static class ChildProcessTracker
{
	private static readonly SafeJobHandle s_jobHandle;

	static ChildProcessTracker()
	{
		if (Environment.OSVersion.Version < new Version(6, 2))
		{
			return; // 중첩 Job은 Windows 8 이상부터 지원
		}

		s_jobHandle = CreateJobObject(IntPtr.Zero, null);
		if (s_jobHandle == null || s_jobHandle.IsInvalid)
		{
			return;
		}

		var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION
		{
			LimitFlags = 0x2000 // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
		};
		var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			BasicLimitInformation = info
		};

		int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
		IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
		try
		{
			Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);
			if (!SetInformationJobObject(s_jobHandle, JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr, (uint)length))
			{
				s_jobHandle.Dispose();
			}
		}
		finally
		{
			Marshal.FreeHGlobal(extendedInfoPtr);
		}
	}

	public static bool TryAssignProcess(Process process, out int errorCode)
	{
		errorCode = 0;
		if (s_jobHandle == null || s_jobHandle.IsInvalid || s_jobHandle.IsClosed)
		{
			errorCode = -1; // 초기화 안됨 또는 미지원 환경
			return false;
		}

		if (AssignProcessToJobObject(s_jobHandle, process.Handle))
		{
			return true;
		}

		errorCode = Marshal.GetLastWin32Error();
		return false;
	}

	private sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		public SafeJobHandle() : base(true) { }
		protected override bool ReleaseHandle()
		{
			return CloseHandle(handle);
		}
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern SafeJobHandle CreateJobObject(IntPtr lpJobAttributes, string lpName);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetInformationJobObject(SafeJobHandle hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool AssignProcessToJobObject(SafeJobHandle job, IntPtr process);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CloseHandle(IntPtr hObject);

	private enum JobObjectInfoType
	{
		ExtendedLimitInformation = 9
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
	{
		public Int64 PerProcessUserTimeLimit;
		public Int64 PerJobUserTimeLimit;
		public UInt32 LimitFlags;
		public UIntPtr MinimumWorkingSetSize;
		public UIntPtr MaximumWorkingSetSize;
		public UInt32 ActiveProcessLimit;
		public Int64 Affinity;
		public UInt32 PriorityClass;
		public UInt32 SchedulingClass;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct IO_COUNTERS
	{
		public UInt64 ReadOperationCount;
		public UInt64 WriteOperationCount;
		public UInt64 OtherOperationCount;
		public UInt64 ReadTransferCount;
		public UInt64 WriteTransferCount;
		public UInt64 OtherTransferCount;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
	{
		public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
		public IO_COUNTERS IoInfo;
		public UIntPtr ProcessMemoryLimit;
		public UIntPtr JobMemoryLimit;
		public UIntPtr PeakProcessMemoryUsed;
		public UIntPtr PeakJobMemoryUsed;
	}
}
