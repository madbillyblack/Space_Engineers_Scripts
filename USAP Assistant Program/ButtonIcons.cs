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
		void DrawActionLabel(MenuButton button, Vector2 position, Vector2 scale, float fontSize, Color iconColor, Color buttonColor)
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
					DrawLight(position, scale, iconColor);
					break;
				case "{LG}":
					DrawLandingGear(position, scale, iconColor, buttonColor);
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
		void DrawCamera(Vector2 position, Vector2 scale, Color color)
        {
			//TODO

        }


		// DRAW THRUSTER //
		void DrawThruster(string symbol, Vector2 position, Vector2 scale, float fontsize, Color iconColor, Color textColor)
        {
			//TODO
        }


		// DRAW LIGHT //
		void DrawLight(Vector2 position, Vector2 scale, Color color)
		{
			//TODO
		}


		// DRAW LANDING GEAR //
		void DrawLandingGear(Vector2 position, Vector2 scale, Color iconColor, Color buttonColor)
		{
			//TODO
		}


		// DRAW MISSILE //
		void DrawMissile(Vector2 position, Vector2 scale, Color color)
        {
			//TODO
        }


		// DRAW GUN //
		void DrawGun(Vector2 position, Vector2 scale, Color color)
        {
			//TODO
        }


		// DRAW CANNON //
		void DrawCannon(Vector2 position, Vector2 scale, Color color)
        {
			//TODO
        }


		// DRAW DOCK //
		void DrawDock(string type, Vector2 position, Vector2 scale, Color iconColor, Color buttonColor)
        {
			//TODO
        }


		// DRAW SEAPARATION //
		void DrawSeparation(Vector2 position, Vector2 scale, Color color)
        {
			//TODO
        }


		// DRAW GAS //
		void DrawGas(string gasType, Vector2 position, Vector2 scale, Color iconColor, Color textColor)
        {
			//TODO
        }


		// DRAW TURRET //
		void DrawTurret(Vector2 position, Vector2 scale, Color color)
        {
			//TODO
        }


		// DRAW MISSILE TURRET //
		void DrawMissileTurret(Vector2 position, Vector2 scale, Color color)
        {
			//TODO
        }


		// DRAW JETTISON //
		void DrawJettison(Vector2 position, Vector2 scale, Color color)
        {
			//TODO
        }
	}
}
