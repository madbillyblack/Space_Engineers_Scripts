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

			int page = menu.CurrentPage;
			float cellWidth = (width / 7);
			float buttonHeight = (height / 2);
			if (buttonHeight > cellWidth)
				buttonHeight = cellWidth - 4;

			// Background
			Vector2 position = center - new Vector2(width / 2, 0);
			DrawTexture("SquareSimple", position, new Vector2(width, height), 0, bgColor);

			// Set Starting Top Edge
			Vector2 topLeft;
			switch (menu.Alignment.ToUpper())
			{
				case "TOP":
					topLeft = center - new Vector2(width / 2, height / 2);
					break;
				case "BOTTOM":
					topLeft = center - new Vector2(width / 2, height / -2 + buttonHeight * 2);
					break;
				case "CENTER":
				default:
					topLeft = center - new Vector2(width / 2, buttonHeight);
					break;
			}

			// Button Backgrounds
			position = topLeft + new Vector2((cellWidth - buttonHeight) / 2, buttonHeight * 1.5f);
			Vector2 buttonScale = new Vector2(buttonHeight, buttonHeight);

			for (int i = 1; i < 8; i++)
			{/*
				Color color;

				// Brighten button if active, otherwise darken
				if (i == menu.ActiveButton)
					color = buttonColor * 2;
				else
					color = buttonColor * 0.5f;

				if (ShouldBeVisible(menu, i))
					DrawTexture("SquareSimple", position, buttonScale, 0, color);
				*/
				DrawTexture("SquareSimple", position, buttonScale, 0, buttonColor);

				position += new Vector2(cellWidth, 0);
			}

			// Menu Title
			position = topLeft + new Vector2(10, 0);
			string title = "MENU " + page;
			string name = menu.Pages[page].Name;

			if (name != "")
				title += ": " + name;

			DrawText(title, position, fontSize, TextAlignment.LEFT, titleColor);

			// Menu ID
			position = topLeft + new Vector2(width - 10, 0);
			if (_menus.Count > 1)
				DrawText("ID: " + menu.IDNumber, position, fontSize, TextAlignment.RIGHT, labelColor);


			// TODO


			// from planetMap3D section between key icons and numbers
			fontSize *= 1.5f;

			// Key 1
			//position = center + new Vector2(-3 * cellWidth, buttonHeight * 0.45f);
			position = topLeft + new Vector2(cellWidth / 2, buttonHeight * 1.35f);
			DrawText("1", position, fontSize, TextAlignment.CENTER, bgColor);

			// Key 2
			position += new Vector2(cellWidth, 0);
			DrawText("2", position, fontSize, TextAlignment.CENTER, bgColor);

			// Key 3
			position += new Vector2(cellWidth, 0);
			DrawText("3", position, fontSize, TextAlignment.CENTER, bgColor);

			// Key 4
			position += new Vector2(cellWidth, 0);
			DrawText("4", position, fontSize, TextAlignment.CENTER, bgColor);

			// Key 5
			position += new Vector2(cellWidth, 0);
			DrawText("5", position, fontSize, TextAlignment.CENTER, bgColor);

			// Key 6
			position += new Vector2(cellWidth, 0);
			DrawText("6", position, fontSize, TextAlignment.CENTER, bgColor);

			// Key 7
			position += new Vector2(cellWidth, 0);
			DrawText("7", position, fontSize, TextAlignment.CENTER, bgColor);

			_frame.Dispose();
		}

		/* / DRAW MENU //
		public void DrawMenu(MapMenu menu)
		{
			_frame = menu.Surface.DrawFrame();

			// Set height and width variables

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

			int page = menu.CurrentPage;
			float cellWidth = (width / 7);
			float buttonHeight = (height / 2);
			if (buttonHeight > cellWidth)
				buttonHeight = cellWidth - 4;

			// Background
			Vector2 position = center - new Vector2(width / 2, 0);
			DrawTexture("SquareSimple", position, new Vector2(width, height), 0, bgColor);

			// Set Starting Top Edge
			Vector2 topLeft;
			switch (menu.Alignment.ToUpper())
			{
				case "TOP":
					topLeft = center - new Vector2(width / 2, height / 2);
					break;
				case "BOTTOM":
					topLeft = center - new Vector2(width / 2, height / -2 + buttonHeight * 2);
					break;
				case "CENTER":
				default:
					topLeft = center - new Vector2(width / 2, buttonHeight);
					break;
			}

			// Button Backgrounds
			position = topLeft + new Vector2((cellWidth - buttonHeight) / 2, buttonHeight * 1.5f);
			Vector2 buttonScale = new Vector2(buttonHeight, buttonHeight);

			for (int i = 1; i < 8; i++)
			{
				Color color;

				// Brighten button if active, otherwise darken
				if (i == menu.ActiveButton)
					color = buttonColor * 2;
				else
					color = buttonColor * 0.5f;

				if (ShouldBeVisible(menu, i))
					DrawTexture("SquareSimple", position, buttonScale, 0, color);

				position += new Vector2(cellWidth, 0);
			}

			// Menu Title
			position = topLeft + new Vector2(10, 0);
			DrawText("MENU " + page + ": " + _menuTitle[page], position, fontSize, TextAlignment.LEFT, titleColor);

			// Menu ID
			position = topLeft + new Vector2(width * 0.67f, 0);
			if (_mapMenus.Count > 1)
				DrawText("ID: " + menu.IDNumber, position, fontSize, TextAlignment.CENTER, labelColor);

			// Current Map
			position = topLeft + new Vector2(width - 10, 0);
			if (_mapList.Count > 1)
				DrawText("MAP: " + menu.CurrentMapIndex, position, fontSize, TextAlignment.RIGHT, titleColor);

			// Label A
			position = topLeft + new Vector2(cellWidth, buttonHeight * 0.6f);
			DrawText(_labelA[page], position, fontSize * 0.9f, TextAlignment.CENTER, labelColor);

			// Label B
			position += new Vector2(cellWidth * 2, 0);
			DrawText(_labelB[page], position, fontSize * 0.9f, TextAlignment.CENTER, labelColor);

			// Label C
			position += new Vector2(cellWidth * 2, 0);

			string cLabel = _labelC[page];
			if ((menu.CurrentPage == 1 && _mapList.Count < 2) || (menu.CurrentPage == 6 && _dataDisplays.Count < 2))
				cLabel = ""; // Blank if no maps or data displays on relevant page
			else if (menu.CurrentPage == 6 && _dataDisplays.Count > 1 && menu.DataDisplay != null)
				cLabel += " " + menu.DataDisplay.IDNumber; // Add Display number if in menu 6

			DrawText(cLabel, position, fontSize * 0.9f, TextAlignment.CENTER, labelColor);

			// Label D
			position += new Vector2(cellWidth * 1.5f, 0);
			DrawText(_labelD[page], position, fontSize * 0.75f, TextAlignment.CENTER, labelColor);


			Vector2 iconScale = buttonScale * 0.33f;

			// Cmd 1
			//position = center + new Vector2(-3 * cellWidth, buttonHeight * 0.33f);
			position = topLeft + new Vector2(cellWidth / 2, buttonHeight * 1.27f);
			//DrawText(_cmd1[page], position, fontSize, TextAlignment.CENTER, bgColor);
			StringToIcon(_cmd1[page], position, iconScale, bgColor, bigScreen);

			// Cmd 2
			position += new Vector2(cellWidth, 0);
			StringToIcon(_cmd2[page], position, iconScale, bgColor, bigScreen);

			// Cmd 3
			position += new Vector2(cellWidth, 0);
			StringToIcon(_cmd3[page], position, iconScale, bgColor, bigScreen);

			// Cmd 4
			position += new Vector2(cellWidth, 0);
			StringToIcon(_cmd4[page], position, iconScale, bgColor, bigScreen);

			// Cmd 5
			position += new Vector2(cellWidth, 0);
			StringToIcon(_cmd5[page], position, iconScale, bgColor, bigScreen);

			// Cmd 6
			position += new Vector2(cellWidth, 0);
			StringToIcon(_cmd6[page], position, iconScale, bgColor, bigScreen);

			// Cmd 7
			position += new Vector2(cellWidth, 0);
			StringToIcon(_cmd7[page], position, iconScale, bgColor, bigScreen);

			fontSize *= 1.5f;

			// Key 1
			//position = center + new Vector2(-3 * cellWidth, buttonHeight * 0.45f);
			position = topLeft + new Vector2(cellWidth / 2, buttonHeight * 1.35f);
			DrawText("1", position, fontSize, TextAlignment.CENTER, bgColor);

			// Key 2
			position += new Vector2(cellWidth, 0);
			DrawText("2", position, fontSize, TextAlignment.CENTER, bgColor);

			// Key 3
			position += new Vector2(cellWidth, 0);
			DrawText("3", position, fontSize, TextAlignment.CENTER, bgColor);

			// Key 4
			position += new Vector2(cellWidth, 0);
			DrawText("4", position, fontSize, TextAlignment.CENTER, bgColor);

			// Key 5
			position += new Vector2(cellWidth, 0);
			DrawText("5", position, fontSize, TextAlignment.CENTER, bgColor);

			// Key 6
			position += new Vector2(cellWidth, 0);
			DrawText("6", position, fontSize, TextAlignment.CENTER, bgColor);

			// Key 7
			position += new Vector2(cellWidth, 0);
			DrawText("7", position, fontSize, TextAlignment.CENTER, bgColor);


			if (menu.Decals != "")
			{
				DrawDecals(menu, topLeft, buttonHeight, menu.Alignment, menu.Decals);
			}

			// Test Circle - Center
			//DrawTexture("CircleHollow", center - new Vector2(buttonHeight/2, 0), buttonScale, 0, Color.Red);

			_frame.Dispose();
		}*/
	}
}
