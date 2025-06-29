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

        static IMyTextSurface _dataScreen;
        static IMyTextSurface _logScreen;
        static Logger _logger;
        static string _basicData;
        

        public Program()
        {
            _defBgColor = ParseColor(DF_BG);
            _defButtonColor = _defBgColor * 1.12f;
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

            InitializeGridId();
            AddDataScreens();
            AddSorters();
            AddMenuViewers();
            DrawAllMenus();
            ShowData();
        }


        // ADD DATA SCREENS //
        public void AddDataScreens()
        {
            _dataScreen = GetProgramScreen(_programIni.GetKey(MAIN_HEADER, "DataScreen", "0"));
            
            if(_dataScreen != null)
                _dataScreen.ContentType = ContentType.TEXT_AND_IMAGE;

            _logScreen = GetProgramScreen(_programIni.GetKey(MAIN_HEADER, "LogScreen", "1"));

            if(_logScreen != null)
                _logScreen.ContentType = ContentType.TEXT_AND_IMAGE;
        }

        IMyTextSurface GetProgramScreen(string screenIndex)
        {
            switch(screenIndex)
            {
                case "0":
                    return _me.GetSurface(0);
                case "1":
                    return _me.GetSurface(1);
                default:
                    return null;
            }
        }

        // SHOW DATA //
        public void ShowData()
        {
            UpdateData();

            string logData = _logger.PrintMessages();
            string allData = _basicData + "\n" + logData;

            Echo(allData);

            if(_dataScreen != null && _logScreen != null)
            {
                _dataScreen.WriteText(_basicData);
                _logScreen.WriteText(logData);
            }
            else if (_dataScreen != null)
            {
                _dataScreen.WriteText(allData);
            }
            else if(_logScreen != null)
            {
                _logScreen.WriteText(logData);
            }
        }

        void UpdateData()
        {
            _basicData = "// GSIP " + SLASHES + SLASHES + "\n"
                + "   Sorter Count: " + _sorters.Count + "  -  Viewer Count: " + _menuViewers.Count;

            if (_menuViewers.Count > 0)
            {
                _basicData += "\n\nMENU VIEWERS:";
                foreach (int key in _menuViewers.Keys)
                {
                    MenuViewer viewer = _menuViewers[key];
                    _basicData += "\n * " + key + ": viewing sorter " + viewer.GSorter.Tag + " - Page " + viewer.CurrentPage + " of " + viewer.PageCount;
                    //_basicData += " - " + viewer.Viewport.Width + " x " + viewer.Viewport.Height + " - ";
                }
            }

            if (_sorters.Count > 0)
            {
                _basicData += "\n\nSORTERS:";
                foreach (string key in _sorters.Keys)
                {
                    GSorter sorter = _sorters[key];

                    List<MyInventoryItemFilter> currentFilters = new List<MyInventoryItemFilter>();
                    sorter.SorterBlock.GetFilterList(currentFilters);

                    string mode = sorter.SorterBlock.Mode.ToString();

                    _basicData += "\n * " + key + " - " + sorter.SorterBlock.CustomName + "  -  " + mode + " - Active Filters: " + sorter.ActiveFilterCount();
                }
            }
        }
    }
}
