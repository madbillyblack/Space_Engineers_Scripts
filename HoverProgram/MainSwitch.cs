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
        public void MainSwitch(string arg)
        {
            _lastCommand = arg;
            string cmd;
            string data;
            string[] args = arg.Split(' ');

            if(args.Length > 1)
            {
                cmd = args[0].Trim();  
                data = args[1].Trim();
            }
            else
            {
                cmd = arg;
                data = "";
            }

            switch (cmd.ToUpper())
            {
                case "TOGGLE_HOVER":
                    ToggleHoverThrusters();
                    break;
                case "HOVER_ON":
                    StartHover();
                    break;
                case "HOVER_OFF":
                    StopHover();
                    break;
                case "REFRESH":
                    Build();
                    break;
                case "SET_GRID_ID":
                    SetGridID(data);
                    break;
                case "SET_HEIGHT":
                    SetTargetHeight(data);
                    break;
                case "DECREASE_HEIGHT":
                    DecreaseHeight(data);
                    break;
                case "INCREASE_HEIGHT":
                    IncreaseHeight(data);
                    break;
                case "SET_PARK":
                case "SET_PARK_HEIGHT":
                    SetParkHeight();
                    break;
                default:
                    _statusMessage = "UNKOWN COMMAND: \"" + arg + "\"";
                    break;
            }
        }
    }
}
