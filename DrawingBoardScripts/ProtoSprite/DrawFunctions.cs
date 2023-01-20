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


		// SURFACE FROM BLOCK //
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
