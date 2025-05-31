using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        //const string SLASHES = "///////////////";
        const string DASHES = " ------------------- ";

        static string _statusMessage;
        IMyTextSurface _dataScreen;


        public Program()
        {
            Build();
        }

        public void Save(){}

        public void Main(string argument, UpdateType updateSource)
        {
            ShowData();
        }

        public void Build()
        {
            _me = Me;
            _programIni = new MyIniHandler(Me);

            _statusMessage = "";
            _gridID = _programIni.GetKey(SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString());

            AddDataScreen();
            AddSorters();

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
            string data = "-- GSIP " + DASHES ;

            if (_statusMessage != "")
                data += "\n" + _statusMessage;

            data += "\n" + "Sorter Count: " + _sorters.Count;

            if (_sorters.Count > 0)
            {
                foreach (string key in _sorters.Keys)
                {
                    data += "\n* " + _sorters[key].Sorter.CustomName;
                }
            }

            Echo(data);

            if (_dataScreen != null)
            {
                _dataScreen.WriteText(data);
            }
        }
    }
}
