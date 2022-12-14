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


		// DRAW ACTION LABEL //
		void DrawActionLabel(MenuButton button, Vector2 position, float scale, float fontSize, Color iconColor, Color buttonColor)
		{
			string labelA = button.ActionLabel[0];
			string labelB = button.ActionLabel[1];

			// Variables for PlanetMap3D icons
			float arrowScale = scale * 0.33f;
			Vector2 arrowPos = position + new Vector2(0, scale * 0.132f);

			switch (labelA.ToUpper())
			{
				case "{CAMERA}":
					DrawCamera(position, scale, iconColor);
					break;
				case "{THRUSTER}":
					DrawThruster("", position, scale, fontSize, iconColor, buttonColor);
					break;
				case "{+THRUST}":
					DrawThruster("+", position, scale, fontSize, iconColor, buttonColor);
					break;
				case "{-THRUST}":
					DrawThruster("-", position, scale, fontSize, iconColor, buttonColor);
					break;
				case "{LIGHT}":
					DrawLight(position, scale, fontSize, iconColor);
					break;
				case "{GEAR}":
					DrawLandingGear(position, scale, iconColor, buttonColor);
					break;
				case "{GEAR_DOWN}":
					DrawLandingGear(position, scale, iconColor, buttonColor, DOWN);
					break;
				case "{GEAR_UP}":
					DrawLandingGear(position, scale, iconColor, buttonColor, UP);
					break;
				case "{MISSILE}":
					DrawMissile(position, scale, iconColor, buttonColor);
					break;
				case "{GATLING}":
					DrawGatling(position, scale, fontSize, iconColor);
					break;
				case "{CANNON}":
					DrawCannon(position, scale, iconColor);
					break;
				case "{DOCK A}":
					DrawDock("A", position, scale, iconColor);
					break;
				case "{DOCK B}":
					DrawDock("B", position, scale, iconColor);
					break;
				case "{UNDOCK}":
					DrawSeparation(position, scale, iconColor);
					break;
				case "{H2}":
					DrawGas("H2", position, scale, iconColor, buttonColor);
					break;
				case "{O2}":
					DrawGas("O2", position, scale, iconColor, buttonColor);
					break;
				case "{TURRET}":
					DrawTurret(position, scale, iconColor);
					break;
				case "{M_TURRET}":
					DrawMissileTurret(position, scale, iconColor);
					break;
				case "{JETTISON}":
					DrawJettison(position, scale, iconColor);
					break;
				case "{<}":
					DrawTriangle(arrowPos, arrowScale, iconColor, LEFT);
					break;
				case "{>}":
					DrawTriangle(arrowPos, arrowScale, iconColor, RIGHT);
					break;
				case "{^}":
					DrawTriangle(arrowPos, arrowScale, iconColor, UP);
					break;
				case "{V}":
					DrawTriangle(arrowPos, arrowScale, iconColor, DOWN);
					break;
				case "{<<}":
					DrawDoubleTriangle(arrowPos, arrowScale, iconColor, LEFT);
					break;
				case "{>>}":
					DrawDoubleTriangle(arrowPos, arrowScale, iconColor, RIGHT);
					break;
				case "{^^}":
					DrawDoubleTriangle(arrowPos, arrowScale, iconColor, UP);
					break;
				case "{VV}":
					DrawDoubleTriangle(arrowPos, arrowScale, iconColor, DOWN);
					break;
				case "{TOGGLE}":
					DrawToggle(arrowPos, arrowScale, iconColor);
					break;
				case "{CYCLE}":
					DrawCycle(arrowPos, arrowScale, iconColor);
					break;
				default:
					DrawActionText(labelA, labelB, position, fontSize * 0.67f, iconColor);
					break;
			}
		}


		// DRAW CAMERA ICON //
		void DrawCamera(Vector2 position, float scale, Color color)
        {
			Vector2 pos = position + new Vector2(-scale * 0.2f, scale * 0.125f);

			// Camera Body
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.33f, scale *0.25f), 0, color);

			pos += new Vector2(scale / 5, 0);

			// Camera Lens
			DrawTexture(TRIANGLE, pos, new Vector2(scale * 0.25f, scale * 0.25f), PI * 1.5f, color);
        }


		// DRAW THRUSTER //
		void DrawThruster(string symbol, Vector2 position, float scale, float fontsize, Color iconColor, Color textColor)
        {
			Vector2 pos = position + new Vector2(-scale * 0.167f ,scale *0.2f);

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

			pos -= new Vector2(0, scale * 0.33f);
			DrawText(symbol, pos, symbolScale, TextAlignment.CENTER, textColor);
        }


		// DRAW LIGHT //
		void DrawLight(Vector2 position, float scale, float fontSize, Color color)
		{
			Vector2 pos = position - new Vector2(scale * 0.167f, scale * -0.15f);

			DrawTexture(SEMI, pos, new Vector2(scale * 0.33f, scale * 0.33f), PI * 1.5f, color);

			pos += new Vector2(scale * 0.167f, scale * -0.167f);

			DrawText("=", pos, fontSize, TextAlignment.LEFT, color);

			pos -= new Vector2(0, fontSize * 12);

			DrawText("=", pos, fontSize, TextAlignment.LEFT, color);
		}


		// DRAW LANDING GEAR //
		void DrawLandingGear(Vector2 position, float scale, Color iconColor, Color buttonColor, string direction = "")
		{
			Vector2 pos = position - new Vector2(scale * 0.375f, 0);
			
			//Bay
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.75f, scale * 0.1f), 0, iconColor);

			// Strut
			pos += new Vector2(0, scale * 0.125f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.375f, scale * 0.05f), PI * 0.167f, iconColor);

			// Wheel
			pos = position + new Vector2(-scale * 0.125f, scale * 0.25f);
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
			Vector2 pos = position + new Vector2(scale * -0.35f, scale * 0.125f);

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
			pos = position + new Vector2(scale * -0.5f, scale * 0.05f);
			DrawTexture(BOX, pos, new Vector2(scale, scale * 0.33f), PI, bgColor);
			pos += new Vector2(0, scale * 0.167f);
			DrawTexture(BOX, pos, new Vector2(scale, scale * 0.33f), 0, bgColor);
		}


		// DRAW GATLING //
		void DrawGatling(Vector2 position, float scale, float fontSize, Color color)
        {
			Vector2 pos = position + new Vector2(scale * -0.35f, scale * 0.0625f);
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
			Vector2 pos = position + new Vector2(scale * -0.33f, scale * 0.125f);

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
			Vector2 pos = position + new Vector2(scale * -0.33f, scale * 0.167f);

			// Left collar
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.2f, scale * 0.2f), 0, color);

			// Left base
			pos -= new Vector2(scale * 0.167f, 0);
			DrawTexture(SEMI, pos, new Vector2(scale * 0.33f, scale * 0.33f), PI * 0.5f, color);

			// Right collar
			pos = position + new Vector2(scale * 0.13f, scale * 0.167f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.2f, scale * 0.2f), 0, color);

			// Right Base
			pos += new Vector2(scale * 0.037f, 0);
			DrawTexture(SEMI, pos, new Vector2(scale * 0.33f, scale * 0.33f), PI * 1.5f, color);

			if(type == "A")
            {
				// Right Seal
				pos = position + new Vector2(scale * 0.067f, scale * 0.167f);
				DrawTexture(SQUARE, pos, new Vector2(scale * 0.025f, scale * 0.167f), 0, color);

				// Left Seal
				pos -= new Vector2(scale * 0.159f, 0);
				DrawTexture(SQUARE, pos, new Vector2(scale * 0.025f, scale * 0.167f), 0, color);
			}
			else if (type == "B")
            {
				// Upper Link
				pos = position + new Vector2(scale * -0.0835f, scale * 0.125f);
				DrawTexture(SQUARE, pos, new Vector2(scale * 0.167f, scale * 0.05f), 0, color);

				// Lower Link
				pos += new Vector2(0, scale * 0.105f);
				DrawTexture(SQUARE, pos, new Vector2(scale * 0.167f, scale * 0.05f), 0, color);
			}
		}


		// DRAW SEAPARATION //
		void DrawSeparation(Vector2 position, float scale, Color color)
        {
			Vector2 pos = position + new Vector2(scale * -0.33f, scale * 0.167f);

			// Left
			DrawTexture(SEMI, pos, new Vector2(scale * 0.33f, scale * 0.33f), PI * 1.5f, color);

			// Right
			pos += new Vector2(scale * 0.33f, 0);
			DrawTexture(SEMI, pos, new Vector2(scale * 0.33f, scale * 0.33f), PI * 0.5f, color);

			// Upper Line
			pos = position + new Vector2(scale * -0.125f, scale * 0.08f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.25f, scale * 0.03f), 0, color);

			// Middle Line
			pos += new Vector2(0, scale * 0.09f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.25f, scale * 0.03f), 0, color);

			// Lower Line
			pos += new Vector2(0, scale * 0.08f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.25f, scale * 0.03f), 0, color);
		}


		// DRAW GAS //
		void DrawGas(string gasType, Vector2 position, float scale, Color iconColor, Color textColor)
        {
			Vector2 pos = position + new Vector2(scale * -0.2f, scale * 0.175f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.4f, scale * 0.4f), 0, iconColor);

			// Element
			pos += new Vector2(scale * 0.15f, scale * -0.225f);
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
			Vector2 pos = position + new Vector2(scale * -0.25f, scale * 0.33f);

			DrawTexture(SEMI, pos, new Vector2(scale * 0.4f, scale * 0.4f), 0, color);

			pos += new Vector2(scale * 0.125f, scale * -0.15f);
			DrawTexture(SQUARE, pos, new Vector2(scale * 0.4f, scale * 0.05f), PI * -0.0625f, color);
		}


		// DRAW MISSILE TURRET //
		void DrawMissileTurret(Vector2 position, float scale, Color color)
        {
			//TODO
        }


		// DRAW JETTISON //
		void DrawJettison(Vector2 position, float scale, Color color)
        {
			//TODO
        }

		// DRAW TRIANGLE //
		void DrawTriangle(Vector2 position, float scale, Color color, string direction)
		{
			float rotation;
			//Vector2 pos = position + new Vector2(0, scale * 0.4f);
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

			DrawTexture(TRIANGLE, position - offset, new Vector2(scale, scale), PI * rotation, color);
		}


		// DRAW DOUBLE TRIANGLE //
		void DrawDoubleTriangle(Vector2 position, float scale, Color color, string direction)
		{
			//Vector2 pos = position + new Vector2(0, scale * 0.4f);
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
					offset1 = new Vector2(scale *0.25f, scale * 0.125f);
					offset2 = new Vector2(0, length);
					break;
			}

			rotation *= PI;
			position -= offset1;

			Vector2 triangleSize = new Vector2(scale, scale) * 0.5f;

			DrawTexture(TRIANGLE, position, triangleSize, rotation, color);

			position += offset2;

			DrawTexture(TRIANGLE, position, triangleSize, rotation, color);
		}


		// DRAW TOGGLE //
		void DrawToggle(Vector2 position, float scale, Color color)
		{
			//Vector2 pos = position + new Vector2(0, scale * 0.4f);
			Vector2 blockScale = new Vector2(scale, scale);
			position -= new Vector2(scale * 0.5f, 0);
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
			position -= new Vector2(scale / 2, 0);
			DrawTexture(RING, position, blockScale, 0, color);
			DrawTexture(RING, position + new Vector2(scale * 0.05f, 0), blockScale * 0.9f, 0, color);
			DrawTexture(RING, position + new Vector2(scale * 0.075f, 0), blockScale * 0.85f, 0, color);
			DrawTexture(TRIANGLE, position + new Vector2(scale * 0.67f, -scale * 0.25f), blockScale * 0.5f, PI * 0.75f, color);
		}
	}
}
