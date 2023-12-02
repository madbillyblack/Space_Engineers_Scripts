using Sandbox.Game.EntityComponents;
using Sandbox.Game.WorldEnvironment.Modules;
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
        // USER SETTINGS //
        const string HOVER_GROUP = "Hover Thrusters"; // Name of the group for hover thrusters
        const string COCKPIT_TAG = "[Reference]"; // Include this tag in the name of your reference cockpit
        const string MAIN_TAG = "[HOVER]";
        const double KP = 1;
        const double KI = 0;
        const double KD = 0;
        const double TIME_STEP = 1.0 / 6.0;
        const int TICK_RATE = 1;
        const double HEIGHT_STEP = 0.5;
        const double PARKING_MOD = 0.025;
        const double LANDING_SPEED = 0.01;

        // DO NOT CHANGE ANYTHING BELOW THIS LINE!!! ////////////////////////////////////////////////////////////////////

        // PROGRAM CONSTRUCTOR //
        public Program()
        {
            _lastCommand = "";
            Build();
        }


        // SAVE //
        public void Save(){}


        // MAIN //
        public void Main(string argument, UpdateType updateSource)
        {
            // Uncomment Block below for debugging purposes
            /*
            string message = "// HOVER PROGRAM //\nMode: " + _mode +
                            "\nGains: " + _kP + "," + _kI + "," + _kD +
                            "\nCmd: " + _lastCommand + "\nMsg:\n" + _statusMessage;
            Echo(message);
            */

            if (!_hasHoverThrusters)
            {
                Echo(_statusMessage);
                Echo("ADD HOVER THRUSTERS GROUP");
                return;
            }

            if (!string.IsNullOrEmpty(argument))
            {
                MainSwitch(argument);
                DisplayData();
            }
            else
            {
                ControlHover();
                CycleBreath();
            }


            Echo(_data);
        }
    }
}
