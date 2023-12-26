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
    partial class Program : MyGridProgram
    {
        // USER TAGS // - Adjust as needed
        const string MENU_TAG = "[VHB]"; // Tag use to assign constext menus to block
        const string SYSTEMS = "Flight Systems";
        const string BATTERIES = "Batteries";
        const string HYDROGEN_TANKS = "Hydrogen Tanks";
        const string DRILLS = "Drills";
        const string STONE_GROUP = "Stone Out";
        const string ICE_GROUP = "Ice Out";

        // USAP CONSTANTS
        const string USAP = "P:USAP Program"; // Name of USAP Program Block
        const string GEAR_TIMER = "Gear Timer [LG]"; // Timer that triggers USAP landing Gear Command

        // DO NOT EDIT BELOW THIS LINE ///////////////////////////////////////////////////////////////////////////////////////////////

        static IMyProgrammableBlock _me;

        //const string SLASHES = "///////////////";
        const string DASHES = " ------------------- ";

        static string _statusMessage;

        readonly string[] _breather = { "|", "/", "--", "\\" };
        static Byte _breath;

        public Program()
        {
            _me = Me;

            SetMainIni();

            Build();

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save(){}

        public void Main(string argument, UpdateType updateSource)
        {
            PrintHeader();
            PrintStats();
            CheckLitButtons();

            if (_statusMessage != "")
                Echo(DASHES + " MESSAGES " + DASHES + "\n" + _statusMessage);

            MainSwitch(argument);
            //MenuDebug();
        }

        void Build()
        {
            _breath = 0;
            _statusMessage = "";
            

            AssignMenus();
        }

        // PRINT HEADER //
        void PrintHeader()
        {
            Echo("VIRTUAL                 " + _breather[_breath] + "\nHOTBAR\n" + DASHES);

            _breath++;
            if (_breath >= _breather.Length)
                _breath = 0;
        }


        // PRINT STATS //
        void PrintStats()
        {
            if (_menus.Count < 1)
                Echo("NO MENUS FOUND");

            foreach(int key in _menus.Keys)
            {
                Menu menu = _menus[key];

                Echo("MENU " + menu.IDNumber + DASHES);
                Echo(" * Pages: " + menu.PageCount);
                foreach(int pageKey in menu.Pages.Keys)
                {
                    MenuPage page = menu.Pages[pageKey];

                }
            }
        }
    }
}
