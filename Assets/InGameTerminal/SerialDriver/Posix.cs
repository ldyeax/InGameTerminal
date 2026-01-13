#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System;
using System.Runtime.InteropServices;

namespace InGameTerminal.SerialDriver
{
	public sealed class SerialDriver : ISerialDriver
	{
		private int _fd = -1;

		public bool IsOpen => _fd >= 0;

		public void Open(string path, int baud)
		{
			// O_RDWR | O_NOCTTY | O_NONBLOCK
			_fd = open(path, O_RDWR | O_NOCTTY | O_NONBLOCK);
			if (_fd < 0)
				throw new InvalidOperationException($"Failed to open {path}, errno={Marshal.GetLastWin32Error()}");

			// Get current termios settings
			if (tcgetattr(_fd, out var tty) != 0)
			{
				int err = Marshal.GetLastWin32Error();
				close(_fd);
				_fd = -1;
				throw new InvalidOperationException($"tcgetattr failed, errno={err}");
			}

			// Set baud rate
			uint speed = BaudToSpeed(baud);
			cfsetispeed(ref tty, speed);
			cfsetospeed(ref tty, speed);

			// Configure for raw mode (8N1, no flow control)
			// c_cflag: enable receiver, local mode, 8 bits, no parity, 1 stop bit
			tty.c_cflag = (tty.c_cflag & ~(CSIZE | PARENB | CSTOPB)) | CS8 | CREAD | CLOCAL;
			
			// c_iflag: no input processing
			tty.c_iflag &= ~(IGNBRK | BRKINT | PARMRK | ISTRIP | INLCR | IGNCR | ICRNL | IXON | IXOFF | IXANY);
			
			// c_lflag: no local processing (non-canonical mode)
			tty.c_lflag &= ~(ECHO | ECHONL | ICANON | ISIG | IEXTEN);
			
			// c_oflag: no output processing
			tty.c_oflag &= ~OPOST;

			// c_cc: read returns immediately with whatever is available
			tty.c_cc_VMIN = 0;
			tty.c_cc_VTIME = 1; // 0.1 second timeout

			if (tcsetattr(_fd, TCSANOW, ref tty) != 0)
			{
				int err = Marshal.GetLastWin32Error();
				close(_fd);
				_fd = -1;
				throw new InvalidOperationException($"tcsetattr failed, errno={err}");
			}

			// Clear non-blocking after configuration
			int flags = fcntl(_fd, F_GETFL, 0);
			fcntl(_fd, F_SETFL, flags & ~O_NONBLOCK);
		}

		public int Read(byte[] buffer, int offset, int count)
		{
			if (_fd < 0) return 0;

			IntPtr ptr = Marshal.AllocHGlobal(count);
			try
			{
				int bytesRead = (int)read(_fd, ptr, (IntPtr)count);
				if (bytesRead > 0)
				{
					Marshal.Copy(ptr, buffer, offset, bytesRead);
				}
				return bytesRead > 0 ? bytesRead : 0;
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}
		}

		public void Write(byte[] buffer, int offset, int count)
		{
			if (_fd < 0) return;

			IntPtr ptr = Marshal.AllocHGlobal(count);
			try
			{
				Marshal.Copy(buffer, offset, ptr, count);
				int written = (int)write(_fd, ptr, (IntPtr)count);
				if (written < 0)
					throw new InvalidOperationException($"write failed, errno={Marshal.GetLastWin32Error()}");
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}
		}

		public void Close()
		{
			if (_fd >= 0)
			{
				close(_fd);
				_fd = -1;
			}
		}

		public void Dispose()
		{
			Close();
		}

		private static uint BaudToSpeed(int baud)
		{
			return baud switch
			{
				50 => B50,
				75 => B75,
				110 => B110,
				134 => B134,
				150 => B150,
				200 => B200,
				300 => B300,
				600 => B600,
				1200 => B1200,
				1800 => B1800,
				2400 => B2400,
				4800 => B4800,
				9600 => B9600,
				19200 => B19200,
				38400 => B38400,
				57600 => B57600,
				115200 => B115200,
				230400 => B230400,
				_ => B19200
			};
		}

		// libc interop
		private const string LIBC = "libc";

		// open flags
		private const int O_RDWR = 0x0002;
		private const int O_NOCTTY = 0x0100;
		private const int O_NONBLOCK = 0x0800;

		// fcntl commands
		private const int F_GETFL = 3;
		private const int F_SETFL = 4;

		// tcsetattr actions
		private const int TCSANOW = 0;

		// c_cflag bits
		private const uint CSIZE = 0x0030;
		private const uint CS8 = 0x0030;
		private const uint CSTOPB = 0x0040;
		private const uint CREAD = 0x0080;
		private const uint PARENB = 0x0100;
		private const uint CLOCAL = 0x0800;

		// c_iflag bits
		private const uint IGNBRK = 0x0001;
		private const uint BRKINT = 0x0002;
		private const uint PARMRK = 0x0008;
		private const uint ISTRIP = 0x0020;
		private const uint INLCR = 0x0040;
		private const uint IGNCR = 0x0080;
		private const uint ICRNL = 0x0100;
		private const uint IXON = 0x0400;
		private const uint IXOFF = 0x1000;
		private const uint IXANY = 0x0800;

		// c_lflag bits
		private const uint ECHO = 0x0008;
		private const uint ECHONL = 0x0040;
		private const uint ICANON = 0x0002;
		private const uint ISIG = 0x0001;
		private const uint IEXTEN = 0x8000;

		// c_oflag bits
		private const uint OPOST = 0x0001;

		// Baud rate constants (Linux values)
		private const uint B50 = 0x0001;
		private const uint B75 = 0x0002;
		private const uint B110 = 0x0003;
		private const uint B134 = 0x0004;
		private const uint B150 = 0x0005;
		private const uint B200 = 0x0006;
		private const uint B300 = 0x0007;
		private const uint B600 = 0x0008;
		private const uint B1200 = 0x0009;
		private const uint B1800 = 0x000A;
		private const uint B2400 = 0x000B;
		private const uint B4800 = 0x000C;
		private const uint B9600 = 0x000D;
		private const uint B19200 = 0x000E;
		private const uint B38400 = 0x000F;
		private const uint B57600 = 0x1001;
		private const uint B115200 = 0x1002;
		private const uint B230400 = 0x1003;

		[DllImport(LIBC, SetLastError = true)]
		private static extern int open(string pathname, int flags);

		[DllImport(LIBC, SetLastError = true)]
		private static extern int close(int fd);

		[DllImport(LIBC, SetLastError = true)]
		private static extern IntPtr read(int fd, IntPtr buf, IntPtr count);

		[DllImport(LIBC, SetLastError = true)]
		private static extern IntPtr write(int fd, IntPtr buf, IntPtr count);

		[DllImport(LIBC, SetLastError = true)]
		private static extern int fcntl(int fd, int cmd, int arg);

		[DllImport(LIBC, SetLastError = true)]
		private static extern int tcgetattr(int fd, out Termios termios);

		[DllImport(LIBC, SetLastError = true)]
		private static extern int tcsetattr(int fd, int optional_actions, ref Termios termios);

		[DllImport(LIBC)]
		private static extern void cfsetispeed(ref Termios termios, uint speed);

		[DllImport(LIBC)]
		private static extern void cfsetospeed(ref Termios termios, uint speed);

		// Termios structure - simplified for serial port use
		// Note: actual layout varies by platform; this is Linux x86_64
		[StructLayout(LayoutKind.Sequential)]
		private struct Termios
		{
			public uint c_iflag;
			public uint c_oflag;
			public uint c_cflag;
			public uint c_lflag;
			public byte c_line;
			// c_cc array - we only care about VMIN and VTIME
			private byte c_cc_0, c_cc_1, c_cc_2, c_cc_3;
			public byte c_cc_VTIME; // index 5
			public byte c_cc_VMIN;  // index 6
			private byte c_cc_7, c_cc_8, c_cc_9, c_cc_10, c_cc_11;
			private byte c_cc_12, c_cc_13, c_cc_14, c_cc_15, c_cc_16;
			private byte c_cc_17, c_cc_18, c_cc_19, c_cc_20, c_cc_21;
			private byte c_cc_22, c_cc_23, c_cc_24, c_cc_25, c_cc_26;
			private byte c_cc_27, c_cc_28, c_cc_29, c_cc_30, c_cc_31;
			public uint c_ispeed;
			public uint c_ospeed;
		}
	}
}
#endif
