using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
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
        const string MAIN_HEADER = "GSIP";


        static IMyProgrammableBlock _me;

        const string SLASHES = "///////////////";
        const string DASHES = "-------------------";

        IMyTextSurface _dataScreen;
        static Logger _logger;


        public Program()
        {
            Build();
        }

        public void Save(){}

        public void Main(string argument, UpdateType updateSource)
        {
            MainSwitch(argument);
            ShowData();
        }

        public void Build()
        {
            _logger = new Logger();
            _me = Me;
            _programIni = new MyIniHandler(Me);

            _gridID = _programIni.GetKey(SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString());

            AddDataScreen();
            AddSorters();
            AddMenuViewers();
            // TODO

            // TODONE

            ShowData();
        }


        // ADD DATA SCREEN //
        public void AddDataScreen()
        {
            string screenIndex = _programIni.GetKey(MAIN_HEADER, "DataScreen", "0");

            switch (screenIndex)
            {
                case "0":
                    _dataScreen = _me.GetSurface(0);
                    break;
                case "1":
                    _dataScreen = _me.GetSurface(1);
                    break;
                default:
                    _dataScreen = null;
                    return;
            }

            _dataScreen.ContentType = ContentType.TEXT_AND_IMAGE;
        }


        // SHOW DATA //
        public void ShowData()
        {
            string data = "// GSIP " + SLASHES + SLASHES + "\n";

            data += "   Sorter Count: " + _sorters.Count + "\n"
                  + "   Viewer Count: " + _menuViewers.Count;

            if(_menuViewers.Count > 0)
            {
                data += "\nMENU VIEWERS:";
                foreach(int key in _menuViewers.Keys)
                {
                    MenuViewer viewer = _menuViewers[key];
                    data += "\n   " + key + ": viewing sorter " + viewer.GSorter.Tag + " - Page " + viewer.CurrentPage + " of " + viewer.PageCount;

                    if (viewer.MenuPage == null || viewer.MenuPage.Buttons.Count == 0) continue;

                    for (int i = 0; i < viewer.ButtonCount; i++)
                    {
                        MenuButton button = viewer.MenuPage.Buttons[i];
                        string active = button.Active ? "On " : "Off ";
                        data += "\n    [" + (i + 1) + "] " + active + button.Filter;
                    }
                }
            }


            /*
            if (_sorters.Count > 0)
            {
                foreach (string key in _sorters.Keys)
                {
                    GSorter sorter = _sorters[key];
                    data += "\n* " + sorter.SorterBlock.CustomName;

                    if(sorter.Filters.Length < 1) { continue; }

                    for(int i = 0; i < sorter.Filters.Length; i++)
                    {
                        data += "\n  - " + sorter.Filters[i]; 
                    }
                }
            }
            */

            data += "\n" + _logger.PrintMessages();

            Echo(data);

            _dataScreen.WriteText(data);
        }
    }
}
