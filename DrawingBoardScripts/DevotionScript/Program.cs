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
        List<IMyWarhead> _warheads;
        List<IMyThrust> _thrusters;

        public Program()
        {
            _warheads = new List<IMyWarhead>();
            GridTerminalSystem.GetBlocksOfType<IMyWarhead>(_warheads);

            _thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(_thrusters);
        }

        public void Save(){}

        public void Main(string argument, UpdateType updateSource)
        {
            switch(argument.ToUpper().Trim())
            {
                case "ALL HAIL LORD CLANG":
                case "ALL HAIL LORD KLANG":
                case "ALL HAIL LORD KLANG!":
                case "ALL HAIL LORD CLANG!":
                    OfferToClang();
                    break;
            }
        }

        public void GiftOfClang()
        {
            foreach (IMyWarhead warhead in _warheads)
                warhead.Detonate();
        }

        
        public void JudgementOfClang()
        {
            if (_thrusters.Count < 1)
                return;

            foreach(IMyThrust thruster in _thrusters)
            {
                thruster.GetActionWithName("OnOff_Off").Apply(thruster);
            }
        }


        public void OfferToClang()
        {
            if (_warheads.Count > 0)
                GiftOfClang();
            else
                JudgementOfClang();
        }
    }
}
