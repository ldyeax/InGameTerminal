using System;

namespace InGameTerminal.SerialDriver
{
	public interface ISerialDriver : IDisposable
	{
		void Open(string path, int baud);
		int Read(byte[] buffer, int offset, int count);
		void Write(byte[] buffer, int offset, int count);
		void Close();
	}
}