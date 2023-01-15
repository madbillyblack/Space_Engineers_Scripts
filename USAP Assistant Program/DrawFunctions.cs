using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
	partial class Program
	{
		MySpriteDrawFrame _frame;

		// DRAW TEXTURE //
		public void DrawTexture(string shape, Vector2 position, Vector2 size, float rotation, Color color)
		{
			var sprite = new MySprite()
			{
				Type = SpriteType.TEXTURE,
				Data = shape,
				Position = position,
				Size = size,
				RotationOrScale = rotation,
				Color = color
			};
			_frame.Add(sprite);
		}


		// DRAW TEXT //
		void DrawText(string text, Vector2 position, float scale, TextAlignment alignment, Color color)
		{
			var sprite = new MySprite()
			{
				Type = SpriteType.TEXT,
				Data = text,
				Position = position,
				RotationOrScale = scale,
				Color = color,
				Alignment = alignment,
				FontId = "White"
			};
			_frame.Add(sprite);
		}


		// SURFACE FROM BLOCk //
		static IMyTextSurface SurfaceFromBlock(IMyTextSurfaceProvider block, int screenIndex)
		{
			if (screenIndex >= block.SurfaceCount)
				return null;

			return block.GetSurface(screenIndex);
		}


		// PREPARE TEXT SURFACE FOR SPRITES //
		public static void PrepareTextSurfaceForSprites(IMyTextSurface textSurface)
		{
			// Set the sprite display mode
			textSurface.ContentType = ContentType.SCRIPT;
			textSurface.ScriptBackgroundColor = new Color(0, 0, 0);
			// Make sure no built-in script has been selected
			textSurface.Script = "";
		}


		// DRAW MENU //
		public void DrawMenu(Menu menu)
		{
			_frame = menu.Surface.DrawFrame();

			MenuPage page = menu.GetCurrentPage();

			Vector2 center = menu.Viewport.Center;
			float height = menu.Viewport.Height;
			float width = menu.Viewport.Width;

			float fontSize = 0.5f;

			bool bigScreen = menu.Viewport.Width > 500;

			if (bigScreen)
				fontSize *= 1.5f;

			Color bgColor = menu.BackgroundColor;
			Color titleColor = menu.TitleColor;
			Color labelColor = menu.LabelColor;
			Color buttonColor = menu.ButtonColor;

			//int page = menu.CurrentPage;
			float cellWidth = (width / 7);
			float buttonHeight = (height * 0.5f);
			if (buttonHeight > cellWidth)
				buttonHeight = cellWidth - 4;

			// Background
			Vector2 position = center - new Vector2(width * 0.5f, 0);
			DrawTexture(SQUARE, position, new Vector2(width, height), 0, bgColor);

			// Set Starting Top Edge
			Vector2 topLeft;
			switch (menu.Alignment.ToUpper())
			{
				case "TOP":
					topLeft = center - new Vector2(width * 0.5f, height * 0.5f);
					break;
				case "BOTTOM":
					topLeft = center - new Vector2(width * 0.5f, height * -0.5f + buttonHeight * 2);
					break;
				case "CENTER":
				default:
					topLeft = center - new Vector2(width * 0.5f, buttonHeight);
					break;
			}

			// Button Backgrounds
			position = topLeft + new Vector2((cellWidth - buttonHeight) * 0.5f, buttonHeight * 1.175f);
			Vector2 buttonScale = new Vector2(buttonHeight, buttonHeight);

			for (int i = 1; i < 8; i++)
			{
				MenuButton button = page.Buttons[i];

				DrawButton(button, position, buttonScale, fontSize, buttonColor, bgColor, labelColor);

				position += new Vector2(cellWidth, 0);
			}

			if (menu.Decals != "")
			{
				DrawDecals(menu, topLeft, buttonHeight, menu.Alignment, menu.Decals);
			}

			// Menu Title
			position = topLeft + new Vector2(10, 0);
			string title = "MENU " + page.Number;
			string name = page.Name;

			if (name != "")
				title += ": " + name;

			DrawText(title, position, fontSize, TextAlignment.LEFT, titleColor);

			// Menu ID
			position = topLeft + new Vector2(width - 10, 0);
			if (_menus.Count > 1)
				DrawText("ID: " + menu.IDNumber, position, fontSize, TextAlignment.RIGHT, labelColor);

			_frame.Dispose();
		}


		// DRAW BUTTON //
		void DrawButton(MenuButton button, Vector2 startPosition, Vector2 buttonScale, float fontSize, Color buttonColor, Color backgroundColor, Color labelColor)
        {
			if (button.IsUnassigned())
				return;

			Color color;
			Vector2 position = startPosition;

			float xScale = buttonScale.X;
			float yScale = buttonScale.Y;

			// Brighten button if active, otherwise darken
			if (button.IsActive)
				color = buttonColor * 2;
			else
				color = buttonColor * 0.5f;

			//if (ShouldBeVisible(menu, i))
			//DrawTexture(SQUARE, position, buttonScale, 0, color);

			DrawTexture(SQUARE, position, buttonScale, 0, color);

			// Block Label
			position += new Vector2(xScale * 0.5f, - yScale * 0.8f);
			DrawText(button.BlockLabel, position, fontSize * 0.67f, TextAlignment.CENTER, labelColor);

			// Action Label
			position += new Vector2(0, yScale * 0.6f);
			DrawActionLabel(button, position, xScale * 1.25f, fontSize, backgroundColor, color);

			// Number Label
			position = startPosition + new Vector2(xScale * 0.5f, xScale * 0.45f);
			DrawText(button.Number.ToString(), position, fontSize *1.125f, TextAlignment.CENTER, labelColor);
		}


		// DRAW ACTION TEXT //
		void DrawActionText(string upperText, string lowerText, Vector2 centerPostion, float fontSize, Color fontColor)
        {
			Vector2 position = centerPostion;

			if(lowerText != "")
            {
				position += new Vector2(0, fontSize * 12);

				DrawText(lowerText, position, fontSize, TextAlignment.CENTER, fontColor);

				position -= new Vector2(0, fontSize * 24);
            }

			DrawText(upperText, position, fontSize, TextAlignment.CENTER, fontColor);
		}


		// DRAW ALL MENUS //
		void DrawAllMenus()
        {
			if (_menus.Count < 1)
				return;

			foreach (int key in _menus.Keys)
				DrawMenu(_menus[key]);
        }


		// DRAW ROTATED TRIANGLE //
		void DrawRotatedTriangle(Vector2 position, Vector2 offset, Vector2 scale, float rotation, Color color)
        {
			//ref circle
			//DrawTexture(CIRCLE, position - new Vector2(10, 0), new Vector2(20, 20), 0, Color.Red);

			Vector2 pos = position + RotateVector2(offset, rotation);

			DrawTexture(TRIANGLE, pos, scale, rotation, color);
        }


		// ROTATE VECTOR2 // - Rotates input vector by angle in radians
		Vector2 RotateVector2(Vector2 vectorIn, float angle)
        {
			float x = vectorIn.X;
			float y = vectorIn.Y;

			double xOut = x * Math.Cos(angle) - y * Math.Sin(angle);
			double yOut = x * Math.Sin(angle) + y * Math.Cos(angle);

			return new Vector2((float) xOut, (float) yOut);
        }
	}
}
