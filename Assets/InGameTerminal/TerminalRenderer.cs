/**
 * Attributes are composited in this order:
 * - Base glyph fetch
 * - Italic shear
 * - Bold OR-shift
 * - Underline row force
 * - Reverse video
 * - Blink mask
 * 
 * Horizontal line at 17,7
 * Vertical line at 24,7
 **/

using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;

namespace InGameTerminal
{
	[ExecuteAlways]
	public class TerminalRenderer : MonoBehaviour
	{
		private RenderTexture uiRenderTexture = null;
		[SerializeField]
		private Camera uiCamera = null;
		[SerializeField]
		private Shader[] effects;

		private ITerminalDefinition _terminalDefinition;

		private TerminalState terminalState;
		private List<TerminalCommand> terminalCommands = new List<TerminalCommand>();

		[SerializeField]
		private Terminal terminal;
		[SerializeField]
		private int simulatedBaudRate = 115200;

		[SerializeField]
		[Tooltip("Snap vertex positions to pixel boundaries for crisp 1-pixel lines")]
		private bool pixelSnap = true;

		private Canvas _unityCanvas;
		private CanvasRenderer _canvasRenderer;

		private void Awake()
		{
			_canvasRenderer = Util.GetOrCreateComponent<CanvasRenderer>(gameObject);

			if (_mesh == null)
			{
				_mesh = new Mesh();
				_mesh.MarkDynamic();
			}
		}

		#region mesh
		private Mesh _mesh;

		/// <summary>
		/// Snaps an X position to the nearest pixel boundary.
		/// </summary>
		private float SnapToPixelX(float value)
		{
			if (!pixelSnap)
				return value;
			
			// X has no scaling, so just round to nearest pixel
			return Mathf.Round(value);
		}

		/// <summary>
		/// Snaps a Y position to the nearest pixel boundary, accounting for PixelHeight scale.
		/// The local Y coordinate is scaled by PixelHeight, so we need to:
		/// 1. Calculate what the screen Y would be (localY * pixelHeight)
		/// 2. Round that to a pixel boundary
		/// 3. Convert back to local space (screenY / pixelHeight)
		/// </summary>
		private float SnapToPixelY(float localY)
		{
			if (!pixelSnap || _terminalDefinition == null)
				return localY;
			
			float pixelHeight = _terminalDefinition.PixelHeight;
			if (pixelHeight <= 0)
				pixelHeight = 1.0f;
			
			// Transform to screen space, snap, transform back
			float screenY = localY * pixelHeight;
			float snappedScreenY = Mathf.Round(screenY);
			return snappedScreenY / pixelHeight;
		}

		private void InitMesh()
		{
			_mesh.Clear();
			vertices.Clear();
			uvs.Clear();
			colors.Clear();
			triangles.Clear();
			vertexOffset = 0;

			for (int y = 0; y < terminal.Height; y++)
			{
				for (int x = 0; x < terminal.Width; x++)
				{
					InitChar(x, y, '%');
				}
			}

			_mesh.SetVertices(vertices);
			_mesh.SetUVs(0, uvs);
			_mesh.SetColors(colors);
			_mesh.SetTriangles(triangles, 0);
			_mesh.RecalculateBounds();

			_canvasRenderer.SetMesh(_mesh);
			_canvasRenderer.SetMaterial(_terminalDefinition.Atlas, null);
		}

		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> triangles = new List<int>();
		List<Color32> colors = new List<Color32>();

		int vertexOffset = 0;
		private void InitChar(int terminalX, int terminalY, char c)
		{
			var atlastXY = _terminalDefinition.CharToXY(c);
			var atlasX = atlastXY.x;
			var atlasY = atlastXY.y;
			
			// Calculate pixel positions - use integer math to ensure exact positioning
			float pixelX = terminalX * _terminalDefinition.GlyphWidth;
			float pixelY = terminalY * _terminalDefinition.GlyphHeight;

			// Calculate UVs
			float uvLeft = (float)atlasX / _terminalDefinition.AtlasCols;
			float uvRight = (float)(atlasX + 1) / _terminalDefinition.AtlasCols;
			float uvTop = 1.0f - (float)atlasY / _terminalDefinition.AtlasRows;
			float uvBottom = 1.0f - (float)(atlasY + 1) / _terminalDefinition.AtlasRows;

			// Calculate vertex positions with pixel snapping for crisp lines
			// X snapping is straightforward since X scale is 1
			float xLeft = SnapToPixelX(pixelX);
			float xRight = SnapToPixelX(pixelX + _terminalDefinition.GlyphWidth);
			
			// Y snapping must account for PixelHeight scale factor
			// Local Y is negated for UI space (top-left origin)
			float yTop = SnapToPixelY(-pixelY);
			float yBottom = SnapToPixelY(-pixelY - _terminalDefinition.GlyphHeight);

			// Add quad vertices (top-left origin)
			vertices.Add(new Vector3(xLeft, yTop, 0));
			vertices.Add(new Vector3(xRight, yTop, 0));
			vertices.Add(new Vector3(xRight, yBottom, 0));
			vertices.Add(new Vector3(xLeft, yBottom, 0));

			// Add UVs
			uvs.Add(new Vector2(uvLeft, uvTop));
			uvs.Add(new Vector2(uvRight, uvTop));
			uvs.Add(new Vector2(uvRight, uvBottom));
			uvs.Add(new Vector2(uvLeft, uvBottom));

			// Add colors (white by default)
			colors.Add(Color.white);
			colors.Add(Color.white);
			colors.Add(Color.white);
			colors.Add(Color.white);

			// Add triangles (two triangles per quad)
			triangles.Add(vertexOffset + 0);
			triangles.Add(vertexOffset + 1);
			triangles.Add(vertexOffset + 2);

			triangles.Add(vertexOffset + 0);
			triangles.Add(vertexOffset + 2);
			triangles.Add(vertexOffset + 3);

			vertexOffset += 4;
		}

		/// <summary>
		/// Find the existing character quad in the mesh and update its UVs to match the given atlas coordinates
		/// </summary>
		private void DrawCharToMesh(
			int atlasX,
			int atlasY,
			int terminalX,
			int terminalY
		)
		{
			// Each cell has 4 vertices, calculate the starting index
			int cellIndex = terminalY * terminal.Width + terminalX;
			int vertexIndex = cellIndex * 4;

			// Calculate UVs
			float uvLeft = (float)atlasX / _terminalDefinition.AtlasCols;
			float uvRight = (float)(atlasX + 1) / _terminalDefinition.AtlasCols;
			float uvTop = 1.0f - (float)atlasY / _terminalDefinition.AtlasRows;
			float uvBottom = 1.0f - (float)(atlasY + 1) / _terminalDefinition.AtlasRows;

			// Update UVs for the quad
			uvs[vertexIndex + 0] = new Vector2(uvLeft, uvTop);
			uvs[vertexIndex + 1] = new Vector2(uvRight, uvTop);
			uvs[vertexIndex + 2] = new Vector2(uvRight, uvBottom);
			uvs[vertexIndex + 3] = new Vector2(uvLeft, uvBottom);
		}
		#endregion mesh


		private void DrawBuffer()
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref var previousTerminalBuffer = ref terminalState.previousTerminalBuffer;

			//ref TerminalBufferValue testCell = ref terminalBuffer[79, 0];
			//testCell.SetChar(_terminalDefinition, '&');

			for (int y = 0; y < terminal.Height; y++)
			{
				for (int x = 0; x < terminal.Width; x++)
				{
					var cell = terminalBuffer[x, y];
					var previousCell = previousTerminalBuffer[x, y];

					//if (cell != previousCell || forceRedraw)
					{
						DrawCharToMesh(cell.AtlasX, cell.AtlasY, x, y);
						// Debug.Log("DrawCharToMesh " + cell.GetChar(_terminalDefinition) + " at " + x + "," + y);
					}
				}
			}
		}
		private void UpdateUVs()
		{
			// Update the mesh UVs
			_mesh.SetUVs(0, uvs);
			_mesh.UploadMeshData(false);

			// Force the canvas renderer to update
			_canvasRenderer.SetMesh(_mesh);
			
		}
		private struct DrawTerminalCommandsState
		{
			public Vector2Int Position;
			public bool Italic;
			public bool Bold;
			public bool Underline;
			public bool Inverted;
			public bool Blink;
		}
		private DrawTerminalCommandsState drawTerminalCommandsState;
		private void DrawTerminalCommandsToMesh(List<TerminalCommand> terminalCommands, int start, int end)
		{
			ref Vector2Int position = ref drawTerminalCommandsState.Position;
			ref bool italic = ref drawTerminalCommandsState.Italic;
			ref bool bold = ref drawTerminalCommandsState.Bold;
			ref bool underline = ref drawTerminalCommandsState.Underline;
			ref bool inverted = ref drawTerminalCommandsState.Inverted;
			ref bool blink = ref drawTerminalCommandsState.Blink;

			Vector2Int spaceXY = _terminalDefinition.CharToXY(' ');

			//foreach (var command in terminalCommands)
			for (int i = start; i < end; i++)
			{
				if (i >= terminalCommands.Count)
				{
					break;
				}
				var command = terminalCommands[i];
				switch (command.CommandType)
				{
					case TerminalCommandType.Char:
					case TerminalCommandType.Byte:
						if (position.x >= 0 && position.x < terminal.Width &&
							position.y >= 0 && position.y < terminal.Height)
						{
							Vector2Int atlasXY = _terminalDefinition.CharToXY((char)command.X);
							DrawCharToMesh(atlasXY.x, atlasXY.y, position.x, position.y);
						}
						position.x++;
						//if (position.x >= terminal.Width)
						//{
						//	position.x = 0;
						//	position.y++;
						//}
						//if (position.y >= terminal.Height)
						//{
						//	position.y = 0;
						//}
						break;
					case TerminalCommandType.Box_Horizontal:
					case TerminalCommandType.Box_Vertical:
					case TerminalCommandType.Box_TopLeftCorner:
					case TerminalCommandType.Box_TopRightCorner:
					case TerminalCommandType.Box_BottomLeftCorner:
					case TerminalCommandType.Box_BottomRightCorner:
					case TerminalCommandType.Box_Cross:
					case TerminalCommandType.Box_LeftTee:
					case TerminalCommandType.Box_RightTee:
					case TerminalCommandType.Box_UpTee:
					case TerminalCommandType.Box_DownTee:
						DrawCharToMesh(command.X, command.Y, position.x, position.y);
						position.x++;
						//if (position.x >= terminal.Width)
						//{
						//	position.x = 0;
						//	position.y++;
						//}
						//if (position.y >= terminal.Height)
						//{
						//	position.y = 0;
						//}
						break;
					//case TerminalCommandType.Byte:
					//	if (position.x >= 0 && position.x < terminal.Width &&
					//		position.y >= 0 && position.y < terminal.Height)
					//	{
					//		Vector2Int atlasXY = _terminalDefinition.ByteToXY((byte)command.X);
					//		DrawCharToMesh(atlasXY.x, atlasXY.y, position.x, position.y);
					//	}
					//	position.x++;
					//	if (position.x >= terminal.Width)
					//	{
					//		position.x = 0;
					//		position.y++;
					//	}
					//	if (position.y >= terminal.Height)
					//	{
					//		position.y = 0;
					//	}
					//	break;
					case TerminalCommandType.Up:
						position.y--;
						break;

					case TerminalCommandType.Down:
						position.y++;
						break;

					case TerminalCommandType.Left:
						position.x--;
						break;

					case TerminalCommandType.Right:
						position.x++;
						break;

					case TerminalCommandType.MoveTo:
						position.x = command.X;
						position.y = command.Y;
						break;

					case TerminalCommandType.CarriageReturn:
						position.x = 0;
						break;

					case TerminalCommandType.LineFeed:
						position.y++;
						break;

					case TerminalCommandType.Italic:
						italic = !italic;
						break;

					case TerminalCommandType.Bold:
						bold = !bold;
						break;

					case TerminalCommandType.Underline:
						underline = !underline;
						break;

					case TerminalCommandType.Invert:
						inverted = !inverted;
						break;

					case TerminalCommandType.Blink:
						blink = !blink;
						break;

					case TerminalCommandType.EL_CursorToEnd:
						// Erase in Line - clear from cursor to end of line
						
						for (int x = position.x; x < terminal.Width; x++)
						{
							DrawCharToMesh(spaceXY.x, spaceXY.y, x, position.y);
						}
						break;

					case TerminalCommandType.EL_BeginningToCursor:
						// Erase in Line - clear from beginning of line to cursor
						for (int x = 0; x <= position.x; x++)
						{
							DrawCharToMesh(spaceXY.x, spaceXY.y, x, position.y);
						}
						break;

					case TerminalCommandType.EraseInDisplay:
						// Erase in Display - clear from cursor to end of display
						for (int y = position.y; y < terminal.Height; y++)
						{
							int startX = (y == position.y) ? position.x : 0;
							for (int x = startX; x < terminal.Width; x++)
							{
								DrawCharToMesh(spaceXY.x, spaceXY.y, x, y);
							}
						}
						break;

					case TerminalCommandType.HomeCursor:
						position.x = 0;
						position.y = 0;
						break;
				}
			}
		}
		
		


		
		private void OnDestroy()
		{
			if (_mesh != null)
			{
				DestroyImmediate(_mesh);
			}
		}


		public bool DebugUpdate = false;
		public bool DebugReadyToUpdate = false;

		public int FrameRate = 60;
		private void UpdateBuffer_Player(bool redraw)
		{
			ref var terminalState = ref this.terminalState;
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref var previousTerminalBuffer = ref terminalState.previousTerminalBuffer;
			if (terminalBuffer == null)
			{
				redraw = true;
				terminalBuffer = new TerminalBufferValue[terminal.Width, terminal.Height];
				previousTerminalBuffer = new TerminalBufferValue[terminal.Width, terminal.Height];
				InitMesh();
			}
			terminal.BuildBuffer(ref terminalState);
			terminal.BuildTerminalCommands(ref terminalState, terminalCommands, redraw);
		}
		private IEnumerator UpdateCoroutine()
		{
			bool first = false;
			while (true)
			{
				if (!CheckSetupForUpdate())
				{
					yield return null;
					continue;
				}
				UpdateBuffer_Player(first);

				int commandsPerSecond = (int)(simulatedBaudRate / 8.0f);
				int commandsPerFrame = commandsPerSecond / FrameRate;
				if (commandsPerFrame < 1)
				{
					commandsPerFrame = 1;
				}
				int start = 0;
				int end = start + commandsPerFrame;
				DrawTerminalCommandsToMesh(terminalCommands, start, end);
				UpdateUVs();
				while (end < terminalCommands.Count)
				{
					start = end;
					end = start + commandsPerFrame;
					yield return new WaitForSeconds((float)commandsPerFrame / commandsPerSecond);
					DrawTerminalCommandsToMesh(terminalCommands, start, end);
					// Debug.Log($"DrawTerminalCommandsToMesh commands {start} to {end}");
					UpdateUVs();
				}

				//else
				//{
				//	Debug.Log("Not ReadyToDraw TerminalRenderer UpdateCoroutine");
				//}
				yield return null; // Always yield to prevent infinite loop
			}
		}
		private bool CheckSetupForUpdate()
		{
			if (DebugUpdate && !DebugReadyToUpdate)
			{
				return false;
			}
			DebugReadyToUpdate = false;
			if (!terminal)
			{
				terminal = GetComponent<Terminal>();
			}
			if (!terminal)
			{
				Debug.Log($"TerminalRenderer on GameObject '{gameObject.name}' is missing a Terminal component.");
				return false;
			}
			if (!_unityCanvas)
			{
				_unityCanvas = terminal.GetCanvas();
			}
			if (_terminalDefinition == null)
			{
				_terminalDefinition = terminal.TerminalDefinition;
			}
			if (_terminalDefinition == null)
			{
				Debug.Log($"TerminalRenderer on GameObject '{gameObject.name}' is missing a TerminalDefinition.");
				return false;
			}
			if (_canvasRenderer == null)
			{
				Debug.Log($"TerminalRenderer on GameObject '{gameObject.name}' is missing a CanvasRenderer.");
				return false;
			}

			this.transform.localScale = Vector3.one;
			GetComponent<RectTransform>().sizeDelta = new Vector2(terminal.CanvasWidth, terminal.CanvasHeight);

			return true;
		}
		private void UpdateInEditor()
		{
			ref var terminalState = ref this.terminalState;
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref var previousTerminalBuffer = ref terminalState.previousTerminalBuffer;
			if (terminalBuffer == null)
			{
				terminalBuffer = new TerminalBufferValue[terminal.Width, terminal.Height];
				previousTerminalBuffer = new TerminalBufferValue[terminal.Width, terminal.Height];
				InitMesh();
			}
			terminal.BuildBuffer(ref terminalState);
			DrawBuffer();
			UpdateUVs();
		}
		private void Update()
		{
			if (CheckSetupForUpdate() && !Application.isPlaying)
			{
				UpdateInEditor();
			}
		}
		public Camera GetCamera()
		{
			return uiCamera;
		}
		private void OnEnable()
		{
			if (Application.isPlaying)
			{
				StartCoroutine(UpdateCoroutine());
			}

			uiRenderTexture = new RenderTexture(terminal.CanvasWidth, terminal.CanvasHeight, 0, RenderTextureFormat.ARGB32)
			{
				name = "UI_Capture_RT",
				enableRandomWrite = false,   // input texture doesn't need RW
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};
			uiRenderTexture.Create();

			//uiCamera = Util.GetOrCreateComponent<Camera>(gameObject);
			if (uiCamera)
			{
				uiCamera.targetTexture = uiRenderTexture;
				uiCamera.orthographic = true;
				uiCamera.orthographicSize = terminal.CanvasHeight / 2.0f;
			}


			foreach (var terminalShader in GetComponents<Shaders.ITerminalShader>())
			{
				terminalShader.Init(uiRenderTexture);
			}
		}
		private void OnDisable()
		{
			
		}


	}
}
