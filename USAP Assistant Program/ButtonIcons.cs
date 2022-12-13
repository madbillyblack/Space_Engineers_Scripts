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
		// DRAW ACTION LABEL //
		void DrawActionLabel(MenuButton button, Vector2 position, float scale, float fontSize, Color iconColor, Color buttonColor)
		{
			string labelA = button.ActionLabel[0];
			string labelB = button.ActionLabel[1];
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
					DrawLandingGear(position, scale, iconColor, buttonColor, "DOWN");
					break;
				case "{GEAR_UP}":
					DrawLandingGear(position, scale, iconColor, buttonColor, "UP");
					break;
				case "{MISSILE}":
					DrawMissile(position, scale, iconColor);
					break;
				case "{GUN}":
					DrawGun(position, scale, iconColor);
					break;
				case "{CANNON}":
					DrawCannon(position, scale, iconColor);
					break;
				case "{DOCK A}":
					DrawDock("A", position, scale, iconColor, buttonColor);
					break;
				case "{DOCK B}":
					DrawDock("B", position, scale, iconColor, buttonColor);
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
				case "JETTISON":
					DrawJettison(position, scale, iconColor);
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
			DrawTexture("SquareSimple", pos, new Vector2(scale * 0.33f, scale *0.25f), 0, color);

			pos += new Vector2(scale / 5, 0);

			// Camera Lens
			DrawTexture("Triangle", pos, new Vector2(scale * 0.25f, scale * 0.25f), PI * 1.5f, color);
        }


		// DRAW THRUSTER //
		void DrawThruster(string symbol, Vector2 position, float scale, float fontsize, Color iconColor, Color textColor)
        {
			Vector2 pos = position + new Vector2(-scale * 0.167f ,scale *0.2f);

			// Thruster Bell
			DrawTexture("SemiCircle", pos, new Vector2(scale *0.33f, scale *0.6f), 0, iconColor);

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

			DrawTexture("SemiCircle", pos, new Vector2(scale * 0.33f, scale * 0.33f), PI * 1.5f, color);

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
			DrawTexture("SquareSimple", pos, new Vector2(scale * 0.75f, scale * 0.1f), 0, iconColor);

			// Strut
			pos += new Vector2(0, scale * 0.125f);
			DrawTexture("SquareSimple", pos, new Vector2(scale * 0.375f, scale * 0.05f), PI * 0.167f, iconColor);

			// Wheel
			pos = position + new Vector2(-scale * 0.125f, scale * 0.25f);
			DrawTexture("Circle", pos, new Vector2(scale * 0.25f, scale * 0.25f), 0, iconColor);

			// Hubcap
			pos += new Vector2(scale * 0.0625f, 0);
			DrawTexture("Circle", pos, new Vector2(scale * 0.125f, scale * 0.125f), 0, buttonColor);

			// Up Arrow
			pos += new Vector2(scale * 0.25f, scale * -0.1f);
			if(direction != "DOWN")
			DrawTexture("Triangle", pos, new Vector2(scale * 0.125f, scale * 0.125f), 0, iconColor);

			// Down Arrow
			if(direction != "UP")
            {
				pos += new Vector2(0, scale * 0.175f);
				DrawTexture("Triangle", pos, new Vector2(scale * 0.125f, scale * -0.125f), 0, iconColor);
			}
		}


		// DRAW MISSILE //
		void DrawMissile(Vector2 position, float scale, Color color)
        {
			//TODO
        }


		// DRAW GUN //
		void DrawGun(Vector2 position, float scale, Color color)
        {
			//TODO
        }


		// DRAW CANNON //
		void DrawCannon(Vector2 position, float scale, Color color)
        {
			//TODO
        }


		// DRAW DOCK //
		void DrawDock(string type, Vector2 position, float scale, Color iconColor, Color buttonColor)
        {
			//TODO
        }


		// DRAW SEAPARATION //
		void DrawSeparation(Vector2 position, float scale, Color color)
        {
			//TODO
        }


		// DRAW GAS //
		void DrawGas(string gasType, Vector2 position, float scale, Color iconColor, Color textColor)
        {
			//TODO
        }


		// DRAW TURRET //
		void DrawTurret(Vector2 position, float scale, Color color)
        {
			//TODO
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
	}
}
