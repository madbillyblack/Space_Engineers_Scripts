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
		const string SQUARE = "SquareSimple";
		const string BOX = "SquareHollow";
		const string SEMI = "SemiCircle";
		const string CIRCLE = "Circle";
		const string TRIANGLE = "Triangle";
		const string RING = "CircleHollow";
		const string UP = "UP";
		const string DOWN = "DOWN";
		const string LEFT = "LEFT";
		const string RIGHT = "RIGHT";

		readonly Vector2 _shadowOffset = new Vector2(1,1);


		// DRAW CAMERA ICON //
		void DrawCamera(Vector2 position, float scale, Color color)
        {
			Vector2 pos = position + new Vector2(-scale * 0.2f, 0);// scale * 0.125f);

			// Camera Body
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.33f, scale *0.25f), 0, color);

			pos += new Vector2(scale / 5, 0);

			// Camera Lens
			DrawTexture(TRIANGLE, pos, new Vector2(scale * 0.25f, scale * 0.25f), PI * 1.5f, color);
        }


		// DRAW THRUSTER //
		void DrawThruster(string symbol, Vector2 position, float scale, float fontsize, Color iconColor, Color textColor)
        {
			Vector2 pos = position + new Vector2(-scale * 0.167f, 0);// scale *0.2f);

			// Thruster Bell
			DrawTexture(SEMI, pos, new Vector2(scale *0.33f, scale *0.6f), 0, iconColor);

			pos += new Vector2(scale * 0.167f, scale * -0.025f);
			DrawText("|||||", pos, fontsize * 0.75f, TextAlignment.CENTER, iconColor);

			if (symbol == "")
				return;

			// +/- symbol
			float symbolScale = fontsize;

			if (symbol == "+")
				symbolScale *= 0.9f;

			pos -= new Vector2(0, symbolScale * 25);
			DrawText(symbol, pos, symbolScale, TextAlignment.CENTER, textColor);
        }


		// DRAW LIGHT //
		void DrawLight(Vector2 position, float scale, float fontSize, Color color)
		{
			Vector2 pos = position - new Vector2(scale * 0.167f, 0);// scale * -0.15f);

			DrawTexture(SEMI, pos, new Vector2(scale * 0.33f, scale * 0.33f), PI * 1.5f, color);

			pos += new Vector2(scale * 0.167f, scale * -0.167f);

			DrawText("=", pos, fontSize, TextAlignment.LEFT, color);

			pos -= new Vector2(0, fontSize * 12);

			DrawText("=", pos, fontSize, TextAlignment.LEFT, color);
		}


		// DRAW LANDING GEAR //
		void DrawLandingGear(Vector2 position, float scale, Color iconColor, Color buttonColor, string direction = "")
		{
			Vector2 pos = position - new Vector2(scale * 0.375f, scale * 0.15f);

			//Bay
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.75f, scale * 0.1f), 0, iconColor);

			// Strut
			pos += new Vector2(0, scale * 0.125f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.375f, scale * 0.05f), PI * 0.167f, iconColor);

			// Wheel
			pos = position + new Vector2(-scale * 0.125f, scale * 0.05f);
			DrawTexture(CIRCLE, pos, new Vector2(scale * 0.25f, scale * 0.25f), 0, iconColor);

			// Hubcap
			pos += new Vector2(scale * 0.0625f, 0);
			DrawTexture(CIRCLE, pos, new Vector2(scale * 0.125f, scale * 0.125f), 0, buttonColor);

			// Up Arrow
			pos += new Vector2(scale * 0.25f, scale * -0.1f);
			if(direction != DOWN)
			DrawTexture(TRIANGLE, pos, new Vector2(scale * 0.125f, scale * 0.125f), 0, iconColor);

			// Down Arrow
			if(direction != UP)
            {
				pos += new Vector2(0, scale * 0.175f);
				DrawTexture(TRIANGLE, pos, new Vector2(scale * 0.125f, scale * -0.125f), 0, iconColor);
			}
		}


		// DRAW MISSILE //
		void DrawMissile(Vector2 position, float scale, Color iconColor, Color bgColor)
        {
			Vector2 pos = position + new Vector2(scale * -0.35f, 0);

			// Body
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.6f, scale * 0.125f), 0, iconColor);

			// Back Fin
			DrawTexture(TRIANGLE, pos, new Vector2(scale * 0.33f, scale * 0.33f), PI * 0.5f, iconColor);

			// Forward Fin
			pos += new Vector2(scale * 0.33f, 0);
			DrawTexture(TRIANGLE, pos, new Vector2(scale * 0.33f, scale * 0.33f), PI * 0.5f, iconColor);

			// Tip
			pos += new Vector2(scale * 0.22f, 0);
			DrawTexture(SEMI, pos, new Vector2(scale * 0.125f, scale * 0.167f), PI * 0.5f, iconColor);

			// Fin Mask
			pos = position - new Vector2(scale * 0.4f, scale * 0.225f);
			DrawTexture(BOX, pos, new Vector2(scale * 0.8f, scale * 0.3375f), 0, bgColor);
			pos = position + new Vector2(scale * -0.4f, scale * 0.225f);
			DrawTexture(BOX, pos, new Vector2(scale * 0.8f, scale * 0.3375f), 0, bgColor);
		}


		// DRAW GATLING //
		void DrawGatling(Vector2 position, float scale, float fontSize, Color color)
        {
			Vector2 pos = position - new Vector2(scale * 0.35f, scale * 0.0625f);
			Vector2 barrelSize = new Vector2 (scale * 0.25f, scale * 0.01f);

			// Barrels
			DrawTexture(SQUARE, pos, barrelSize, 0, color);
			pos += new Vector2(0, scale * 0.125f);
			DrawTexture(SQUARE, pos, barrelSize, 0, color);
			pos -= new Vector2(0, scale * 0.0625f);
			DrawTexture(SQUARE, pos, barrelSize, 0, color);

			// Muzzle
			pos += new Vector2(barrelSize.X, 0);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.125f, scale * 0.18f), 0, color);

			// Bullets
			pos += new Vector2(scale * 0.15f, scale * -0.25f);
			DrawText("---", pos, fontSize, TextAlignment.LEFT, color);
		}
			
		// DRAW CANNON //
		void DrawCannon(Vector2 position, float scale, Color color)
        {
			Vector2 pos = position + new Vector2(scale * -0.33f, 0);// scale * 0.125f);

			// Barrel
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.5f, scale * 0.125f), 0, color);

			// Body
			pos += new Vector2(scale * 0.125f, 0);
			DrawTexture(TRIANGLE, pos, new Vector2(scale * 0.2f, scale * 0.5f), PI * 0.5f, color);

			// Shell
			pos += new Vector2(scale * 0.425f, 0);
			DrawTexture(SEMI, pos, new Vector2(scale * 0.075f, scale * 0.25f), PI * 0.5f, color);
		}


		// DRAW DOCK //
		void DrawDock(string type, Vector2 position, float scale, Color color)
        {
			Vector2 pos = position + new Vector2(scale * -0.33f, 0);// scale * 0.167f);

			// Left collar
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.2f, scale * 0.2f), 0, color);

			// Left base
			pos -= new Vector2(scale * 0.167f, 0);
			DrawTexture(SEMI, pos, new Vector2(scale * 0.33f, scale * 0.33f), PI * 0.5f, color);

			// Right collar
			pos = position + new Vector2(scale * 0.13f, 0);// scale * 0.167f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.2f, scale * 0.2f), 0, color);

			// Right Base
			pos += new Vector2(scale * 0.037f, 0);
			DrawTexture(SEMI, pos, new Vector2(scale * 0.33f, scale * 0.33f), PI * 1.5f, color);

			if(type == "A")
            {
				// Right Seal
				pos = position + new Vector2(scale * 0.067f, 0);//scale * 0.167f);
				DrawTexture(SQUARE, pos, new Vector2(scale * 0.025f, scale * 0.167f), 0, color);

				// Left Seal
				pos -= new Vector2(scale * 0.159f, 0);
				DrawTexture(SQUARE, pos, new Vector2(scale * 0.025f, scale * 0.167f), 0, color);
			}
			else if (type == "B")
            {
				// Upper Link
				pos = position + new Vector2(scale * -0.0835f, scale * -0.05f);
				DrawTexture(SQUARE, pos, new Vector2(scale * 0.167f, scale * 0.05f), 0, color);

				// Lower Link
				pos += new Vector2(0, scale * 0.105f);
				DrawTexture(SQUARE, pos, new Vector2(scale * 0.167f, scale * 0.05f), 0, color);
			}
		}


		// DRAW SEAPARATION //
		void DrawSeparation(Vector2 position, float scale, Color color)
        {
			Vector2 pos = position + new Vector2(scale * -0.33f, 0);//scale * 0.167f);

			// Left
			DrawTexture(SEMI, pos, new Vector2(scale * 0.33f, scale * 0.33f), PI * 1.5f, color);

			// Right
			pos += new Vector2(scale * 0.33f, 0);
			DrawTexture(SEMI, pos, new Vector2(scale * 0.33f, scale * 0.33f), PI * 0.5f, color);

			// Upper Line
			pos = position + new Vector2(scale * -0.125f, scale * -0.09f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.25f, scale * 0.03f), 0, color);

			// Middle Line
			pos += new Vector2(0, scale * 0.09f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.25f, scale * 0.03f), 0, color);

			// Lower Line
			pos += new Vector2(0, scale * 0.08f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.25f, scale * 0.03f), 0, color);
		}


		// DRAW GAS //
		void DrawGas(string gasType, string shape, Vector2 position, float scale, Color iconColor, Color textColor)
        {
			Vector2 pos = position + new Vector2(scale * -0.25f, 0);// scale * 0.175f);
			DrawTexture(shape, pos, new Vector2(scale * 0.5f, scale * 0.5f), 0, iconColor);

			// Element
			pos += new Vector2(scale * 0.2f, scale * -0.225f);
			if(gasType == "H2")
				DrawText("H", pos, scale * 0.0125f, TextAlignment.CENTER, textColor);
			else if (gasType == "O2")
				DrawText("O", pos, scale * 0.0125f, TextAlignment.CENTER, textColor);

			// 2
			pos += new Vector2(scale * 0.15f, scale * 0.167f);
			DrawText("2", pos, scale * 0.00625f, TextAlignment.CENTER, textColor);
		}


		// DRAW TURRET //
		void DrawTurret(Vector2 position, float scale, Color color)
        {
			Vector2 pos = position + new Vector2(scale * -0.25f, scale * 0.1f);

			DrawTexture(SEMI, pos, new Vector2(scale * 0.4f, scale * 0.4f), 0, color);

			pos += new Vector2(scale * 0.125f, scale * -0.15f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.4f, scale * 0.05f), PI * -0.0625f, color);
		}


		// DRAW MISSILE TURRET //
		void DrawMissileTurret(Vector2 position, float scale, Color color)
        {
			Vector2 pos = position - new Vector2(scale * 0.05f, scale * 0.125f);

			// Barrel
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.125f, scale * 0.5f), PI * -0.5625f, color);

			pos += new Vector2(scale * -0.05f, scale * 0.08f);
			DrawTexture(TRIANGLE, pos, new Vector2(scale * 0.25f, scale * 0.4f), 0, color);
		}


		// DRAW JETTISON //
		void DrawJettison(Vector2 position, float scale, Color color)
        {
			Vector2 pos = position - new Vector2(scale * 0.33f, scale * 0.15f);
			Vector2 size = new Vector2(scale * 0.2f, scale * 0.2f);

			//Bay
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.67f, scale * 0.1f), 0, color);

			pos += new Vector2(scale * 0.14f, scale * 0.05f);
			DrawTexture(CIRCLE, pos, size, 0, color);

			pos += new Vector2(scale * 0.2f, scale * 0.133f);
			DrawTexture(CIRCLE, pos, size, 0, color);

			pos += new Vector2(scale * -0.2f, scale * 0.133f);
			DrawTexture(CIRCLE, pos, size, 0, color);
		}


		// DRAW DRILL //
		void DrawDrill(Vector2 position, float scale, Color iconColor, Color bgColor)
        {
			Vector2 pos = position + new Vector2(scale  * -0.125f, 0);// scale * 0.167f);

			DrawTexture(TRIANGLE, pos, new Vector2(scale * 0.33f, scale * 0.5f), PI * 0.5f, iconColor);

			float threadAngle = 1.8326f; // in Radians

			pos -= new Vector2(scale * 0.125f, scale * -0.015f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.25f, scale * 0.02f), threadAngle, bgColor);

			pos += new Vector2(scale * 0.15f, 0);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.167f, scale * 0.015f), threadAngle, bgColor);

			pos += new Vector2(scale * 0.133f, 0);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.1f, scale * 0.015f), threadAngle, bgColor);
		}


		// DRAW WELDER //
		void DrawWelder(Vector2 position, float scale, float fontSize, Color color)
        {
			// Body
			Vector2 pos = position + new Vector2(scale * - 0.33f, 0);//scale * 0.167f);
			DrawTexture(SEMI, pos, new Vector2(scale * 0.33f, scale * 0.25f), PI * 0.5f, color);

			// Arms
			Vector2 armSize = new Vector2(scale * 0.25f, scale * 0.05f);
			float armAngle = PI * 0.167f;

			pos += new Vector2(scale * 0.2f, scale * -0.125f);
			DrawTexture(SQUARE, pos, armSize, armAngle, color);
			
			pos += new Vector2(0, scale * 0.25f);
			DrawTexture(SQUARE, pos, armSize, -armAngle, color);

			// Spark
			float size = fontSize * 1.5f;

			pos = position + new Vector2(scale * 0.1f, scale * -0.2f);
			DrawText("*", pos, size, TextAlignment.LEFT, color);
		}


		// DRAW GRINDER //
		void DrawGrinder(Vector2 position, float scale, Color iconColor, Color bgColor)
        {
			Vector2 pos = position + new Vector2(-scale * 0.0625f, 0);//scale * 0.167f);
			Vector2 size = new Vector2(scale * 0.167f, scale * 0.33f);
			Vector2 offset = new Vector2(size.X * -0.5f, size.Y * -0.4f);

			//DrawTexture(TRIANGLE, pos, size, PI * -0.25f, color);
			float angle = 0;

			DrawRotatedTriangle(pos, offset, size, angle, iconColor);

			for(int i = 0; i < 5; i++)
            {
				angle += PI * 0.333f;
				DrawRotatedTriangle(pos, offset, size, angle, iconColor);
			}

			DrawTexture(CIRCLE, pos, new Vector2(scale * 0.167f, scale * 0.167f), 0, bgColor);
		}

		// DRAW POWER //
		void DrawPower(string shape, Vector2 position, float scale, Color color, Color bgColor)
        {
			Vector2 pos = position + new Vector2(scale * -0.25f, 0);// scale * 0.167f);

			DrawTexture(shape, pos, new Vector2(scale * 0.5f, scale * 0.5f), 0, color);

			pos += new Vector2(scale * 0.25f, 0);// scale * -0.125f);

			DrawBolt(pos, scale * 0.75f, bgColor);
		}


		// DRAW BOLT // - Draws Electricity Bolt icon
		void DrawBolt(Vector2 position, float scale, Color color)
        {
			Vector2 pos = position - new Vector2(scale * 0.125f, scale * 0.125f);
			Vector2 size = new Vector2(scale * 0.125f, scale * 0.33f);

			DrawTexture(TRIANGLE, pos, size, PI * -0.25f, color);

			pos += new Vector2(scale * 0.125f, scale * 0.25f);

			DrawTexture(TRIANGLE, pos, size, PI * 0.75f, color);
		}


		// DRAW TRIANGLE //
		void DrawTriangle(Vector2 position, float scale, Color color, string direction)
		{
			float rotation;
			Vector2 pos = position - new Vector2(0, scale * 0.4f);
			Vector2 offset;

			switch (direction.ToUpper())
			{
				case RIGHT:
					rotation = 0.5f;
					offset = new Vector2(scale * 0.33f, 0);
					break;
				case DOWN:
					rotation = 1;
					offset = new Vector2(scale / 2, scale * -0.1f);
					break;
				case LEFT:
					rotation = 1.5f;
					offset = new Vector2(scale * 0.67f, 0);
					break;
				case UP:
				default:
					rotation = 0;
					offset = new Vector2(scale / 2, scale * 0.1f);
					break;
			}

			DrawTexture(TRIANGLE, pos - offset, new Vector2(scale, scale), PI * rotation, color);
		}


		// DRAW DOUBLE TRIANGLE //
		void DrawDoubleTriangle(Vector2 position, float scale, Color color, string direction)
		{
			Vector2 pos = position - new Vector2(0, scale * 0.2f);
			float rotation;
			float length = scale * 0.33f;
			Vector2 offset1;
			Vector2 offset2;

			switch (direction.ToUpper())
			{
				case RIGHT:
					rotation = 0.5f;
					offset1 = new Vector2(scale * 0.33f, 0);
					offset2 = new Vector2(length, 0);
					break;
				case DOWN:
					rotation = 1;
					offset1 = new Vector2(scale * 0.25f, scale * -0.25f);
					offset2 = new Vector2(0, -length);
					break;
				case LEFT:
					rotation = 1.5f;
					offset1 = new Vector2(scale * 0.167f, 0);
					offset2 = new Vector2(-length, 0);
					break;
				case UP:
				default:
					rotation = 0;
					offset1 = new Vector2(scale *0.25f, scale * 0.2f);
					offset2 = new Vector2(0, length);
					break;
			}

			rotation *= PI;
			pos -= offset1;

			Vector2 triangleSize = new Vector2(scale, scale) * 0.5f;

			DrawTexture(TRIANGLE, pos, triangleSize, rotation, color);

			pos += offset2;

			DrawTexture(TRIANGLE, pos, triangleSize, rotation, color);
		}


		// DRAW TOGGLE //
		void DrawToggle(Vector2 position, float scale, Color color)
		{
			//Vector2 pos = position + new Vector2(0, scale * 0.4f);
			Vector2 blockScale = new Vector2(scale, scale);
			position -= new Vector2(scale * 0.5f, scale * 0.3f);
			DrawTexture(SEMI, position, blockScale, PI * 1.5f, color);
			DrawTexture(RING, position, blockScale, 0, color);
			DrawTexture(RING, position + new Vector2(scale * 0.05f, 0), blockScale * 0.9f, 0, color);
			DrawTexture(RING, position + new Vector2(scale * 0.075f, 0), blockScale * 0.85f, 0, color);
		}


		// DRAW CYCLE //
		void DrawCycle(Vector2 position, float scale, Color color)
		{
			//Vector2 pos = position + new Vector2(0, scale * 0.4f);
			Vector2 blockScale = new Vector2(scale, scale);
			position -= new Vector2(scale * 0.5f, scale * 0.3f);
			DrawTexture(RING, position, blockScale, 0, color);
			DrawTexture(RING, position + new Vector2(scale * 0.05f, 0), blockScale * 0.9f, 0, color);
			DrawTexture(RING, position + new Vector2(scale * 0.075f, 0), blockScale * 0.85f, 0, color);
			DrawTexture(TRIANGLE, position + new Vector2(scale * 0.67f, -scale * 0.25f), blockScale * 0.5f, PI * 0.75f, color);
		}


		// DRAW ACTION TEXT //
		void DrawActionText(string upperText, string lowerText, Vector2 position, float fontSize, Color fontColor)
		{
			Vector2 pos = position - new Vector2(0, fontSize * 15);

			if (lowerText != "")
			{
				pos += new Vector2(0, fontSize * 12);

				DrawText(lowerText, pos, fontSize, TextAlignment.CENTER, fontColor);
				
				pos += _shadowOffset;
				DrawText(lowerText, pos, fontSize, TextAlignment.CENTER, fontColor);

				pos -= new Vector2(0, fontSize * 24);
			}

			DrawText(upperText, pos, fontSize, TextAlignment.CENTER, fontColor);

			pos -= _shadowOffset;
			DrawText(upperText, pos, fontSize, TextAlignment.CENTER, fontColor);
		}


		void DrawTarget(Vector2 position, float scale, Color color)
        {
			Vector2 pos = position - new Vector2(scale * 0.25f, 0);

			DrawTexture(RING, pos, new Vector2(scale * 0.5f, scale * 0.5f), 0, color);
			pos += _shadowOffset;
			DrawTexture(RING, pos, new Vector2(scale * 0.5f, scale * 0.5f), 0, color);

			pos = position - new Vector2(scale * 0.3333f, 0);

			DrawTexture(SQUARE, pos, new Vector2(scale * 0.6667f, scale * 0.01f), 0, color);

			pos = position - new Vector2(scale * 0.005f, 0);

			DrawTexture(SQUARE, pos, new Vector2(scale * 0.01f, scale * 0.6667f), 0, color);
		}


		void DrawTargetType(string shape, Vector2 position, float scale, Color color, Color bgColor)
        {
			Vector2 pos = position - new Vector2(scale * 0.33f, 0);
			
			float vertMod;
			if (shape == CIRCLE)
				vertMod = 1;
			else
				vertMod = 1.33f;

			float size = scale * 0.67f;

			DrawTexture(shape, pos, new Vector2(size, size * vertMod), 0, color);

			pos = position;

			float targetMod = 1.25f;

			if (shape == TRIANGLE)
            {
				targetMod = 1;
				pos += new Vector2(0, scale * 0.2f);
			}
			
			DrawTarget(pos, size * targetMod, bgColor);
		}
	}
}
