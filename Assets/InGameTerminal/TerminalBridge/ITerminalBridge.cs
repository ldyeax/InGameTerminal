using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGameTerminal
{
	public interface ITerminalBridge
	{
		public bool Update(Terminal terminal, bool redraw);
	}
}