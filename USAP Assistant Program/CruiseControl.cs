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
        float _totalThrust;
        double _thrustWeightRatio;

        void SetThrustWeightRatio()
        {
            _totalThrust = 0;
            _thrustWeightRatio = 0;

            if (_escapeThrusters.Count < 1 || _cockpit == null)
                return;

            foreach (IMyThrust thruster in _escapeThrusters)
                _totalThrust += thruster.MaxThrust;

            _thrustWeightRatio = _totalThrust / (_cockpit.CalculateShipMass().TotalMass * 9.81);
        }


        void SetGain()
        {
            if (_thrustWeightRatio <= 0)
                _Kp = 1/INVERSE_GAIN;
            else
                _Kp = (2.33 / INVERSE_GAIN) / _thrustWeightRatio;

            Echo("P-Gain:\n" + _Kp + "\n");
        }
    }
}
