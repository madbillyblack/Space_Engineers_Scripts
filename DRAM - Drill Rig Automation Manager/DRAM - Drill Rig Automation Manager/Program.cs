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
        const string MAIN_TAG = "[DRAM]";
        const float H_STEP = 1;
        const float V_STEP = 1;
        const float B_START = 5;
        const float SPEED = 3;
        const string STOP = "STOPPED";
        const string CYCLE = "CYCLING";
        const string RETRACT = "RETRACTING";

        public string _state; // Current operational state of the rig
        public float _baseStart; // Starting max distance of BasePiston Assembly
        public float _vertStep; // Total vertical step taken after rig retracts
        public float _horzStep; // Total horizontal step taken after rig completes a basic cycle
        public static string _statusMessage;

        public Program()
        {
            _me = Me;
            SetMainIni();
            _statusMessage = "";

            Build();
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {

        }

        public void Build()
        {
            _state = GetMainKey(MAIN_TAG, "STATE", STOP);
            _horzStep = ParseFloat(GetMainKey(MAIN_TAG, "Horizontal Step", H_STEP.ToString()), H_STEP);
            _vertStep = ParseFloat(GetMainKey(MAIN_TAG, "Vertical Step", V_STEP.ToString()), V_STEP);
            _baseStart = ParseFloat(GetMainKey(MAIN_TAG, "Base Start", B_START.ToString()), B_START);

            _BasePistons = new PistonAssembly();
            _VertPistons = new PistonAssembly();
            _HorzPistons = new PistonAssembly();

            AddPistons();
        }
    }
}
