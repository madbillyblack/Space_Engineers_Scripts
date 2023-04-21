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
        void MainSwitch(string arg)
        {
			_statusMessage = "";
			_previosCommand = arg;

			string[] args = arg.Split(' ');

			// Main command
			string command = args[0].ToUpper();

			// Trailing command arguments
			string cmdArg = "";
			if (args.Length > 1)
			{
				for (int i = 1; i < args.Length; i++)
					cmdArg += args[i] + " ";

				cmdArg = cmdArg.Trim();
			}

			// Command Switch
			switch (command)
			{
				case "OPEN_LOCK":
				case "OPENLOCK":
					OpenLock(cmdArg, false);
					break;
				case "OPEN_ALL":
					OpenLock(cmdArg, true);
					break;
				case "TIMER_CALL":
					TimerCall(cmdArg);
					break;
				case "CLOSE_LOCK":
				case "CLOSELOCK":
					CloseLock(cmdArg);
					break;
				case "CYCLE_LOCK":
				case "CYCLELOCK":
					CycleLock(cmdArg);
					break;
				case "DOCK_SEAL":
					DockSeal(GetSector(cmdArg));
					break;
				case "UNDOCK":
					Undock(GetSector(cmdArg));
					break;
				case "OVERRIDE":
					Override(cmdArg, 1);
					break;
				case "RESTORE":
				case "RESTORE_OVERRIDE":
					Override(cmdArg, 0);
					break;
				case "TOGGLE_OVERRIDE":
					Override(cmdArg, 2);
					break;
				case "REFRESH":
					Build();
					break;
				case "SET_GRID_ID":
					SetGridID(cmdArg);
					break;
				case "SET_LIGHT_COLOR":
					SetSectorParameter(cmdArg, "color", false);
					break;
				case "SET_EMERGENCY_COLOR":
					SetSectorParameter(cmdArg, "color", true);
					break;
				case "SET_LIGHT_RADIUS":
					SetSectorParameter(cmdArg, "radius", false);
					break;
				case "SET_EMERGENCY_RADIUS":
					SetSectorParameter(cmdArg, "radius", true);
					break;
				case "SET_LIGHT_INTENSITY":
					SetSectorParameter(cmdArg, "intensity", false);
					break;
				case "SET_EMERGENCY_INTENSITY":
					SetSectorParameter(cmdArg, "intensity", true);
					break;
				case "SET_AUTO_CLOSE":
					SetSectorParameter(cmdArg, "auto_close_delay", false);
					break;
				case "VENT_CHECK_1": //For event triggered operation of script. Call from Vent actions, etc.
				case "VENT_CHECK_2":
					Sector mySector = GetSector(cmdArg);
					if (!UnknownSector(mySector, "Room"))
						mySector.Check();
					break;
				case "CLEAR":
					_statusMessage = "";
					break;
				case "SHOW_BUILD":
					_statusMessage = _buildMessage;
					break;
				default:
					_statusMessage = "UNRECOGNIZED COMMAND: " + arg;
					break;
			}
		}
    }
}
