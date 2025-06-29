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
            _logger.Command = argument;

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
                case "BUTTON_1":
                    PressButton(1, cmdArg);
                    break;
                case "BUTTON_2":
                    PressButton(2, cmdArg);
                    break;
                case "BUTTON_3":
                    PressButton(3, cmdArg);
                    break;
                case "BUTTON_4":
                    PressButton(4, cmdArg);
                    break;
                case "BUTTON_5":
                    PressButton(5, cmdArg);
                    break;
                case "BUTTON_6":
                    PressButton(6, cmdArg);
                    break;
                case "BUTTON_7":
                    PressButton(7, cmdArg);
                    break;
                case "BUTTON_8":
                    PressButton(8, cmdArg);
                    break;
                case "BUTTON_9":
                    PressButton(9, cmdArg);
                    break;
                case "NEXT_PAGE":
                    CyclePage(cmdArg);
                    break;
                case "LAST_PAGE":
                    CyclePage(cmdArg, true);
                    break;
                case "NEXT_SORTER":
                    CycleSorter(cmdArg, "");
                    break;
                case "LAST_SORTER":
                    CycleSorter(cmdArg, "", true);
                    break;
                case "NEXT_ORE":
                    CycleSorter(cmdArg, ORE_TAG);
                    break;
                case "LAST_ORE":
                    CycleSorter(cmdArg, ORE_TAG, true);
                    break;
                case "NEXT_IGT":
                case "NEXT_INGOT":
                    CycleSorter(cmdArg, INGOT_TAG);
                    break;
                case "LAST_IGT":
                case "LAST_INGOT":
                    CycleSorter(cmdArg, INGOT_TAG, true);
                    break;
                case "NEXT_CMP":
                    CycleSorter(cmdArg, CMP_TAG);
                    break;
                case "LAST_CMP":
                    CycleSorter(cmdArg, CMP_TAG, true);
                    break;
                case "NEXT_WEP":
                    CycleSorter(cmdArg, WEP_TAG);
                    break;
                case "LAST_WEP":
                    CycleSorter(cmdArg, WEP_TAG, true);
                    break;
                case "NEXT_AMMO":
                    CycleSorter(cmdArg, AMMO_TAG);
                    break;
                case "LAST_AMMO":
                    CycleSorter(cmdArg, AMMO_TAG, true);
                    break;
                case "NEXT_TOOL":
                    CycleSorter(cmdArg, TOOL_TAG);
                    break;
                case "LAST_TOOL":
                    CycleSorter(cmdArg, TOOL_TAG, true);
                    break;
                case "NEXT_MISC":
                    CycleSorter(cmdArg, MSC_TAG);
                    break;
                case "LAST_MISC":
                    CycleSorter(cmdArg, MSC_TAG, true);
                    break;
                case "TOGGLE_COLOR":
                    ToggleColor(cmdArg);
                    break;
                case "NEXT_LOG":
                    _logger.Scroll();
                    break;
                case "LAST_LOG":
                    _logger.Scroll(true);
                    break;
                case "CLEAR":
                    _logger.Clear();
                    break;
                case "REFRESH":
                    Build();
                    break;
                case "SET_GRIDID":
                case "SET_GRID_ID":
                    SetGridID(cmdArg);
                    break;
                default:
                    _logger.LogError("\nUNRECOGNIZED COMMAND:\n" + argument);
                    break;
            }
        }

        public void PressButton(int button, string menuId)
        {
            MenuViewer viewer = GetMenuViewer(menuId);
            if (viewer == null) return;

            viewer.PressButton(button);

            // Update display for every Menu associated with the same sorter
            string tag = viewer.GSorter.Tag;
            foreach(int key in _menuViewers.Keys)
            {
                MenuViewer menu = _menuViewers[key];
                if (menu.Id != viewer.Id && menu.GSorter.Tag == tag)
                    menu.UpdateActive();
            }
        }

        /*
        public void CycleSorter(string menuId, bool previous = false)
        {
            MenuViewer viewer = GetMenuViewer(menuId);
            if (viewer == null) return;

            viewer.CycleSorter("", previous);
        }
        */

        public void CycleSorter(string menuId, string type, bool previous = false)
        {
            MenuViewer viewer = GetMenuViewer(menuId);
            if (viewer == null) return;

            viewer.CycleSorter(type, previous);
        }

        public void CyclePage(string menuId, bool previous = false)
        {
            MenuViewer viewer = GetMenuViewer(menuId);
            if (viewer == null) return;

            viewer.CyclePages(previous);
        }

        public void ToggleColor(string menuId)
        {
            MenuViewer viewer = GetMenuViewer(menuId);
            if (viewer == null) return;

            viewer.ToggleColor();
        }
    }
}
