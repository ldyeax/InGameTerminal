using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InGameTerminal.TerminalBridge
{
	/// <summary>
	/// VT320 terminal bridge - converts terminal commands to VT320 escape sequences
	/// </summary>
	public class VT320 : ITerminalBridge
	{
		SerialDevice serialDevice = default;

		private TerminalState terminalState;

		private readonly object readyLock = new object();

		private List<TerminalCommand> terminalCommands = new List<TerminalCommand>();

		private bool firstUpdate = true;

		// VT320 control codes
		private const byte ESC = 0x1B;  // Escape
		private const byte CSI_7BIT = 0x5B; // '[' - used after ESC for 7-bit CSI
		
		// Common escape sequence prefixes
		private static readonly byte[] CSI = new byte[] { ESC, CSI_7BIT }; // ESC [

		void ITerminalBridge.Update(Terminal terminal)
		{
			if (Monitor.TryEnter(readyLock))
			{
				try
				{
					terminal.BuildBuffer(ref terminalState, firstUpdate);
					terminal.BuildTerminalCommands(ref terminalState, terminalCommands, firstUpdate);
					firstUpdate = false;
				}
				finally
				{
					Monitor.Exit(readyLock);
				}
			}
		}

		/// <summary>
		/// Converts a TerminalCommand to VT320 escape sequence bytes
		/// </summary>
		private byte[] CommandToBytes(TerminalCommand command)
		{
			switch (command.CommandType)
			{
				case TerminalCommandType.Char:
					// Regular character - just send the byte
					// For characters > 127, this assumes the terminal is set up for the appropriate character set
					if (command.Char < 128)
					{
						return new byte[] { (byte)command.Char };
					}
					else
					{
						
						// Extended characters - send as-is (assumes 8-bit mode)
						return Encoding.Latin1.GetBytes(new char[] { command.Char });
					}

				case TerminalCommandType.Up:
					// CUU - Cursor Up: CSI Pn A
					return new byte[] { ESC, CSI_7BIT, (byte)'A' };

				case TerminalCommandType.Down:
					// CUD - Cursor Down: CSI Pn B
					return new byte[] { ESC, CSI_7BIT, (byte)'B' };

				case TerminalCommandType.Right:
					// CUF - Cursor Forward (Right): CSI Pn C
					return new byte[] { ESC, CSI_7BIT, (byte)'C' };

				case TerminalCommandType.Left:
					// CUB - Cursor Backward (Left): CSI Pn D
					return new byte[] { ESC, CSI_7BIT, (byte)'D' };

				case TerminalCommandType.MoveTo:
					// CUP - Cursor Position: CSI Pl ; Pc H
					// VT320 uses 1-based coordinates
					string moveSeq = $"\x1B[{command.Y + 1};{command.X + 1}H";
					return Encoding.ASCII.GetBytes(moveSeq);

				case TerminalCommandType.CarriageReturn:
					// CR - Carriage Return
					return new byte[] { 0x0D };

				case TerminalCommandType.LineFeed:
					// LF - Line Feed
					return new byte[] { 0x0A };

				case TerminalCommandType.Italic:
					// SGR 3 for italic on, SGR 23 for italic off
					// VT320 may not support italic, but we send it anyway
					return new byte[] { ESC, CSI_7BIT, (byte)'3', (byte)'m' };

				case TerminalCommandType.Bold:
					// SGR 1 for bold on, SGR 22 for bold off
					return new byte[] { ESC, CSI_7BIT, (byte)'1', (byte)'m' };

				case TerminalCommandType.Underline:
					// SGR 4 for underline on, SGR 24 for underline off
					return new byte[] { ESC, CSI_7BIT, (byte)'4', (byte)'m' };

				case TerminalCommandType.Invert:
					// SGR 7 for reverse video on, SGR 27 for reverse off
					return new byte[] { ESC, CSI_7BIT, (byte)'7', (byte)'m' };

				case TerminalCommandType.Blink:
					// SGR 5 for blink on, SGR 25 for blink off
					return new byte[] { ESC, CSI_7BIT, (byte)'5', (byte)'m' };

				case TerminalCommandType.EL:
					// EL - Erase in Line: CSI Ps K
					// Ps = 0: erase from cursor to end of line (default)
					return new byte[] { ESC, CSI_7BIT, (byte)'K' };

				case TerminalCommandType.EraseInDisplay:
					// ED - Erase in Display: CSI Ps J
					// Ps = 0: erase from cursor to end of display (default)
					return new byte[] { ESC, CSI_7BIT, (byte)'J' };

				case TerminalCommandType.HomeCursor:
					// CUP with no parameters moves to home (1,1)
					return new byte[] { ESC, CSI_7BIT, (byte)'H' };

				default:
					return Array.Empty<byte>();
			}
		}

		void ThreadLoop()
		{
			while (true)
			{
				if (Monitor.TryEnter(readyLock, TimeSpan.FromMilliseconds(100)))
				{
					try
					{
						if (serialDevice.IsOpen)
						{
							// Write commands to VT320
							foreach (var command in terminalCommands)
							{
								byte[] data = CommandToBytes(command);
								if (data.Length > 0 && serialDevice.IsOpen)
								{
									serialDevice.Write(data, 0, data.Length);
								}
							}
							terminalCommands.Clear();
						}
					}
					finally
					{
						Monitor.Exit(readyLock);
					}
				}
			}
		}

		public VT320(SerialDevice serialDevice)
		{
			this.serialDevice = serialDevice;
			new Thread(ThreadLoop)
			{
				IsBackground = true
			}.Start();
		}
	}
}
