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
        // Action Constants
        const string ON_OFF = "OnOff";
        const string RECHARGE = "Recharge";
        const string STOCKPILE = "Stockpile";
        const string TRIGGER = "TriggerNow";       

        // LOAD PROFILE //
        public void LoadProfile(string arguments)
        {
            string menuKey = "";
            string profile;
            string[] args = arguments.Trim().Split(' ');

            if (args.Length > 0)
                profile = args[0];
            else
                profile = "GENERAL";

            if (args.Length > 1)
                menuKey = args[1];

            Menu menu = GetMenuByString(menuKey);
            if (MenuNotFound(menu)) return;

            // Set button count to 8
            menu.SetButtonLimit(8);

            switch(profile.ToUpper())
            {
                case "GENERAL":
                    LoadGeneralProfile(menu);
                    break;
                case "MINING":
                case "MINER":
                    LoadMiningProfile(menu);
                    break;
                case "CONSTRUCTION":
                case "CONSTRUCTOR":
                    LoadConstructorProfile(menu);
                    break;
                case "COMBAT":
                    LoadCombatProfile(menu);
                    break;
                case "MISSILE":
                case "LAMP":
                    LoadMissileProfile(menu);
                    break;
                default:
                    _statusMessage += "\nPROFILE \"" + profile + "\" not found.";
                    break;
            }

        }


        // LOAD GENERAL PROFILE //
        public void LoadGeneralProfile(Menu menu)
        {

        }


        // LOAD MINING PROFILE //
        public void LoadMiningProfile(Menu menu)
        {
            // Set Page Title
            menu.SetPageTitleKey(1, "Mining Systems");

            // Set Buttons 1 - 3
            SetMainThree(menu);

            // Button 4
            string header = ButtonHeader(1, 4);
            menu.SetButtonKeys(header, USAP, "UNLOAD", "UNLOAD", JETTISON, "");

            // Button 5
            header = ButtonHeader(1, 5);
            string toggleBlock = FirstNameFromGroup(DRILLS);
            menu.SetButtonKeys(header, "G:" + DRILLS, DRILLS, ON_OFF, "{DRILL}", toggleBlock);

            // Button 6
            header = ButtonHeader(1, 6);
            toggleBlock = FirstNameFromGroup(STONE_GROUP);
            menu.SetButtonKeys(header, "G:" + STONE_GROUP, "STONE", ON_OFF, JETTISON, toggleBlock);

            // Button 7
            header = ButtonHeader(1, 7);
            toggleBlock = FirstNameFromGroup(ICE_GROUP);
            menu.SetButtonKeys(header, "G:" + ICE_GROUP, "ICE", ON_OFF, JETTISON, toggleBlock);

            // Button 8
            header = ButtonHeader(1, 8);
            menu.SetButtonKeys(header, GEAR_TIMER, "GEAR", TRIGGER, GEAR_LABEL, "");

            Build();
        }


        // LOAD CONSTRUCTOR PROFILE //
        public void LoadConstructorProfile(Menu menu)
        {

        }


        // LOAD COMBAT PROFILE //
        public void LoadCombatProfile(Menu menu)
        {

        }


        // LOAD MISSILE PROFILE //
        public void LoadMissileProfile(Menu menu)
        {

        }


        // SET MAIN THREE // - Sets the first/main 3 buttons for most menus
        public void SetMainThree(Menu menu)
        {
            // Button 1
            string header = ButtonHeader(1, 1);
            string toggleBlock = FirstNameFromGroup(SYSTEMS);
            menu.SetButtonKeys(header, "G:" + SYSTEMS, "SYS.", ON_OFF, "{TOGGLE}", toggleBlock);

            // Button 2
            header = ButtonHeader(1, 2);
            toggleBlock = FirstNameFromGroup(BATTERIES);
            menu.SetButtonKeys(header, "G:" + BATTERIES, "BATT", RECHARGE, BATT_LABEL, toggleBlock + ";" + RECHARGE);

            // Button 3
            header = ButtonHeader(1, 3);
            toggleBlock = FirstNameFromGroup(HYDROGEN_TANKS);
            menu.SetButtonKeys(header, "G:" + HYDROGEN_TANKS, "FUEL", STOCKPILE, H2_TANK, toggleBlock + ";" + STOCKPILE);
        }


        // FIRST NAME OF GROUP //
        public string FirstNameFromGroup(string groupName)
        {
            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(groupName);

            if (group == null)
                return "";

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            group.GetBlocks(blocks);

            if (blocks.Count > 0)
                return blocks[0].CustomName;

            return "";
        }
    }
}
