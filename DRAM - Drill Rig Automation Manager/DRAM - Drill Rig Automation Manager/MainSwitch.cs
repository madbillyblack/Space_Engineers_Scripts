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
        public void MainSwitch(string cmd, string arg)
        {
            switch (cmd.ToUpper())
            {
                case "CYCLE_CALL":
                    CycleCall();
                    break;
                case "START":
                    StartRig();
                    break;
                case "STOP":
                    StopRig();
                    break;
                case "RESET":
                    ResetRig();
                    break;
                case "REFRESH":
                    Build();
                    break;
                default:
                    _statusMessage = "UNKOWN COMMAND: \"" + cmd + "\"";
                    break;
            }
        }


        public void CycleCall()
        {

        }


        public void StartRig()
        {

        }


        public void StopRig()
        {
        
        }


        public void ResetRig()
        {

        }
    }
}
