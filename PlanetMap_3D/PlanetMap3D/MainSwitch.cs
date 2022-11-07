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
        void MainSwitch(string argument)
        {
			string[] args = argument.Split(' ');
			string[] cmds = args[0].ToUpper().Split('_');
			string command = cmds[0];
			string cmdArg = "";
			if (cmds.Length > 1)
				cmdArg = cmds[1];

			// Account for single instance commands with underscores
			if (cmdArg == "RADIUS" || cmdArg == "SHIP" || cmdArg == "JUMP")
				command = args[0];

			string argData = "";
			_statusMessage = "";
			_activeWaypoint = "";
			_previousCommand = "Command: " + argument;

			// If there are multiple words in the argument. Combine the latter words into the entity name.
			if (args.Length == 1)
			{
				argData = "0";
			}
			else if (args.Length > 1)
			{
				argData = args[1];
				if (args.Length > 2)
				{
					for (int q = 2; q < args.Length; q++)
					{
						argData += " " + args[q];
					}
				}
			}

			List<StarMap> maps = ArgToMaps(argData);

			switch (command)
			{
				case "ZOOM":
					ZoomMaps(maps, cmdArg);
					break;
				case "MOVE":
					MoveMaps(maps, cmdArg);
					break;
				case "DEFAULT":
					MapsToDefault(maps);
					break;
				case "ROTATE":
					RotateMaps(maps, cmdArg);
					break;
				case "SPIN":
					SpinMaps(maps, cmdArg, ANGLE_STEP / 2);
					break;
				case "TRACK":
					TrackMaps(maps, cmdArg);
					break;
				case "STOP":
					StopMaps(maps);
					break;
				case "GPS":
					if (cmdArg == "ON")
					{
						Show(maps, "GPS", 1);
					}
					else
					{
						Show(maps, "GPS", 0);
					}
					break;
				case "HIDE":
					if (cmdArg == "WAYPOINT")
					{
						SetWaypointState(argData, 0);
					}
					else
					{
						Show(maps, cmdArg, 0);
					}
					break;
				case "SHOW":
					if (cmdArg == "WAYPOINT")
					{
						SetWaypointState(argData, 1);
					}
					else
					{
						Show(maps, cmdArg, 1);
					}
					break;
				case "TOGGLE":
					if (cmdArg == "WAYPOINT")
					{
						SetWaypointState(argData, 2);
					}
					else
					{
						Show(maps, cmdArg, 3);
					}
					break;
				case "CYCLE"://GPS
					cycleGPSForList(maps);
					break;
				case "NEXT":
					nextLast(maps, cmdArg, true);
					break;
				case "PREVIOUS":
					nextLast(maps, cmdArg, false);
					break;
				case "WORLD"://MODE
					ChangeMode("WORLD", maps);
					break;
				case "SHIP"://MODE
					ChangeMode("SHIP", maps);
					break;
				case "CHASE"://MODE
					ChangeMode("CHASE", maps);
					break;
				case "PLANET"://MODE
					ChangeMode("PLANET", maps);
					break;
				case "FREE"://MODE
					ChangeMode("FREE", maps);
					break;
				case "ORBIT"://MODE
					ChangeMode("ORBIT", maps);
					break;
				case "DECREASE_RADIUS":
					AdjustRadiusForList(maps, false);
					break;
				case "INCREASE_RADIUS":
					AdjustRadiusForList(maps, true);
					break;
				case "CENTER_SHIP":
					MapsToShip(maps);
					break;
				case "WAYPOINT":
					waypointCommand(cmdArg, argData);
					break;
				case "PASTE":
					ClipboardToLog(cmdArg, argData);
					break;
				case "EXPORT"://WAYPOINT
					_clipboard = LogToClipboard(argData);
					break;
				case "PROJECT":
					ProjectPoint(cmdArg, argData);
					break;
				case "NEW"://PLANET
					NewPlanet(argData);
					break;
				case "LOG":
					if (cmdArg == "NEXT")
					{
						LogNext(argData);
					}
					else if (cmdArg == "BATCH")
					{
						LogBatch();
					}
					else
					{
						LogWaypoint(argData, _myPos, cmdArg, "WHITE");
					}
					break;
				case "COLOR":
					if (cmdArg == "PLANET")
					{
						SetPlanetColor(argData);
					}
					else
					{
						SetWaypointColor(argData);
					}
					break;
				case "MAKE":
					SetWaypointType(cmdArg, argData);
					break;
				case "PLOT_JUMP":
					PlotJumpPoint(argData);
					break;
				case "SCROLL":
					pageScroll(cmdArg);
					break;
				case "BRIGHTEN":
					if (_brightnessMod < BRIGHTNESS_LIMIT)
						_brightnessMod += 0.25f;
					break;
				case "DARKEN":
					if (_brightnessMod > 1)
						_brightnessMod -= 0.25f;
					break;
				case "DELETE":
					if (cmdArg == "PLANET")
					{
						DeletePlanet(argData);
					}
					else
					{
						SetWaypointState(argData, 3);
					}
					break;
				case "SYNC":
					sync(cmdArg, argData);
					break;
				case "REFRESH":
					Build();
					break;
				case "UPDATE": // TAGS / GRID_ID
				case "SET": // GRID_ID
					if (cmdArg.Contains("GRID") || cmdArg.Contains("TAGS"))
						SetGridID();
					break;
				case "BUTTON":
					ButtonPress(cmdArg, argData);
					break;
				default:
					_statusMessage = "UNRECOGNIZED COMMAND!";
					break;
			}

			if (maps.Count > 0)
			{
				foreach (StarMap cmdMap in maps)
				{
					UpdateMap(cmdMap);
					MapToParameters(cmdMap);
				}
			}
		}


		// NEXT LAST // - Multi level switch command
		void nextLast(List<StarMap> maps, string arg, bool state)
		{
			switch (arg)
			{
				case "PLANET":
					CyclePlanetsForList(maps, state);
					break;
				case "WAYPOINT":
					CycleWaypointsForList(maps, state);
					break;
				case "MODE":
					CycleModeForList(maps, state);
					break;
				case "PAGE":
					NextPage(state);
					break;
				case "MENU":
					NextMenu(arg, state);
					break;
			}
		}


		// BRIDGE FUNCTIONS // Ensure that commands from old switch are backwards compatible /////////////////////////////////////////////

		// WAYPOINT COMMAND // Bridge function to eliminate old switch cases.
		void waypointCommand(string arg, string waypointName)
		{
			int state = 0;
			if (arg == "ON")
				state = 1;

			SetWaypointState(waypointName, state);
		}
	}
}
