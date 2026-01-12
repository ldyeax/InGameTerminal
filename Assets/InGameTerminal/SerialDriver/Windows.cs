#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace InGameTerminal.SerialDriver
{
	public sealed class SerialDriver : ISerialDriver
	{
		private SafeFileHandle _handle;

		public void Open(string portName /* e.g. "COM3" */, int baud = 115200)
		{
			string path = portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
				? (int.TryParse(portName.AsSpan(3), out var n) && n >= 10 ? @"\\.\" + portName : portName)
				: portName;

			_handle = CreateFile(path,
				GENERIC_READ | GENERIC_WRITE,
				0,
				IntPtr.Zero,
				OPEN_EXISTING,
				FILE_ATTRIBUTE_NORMAL,
				IntPtr.Zero);

			if (_handle.IsInvalid)
				throw new InvalidOperationException($"CreateFile failed for {path}, err={Marshal.GetLastWin32Error()}");

			// You MUST configure the port (baud, parity, etc.)
			if (!GetCommState(_handle, out var dcb))
				throw new InvalidOperationException($"GetCommState failed err={Marshal.GetLastWin32Error()}");

			dcb.BaudRate = (uint)baud;
			dcb.ByteSize = 8;
			dcb.Parity = 0;   // N
			dcb.StopBits = 0; // 1

			if (!SetCommState(_handle, ref dcb))
				throw new InvalidOperationException($"SetCommState failed err={Marshal.GetLastWin32Error()}");

			// Timeouts are important or ReadFile will block forever
			var timeouts = new COMMTIMEOUTS
			{
				ReadIntervalTimeout = 1,
				ReadTotalTimeoutConstant = 1,
				ReadTotalTimeoutMultiplier = 1,
				WriteTotalTimeoutConstant = 50,
				WriteTotalTimeoutMultiplier = 10
			};
			if (!SetCommTimeouts(_handle, ref timeouts))
				throw new InvalidOperationException($"SetCommTimeouts failed err={Marshal.GetLastWin32Error()}");
		}

		public int Read(byte[] buffer, int offset, int count)
		{
			if (_handle == null || _handle.IsInvalid) return 0;
			if (!ReadFile(_handle, buffer, count, out int bytesRead, IntPtr.Zero))
				return 0;
			// NOTE: this reads into buffer[0..count]. For offset support, copy/slice yourself.
			return bytesRead;
		}

		public void Write(byte[] buffer, int offset, int count)
		{
			if (_handle == null || _handle.IsInvalid) return;
			if (!WriteFile(_handle, buffer, count, out int written, IntPtr.Zero))
				throw new InvalidOperationException($"WriteFile failed err={Marshal.GetLastWin32Error()}");
		}

		public void Dispose()
		{
			_handle?.Dispose();
			_handle = null;
		}

		// Win32 interop
		private const uint GENERIC_READ = 0x80000000;
		private const uint GENERIC_WRITE = 0x40000000;
		private const uint OPEN_EXISTING = 3;
		private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern SafeFileHandle CreateFile(
			string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			IntPtr lpSecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool ReadFile(SafeFileHandle hFile, byte[] lpBuffer, int nNumberOfBytesToRead,
			out int lpNumberOfBytesRead, IntPtr lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool WriteFile(SafeFileHandle hFile, byte[] lpBuffer, int nNumberOfBytesToWrite,
			out int lpNumberOfBytesWritten, IntPtr lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool GetCommState(SafeFileHandle hFile, out DCB lpDCB);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetCommState(SafeFileHandle hFile, ref DCB lpDCB);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetCommTimeouts(SafeFileHandle hFile, ref COMMTIMEOUTS lpCommTimeouts);

		[StructLayout(LayoutKind.Sequential)]
		private struct DCB
		{
			public uint DCBlength;
			public uint BaudRate;
			public uint Flags; // bitfield (parity, flow control, etc.) — real impl must set bits carefully
			public ushort wReserved;
			public ushort XonLim;
			public ushort XoffLim;
			public byte ByteSize;
			public byte Parity;
			public byte StopBits;
			public sbyte XonChar;
			public sbyte XoffChar;
			public sbyte ErrorChar;
			public sbyte EofChar;
			public sbyte EvtChar;
			public ushort wReserved1;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct COMMTIMEOUTS
		{
			public uint ReadIntervalTimeout;
			public uint ReadTotalTimeoutMultiplier;
			public uint ReadTotalTimeoutConstant;
			public uint WriteTotalTimeoutMultiplier;
			public uint WriteTotalTimeoutConstant;
		}
	}
#endif

}
