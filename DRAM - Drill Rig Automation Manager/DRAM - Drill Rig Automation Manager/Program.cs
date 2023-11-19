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
        const string MAIN_HEADER = "DRAM";
        const string MAIN_TAG = "[DRAM]";
       
        const string STOP = "STOPPED";
        const string CYCLE = "CYCLING";
        const string RETRACT = "RETRACTING";
        const string PHASE = "PHASE"; // Ini Key for drill phase

        public string _phase; // Current operational state of the rig
        public float _baseStart; // Starting max distance of BasePiston Assembly
        public float _vertStep; // Total vertical step taken after rig retracts
        public float _horzStep; // Total horizontal step taken after rig completes a basic cycle
        public float _pistonSpeed; // Magnitude of piston velocity
        public float _rotorSpeed; // Magnitude of rotor velocity
        public static string _statusMessage;
        public int _vertCount, _horzCount, _baseCount;

        public List<IMyShipDrill> _drills;

        public Program()
        {
            _me = Me;
            SetMainIni();
            _statusMessage = "";

            Build();
        }

        public void Save(){}

        public void Main(string argument, UpdateType updateSource)
        {
            MainSwitch(argument);
        }

        public void Build()
        {
            _horzStep = ParseFloat(GetMainKey(MAIN_HEADER, "Horizontal Step", H_STEP.ToString()), H_STEP);
            _vertStep = ParseFloat(GetMainKey(MAIN_HEADER, "Vertical Step", V_STEP.ToString()), V_STEP);
            _baseStart = ParseFloat(GetMainKey(MAIN_HEADER, "Base Start", B_START.ToString()), B_START);
            _pistonSpeed = ParseFloat(GetMainKey(MAIN_HEADER, "Piston Speed", PISTON_SPEED.ToString()), PISTON_SPEED);
            _rotorSpeed = ParseFloat(GetMainKey(MAIN_HEADER, "Rotor Speed", ROTOR_SPEED.ToString()), ROTOR_SPEED);
            _phase = GetMainKey(MAIN_HEADER, PHASE, STOP);

            _BasePistons = new PistonAssembly();
            _VertPistons = new PistonAssembly();
            _HorzPistons = new PistonAssembly();

            AddPistons();
        }


        // ADD DRILLS //
        public void AddDrills()
        {
            _drills = new List<IMyShipDrill>();

            List<IMyShipDrill> allDrills = new List<IMyShipDrill>();
            GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(allDrills);

            if (allDrills.Count < 1)
            {
                _statusMessage += "No Drills found!\n";
                return;
            }

            foreach (IMyShipDrill drill in allDrills)
            {
                if(drill.CustomName.Contains(MAIN_TAG))
                {
                    _drills.Add(drill);
                }
            }

            if (_drills.Count < 1)
                _statusMessage += "No Drills found with tag "+ MAIN_TAG +"!\n";
        }


        // ACTIVATE DRILLS // - Powers drills on/off depending on bool argument
        public void ActivateDrills(bool turnOn)
        {
            if (_drills.Count < 1) return;

            foreach(IMyShipDrill drill in  _drills)
            {
                if (turnOn)
                    drill.GetActionWithName(ON).Apply(drill);
                else
                    drill.GetActionWithName(OFF).Apply(drill);
            }
        }
    }
}
