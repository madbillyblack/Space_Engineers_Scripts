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
        const float CRUISE_STEP = 10;

        float _totalThrust;
        static float _targetThrottle;
        double _thrustWeightRatio;

        double _ki = 0;
        double _kd = 0;


        // SET THRUST WEIGHT RATIO //
        void SetThrustWeightRatio()
        {
            _totalThrust = 0;
            _thrustWeightRatio = 0;

            if (_cruiseThrusters.Count < 1 || _cockpit == null)
                return;

            foreach (IMyThrust thruster in _cruiseThrusters)
                _totalThrust += thruster.MaxThrust;

            _thrustWeightRatio = _totalThrust / (_cockpit.CalculateShipMass().TotalMass * 9.81);
        }


        // SET GAIN //
        void SetGain()
        {
            if (_thrustWeightRatio <= 0)
                _Kp = _cruiseFactor/INVERSE_GAIN;
            else
                _Kp = _cruiseFactor * (2.33 / INVERSE_GAIN) / _thrustWeightRatio;

            Echo("P-Gain:\n" + _Kp + "\n");
        }


        // THROTTLE UP //
        void ThrottleUp(string arg)
        {
            float value;

            if (arg == "")
                value = CRUISE_STEP;
            else
                value = ParseFloat(arg, -1);

            if (value > 0)
            {
                _targetThrottle += value;
                
                if (_targetThrottle > _maxSpeed)
                    _targetThrottle = _maxSpeed;

                if (!_cruiseThrustersOn)
                    CruiseThrustersOn();
            }
            else
                _statusMessage += "INVALID THROTTLE ARGUMENT:\n\"" + arg + "\"\n";      
        }

        
        // THROTTLE DOWN //
        void ThrottleDown(string arg)
        {
            float value;

            if (arg == "")
                value = CRUISE_STEP;
            else
                value = ParseFloat(arg, -1);

            if (value > 0)
            {
                _targetThrottle -= value;
                if (_targetThrottle <= 0)
                {
                    _targetThrottle = 0;
                    CruiseThrustersOff();
                }
            }
            else
                _statusMessage += "INVALID THROTTLE ARGUMENT:\n\"" + arg + "\"\n";
        }


        // CRUISE THRUSTERS ON //
        void CruiseThrustersOn()
        {
            if (_cruiseThrusters.Count < 1)
                return;

            _cruiseThrustersOn = true;

            _pid = new PID(_Kp, _ki, _kd, TIME_STEP);
            //Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }


        // CRUISE THRUSTERS OFF //
        void CruiseThrustersOff()
        {
            ThrottleThrusters(0);
            _cruiseThrustersOn = false;
            _runningNumber = 0;
            UpdateThrustDisplay(0);
            //Runtime.UpdateFrequency = UpdateFrequency.None;
        }


        // TOGGLE CRUISE THRUSTERS //
        void ToggleCruiseThrusters()
        {
            if (!_cruiseThrustersOn)
            {
                if (_targetThrottle <= 0)
                    _targetThrottle = _maxSpeed;

                CruiseThrustersOn();
            }
            else
                CruiseThrustersOff();
        }


        // GET FORWARD VELOCITY //
        public double GetForwardVelocity()
        {
            return Vector3D.Dot(_cockpit.WorldMatrix.Forward, _cockpit.GetShipVelocities().LinearVelocity);
        }


        // THROTTLE THRUSTERS //
        void ThrottleThrusters(float input)
        {
            if (_cruiseThrusters.Count < 1)
                return;

            foreach (IMyThrust thruster in _cruiseThrusters)
            {
                thruster.ThrustOverridePercentage = input;
            }

            UpdateThrustDisplay(_cruiseThrusters[0].ThrustOverridePercentage);
        }


        // CHECK GRAVITY //
        void CheckGravity()
        {
            if (_gravityDisengage && _cockpit.GetNaturalGravity().Length() < 0.04)
            {
                CruiseThrustersOff();
                _statusMessage += "GRAVITY WELL VACATED\nThrusters Disengaged\n";
            }
        }


        // SAFETY CHECK //
        void SafetyCheck()
        {
            double altitude;
            if (_cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude))
            {
                if (altitude < _safetyElevation)
                {
                    double speed = _cockpit.GetShipVelocities().LinearVelocity.Length();

                    if (speed > 0)
                    {
                        Vector3D gravity = _cockpit.GetNaturalGravity();

                        //Get cosine of angle between heading and gravity vector
                        double cos = Vector3D.Dot(_cockpit.WorldMatrix.Forward, gravity) / gravity.Length();

                        if (cos > 0.707) // If angle is within 45 degrees of gravity vector, disengage cruise thrusters
                        {
                            CruiseThrustersOff();
                            _statusMessage += "SAFETY THRUSTER DISENGAGE!\n";
                        }
                    }
                }
            }
        }


        // ASSIGN THRUSTERS //
        void AssignThrusters()
        {
            _cruiseThrusters = new List<IMyThrust>();
            _cruiseTag = GetKey(Me, INI_HEAD, "Cruise Thrusters", "");

            float[] gains = GainsFromString(GetKey(Me, INI_HEAD, "Cruise Gains", "1,0,0"));

            // Set user gain factors
            _cruiseFactor = gains[0];
            _ki = gains[1];
            _kd = gains[2];        

            if (_cruiseTag == "")
                return;

            IMyBlockGroup cruiseGroup = GridTerminalSystem.GetBlockGroupWithName(_cruiseTag);

            if (cruiseGroup == null)
            {
                _statusMessage += "NO GROUP WITH NAME \"" + _cruiseTag + "\" FOUND!\n";
                return;
            }

            cruiseGroup.GetBlocksOfType<IMyThrust>(_cruiseThrusters);
            _statusMessage += "CRUISE THRUSTERS: " + _cruiseTag + "\nThruster Count: " + _cruiseThrusters.Count + "\n";
            //AssignThrustDisplay();
        }


        // UPDATE THRUST DISPLAY //
        void UpdateThrustDisplay(float power)
        {
            if (!_cruiseThrustersOn)
            {
                _currentPower = "OFF";
                return;
            }

            int value = (int)(power * 100);
            _currentPower = value + "%";
        }


        // GAINS FROM STRING //
        float[] GainsFromString(string gainArray)
        {
            float [] output = {1, 0, 0};
            string[] values = gainArray.Split(',');

            if(values.Length > 2)
            {
                for(int i = 0; i < 3; i++)
                {
                    output[i] = ParseFloat(values[i], 0);
                }
            }

            if (output[0] <= 0)
                output[0] = 1;

            return output;
        }
    }
}
