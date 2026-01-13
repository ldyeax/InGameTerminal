using InGameTerminal;
using InGameTerminal.Elements;
using InGameTerminal.SerialDriver;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace InGameTerminal
{

	[ExecuteAlways]
	public sealed class Terminal : MonoBehaviour
	{
		private const string RootMenu = "GameObject/InGameTerminal/";
		[MenuItem(RootMenu + "Terminal Screen", false, 10)]
		public static void CreateScreenCanvas(MenuCommand cmd)
		{
			var go = new GameObject("Terminal Screen", typeof(Terminal));
			GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
			Undo.RegisterCreatedObjectUndo(go, "Create Terminal Screen");
			Selection.activeObject = go;
			EditorSceneManager.MarkSceneDirty(go.scene);
		}

		[SerializeField]
		public int Width = 80;
		[SerializeField]
		public int Height = 24;
		[SerializeField]
		public UnityTerminalDefinitionBase TerminalDefinition;

		[SerializeField]
		private Canvas _unityCanvas;
		public Canvas GetCanvas()
		{
			EnsureSetup();
			return _unityCanvas;
		}
		[SerializeField]
		private RectTransform _rectTransform;
		public RectTransform RectTransform => _rectTransform;

		private SerialDriver.SerialDriver testSerialDriver = null;
		private ITerminalBridge testTerminalBridge;
		private void EnsureSetup()
		{
			_unityCanvas = Util.GetOrCreateComponent<Canvas>(gameObject);
			_unityCanvas.renderMode = RenderMode.WorldSpace;
			_unityCanvas.pixelPerfect = true;

			CanvasScaler scaler = Util.GetOrCreateComponent<CanvasScaler>(gameObject);
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

			var raycaster = Util.GetOrCreateComponent<GraphicRaycaster>(gameObject);

			_rectTransform = Util.GetOrCreateComponent<RectTransform>(gameObject);
		}

		private void Reset() => EnsureSetup();
		private void OnValidate() => EnsureSetup();
		private void Awake() { if (!Application.isPlaying) EnsureSetup(); }
		public Vector2Int GetTerminalPosition(Element element)
		{
			float x = element.RectTransform.offsetMin.x;
			float y = -element.RectTransform.offsetMax.y;
			Vector2 fRet = new Vector2(x, y);
			fRet.x /= TerminalDefinition.GlyphWidth;
			fRet.y /= TerminalDefinition.GlyphHeight;
			var ret = new Vector2Int((int)fRet.x, (int)fRet.y);
			return ret;
		}

		void Start()
		{

		}
		private int nextConnectorID = 1;
		private void ActualizeConnectedLinesToBuffer(ref TerminalState terminalState)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					ref TerminalBufferValue cell = ref terminalBuffer[x, y];
					if (cell.ConnectorID != 0)
					{
						int aboveID = 0;
						if (y > 0)
						{
							aboveID = terminalBuffer[x, y - 1].ConnectorID;
						}
						int belowID = 0;
						if (y < Height - 1)
						{
							belowID = terminalBuffer[x, y + 1].ConnectorID;
						}
						int leftID = 0;
						if (x > 0)
						{
							leftID = terminalBuffer[x - 1, y].ConnectorID;
						}
						int rightID = 0;
						if (x < Width - 1)
						{
							rightID = terminalBuffer[x + 1, y].ConnectorID;
						}

						bool matchAbove = aboveID == cell.ConnectorID;
						bool matchBelow = belowID == cell.ConnectorID;
						bool matchLeft = leftID == cell.ConnectorID;
						bool matchRight = rightID == cell.ConnectorID;

						if (matchAbove)
						{
							if (matchBelow)
							{
								if (matchLeft)
								{
									if (matchRight)
									{
										// All four directions
										cell.AtlasX = TerminalDefinition.CrossX;
										cell.AtlasY = TerminalDefinition.CrossY;
										cell.TerminalCommandType = TerminalCommandType.Box_Cross;
										cell.CharacterBank = TerminalCharacterBank.G1;
										cell.HasTerminalCommand = true;
									}
									else
									{
										// Above, below, left (no right) - LeftTee
										cell.AtlasX = TerminalDefinition.LeftTeeX;
										cell.AtlasY = TerminalDefinition.LeftTeeY;
										cell.TerminalCommandType = TerminalCommandType.Box_LeftTee;
										cell.CharacterBank = TerminalCharacterBank.G1;
										cell.HasTerminalCommand = true;
									}
								}
								else if (matchRight)
								{
									// Above, below, right (no left) - RightTee
									cell.AtlasX = TerminalDefinition.RightTeeX;
									cell.AtlasY = TerminalDefinition.RightTeeY;
									cell.TerminalCommandType = TerminalCommandType.Box_RightTee;
									cell.CharacterBank = TerminalCharacterBank.G1;
									cell.HasTerminalCommand = true;
								}
								else
								{
									// Above and below only - Vertical line
									cell.AtlasX = TerminalDefinition.VerticalLineX;
									cell.AtlasY = TerminalDefinition.VerticalLineY;
									cell.TerminalCommandType = TerminalCommandType.Box_Vertical;
									cell.CharacterBank = TerminalCharacterBank.G1;
									cell.HasTerminalCommand = true;
								}
							}
							else // no below
							{
								if (matchLeft)
								{
									if (matchRight)
									{
										// Above, left, right (no below) - UpTee
										cell.AtlasX = TerminalDefinition.UpTeeX;
										cell.AtlasY = TerminalDefinition.UpTeeY;
										cell.TerminalCommandType = TerminalCommandType.Box_UpTee;
										cell.CharacterBank = TerminalCharacterBank.G1;
										cell.HasTerminalCommand = true;
									}
									else
									{
										// Above, left (no below, no right) - BottomRightCorner
										cell.AtlasX = TerminalDefinition.BottomRightCornerX;
										cell.AtlasY = TerminalDefinition.BottomRightCornerY;
										cell.TerminalCommandType = TerminalCommandType.Box_BottomRightCorner;
										cell.CharacterBank = TerminalCharacterBank.G1;
										cell.HasTerminalCommand = true;
									}
								}
								else if (matchRight)
								{
									// Above, right (no below, no left) - BottomLeftCorner
									cell.AtlasX = TerminalDefinition.BottomLeftCornerX;
									cell.AtlasY = TerminalDefinition.BottomLeftCornerY;
									cell.TerminalCommandType = TerminalCommandType.Box_BottomLeftCorner;
									cell.CharacterBank = TerminalCharacterBank.G1;
									cell.HasTerminalCommand = true;
								}
								else
								{
									// Above only - Vertical line
									cell.AtlasX = TerminalDefinition.VerticalLineX;
									cell.AtlasY = TerminalDefinition.VerticalLineY;
									cell.TerminalCommandType = TerminalCommandType.Box_Vertical;
									cell.CharacterBank = TerminalCharacterBank.G1;
									cell.HasTerminalCommand = true;
								}
							}
						}
						else // no above
						{
							if (matchBelow)
							{
								if (matchLeft)
								{
									if (matchRight)
									{
										// Below, left, right (no above) - DownTee
										cell.AtlasX = TerminalDefinition.DownTeeX;
										cell.AtlasY = TerminalDefinition.DownTeeY;
										cell.TerminalCommandType = TerminalCommandType.Box_DownTee;
										cell.CharacterBank = TerminalCharacterBank.G1;
										cell.HasTerminalCommand = true;
									}
									else
									{
										// Below, left (no above, no right) - TopRightCorner
										cell.AtlasX = TerminalDefinition.TopRightCornerX;
										cell.AtlasY = TerminalDefinition.TopRightCornerY;
										cell.TerminalCommandType = TerminalCommandType.Box_TopRightCorner;
										cell.CharacterBank = TerminalCharacterBank.G1;
										cell.HasTerminalCommand = true;
									}
								}
								else if (matchRight)
								{
									// Below, right (no above, no left) - TopLeftCorner
									cell.AtlasX = TerminalDefinition.TopLeftCornerX;
									cell.AtlasY = TerminalDefinition.TopLeftCornerY;
									cell.TerminalCommandType = TerminalCommandType.Box_TopLeftCorner;
									cell.CharacterBank = TerminalCharacterBank.G1;
									cell.HasTerminalCommand = true;
								}
								else
								{
									// Below only - Vertical line
									cell.AtlasX = TerminalDefinition.VerticalLineX;
									cell.AtlasY = TerminalDefinition.VerticalLineY;
									cell.TerminalCommandType = TerminalCommandType.Box_Vertical;
									cell.CharacterBank = TerminalCharacterBank.G1;
									cell.HasTerminalCommand = true;
								}
							}
							else // no above, no below
							{
								if (matchLeft)
								{
									if (matchRight)
									{
										// Left and right only - Horizontal line
										cell.AtlasX = TerminalDefinition.HorizontalLineX;
										cell.AtlasY = TerminalDefinition.HorizontalLineY;
										cell.TerminalCommandType = TerminalCommandType.Box_Horizontal;
										cell.CharacterBank = TerminalCharacterBank.G1;
										cell.HasTerminalCommand = true;
									}
									else
									{
										// Left only - Horizontal line
										cell.AtlasX = TerminalDefinition.HorizontalLineX;
										cell.AtlasY = TerminalDefinition.HorizontalLineY;
										cell.TerminalCommandType = TerminalCommandType.Box_Horizontal;
										cell.CharacterBank = TerminalCharacterBank.G1;
										cell.HasTerminalCommand = true;
									}
								}
								else if (matchRight)
								{
									// Right only - Horizontal line
									cell.AtlasX = TerminalDefinition.HorizontalLineX;
									cell.AtlasY = TerminalDefinition.HorizontalLineY;
									cell.TerminalCommandType = TerminalCommandType.Box_Horizontal;
									cell.CharacterBank = TerminalCharacterBank.G1;
									cell.HasTerminalCommand = true;
								}
								else
								{
									// No matches - default to vertical line
									cell.AtlasX = TerminalDefinition.VerticalLineX;
									cell.AtlasY = TerminalDefinition.VerticalLineY;
									cell.TerminalCommandType = TerminalCommandType.Box_Vertical;
									cell.CharacterBank = TerminalCharacterBank.G1;
									cell.HasTerminalCommand = true;
								}
							}
						}
					}
				}
			}
		}
		private List<Element> elementPool = new();
		private List<TerminalCommand> terminalCommands = new List<TerminalCommand>();
		public void BuildTerminalCommands(
			ref TerminalState terminalState,
			List<TerminalCommand> terminalCommands,
			bool redraw
		)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref var previousTerminalBuffer = ref terminalState.previousTerminalBuffer;

			terminalCommands.Clear();
			terminalCommands.Add(new TerminalCommand()
			{
				CommandType = TerminalCommandType.HomeCursor
			});
			terminalCommands.Add(new TerminalCommand()
			{
				CommandType = TerminalCommandType.CharacterBank,
				X = (int)TerminalCharacterBank.ASCII
			});
			if (redraw)
			{
				terminalCommands.Add(new TerminalCommand()
				{
					CommandType = TerminalCommandType.EraseInDisplay
				});
				terminalCommands.Add(new TerminalCommand()
				{
					CommandType = TerminalCommandType.InitBanks
				});
			}

			/**
			 * expectedTerminalCursorPosition tracks where we expect the actual terminal's cursor to have been placed.
			 * If we put down a glyph, we expect the terminal to move the cursor to the right and wrap if necessary.
			 * But just sending an escape sequence etc. does not necessarily move the cursor.
			 * If we skip over a character because it hasn't changed, then later on we will need to move the cursor to catch up to the new position.
			 **/
			Vector2Int expectedTerminalCursorPosition = default;
			// Track the current character bank state of the terminal (starts as ASCII)
			TerminalCharacterBank currentCharacterBank = TerminalCharacterBank.ASCII;

			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					TerminalBufferValue cell = terminalBuffer[x, y];
					if (redraw
						&& !cell.HasTerminalCommand
						&& cell.CharacterBank == TerminalCharacterBank.ASCII
						&& cell.GetChar(TerminalDefinition) == ' '
					)
					{
						continue;
					}
					TerminalBufferValue previousCell = previousTerminalBuffer[x, y];
					bool drawCell = redraw;
					if (!drawCell)
					{
						drawCell = cell != previousCell;
					}
					if (drawCell)
					{
						bool movedCursor = false;
						//Debug.Log($"Cell changed at {x},{y} from '{previousCell.GetChar(TerminalDefinition)}' to '{cell.GetChar(TerminalDefinition)}'");
						if (expectedTerminalCursorPosition.x != x || expectedTerminalCursorPosition.y != y)
						{
							terminalCommands.Add(new TerminalCommand()
							{
								CommandType = TerminalCommandType.MoveTo,
								X = x,
								Y = y
							});
							expectedTerminalCursorPosition.x = x;
							expectedTerminalCursorPosition.y = y;
						}
						// Check against the current terminal state, not the previous cell
						if (currentCharacterBank != cell.CharacterBank)
						{
							terminalCommands.Add(new TerminalCommand()
							{
								CommandType = TerminalCommandType.CharacterBank,
								X = (int)cell.CharacterBank
							});
							currentCharacterBank = cell.CharacterBank;
						}
						if (cell.HasTerminalCommand)
						{
							terminalCommands.Add(new TerminalCommand()
							{
								CommandType = cell.TerminalCommandType,
								X = cell.AtlasX,
								Y = cell.AtlasY
							});
							movedCursor = true;
						}
						else
						{
							byte b = cell.GetByte(TerminalDefinition);
							if (b < 127)
							{
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.Char,
									X = cell.GetChar(TerminalDefinition)
								});
								movedCursor = true;
							}
							else
							{
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.Byte,
									X = cell.GetByte(TerminalDefinition)
								});
								movedCursor = true;
							}
						}



						if (movedCursor)
						{
							expectedTerminalCursorPosition.x++;
							if (expectedTerminalCursorPosition.x >= Width)
							{
								expectedTerminalCursorPosition.x = 0;
								expectedTerminalCursorPosition.y++;
							}
							if (expectedTerminalCursorPosition.y >= Height)
							{
								expectedTerminalCursorPosition.y = 0;
							}
						}
					}
				}
			}
			if (terminalCommands.Count == 1)
			{
				terminalCommands.Clear();
			}
		}
		private void BuildBufferFromChildren(RectTransform rectTransform, TerminalBufferValue currentState, ref TerminalState terminalState)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref var previousTerminalBuffer = ref terminalState.previousTerminalBuffer;

			int childCount = rectTransform.childCount;
			for (int i_outer = 0; i_outer < childCount; i_outer++)
			{
				var child = rectTransform.GetChild(i_outer);
				if (!child.gameObject.activeInHierarchy)
				{
					continue;
				}
				var element = child.GetComponent<Element>();
				Vector2Int position = element.GetTerminalPosition(TerminalDefinition);
				RectInt bounds = element.GetTerminalBounds(TerminalDefinition);

				if (bounds.x < 0 || bounds.xMax > Width || bounds.y < 0 || bounds.yMax > Height)
				{
					continue;
				}

				if (element is Elements.Text text)
				{
					string contents = text.Contents;
					if (string.IsNullOrEmpty(contents))
						continue;
					contents = contents.Replace("\r\n", "\n").Replace("\r", "\n");
					for (int i = 0; i < contents.Length; i++)
					{
						char c = contents[i];

						bool isNewline = c == '\n';

						if (isNewline)
						{
							position.y++;
							position.x = bounds.xMin;
						}

						// Bounds check before writing
						if (position.x < 0 || position.x >= Width ||
							position.y < 0 || position.y >= Height)
						{
							//Debug.Log("Bounds check");
							goto endText;
						}

						if (isNewline)
						{
							continue;
						}

						if (!(text.Transparent && c == text.TransparentChar))
						{
							ref TerminalBufferValue cell = ref terminalBuffer[
								position.x,
								position.y
							];
							cell = currentState;
							cell.CharacterBank = TerminalCharacterBank.ASCII;
							cell.HasTerminalCommand = false;
							cell.SetChar(TerminalDefinition, c);
							cell.ConnectorID = 0;
						}

						position.x++;

						// Wrap to next line if past the right edge of the element bounds
						if (position.x >= bounds.xMax)
						{
							position.x = bounds.xMin;
							position.y++;
						}
						// Stop if past the bottom edge of the element bounds
						if (position.y >= bounds.yMax)
						{
							//Debug.Log("Bounds check 2");
							goto endText;
						}
					}
				endText:
					BuildBufferFromChildren(text.RectTransform, currentState, ref terminalState);
					continue;
				}
				if (element is Elements.HorizontalLine hline)
				{
					for (int y = bounds.yMin; y < bounds.yMax; y++)
					{
						for (int x = bounds.xMin; x < bounds.xMax; x++)
						{
							if (x < 0 || x >= Width || y < 0 || y >= Height)
								goto endHorizontalLine;

							ref TerminalBufferValue cell = ref terminalBuffer[
								x,
								y
							];
							cell.AtlasX = TerminalDefinition.HorizontalLineX;
							cell.AtlasY = TerminalDefinition.HorizontalLineY;
							cell.CharacterBank = TerminalCharacterBank.G1;
							cell.HasTerminalCommand = true;
							cell.TerminalCommandType = TerminalCommandType.Box_Horizontal;
						}
					}
				endHorizontalLine:
					BuildBufferFromChildren(hline.RectTransform, currentState, ref terminalState);
					continue;
				}
				if (element is ConnectedLinesGroup connectedLinesGroup)
				{
					currentState.ConnectorID = nextConnectorID++;
					BuildBufferFromChildren(connectedLinesGroup.RectTransform, currentState, ref terminalState);
					continue;
				}
				if (element is ConnectedLine line)
				{
					for (int y = bounds.yMin; y < bounds.yMax; y++)
					{
						for (int x = bounds.xMin; x < bounds.xMax; x++)
						{
							if (x < 0 || x >= Width || y < 0 || y >= Height)
								goto endConnectedLine;

							ref TerminalBufferValue cell = ref terminalBuffer[x, y];
							cell.ConnectorID = currentState.ConnectorID;
							// Set initial glyph - will be resolved by ActualizeConnectedLinesToBuffer
							cell.AtlasX = TerminalDefinition.HorizontalLineX;
							cell.AtlasY = TerminalDefinition.HorizontalLineY;
						}
					}

				endConnectedLine:
					BuildBufferFromChildren(line.RectTransform, currentState, ref terminalState);
					continue;
				}
				if (element is Elements.Box box)
				{
					DrawBoxToBuffer(
						ref terminalState,
						bounds.xMin,
						bounds.yMin,
						bounds.xMax - 1,
						bounds.yMax - 1
					);
					BuildBufferFromChildren(box.RectTransform, currentState, ref terminalState);
					continue;
				}
			}
		}
		public void BuildBuffer(ref TerminalState terminalState, bool firstUpdate)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref var previousTerminalBuffer = ref terminalState.previousTerminalBuffer;

			if (firstUpdate)
			{
				for (int y = 0; y < Height; y++)
				{
					for (int x = 0; x < Width; x++)
					{
						ref TerminalBufferValue previousChar = ref previousTerminalBuffer[x, y];
						ref TerminalBufferValue currentChar = ref terminalBuffer[x, y];
						previousChar = default;
						previousChar.SetChar(TerminalDefinition, ' ');
						currentChar = default;
						currentChar.SetChar(TerminalDefinition, ' ');
					}
				}
				return;
			}
			SwapAndClearBuffer(ref terminalState);

			// Reset connector ID counter so IDs are deterministic between frames
			nextConnectorID = 1;

			TerminalBufferValue currentState = default;

			BuildBufferFromChildren(
				RectTransform,
				currentState,
				ref terminalState
			);

			// Resolve connected line characters based on neighbors
			ActualizeConnectedLinesToBuffer(ref terminalState);
		}
		#region Draw x to buffer
		private void DrawHorizontalLineToBuffer(
			ref TerminalState terminalState,
			int terminalY,
			int startTerminalX,
			int endTerminalX,
			int connectorID = 0
		)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			for (int x = startTerminalX; x <= endTerminalX; x++)
			{
				ref TerminalBufferValue cell = ref terminalBuffer[x, terminalY];
				cell.AtlasX = TerminalDefinition.HorizontalLineX;
				cell.AtlasY = TerminalDefinition.HorizontalLineY;
				cell.ConnectorID = connectorID;
				cell.CharacterBank = TerminalCharacterBank.G1;
				cell.HasTerminalCommand = true;
				cell.TerminalCommandType = TerminalCommandType.Box_Horizontal;
			}
		}

		private void DrawVerticalLineToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int startTerminalY,
			int endTerminalY,
			int connectorID = 0
		)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			for (int y = startTerminalY; y <= endTerminalY; y++)
			{
				ref TerminalBufferValue cell = ref terminalBuffer[terminalX, y];
				cell.AtlasX = TerminalDefinition.VerticalLineX;
				cell.AtlasY = TerminalDefinition.VerticalLineY;
				cell.ConnectorID = connectorID;
				cell.CharacterBank = TerminalCharacterBank.G1;
				cell.HasTerminalCommand = true;
				cell.TerminalCommandType = TerminalCommandType.Box_Vertical;
			}
		}

		private void DrawTopLeftCornerToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.TopLeftCornerX;
			cell.AtlasY = TerminalDefinition.TopLeftCornerY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_TopLeftCorner;
		}

		private void DrawTopRightCornerToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.TopRightCornerX;
			cell.AtlasY = TerminalDefinition.TopRightCornerY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_TopRightCorner;
		}

		private void DrawBottomLeftCornerToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.BottomLeftCornerX;
			cell.AtlasY = TerminalDefinition.BottomLeftCornerY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_BottomLeftCorner;
		}

		private void DrawBottomRightCornerToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.BottomRightCornerX;
			cell.AtlasY = TerminalDefinition.BottomRightCornerY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_BottomRightCorner;
		}

		private void DrawLeftTeeToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.LeftTeeX;
			cell.AtlasY = TerminalDefinition.LeftTeeY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_LeftTee;
		}

		private void DrawRightTeeToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.RightTeeX;
			cell.AtlasY = TerminalDefinition.RightTeeY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_RightTee;
		}

		private void DrawUpTeeToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.UpTeeX;
			cell.AtlasY = TerminalDefinition.UpTeeY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_UpTee;
		}

		private void DrawDownTeeToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.DownTeeX;
			cell.AtlasY = TerminalDefinition.DownTeeY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_DownTee;
		}

		private void DrawCrossToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.CrossX;
			cell.AtlasY = TerminalDefinition.CrossY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_Cross;
		}

		private void DrawBoxToBuffer(
			ref TerminalState terminalState,
			int startTerminalX,
			int startTerminalY,
			int endTerminalX,
			int endTerminalY,
			int connectorID = -1
		)
		{
			// Draw horizontal lines
			DrawHorizontalLineToBuffer(ref terminalState, startTerminalY, startTerminalX + 1, endTerminalX - 1, connectorID);
			DrawHorizontalLineToBuffer(ref terminalState, endTerminalY, startTerminalX + 1, endTerminalX - 1, connectorID);

			// Draw vertical lines
			DrawVerticalLineToBuffer(ref terminalState, startTerminalX, startTerminalY + 1, endTerminalY - 1, connectorID);
			DrawVerticalLineToBuffer(ref terminalState, endTerminalX, startTerminalY + 1, endTerminalY - 1, connectorID);

			// Draw corners
			DrawTopLeftCornerToBuffer(ref terminalState, startTerminalX, startTerminalY, connectorID);
			DrawTopRightCornerToBuffer(ref terminalState, endTerminalX, startTerminalY, connectorID);
			DrawBottomLeftCornerToBuffer(ref terminalState, startTerminalX, endTerminalY, connectorID);
			DrawBottomRightCornerToBuffer(ref terminalState, endTerminalX, endTerminalY, connectorID);
		}

		private void DrawConnectedAreaToBuffer(
			ref TerminalState terminalState,
			int startTerminalX,
			int startTerminalY,
			int endTerminalX,
			int endTerminalY,
			int connectorID
		)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			for (int y = startTerminalY; y <= endTerminalY; y++)
			{
				for (int x = startTerminalX; x <= endTerminalX; x++)
				{
					ref TerminalBufferValue cell = ref terminalBuffer[x, y];
					cell.ConnectorID = connectorID;
					cell.CharacterBank = TerminalCharacterBank.G1;
					cell.HasTerminalCommand = true;
				}
			}
		}
		#endregion Draw x to buffer
		private void SwapAndClearBuffer(ref TerminalState terminalState)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref var previousTerminalBuffer = ref terminalState.previousTerminalBuffer;
			(terminalBuffer, previousTerminalBuffer) = (previousTerminalBuffer, terminalBuffer);
			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					ref TerminalBufferValue currentChar = ref terminalBuffer[x, y];
					currentChar = default;
					currentChar.SetChar(TerminalDefinition, ' ');
				}
			}
		}
		public bool Redraw = false;
		private float lastBridgeUpdateTime = 0f;
		void Update()
		{
			if (TerminalDefinition == null)
			{
				return;
			}
			_rectTransform.offsetMin = Vector3.zero;
			_rectTransform.offsetMax = new Vector3(
				Width * TerminalDefinition.GlyphWidth,
				Height * TerminalDefinition.GlyphHeight,
				0
			);
			_rectTransform.localScale = new Vector3(
				1,
				TerminalDefinition.PixelHeight,
				0
			);

			elementPool.Clear();
			GetComponentsInChildren<Element>(elementPool);
			foreach (var element in elementPool)
			{
				element.Align(TerminalDefinition);
			}

			float timeSinceLastUpdate = Time.time - lastBridgeUpdateTime;
			if (timeSinceLastUpdate > 1.0f / 30.0f)
			{
				if (testTerminalBridge != null)
				{
					testTerminalBridge.Update(this, Redraw);
				}
				lastBridgeUpdateTime = Time.time;
			}

			Redraw = false;
		}

		private void OnEnable()
		{
			Redraw = true;
			if (testSerialDriver == null)
			{
				testSerialDriver = new SerialDriver.SerialDriver();
				testSerialDriver.Open("COM3", 19200);
				testTerminalBridge = new TerminalBridge.VT320(
					testSerialDriver
				);
			}
		}
		private void OnDisable()
		{
			if (testSerialDriver != null)
			{
				testSerialDriver.Close();
				testSerialDriver = null;
				testTerminalBridge = null;
			}
		}
	}
}