using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Remoting.Messaging;
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
            int startPage = 1;

            if (args.Length > 0)
                profile = args[0];
            else
                profile = "GENERAL";

            if (args.Length > 1)
                menuKey = args[1];

            if (args.Length > 2)
                startPage = ParseInt(args[2], 1);


            Menu menu = GetMenuByString(menuKey);
            if (MenuNotFound(menu)) return;

            // Set button count to 8
            menu.SetButtonLimit(8);

            // Add new page if necessary
            if (startPage > menu.PageCount)
            {
                menu.UpdatePageCount(menu.PageCount + 1);

                startPage = menu.PageCount;

                _statusMessage += "Page " + startPage + " added to Menu " + menu.IDNumber + "\n";
            }

            switch (profile.ToUpper())
            {
                case "GENERAL":
                    LoadGeneralProfile(menu, startPage);
                    break;
                case "MINING":
                case "MINER":
                    LoadMiningProfile(menu, startPage);
                    break;
                case "CONSTRUCTION":
                case "CONSTRUCTOR":
                    LoadConstructorProfile(menu, startPage);
                    break;
                case "COMBAT":
                    LoadCombatProfile(menu, startPage);
                    break;
                case "MISSILE":
                case "LAMP":
                    LoadMissileProfile(menu, startPage);
                    break;
                case "RELAY":
                case "TRANSPONDER":
                    LoadTransponderProfile(menu, startPage);
                    break;
                default:
                    _statusMessage += "\nPROFILE \"" + profile + "\" not found.";
                    return;
            }

            Build();
        }


        // LOAD GENERAL PROFILE //
        public void LoadGeneralProfile(Menu menu, int page)
        {
            // Set Page Title
            menu.SetPageTitleKey(page, "Primary Systems");

            // Set Buttons 1 - 4
            SetMainFour(menu);

            //TODO

            // Button 5
            string header = ButtonHeader(page, 5);
            menu.SetButtonKeys(header, "", "", "", "", "");

            // Button 6
            header = ButtonHeader(page, 6);
            menu.SetButtonKeys(header, "", "", "", "", "");

            // Button 7
            header = ButtonHeader(page, 7);
            menu.SetButtonKeys(header, "", "", "", "", "");

            // Button 8
            header = ButtonHeader(page, 8);
            menu.SetButtonKeys(header, "", "", "", "", "");
        }


        // LOAD MINING PROFILE //
        public void LoadMiningProfile(Menu menu, int page)
        {
            // Set Page Title
            menu.SetPageTitleKey(page, "Mining Systems");

            // Set Buttons 1 - 4
            SetMainFour(menu);

            // Button 5
            string header = ButtonHeader(page, 5);
            string groupName = AddShipTag(DRILLS);
            string toggleBlock = FirstNameFromGroup(groupName);
            menu.SetButtonKeys(header, "G:" + groupName, DRILLS, ON_OFF, "{DRILL}", toggleBlock);

            // Button 6
            header = ButtonHeader(page, 6);
            groupName = AddShipTag(STONE_GROUP);
            toggleBlock = FirstNameFromGroup(groupName);
            menu.SetButtonKeys(header, "G:" + groupName, "STONE", ON_OFF, JETTISON, toggleBlock);

            // Button 7
            header = ButtonHeader(page, 7);
            groupName = AddShipTag(ICE_GROUP);
            toggleBlock = FirstNameFromGroup(groupName);
            menu.SetButtonKeys(header, "G:" + groupName, "ICE", ON_OFF, JETTISON, toggleBlock);

            // Button 8
            header = ButtonHeader(page, 8);
            string blockName = AddShipTag(USAP);
            menu.SetButtonKeys(header, "P:" + blockName, "UNLOAD", "UNLOAD", JETTISON, "");
        }


        // LOAD CONSTRUCTOR PROFILE //
        public void LoadConstructorProfile(Menu menu, int page)
        {
            // Set Page Title
            menu.SetPageTitleKey(page, "Construction Systems");

            // Set Buttons 1 - 4
            SetMainFour(menu);

            // Button 5
            string header = ButtonHeader(page, 5);
            string groupName = AddShipTag(WELDERS);
            string toggleBlock = FirstNameFromGroup(groupName);
            menu.SetButtonKeys(header, "G:" + groupName, "WELD", ON_OFF, "{WELDER}", toggleBlock);

            // Button 6
            header = ButtonHeader(page, 6);
            groupName = AddShipTag(GRINDERS);
            toggleBlock = FirstNameFromGroup(groupName);
            menu.SetButtonKeys(header, "G:" + groupName, "GRIND", ON_OFF, "{GRINDER}", toggleBlock);

            // Button 7
            header = ButtonHeader(page, 7);
            string blockName = AddShipTag(BOOM_TIMER);
            string delay = GetTimeFromTimer(blockName).ToString();
            menu.SetButtonKeys(header, blockName, "BOOM", TRIGGER, "{DOWN_PISTON}", "", delay);

            // Button 8
            header = ButtonHeader(page, 8);
            blockName = AddShipTag(USAP);
            menu.SetButtonKeys(header, "P:" + blockName, "SUPPLY", "RESUPPLY", "{CYCLE}", "");
        }


        // LOAD COMBAT PROFILE //
        public void LoadCombatProfile(Menu menu, int page)
        {
            // Set Page Title
            menu.SetPageTitleKey(page, "Combat Systems");

            // Set Buttons 1 - 4
            SetMainFour(menu);

            // TODO

            // Button 5
            string header = ButtonHeader(page, 5);
            string groupName = AddShipTag(WEAPONS);
            string toggleBlock = FirstNameFromGroup(groupName);
            menu.SetButtonKeys(header, "G:" + groupName, "WEP", ON_OFF, "{TARGET}", toggleBlock);

            // Button 6
            header = ButtonHeader(page, 6);
            groupName = AddShipTag(TURRETS);
            toggleBlock = FirstNameFromGroup(groupName);
            menu.SetButtonKeys(header, "G:" + groupName, "TURRET", ON_OFF, "{TURRET}", toggleBlock);

            // Button 7
            header = ButtonHeader(page, 7);
            menu.SetButtonKeys(header, "{AUX}", "CAMERA", "", "{CAMERA}", "");

            // Button 8
            header = ButtonHeader(page, 8);
            string blockName = AddShipTag(USAP);
            menu.SetButtonKeys(header, "P:" + blockName, "RELOAD", "RELOAD", "{CYCLE}", "");
        }


        // LOAD MISSILE PROFILE //
        public void LoadMissileProfile(Menu menu, int page)
        {
            // TODO
        }


        // LOAD TRANSPONDER PROFILE //
        public void LoadTransponderProfile(Menu menu, int page)
        {
            // Set Page Title
            menu.SetPageTitleKey(page, "Action Relay");

            // Button 1
            string header = ButtonHeader(page, 1);
            string blockName = AddShipTag(RELAY);
            menu.SetButtonKeys(header, blockName, "CH-1", "SET 1", "{SIGNAL}", blockName + ";1");

            // Button 2
            header = ButtonHeader(page, 2);
            blockName = AddShipTag(RELAY);
            menu.SetButtonKeys(header, blockName, "CH-2", "SET 2", "{SIGNAL}", blockName + ";2");

            // Button 3
            header = ButtonHeader(page, 3);
            blockName = AddShipTag(RELAY);
            menu.SetButtonKeys(header, blockName, "CH-3", "SET 3", "{SIGNAL}", blockName + ";3");

            // Button 4
            header = ButtonHeader(page, 4);
            blockName = AddShipTag(RELAY);
            menu.SetButtonKeys(header, blockName, "CH-4", "SET 4", "{SIGNAL}", blockName + ";4");

            // Button 5
            header = ButtonHeader(page, 5);
            blockName = AddShipTag(RELAY);
            menu.SetButtonKeys(header, blockName, "CH-5", "SET 5", "{SIGNAL}", blockName + ";5");

            // Button 6
            header = ButtonHeader(page, 6);
            blockName = AddShipTag(RELAY);
            menu.SetButtonKeys(header, blockName, "CH-6", "SET 6", "{SIGNAL}", blockName + ";6");

            // Button 7
            header = ButtonHeader(page, 7);
            menu.SetButtonKeys(header, "{AUX}", "CAMERA", "", "{CAMERA}", "");

            // Button 8
            header = ButtonHeader(page, 8);
            blockName = AddShipTag(RELAY);
            menu.SetButtonKeys(header, blockName, "SEND", "SEND", "{BROADCAST}", "");
        }



        // SET MAIN FOUR // - Sets the first/main 3 buttons for most menus
        public void SetMainFour(Menu menu)
        {
            // Button 1
            string header = ButtonHeader(1, 1);
            string groupName = AddShipTag(SYSTEMS);
            string toggleBlock = FirstNameFromGroup(groupName);
            menu.SetButtonKeys(header, "G:" + groupName, "SYS.", ON_OFF, "{TOGGLE}", toggleBlock);

            // Button 2
            header = ButtonHeader(1, 2);
            groupName = AddShipTag(BATTERIES);
            toggleBlock = FirstNameFromGroup(groupName);
            menu.SetButtonKeys(header, "G:" + groupName, "BATT", RECHARGE, BATT_LABEL, toggleBlock + ";" + RECHARGE);

            // Button 3
            header = ButtonHeader(1, 3);
            groupName = AddShipTag(HYDROGEN_TANKS);
            toggleBlock = FirstNameFromGroup(groupName);
            menu.SetButtonKeys(header, "G:"+groupName, "FUEL", STOCKPILE, H2_TANK, toggleBlock + ";" + STOCKPILE);

            // Button 4
            header = ButtonHeader(1, 4);
            string timerName = AddShipTag(GEAR_TIMER);
            string delay = GetTimeFromTimer(timerName).ToString();
            menu.SetButtonKeys(header, timerName, "GEAR", TRIGGER, GEAR_LABEL, "", delay);
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


        // GET TIME FROM TIMER //
        public float GetTimeFromTimer(string timerName)
        {
            try
            {
                IMyTimerBlock timer = GridTerminalSystem.GetBlockWithName(timerName) as IMyTimerBlock;
                return timer.TriggerDelay;
            }
            catch
            {
                return 0f;
            }
        }


        /* TAG AND GROUP // - Add Ship Tag and Block Prefix to Group String
         * Block Prefixes: 
         *  "G:" - Block Group
         *  "P:" - Program Block   */
        public string AddShipTag(string name)
        {
            if (_suffixTag)
                return name + _shipTag;
            else
                return _shipTag + name;
        }
    }
}
