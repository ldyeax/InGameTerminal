using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using InGameTerminal.SerialDriver;

namespace InGameTerminal.TerminalBridge
{
	/// <summary>
	/// VT320 terminal bridge - converts terminal commands to VT320 escape sequences
	/// </summary>
	public class VT320 : ITerminalBridge, IDisposable
	{
		private readonly ISerialDriver _serialDriver;
		private volatile bool _running = true;

		private TerminalState terminalState;

		private readonly object readyLock = new object();

		private List<TerminalCommand> terminalCommands = new List<TerminalCommand>();

		private bool redraw = true;

		// VT320 control codes
		private const byte ESC = 0x1B;  // Escape
		private const byte CSI_7BIT = 0x5B; // '[' - used after ESC for 7-bit CSI
		private const byte SO = 0x0E;  // Shift Out - invoke G1 character set
		private const byte SI = 0x0F;  // Shift In - invoke G0 character set
		
		// Common escape sequence prefixes
		private static readonly byte[] CSI = new byte[] { ESC, CSI_7BIT }; // ESC [

		// Designate G0 as DEC Special Graphics: ESC ( 0
		private static readonly byte[] DESIGNATE_G0_GRAPHICS = new byte[] { ESC, (byte)'(', (byte)'0' };
		// Designate G0 as ASCII: ESC ( B
		private static readonly byte[] DESIGNATE_G0_ASCII = new byte[] { ESC, (byte)'(', (byte)'B' };

		// ISO-8859-1 (Latin1) encoding for extended characters
		private static readonly Encoding Latin1 = Encoding.GetEncoding("iso-8859-1");

		bool readyForUpdate = true;
		bool readyToDraw = false;

		int lastProcessedcommands = 0;
		int lastLast = -1;
		int lastQueue = -1;

		bool ITerminalBridge.Update(Terminal terminal, bool redraw)
		{
			this.redraw = this.redraw || redraw;
			if (readyForUpdate)
			{
				if (lastProcessedcommands > 0)
				{
					if (lastProcessedcommands != lastLast)
					{
						UnityEngine.Debug.Log($"VT320: Processed {lastProcessedcommands} commands at {UnityEngine.Time.time}");
						lastLast = lastProcessedcommands;
					}
					lastProcessedcommands = 0;
				}
				readyForUpdate = false;
				terminal.BuildBuffer(ref terminalState);
				terminal.BuildTerminalCommands(ref terminalState, terminalCommands, this.redraw);

				if (terminalCommands.Count != lastQueue)
				{
					StringBuilder terminalCommandsDebug = new StringBuilder();
					terminalCommandsDebug.Append("VT320: Queued commands: [");
					foreach (var cmd in terminalCommands)
					{
						terminalCommandsDebug.Append($"{cmd.CommandType} {cmd.X} {cmd.Y}, ");
					}
					terminalCommandsDebug.Append("]");
					UnityEngine.Debug.Log($"VT320: Queued {terminalCommands.Count} commands at {UnityEngine.Time.time}: {terminalCommandsDebug}");
					lastQueue = terminalCommands.Count;
				}

				this.redraw = false;
				readyToDraw = true;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Converts a TerminalCommand to VT320 escape sequence bytes
		/// </summary>
		private byte[] CommandToBytes(TerminalCommand command)
		{
			switch (command.CommandType)
			{
				case TerminalCommandType.Byte:
					return new byte[] { (byte)command.X };
				case TerminalCommandType.Char:
					if (command.X == 0)
					{
						command.X = 0x20; // Replace null with space
					}
					// parse as Latin1
					return Latin1.GetBytes(new char[] { (char)command.X });
				//case TerminalCommandType.Byte:
				//	return new byte[] { (byte)command.X };
				case TerminalCommandType.InitBanks:
					// Initialize character banks: G0 as ASCII, G1 as DEC Special Graphics
					return new byte[]
					{
						ESC, (byte)'(', (byte)'B', // Designate G0 as ASCII
						ESC, (byte)')', (byte)'0'  // Designate G1 as DEC Special Graphics
					};
				case TerminalCommandType.CharacterBank:
					TerminalCharacterBank bank = (TerminalCharacterBank)command.X;
					switch (bank)
					{
						case TerminalCharacterBank.ASCII:
							return new byte[] { SI }; // Shift In - invoke G0 (ASCII)
						case TerminalCharacterBank.G1:
							return new byte[] { SO }; // Shift Out - invoke G1 (DEC Special Graphics)
					}
					return Array.Empty<byte>();
/**
 *Once DEC Special Graphics is selected (via G0+ESC(0 or G1+ESC)0 + SO), these ASCII bytes become line/corner symbols:
 *
 * x = vertical line
 *
 * q = horizontal line
 *
 * l = upper-left corner
 *
 * k = upper-right corner
 *
 * m = lower-left corner
 *
 * j = lower-right corner
 *
 * n = cross / plus
 *
 * t / u / v / w = tees (left/right/bottom/top) (depending on exact DEC set)
 * 
 **/
				case TerminalCommandType.Box_Horizontal:
					return new byte[] { (byte)'q' };

				case TerminalCommandType.Box_Vertical:
					return new byte[] { (byte)'x' };

				case TerminalCommandType.Box_TopLeftCorner:
					return new byte[] { (byte)'l' };

				case TerminalCommandType.Box_TopRightCorner:
					return new byte[] { (byte)'k' };

				case TerminalCommandType.Box_BottomLeftCorner:
					return new byte[] { (byte)'m' };

				case TerminalCommandType.Box_BottomRightCorner:
					return new byte[] { (byte)'j' };

				case TerminalCommandType.Box_Cross:
					return new byte[] { (byte)'n' };

				case TerminalCommandType.Box_LeftTee:
					return new byte[] { (byte)'u' };

				case TerminalCommandType.Box_RightTee:
					return new byte[] { (byte)'t' };

				case TerminalCommandType.Box_UpTee:
					return new byte[] { (byte)'v' };

				case TerminalCommandType.Box_DownTee:
					return new byte[] { (byte)'w' };

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

				case TerminalCommandType.EL_CursorToEnd:
					// EL - Erase in Line: CSI Ps K
					// Ps = 0: erase from cursor to end of line (default)
					return new byte[] { ESC, CSI_7BIT, (byte)'K' };

				case TerminalCommandType.EL_BeginningToCursor:
					// EL - Erase in Line: CSI Ps K
					// Ps = 1: erase from beginning of line to cursor
					return new byte[] { ESC, CSI_7BIT, (byte)'1', (byte)'K' };

				case TerminalCommandType.EraseInDisplay:
					// ED - Erase in Display: CSI Ps J
					// Ps = 2: erase entire display
					return new byte[] { ESC, CSI_7BIT, (byte)'2', (byte)'J' };

				case TerminalCommandType.HomeCursor:
					// CUP with no parameters moves to home (1,1)
					return new byte[] { ESC, CSI_7BIT, (byte)'H' };

				default:
					return Array.Empty<byte>();
			}
		}

		void ThreadLoop()
		{
			while (_running)
			{
				if (readyToDraw)
				{
					readyToDraw = false;
					if (_serialDriver.IsOpen && terminalCommands.Count > 0)
					{
						// Write commands to VT320
						foreach (var command in terminalCommands)
						{
							byte[] data = CommandToBytes(command);
							if (data.Length > 0 && _serialDriver.IsOpen)
							{
								_serialDriver.Write(data, 0, data.Length);
							}
						}
						lastProcessedcommands = terminalCommands.Count;
						terminalCommands.Clear();
					}
					readyForUpdate = true;
				}
			}
		}

		public VT320(ISerialDriver serialDriver)
		{
			terminalState = new TerminalState()
			{
				terminalBuffer = new TerminalBufferValue[80, 24],
				previousTerminalBuffer = new TerminalBufferValue[80, 24]
			};
			_serialDriver = serialDriver ?? throw new ArgumentNullException(nameof(serialDriver));
			new Thread(ThreadLoop)
			{
				IsBackground = true
			}.Start();
		}

		public void Dispose()
		{
			_running = false;
			_serialDriver?.Dispose();
		}
	}
}
