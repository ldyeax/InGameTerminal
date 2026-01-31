using InGameTerminal.Elements;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System;

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

		public int CanvasWidth
		{
			get
			{
				return Width * TerminalDefinition.GlyphWidth;
			}
		}
		public int CanvasHeight
		{
			get
			{
				return Height * TerminalDefinition.GlyphHeight;
			}
		}

		[SerializeField]
		public int Width = 80;
		[SerializeField]
		public int Height = 24;
		[SerializeField]
		public UnityTerminalDefinitionBase TerminalDefinition;
		[SerializeField]
		public TerminalRenderer Renderer;

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
		private bool EnsureSetup()
		{
			_unityCanvas = Util.GetOrCreateComponent<Canvas>(gameObject);
			_unityCanvas.renderMode = RenderMode.WorldSpace;
			_unityCanvas.pixelPerfect = true;

			CanvasScaler scaler = Util.GetOrCreateComponent<CanvasScaler>(gameObject);
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

			var raycaster = Util.GetOrCreateComponent<GraphicRaycaster>(gameObject);

			_rectTransform = Util.GetOrCreateComponent<RectTransform>(gameObject);

#if UNITY_EDITOR
			if (!TerminalDefinition)
			{
				TerminalDefinition = Util.GetDefaultTerminalDefinition();
			}
#endif
			if (!TerminalDefinition)
			{
				Debug.LogError("TerminalDefinition is not set on Terminal!", this);
				enabled = false;
				return false;
			}

			Renderer = Util.GetOrCreateComponent<TerminalRenderer>(gameObject);

			return true;
		}

		private void Reset() => EnsureSetup();
		private void OnValidate() => EnsureSetup();
		private void Awake()
		{
			if (!Application.isPlaying)
				EnsureSetup();
		}
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

			int skipCounter = 0;

			terminalCommands.Clear();
			if (terminalState.ExpectedTerminalPosition.x != 0 || terminalState.ExpectedTerminalPosition.y != 0)
			{
				terminalCommands.Add(new TerminalCommand()
				{
					CommandType = TerminalCommandType.HomeCursor
				});
				skipCounter++;
			}

			terminalCommands.Add(new TerminalCommand()
			{
				CommandType = TerminalCommandType.CharacterBank,
				X = (int)TerminalCharacterBank.ASCII
			});
			skipCounter++;
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
				// Send HomeCursor again after init sequence to ensure cursor is at (0,0)
				// Some terminals may leave cursor in unexpected position after EraseInDisplay/InitBanks
				terminalCommands.Add(new TerminalCommand()
				{
					CommandType = TerminalCommandType.HomeCursor
				});
			}

			/**
			 * expectedTerminalCursorPosition tracks where we expect the actual terminal's cursor to have been placed.
			 * If we put down a glyph, we expect the terminal to move the cursor to the right and wrap if necessary.
			 * But just sending an escape sequence etc. does not necessarily move the cursor.
			 * If we skip over a character because it hasn't changed, then later on we will need to move the cursor to catch up to the new position.
			 **/
			//Vector2Int expectedTerminalCursorPosition = default;
			ref Vector2Int expectedTerminalCursorPosition = ref terminalState.ExpectedTerminalPosition;
			// Track the current character bank state of the terminal (starts as ASCII)
			TerminalCharacterBank currentCharacterBank = TerminalCharacterBank.ASCII;

			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					TerminalBufferValue cell = terminalBuffer[x, y];
					if (redraw && cell.IsSpace(TerminalDefinition)
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
						if (x == 0)
						{
							int startSpaces = 0;
							for (int x2 = 0; x2 < Width; x2++)
							{
								if (!previousTerminalBuffer[x2, y].IsSpace(TerminalDefinition) && terminalBuffer[x2, y].IsSpace(TerminalDefinition))
								{
									startSpaces++;

								}
								else
								{
									break;
								}
							}
							if (startSpaces > 0)
							{
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.MoveTo,
									X = startSpaces - 1,
									Y = y
								});
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.EL_BeginningToCursor
								});
								expectedTerminalCursorPosition.x = x + startSpaces;
								expectedTerminalCursorPosition.y = y;
								x = expectedTerminalCursorPosition.x;
								continue;
							}
						}

						bool movedCursor = false;

						// Debug.Log($"Cell changed at {x},{y} from '{previousCell.GetChar(TerminalDefinition)}' to '{cell.GetChar(TerminalDefinition)}'");
						if (expectedTerminalCursorPosition.x != x || expectedTerminalCursorPosition.y != y)
						{
							if (expectedTerminalCursorPosition.x >= Width && x == 0 && expectedTerminalCursorPosition.y == y - 1)
							{
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.CarriageReturn
								});
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.LineFeed
								});
							}
							else if (expectedTerminalCursorPosition.x == x && expectedTerminalCursorPosition.y == y - 1)
							{
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.LineFeed
								});
							}
							else
							{
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.MoveTo,
									X = x,
									Y = y
								});
							}

							//if (y == 1 && x == 1)
							//{
							//	Debug.Log("Moved cursor to (1,1)");
							//	Debug.Log($"Expected cursor was at ({expectedTerminalCursorPosition.x},{expectedTerminalCursorPosition.y})");
							//	Debug.Log($"Actual cell is '{cell.GetChar(TerminalDefinition)}'");

							//}

							//terminalCommands.Add(new TerminalCommand()
							//{
							//	CommandType = TerminalCommandType.MoveTo,
							//	X = x,
							//	Y = y
							//});

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

						// Sync attributes for ALL cells (including box drawing characters)
						// This ensures the terminal state is correct before rendering any character,
						// so that subsequent characters inherit the correct state
						bool isSpace = cell.IsSpace(TerminalDefinition);
						if (cell.TextAttributes != terminalState.TextAttributes)
						{
							// Bold and Italic don't visually affect space characters, so skip them for spaces
							if (!isSpace && cell.TextAttributes.Bold != terminalState.TextAttributes.Bold)
							{
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.Bold
								});
								terminalState.TextAttributes.Bold = !terminalState.TextAttributes.Bold;
							}
							if (!isSpace && cell.TextAttributes.Italic != terminalState.TextAttributes.Italic)
							{
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.Italic
								});
								terminalState.TextAttributes.Italic = !terminalState.TextAttributes.Italic;
							}
							// Underline and Inverted DO visually affect spaces
							if (cell.TextAttributes.Underline != terminalState.TextAttributes.Underline)
							{
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.Underline
								});
								terminalState.TextAttributes.Underline = !terminalState.TextAttributes.Underline;
							}
							if (cell.TextAttributes.Blink != terminalState.TextAttributes.Blink)
							{
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.Blink
								});
								terminalState.TextAttributes.Blink = !terminalState.TextAttributes.Blink;
							}
							if (cell.TextAttributes.Inverted != terminalState.TextAttributes.Inverted)
							{
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.Invert
								});
								terminalState.TextAttributes.Inverted = !terminalState.TextAttributes.Inverted;
							}
						}

						if (cell.HasTerminalCommand)
						{
							terminalCommands.Add(new TerminalCommand()
							{
								CommandType = cell.TerminalCommandType,
								X = cell.AtlasX,
								Y = cell.AtlasY
							});
							// todo: check logic
							//movedCursor = true;
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
							//if (expectedTerminalCursorPosition.x >= Width)
							//{
							//	expectedTerminalCursorPosition.x = 0;
							//	expectedTerminalCursorPosition.y++;
							//}
							//if (expectedTerminalCursorPosition.y >= Height)
							//{
							//	expectedTerminalCursorPosition.y = 0;
							//}
						}

						// EL check - only use EL optimization if:
						// 1. We're far enough from the end of the line to make it worthwhile
						// 2. All remaining cells on this line are spaces
						// 3. At least some remaining cells actually need to be cleared (differ from previous buffer OR we're doing a redraw)
						if (x < Width - 3)
						{
							bool allRemainingAreSpaces = true;
							bool anyNeedClearing = false;
							bool remainingAttributesMatch = true;
							TextAttributes currentAttributes = terminalState.TextAttributes;
							
							for (int x2 = x + 1; x2 < Width; x2++)
							{
								TerminalBufferValue lookaheadCell = terminalBuffer[x2, y];
								if (!lookaheadCell.IsSpace(TerminalDefinition))
								{
									allRemainingAreSpaces = false;
									break;
								}
								// Check if this cell needs to be updated (previous had content that needs erasing)
								TerminalBufferValue previousLookaheadCell = previousTerminalBuffer[x2, y];
								if (redraw || lookaheadCell != previousLookaheadCell)
								{
									anyNeedClearing = true;
								}

								TextAttributes lookaheadAttributes = lookaheadCell.TextAttributes;
								if (lookaheadAttributes.Bold != currentAttributes.Bold ||
									lookaheadAttributes.Italic != currentAttributes.Italic ||
									lookaheadAttributes.Underline != currentAttributes.Underline ||
									lookaheadAttributes.Blink != currentAttributes.Blink ||
									lookaheadAttributes.Inverted != currentAttributes.Inverted)
								{
									remainingAttributesMatch = false;
								}
							}
							
							if (allRemainingAreSpaces && anyNeedClearing && remainingAttributesMatch)
							{
								// All remaining cells on this line are spaces and need clearing
								terminalCommands.Add(new TerminalCommand()
								{
									CommandType = TerminalCommandType.EL_CursorToEnd
								});
								// Break out of x loop to move to next line
								// Don't increment y here - the outer for loop will do it
								break;
							}
						}
					}
				}

			}
			if (terminalCommands.Count == skipCounter)
			{
				terminalCommands.Clear();
			}
		}
		private void BuildBufferFromChildren(Transform transform, TerminalBufferValue currentState, ref TerminalState terminalState)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref var previousTerminalBuffer = ref terminalState.previousTerminalBuffer;
			// currentState.TextAttributes is passed in by the caller and represents the inherited attributes
			// Don't use terminalState.TextAttributes here - that represents the VT320's current attribute state

			int childCount = transform.childCount;
			for (int i_outer = 0; i_outer < childCount; i_outer++)
			{
				var child = transform.GetChild(i_outer);
				if (!child.gameObject.activeInHierarchy)
				{
					continue;
				}
				var element = child.GetComponent<Element>();
				if (!element)
				{
					BuildBufferFromChildren(
						child,
						currentState,
						ref terminalState
					);
					continue;
				}
				if (TerminalDefinition == null)
				{
					Debug.LogError("TerminalDefinition is null on Terminal!", this);
					return;
				}
				Vector2Int position = element.GetTerminalPosition(TerminalDefinition);
				RectInt bounds = element.GetTerminalBounds(TerminalDefinition);

				//if (bounds.x < 0 || bounds.xMax > Width || bounds.y < 0 || bounds.yMax > Height)
				//{
				//	continue;
				//}

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

						//// Bounds check before writing
						//if (position.x < 0 || position.x >= Width ||
						//	position.y < 0 || position.y >= Height)
						//{
						//	//Debug.Log("Bounds check");
						//	goto endText;
						//}

						if (isNewline)
						{
							continue;
						}

						if (position.x >= 0 && position.x < Width
							&& position.y >= 0 && position.y < Height
							&& !(text.Transparent && c == text.TransparentChar))
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
							cell.TextAttributes = currentState.TextAttributes;
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
								continue;
							//	goto endHorizontalLine;

							ref TerminalBufferValue cell = ref terminalBuffer[
								x,
								y
							];
							cell.AtlasX = TerminalDefinition.HorizontalLineX;
							cell.AtlasY = TerminalDefinition.HorizontalLineY;
							cell.CharacterBank = TerminalCharacterBank.G1;
							cell.HasTerminalCommand = true;
							cell.TerminalCommandType = TerminalCommandType.Box_Horizontal;
							cell.TextAttributes = currentState.TextAttributes;
						}
					}
				//endHorizontalLine:
					BuildBufferFromChildren(hline.RectTransform, currentState, ref terminalState);
					continue;
				}
				if (element is Elements.VerticalLine vline)
				{
					for (int y = bounds.yMin; y < bounds.yMax; y++)
					{
						for (int x = bounds.xMin; x < bounds.xMax; x++)
						{
							if (x < 0 || x >= Width || y < 0 || y >= Height)
								continue;
							//	goto endVerticalLine;

							ref TerminalBufferValue cell = ref terminalBuffer[
								x,
								y
							];
							cell.AtlasX = TerminalDefinition.VerticalLineX;
							cell.AtlasY = TerminalDefinition.VerticalLineY;
							cell.CharacterBank = TerminalCharacterBank.G1;
							cell.HasTerminalCommand = true;
							cell.TerminalCommandType = TerminalCommandType.Box_Vertical;
							cell.TextAttributes = currentState.TextAttributes;
						}
					}
				//endVerticalLine:
					BuildBufferFromChildren(vline.RectTransform, currentState, ref terminalState);
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
								continue;
							//	goto endConnectedLine;

							ref TerminalBufferValue cell = ref terminalBuffer[x, y];
							cell.ConnectorID = currentState.ConnectorID;
							// Set initial glyph - will be resolved by ActualizeConnectedLinesToBuffer
							cell.AtlasX = TerminalDefinition.HorizontalLineX;
							cell.AtlasY = TerminalDefinition.HorizontalLineY;
							cell.TextAttributes = currentState.TextAttributes;
						}
					}

				//endConnectedLine:
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
						bounds.yMax - 1,
						box.Solid
					);
					BuildBufferFromChildren(box.RectTransform, currentState, ref terminalState);
					continue;
				}
				if (element is Elements.Modifier modifier)
				{
					// Create modified state for children - don't touch terminalState.TextAttributes
					// (that represents the VT320's state, not the inherited UI attributes)
					TerminalBufferValue modifiedState = currentState;
					modifiedState.TextAttributes.Bold = currentState.TextAttributes.Bold || modifier.Bold;
					modifiedState.TextAttributes.Italic = currentState.TextAttributes.Italic || modifier.Italic;
					modifiedState.TextAttributes.Underline = currentState.TextAttributes.Underline || modifier.Underline;
					modifiedState.TextAttributes.Blink = currentState.TextAttributes.Blink || modifier.Blink;
					modifiedState.TextAttributes.Inverted = currentState.TextAttributes.Inverted || modifier.Invert;
					BuildBufferFromChildren(modifier.RectTransform, modifiedState, ref terminalState);
					continue;
				}
			}
		}
		public void BuildBuffer(ref TerminalState terminalState)
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref var previousTerminalBuffer = ref terminalState.previousTerminalBuffer;

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
				if (x < 0 || x >= Width || terminalY < 0 || terminalY >= Height)
					continue;
				ref TerminalBufferValue cell = ref terminalBuffer[x, terminalY];
				cell.AtlasX = TerminalDefinition.HorizontalLineX;
				cell.AtlasY = TerminalDefinition.HorizontalLineY;
				cell.ConnectorID = connectorID;
				cell.CharacterBank = TerminalCharacterBank.G1;
				cell.HasTerminalCommand = true;
				cell.TerminalCommandType = TerminalCommandType.Box_Horizontal;
				cell.TextAttributes = terminalState.TextAttributes;
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
				if (terminalX < 0 || terminalX >= Width || y < 0 || y >= Height)
					continue;
				ref TerminalBufferValue cell = ref terminalBuffer[terminalX, y];
				cell.AtlasX = TerminalDefinition.VerticalLineX;
				cell.AtlasY = TerminalDefinition.VerticalLineY;
				cell.ConnectorID = connectorID;
				cell.CharacterBank = TerminalCharacterBank.G1;
				cell.HasTerminalCommand = true;
				cell.TerminalCommandType = TerminalCommandType.Box_Vertical;
				cell.TextAttributes = terminalState.TextAttributes;
			}
		}

		private void DrawTopLeftCornerToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			if (terminalX < 0 || terminalX >= Width || terminalY < 0 || terminalY >= Height)
				return;
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.TopLeftCornerX;
			cell.AtlasY = TerminalDefinition.TopLeftCornerY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_TopLeftCorner;
			cell.TextAttributes = terminalState.TextAttributes;
		}

		private void DrawTopRightCornerToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			if (terminalX < 0 || terminalX >= Width || terminalY < 0 || terminalY >= Height)
				return;
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.TopRightCornerX;
			cell.AtlasY = TerminalDefinition.TopRightCornerY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_TopRightCorner;
			cell.TextAttributes = terminalState.TextAttributes;
		}

		private void DrawBottomLeftCornerToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			if (terminalX < 0 || terminalX >= Width || terminalY < 0 || terminalY >= Height)
				return;
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.BottomLeftCornerX;
			cell.AtlasY = TerminalDefinition.BottomLeftCornerY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_BottomLeftCorner;
			cell.TextAttributes = terminalState.TextAttributes;
		}

		private void DrawBottomRightCornerToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			if (terminalX < 0 || terminalX >= Width || terminalY < 0 || terminalY >= Height)
				return;
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.BottomRightCornerX;
			cell.AtlasY = TerminalDefinition.BottomRightCornerY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_BottomRightCorner;
			cell.TextAttributes = terminalState.TextAttributes;
		}

		private void DrawLeftTeeToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			if (terminalX < 0 || terminalX >= Width || terminalY < 0 || terminalY >= Height)
				return;
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.LeftTeeX;
			cell.AtlasY = TerminalDefinition.LeftTeeY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_LeftTee;
			cell.TextAttributes = terminalState.TextAttributes;
		}

		private void DrawRightTeeToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			if (terminalX < 0 || terminalX >= Width || terminalY < 0 || terminalY >= Height)
				return;
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.RightTeeX;
			cell.AtlasY = TerminalDefinition.RightTeeY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_RightTee;
			cell.TextAttributes = terminalState.TextAttributes;
		}

		private void DrawUpTeeToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			if (terminalX < 0 || terminalX >= Width || terminalY < 0 || terminalY >= Height)
				return;
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.UpTeeX;
			cell.AtlasY = TerminalDefinition.UpTeeY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_UpTee;
			cell.TextAttributes = terminalState.TextAttributes;
		}

		private void DrawDownTeeToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			if (terminalX < 0 || terminalX >= Width || terminalY < 0 || terminalY >= Height)
				return;
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.DownTeeX;
			cell.AtlasY = TerminalDefinition.DownTeeY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_DownTee;
			cell.TextAttributes = terminalState.TextAttributes;
		}

		private void DrawCrossToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			if (terminalX < 0 || terminalX >= Width || terminalY < 0 || terminalY >= Height)
				return;
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.CrossX;
			cell.AtlasY = TerminalDefinition.CrossY;
			cell.ConnectorID = connectorID;
			cell.CharacterBank = TerminalCharacterBank.G1;
			cell.HasTerminalCommand = true;
			cell.TerminalCommandType = TerminalCommandType.Box_Cross;
			cell.TextAttributes = terminalState.TextAttributes;
		}

		private void DrawSpaceToBuffer(
			ref TerminalState terminalState,
			int terminalX,
			int terminalY
		)
		{
			if (terminalX < 0 || terminalX >= Width || terminalY < 0 || terminalY >= Height)
				return;
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = TerminalDefinition.CharToXY(' ').x;
			cell.AtlasY = TerminalDefinition.CharToXY(' ').y;
			cell.ConnectorID = 0;
			cell.CharacterBank = TerminalCharacterBank.ASCII;
			cell.HasTerminalCommand = false;
			cell.TextAttributes = terminalState.TextAttributes;
		}

		private void DrawBoxToBuffer(
			ref TerminalState terminalState,
			int startTerminalX,
			int startTerminalY,
			int endTerminalX,
			int endTerminalY,
			bool solid = false
		)
		{
			// Draw spaces if solid
			if (solid)
			{
				// Draw spaces if solid
				for (int x = startTerminalX; x <= endTerminalX; x++ )
				{
					for (int y = startTerminalY; y <= endTerminalY; y++)
					{
						DrawSpaceToBuffer(ref terminalState, x, y);
					}
				}
			}

			// Draw horizontal lines
			DrawHorizontalLineToBuffer(ref terminalState, startTerminalY, startTerminalX + 1, endTerminalX - 1, 0);
			DrawHorizontalLineToBuffer(ref terminalState, endTerminalY, startTerminalX + 1, endTerminalX - 1, 0);

			// Draw vertical lines
			DrawVerticalLineToBuffer(ref terminalState, startTerminalX, startTerminalY + 1, endTerminalY - 1, 0);
			DrawVerticalLineToBuffer(ref terminalState, endTerminalX, startTerminalY + 1, endTerminalY - 1, 0);

			// Draw corners
			DrawTopLeftCornerToBuffer(ref terminalState, startTerminalX, startTerminalY, 0);
			DrawTopRightCornerToBuffer(ref terminalState, endTerminalX, startTerminalY, 0);
			DrawBottomLeftCornerToBuffer(ref terminalState, startTerminalX, endTerminalY, 0);
			DrawBottomRightCornerToBuffer(ref terminalState, endTerminalX, endTerminalY, 0);
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
					if (x < 0 || x >= Width || y < 0 || y >= Height)
						continue;
					ref TerminalBufferValue cell = ref terminalBuffer[x, y];
					cell.ConnectorID = connectorID;
					cell.CharacterBank = TerminalCharacterBank.G1;
					cell.HasTerminalCommand = true;
					cell.TextAttributes = terminalState.TextAttributes;
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
				Debug.LogError("TerminalDefinition is null on Terminal!", this);
				return;
			}
			//_rectTransform.offsetMin = Vector3.zero;
			_rectTransform.offsetMax = _rectTransform.offsetMin + new Vector2(
				Width * TerminalDefinition.GlyphWidth,
				Height * TerminalDefinition.GlyphHeight
			);
			//_rectTransform.localScale = new Vector3(
			//	2,
			//	TerminalDefinition.PixelHeight * 2,
			//	0
			//);
			transform.localScale = new Vector3(
				1,
				TerminalDefinition.PixelHeight,
				1
			);
			_rectTransform.pivot = new Vector2(0, 1);

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
#pragma warning disable IDE0059 // Unnecessary assignment of a value
#pragma warning disable CS0168 // Variable is declared but never used
				try
				{
					testSerialDriver = new SerialDriver.SerialDriver();
					testSerialDriver.Open("COM3", 19200);
					testTerminalBridge = new TerminalBridge.VT320(
						testSerialDriver
					);
				}
				catch (Exception ex)
				{
					// Debug.LogError("Could not initialize test serial: " + ex);
					testSerialDriver = null;
					testTerminalBridge = null;
				}
#pragma warning restore CS0168 // Variable is declared but never used
#pragma warning restore IDE0059 // Unnecessary assignment of a value
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

		#region visual scripting/etc helpers
		public void MoveElementToVector2Int(Element element, Vector2Int newTerminalPosition)
		{
			if (TerminalDefinition == null)
			{
				Debug.LogError("TerminalDefinition is null on Terminal!", this);
				return;
			}
			element.SetTerminalPosition(
				TerminalDefinition,
				newTerminalPosition
			);
		}
		public void MoveElementToVector2(Element element, Vector2 newTerminalPosition)
		{
			if (TerminalDefinition == null)
			{
				Debug.LogError("TerminalDefinition is null on Terminal!", this);
				return;
			}
			element.SetTerminalPosition(
				TerminalDefinition,
				new Vector2Int(
					Mathf.RoundToInt(newTerminalPosition.x),
					Mathf.RoundToInt(newTerminalPosition.y)
				)
			);
		}
		public void MoveElementByVector2Int(Element element, Vector2Int delta)
		{
			if (TerminalDefinition == null)
			{
				Debug.LogError("TerminalDefinition is null on Terminal!", this);
				return;
			}
			Vector2Int currentPos = element.GetTerminalPosition(TerminalDefinition);
			element.SetTerminalPosition(
				TerminalDefinition,
				currentPos + delta
			);
		}
		public void MoveElementByVector2(Element element, Vector2 delta)
		{
			if (TerminalDefinition == null)
			{
				Debug.LogError("TerminalDefinition is null on Terminal!", this);
				return;
			}
			Vector2Int currentPos = element.GetTerminalPosition(TerminalDefinition);
			element.SetTerminalPosition(
				TerminalDefinition,
				currentPos + new Vector2Int(
					Mathf.RoundToInt(delta.x),
					Mathf.RoundToInt(delta.y)
				)
			);
		}
		public void MoveElementToX(Element element, int deltaX)
		{
			if (TerminalDefinition == null)
			{
				Debug.LogError("TerminalDefinition is null on Terminal!", this);
				return;
			}
			Vector2Int currentPos = element.GetTerminalPosition(TerminalDefinition);
			element.SetTerminalPosition(
				TerminalDefinition,
				new Vector2Int(
					deltaX,
					currentPos.y
				)
			);
		}
		public void MoveElementToY(Element element, int deltaY)
		{
			if (TerminalDefinition == null)
			{
				Debug.LogError("TerminalDefinition is null on Terminal!", this);
				return;
			}
			Vector2Int currentPos = element.GetTerminalPosition(TerminalDefinition);
			element.SetTerminalPosition(
				TerminalDefinition,
				new Vector2Int(
					currentPos.x,
					deltaY
				)
			);
		}
		public void MoveElementByX(Element element, int deltaX)
		{
			if (TerminalDefinition == null)
			{
				Debug.LogError("TerminalDefinition is null on Terminal!", this);
				return;
			}
			Vector2Int currentPos = element.GetTerminalPosition(TerminalDefinition);
			element.SetTerminalPosition(
				TerminalDefinition,
				new Vector2Int(
					currentPos.x + deltaX,
					currentPos.y
				)
			);
		}
		public void MoveElementByY(Element element, int deltaY)
		{
			if (TerminalDefinition == null)
			{
				Debug.LogError("TerminalDefinition is null on Terminal!", this);
				return;
			}
			Vector2Int currentPos = element.GetTerminalPosition(TerminalDefinition);
			element.SetTerminalPosition(
				TerminalDefinition,
				new Vector2Int(
					currentPos.x,
					currentPos.y + deltaY
				)
			);
		}
		#endregion visual scripting/etc helpers
	}
}
