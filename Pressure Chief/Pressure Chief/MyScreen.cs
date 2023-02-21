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
					if (!Byte.TryParse(GetKey(lcd as IMyTerminalBlock, INI_HEAD, "Screen_Index", "0"), out index) || index >= lcd.SurfaceCount)
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
	}
}
