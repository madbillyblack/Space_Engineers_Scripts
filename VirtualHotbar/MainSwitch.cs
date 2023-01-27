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
            if (!string.IsNullOrEmpty(argument))
            {
                Echo("CMD: " + argument);

                string[] args = argument.Split(' ');
                string arg = args[0].ToUpper();

                string cmdArg = "";
                if (args.Length > 1)
                {
                    for (int i = 1; i < args.Length; i++)
                    {
                        cmdArg += args[i] + " ";
                    }

                    cmdArg = cmdArg.Trim();
                }

                switch (arg)
                {
                    case "REFRESH":
                        Build();
                        break;
                    case "BUTTON_1":
                        PressButton(cmdArg, 1);
                        break;
                    case "BUTTON_2":
                        PressButton(cmdArg, 2);
                        break;
                    case "BUTTON_3":
                        PressButton(cmdArg, 3);
                        break;
                    case "BUTTON_4":
                        PressButton(cmdArg, 4);
                        break;
                    case "BUTTON_5":
                        PressButton(cmdArg, 5);
                        break;
                    case "BUTTON_6":
                        PressButton(cmdArg, 6);
                        break;
                    case "BUTTON_7":
                        PressButton(cmdArg, 7);
                        break;
                    case "BUTTON_8":
                        PressButton(cmdArg, 8);
                        break;
                    case "BUTTON_9":
                        PressButton(cmdArg, 9);
                        break;
                    case "NEXT_MENU":
                        NextMenuPage(cmdArg);
                        break;
                    case "PREVIOUS_MENU":
                        PreviousMenuPage(cmdArg);
                        break;
                    case "DRAW_MENUS":
                        DrawAllMenus();
                        break;
                    case "UPDATE_GRID_ID":
                    case "SET_GRID_ID":
                        SetGridID(cmdArg);
                        break;
                    default:
                        _statusMessage += "\nUNRECOGNIZED COMMAND:\n" + arg;
                        break;
                }
            }
        }
    }
}
