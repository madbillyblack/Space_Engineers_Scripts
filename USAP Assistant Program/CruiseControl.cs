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
        static float _targetThrottle;
        double _thrustWeightRatio;


        // SET THRUST WEIGHT RATIO //
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


        // SET GAIN //
        void SetGain()
        {
            if (_thrustWeightRatio <= 0)
                _Kp = 1/INVERSE_GAIN;
            else
                _Kp = (2.33 / INVERSE_GAIN) / _thrustWeightRatio;

            Echo("P-Gain:\n" + _Kp + "\n");
        }


        // THROTTLE UP //
        void ThrottleUp(string arg)
        {
            float value = ParseFloat(arg, -1);

            if (value > 0)
            {
                _targetThrottle += value;
                
                if (_targetThrottle > _maxSpeed)
                    _targetThrottle = _maxSpeed;

                if (!_escapeThrustersOn)
                    EscapeThrustersOn();
            }
            else
                _statusMessage += "INVALID THROTTLE ARGUMENT:\n\"" + arg + "\"\n";      
        }

        
        // THROTTLE DOWN //
        void ThrottleDown(string arg)
        {
            float value = ParseFloat(arg, -1);

            if (value > 0)
            {
                _targetThrottle -= value;
                if (_targetThrottle < 0)
                {
                    _targetThrottle = 0;
                    EscapeThrustersOff();
                }
            }
            else
                _statusMessage += "INVALID THROTTLE ARGUMENT:\n\"" + arg + "\"\n";
        }


        // ESCAPE THRUSTERS ON //
        void EscapeThrustersOn()
        {
            if (_escapeThrusters.Count < 1)
                return;

            _escapeThrustersOn = true;

            _pid = new PID(_Kp, KI, KD, TIME_STEP);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }


        // ESCAPE THRUSTERS OFF //
        void EscapeThrustersOff()
        {
            ThrottleThrusters(0);
            _escapeThrustersOn = false;
            _runningNumber = 0;
            UpdateThrustDisplay(0);
            Runtime.UpdateFrequency = UpdateFrequency.None;
        }


        // TOGGLE ESCAPE THRUSTERS //
        void ToggleEscapeThrusters()
        {
            if (!_escapeThrustersOn)
            {
                if (_targetThrottle <= 0)
                    _targetThrottle = _maxSpeed;

                EscapeThrustersOn();
            }
            else
                EscapeThrustersOff();
        }


        // GET FORWARD VELOCITY //
        public double GetForwardVelocity()
        {
            return Vector3D.Dot(_cockpit.WorldMatrix.Forward, _cockpit.GetShipVelocities().LinearVelocity);
        }
    }
}
