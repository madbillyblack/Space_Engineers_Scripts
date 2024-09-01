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
        public static string _statusMessage;

        public Program()
        {
            Build();
        }

        public void Save(){}

        public void Main(string argument, UpdateType updateSource)
        {

        }

        public void Build()
        { 
            _programIni = new MyIniHandler(Me);

            _statusMessage = "";
            _gridID = _programIni.GetKey(SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString());
            // TODO
        }
    }
}
