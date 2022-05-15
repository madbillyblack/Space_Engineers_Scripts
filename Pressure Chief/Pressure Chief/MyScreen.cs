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
        public class MyScreen
        {
			// DRAW TEXTURE //
			public static void DrawTexture(string shape, Vector2 position, Vector2 scale, float rotation, Color color, MySpriteDrawFrame frame)
			{
				MySprite sprite = new MySprite()
				{
					Type = SpriteType.TEXTURE,
					Data = shape,
					Position = position,
					RotationOrScale = rotation,
					Size = scale,
					Color = color
				};

				frame.Add(sprite);
			}


			// WRITE TEXT //
			public static void WriteText(string text, Vector2 position, TextAlignment alignment, float scale, Color color, MySpriteDrawFrame frame)
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
				frame.Add(sprite);
			}

			public static IMyTextSurface PrepareTextSurface(IMyTextSurfaceProvider lcd)
			{
				if (lcd.SurfaceCount < 1)
					return null;

				byte index = 0;
				if (lcd.SurfaceCount > 1)
				{
					if (!Byte.TryParse(IniKey.GetKey(lcd as IMyTerminalBlock, INI_HEAD, "Screen_Index", "0"), out index) || index >= lcd.SurfaceCount)
					{
						index = 0;
						_statusMessage = "Invalid 'Screen_Index' value in block " + (lcd as IMyTerminalBlock).CustomName;
					}
				}
				IMyTextSurface textSurface = lcd.GetSurface(index);

				// Set the sprite display mode
				textSurface.ContentType = ContentType.SCRIPT;
				// Make sure no built-in script has been selected
				textSurface.Script = "";

				// Set Background Color to black
				textSurface.ScriptBackgroundColor = Color.Black;


				return textSurface;
			}
		}


		// DRAW GAUGE // - Draws the pressure display between room the lcd is locate in and the neighboring room.
		static void DrawGauge(IMyTextSurface drawSurface, Sector sectorA, Sector sectorB, bool locked, bool vertical, bool flipped, float brightness, bool noSides)
		{
			RectangleF viewport = new RectangleF((drawSurface.TextureSize - drawSurface.SurfaceSize) / 2f, drawSurface.SurfaceSize);

			float pressureA = sectorA.Vents[0].GetOxygenLevel();
			float pressureB = sectorB.Vents[0].GetOxygenLevel();

			// Set color of status frame.
			Color statusColor;
			if (locked)
				statusColor = Color.Red * brightness;
			else
				statusColor = Color.Green * brightness;


			var frame = drawSurface.DrawFrame();

			float height = drawSurface.SurfaceSize.Y;
			float width = viewport.Width;
			float textSize = 0.8f;
			float topEdge = viewport.Center.Y - viewport.Height / 2;
			float bottomEdge = viewport.Center.Y + viewport.Height / 2;
			if (width < SCREEN_THRESHHOLD)
				textSize = 0.4f;

			//Vector2 position;// = viewport.Center - new Vector2(width/2, 0);

			int redA = (int)(PRES_RED * (1 - pressureA) * brightness);
			int greenA = (int)(PRES_GREEN * pressureA * brightness);
			int blueA = (int)(PRES_BLUE * pressureA * brightness);
			int redB = (int)(PRES_RED * (1 - pressureB) * brightness);
			int greenB = (int)(PRES_GREEN * pressureB * brightness);
			int blueB = (int)(PRES_BLUE * pressureB * brightness);

			//Variables for position alignment and scale
			Vector2 leftPos, leftScale, leftTextPos, rightPos, rightTextPos, rightScale, leftReadingOffset, gridScale, position;
			TextAlignment leftChamberAlignment, rightChamberAlignment;

			if (vertical)
			{
				if (flipped)
				{
					leftPos = new Vector2(width, bottomEdge - height * pressureA * 0.5f);
					leftScale = new Vector2(-width * 0.425f, height * pressureA);
					leftTextPos = new Vector2(width * 0.78f, topEdge);
					rightPos = new Vector2(0, bottomEdge - height * pressureB * 0.5f);
					rightScale = new Vector2(width * 0.425f, height * pressureB);
					rightTextPos = new Vector2(width * 0.22f, topEdge);
				}
				else
				{
					leftPos = new Vector2(0, bottomEdge - height * pressureA * 0.5f);
					leftScale = new Vector2(width * 0.425f, height * pressureA);
					leftTextPos = new Vector2(width * 0.22f, topEdge);
					rightPos = new Vector2(width, bottomEdge - height * pressureB * 0.5f);
					rightScale = new Vector2(-width * 0.425f, height * pressureB);
					rightTextPos = new Vector2(width * 0.78f, topEdge);
				}

				leftChamberAlignment = TextAlignment.CENTER;
				rightChamberAlignment = TextAlignment.CENTER;
				leftReadingOffset = new Vector2(0, textSize * 25);
				gridScale = new Vector2(width * 20, height);
			}
			else
			{
				if (flipped)
				{
					leftPos = new Vector2(width, viewport.Center.Y);
					leftScale = new Vector2(-width * 0.425f * pressureA, height);
					leftTextPos = new Vector2(width - textSize * 10, topEdge);
					rightPos = new Vector2(0, viewport.Center.Y);
					rightScale = new Vector2(width * 0.425f * pressureB, height);
					rightTextPos = new Vector2(textSize * 10, topEdge);
					leftChamberAlignment = TextAlignment.RIGHT;
					rightChamberAlignment = TextAlignment.LEFT;
					leftReadingOffset = new Vector2(0, textSize * 25);
				}
				else
				{
					leftPos = new Vector2(0, viewport.Center.Y);
					leftScale = new Vector2(width * 0.425f * pressureA, height);
					leftTextPos = new Vector2(textSize * 10, topEdge);
					rightPos = new Vector2(width, viewport.Center.Y);
					rightScale = new Vector2(-width * 0.425f * pressureB, height);
					rightTextPos = new Vector2(width - textSize * 10, topEdge);
					leftChamberAlignment = TextAlignment.LEFT;
					rightChamberAlignment = TextAlignment.RIGHT;
					leftReadingOffset = new Vector2(textSize * 10, textSize * 25);
				}

				gridScale = new Vector2(width, height * 20);
			}

			// Left Chamber
			string leftText;
			Color leftColor;

			// If no assigned Side, draw gauge title in white without asterisk
			if (noSides)
            {
				leftText = sectorA.Name;
				leftColor = _textColor;
				leftReadingOffset = new Vector2 (0, textSize * 25);
			}
			else
            {
				leftText = "*" + sectorA.Name;
				leftColor = _roomColor;
			}

			MyScreen.DrawTexture("SquareSimple", leftPos, leftScale, 0, new Color(redA, greenA, blueA), frame);
			MyScreen.WriteText(leftText, leftTextPos, leftChamberAlignment, textSize, leftColor, frame);
			leftTextPos += leftReadingOffset;
			MyScreen.WriteText((string.Format("{0:0.##}", (pressureA * _atmo * _factor))) + _unit, leftTextPos, leftChamberAlignment, textSize * 0.75f, _textColor, frame);

			// Right Chamber
			MyScreen.DrawTexture("SquareSimple", rightPos, rightScale, 0, new Color(redB, greenB, blueB), frame);
			MyScreen.WriteText(sectorB.Name, rightTextPos, rightChamberAlignment, textSize, _textColor, frame);
			rightTextPos += new Vector2(0, textSize * 25);
			MyScreen.WriteText((string.Format("{0:0.##}", (pressureB * _atmo * _factor))) + _unit, rightTextPos, rightChamberAlignment, textSize * 0.75f, _textColor, frame);

			// Grid Texture
			position = new Vector2(0, viewport.Center.Y);
			MyScreen.DrawTexture("Grid", position, gridScale, 0, Color.Black, frame);
			position += new Vector2(1, 0);
			MyScreen.DrawTexture("Grid", position, new Vector2(width, height * 20), 0, Color.Black, frame);

			// Status Frame
			position = viewport.Center - new Vector2(width * 0.075f, 0);
			MyScreen.DrawTexture("SquareSimple", position, new Vector2(width * 0.15f, height), 0, statusColor, frame);

			// Door Background
			position = viewport.Center - new Vector2(width * 0.0625f, 0);
			MyScreen.DrawTexture("SquareSimple", position, new Vector2(width * 0.125f, height * 0.95f), 0, Color.Black, frame);

			// Door Status
			if (locked)
			{
				position = viewport.Center - new Vector2(width * 0.04f, 0);
				MyScreen.DrawTexture("Cross", position, new Vector2(width * 0.08f, width * 0.08f), 0, Color.White, frame);
			}
			else
			{
				position = viewport.Center - new Vector2(width * 0.06f, 0);
				MyScreen.DrawTexture("Arrow", position, new Vector2(width * 0.08f, width * 0.08f), (float)Math.PI / -2, Color.White, frame);
				position += new Vector2(width * 0.04f, 0);
				MyScreen.DrawTexture("Arrow", position, new Vector2(width * 0.08f, width * 0.08f), (float)Math.PI / 2, Color.White, frame);
			}

			frame.Dispose();
		}
	}
}
