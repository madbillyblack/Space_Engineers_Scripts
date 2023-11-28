using Sandbox;
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
        // Operation Mode Strings
        const string START = "Starting";
        const string ACTIVE = "Hover";
        const string LAND = "Landing";
        const string DESCENT = "Descending";

        const string P_KEY = "P-Gain";
        const string I_KEY = "I-Gain";
        const string D_KEY = "D-Gain";

        PID _pid;
        public bool _hoverThrustersOn;
        public double _kP;
        public double _kI;
        public double _kD;
        string _mode;


        // PID HOVER CHECK //
        public void pidHoverCheck()
        {
            if (!_hoverThrustersOn || _pid == null) return;

            double height;

            if(_cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out height))
            {
                double error = _hoverHeight - height;

                if (error < -5)
                    pidDescentCheck();
                else
                    ControlThrusters((float)_pid.Control(error));
            }
        }


        // PID DESCENT CHECK //
        public void pidDescentCheck()
        {
            double descent = GetDownwardVelocity();

            double error = descent - _descentSpeed;
            ControlThrusters((float)_pid.Control(error));
        }


        // CONTROL THRUSTERS //
        void ControlThrusters(float input)
        {
            if (!_hasHoverThrusters)
                return;

            foreach (IMyThrust thruster in _hoverThrusters)
            {
                thruster.ThrustOverridePercentage = input;
            }
        }


        // HOVER THRUSTERS ON //
        void HoverThrustersOn()
        {
            if (!_hasHoverThrusters)
                return;

            _hoverThrustersOn = true;
            _cockpit.DampenersOverride = false;

            SetMainKey(MAIN_HEADER, HOVER_ON, "true");

            _pid = new PID(_kP, _kI, _kD, TIME_STEP);
            //Runtime.UpdateFrequency = UpdateFrequency.Update10;
            ActivateLandingGear(false);
        }


        // HOVER THRUSTERS OFF //
        void HoverThrustersOff()
        {
            ControlThrusters(0);
            _hoverThrustersOn = false;
            _cockpit.DampenersOverride = true;
            SetMainKey(MAIN_HEADER, HOVER_ON, "false");
        }


        // TOGGLE HOVER THRUSTERS //
        void ToggleHoverThrusters()
        {
            if (!_hoverThrustersOn)
            {
                HoverThrustersOn();
            }
            else
                HoverThrustersOff();
        }


        // ADD HOVER CONTROL //
        void AddHoverControl()
        {
            _hoverThrustersOn = ParseBool(GetMainKey(MAIN_HEADER, HOVER_ON, "false"));
            _mode = GetMainKey(MAIN_HEADER, MODE, ACTIVE);

            if (_hoverThrustersOn)
                _pid = new PID(_kP, _kI, _kD, TIME_STEP);
            else
                _pid = null;
        }


        // SET TARGET HEIGHT //
        public void SetTargetHeight(string value)
        {
            float newTarget = ParseFloat(value, 2.5f);

            if (newTarget < 0)
                newTarget *= -1;

            _hoverHeight = newTarget;
            SetMainKey(MAIN_HEADER, HOVER_KEY, newTarget.ToString());
        }


        // ADJUST TARGET HEIGHT //
        public void AdjustTargetHeight(double heightStep)
        {
            _hoverHeight += heightStep;

            if(_hoverHeight < 0)
                _hoverHeight = 0;

            SetMainKey(MAIN_HEADER, HOVER_KEY, _hoverHeight.ToString());
        }


        // INCREASE HEIGHT //
        public void IncreaseHeight(string value)
        {
            float diff;

            if (value == "")
                diff = (float)HEIGHT_STEP;
            else
                diff = ParseFloat(value, (float)HEIGHT_STEP);

            AdjustTargetHeight(diff);
        }


        // DECREASE HEIGHT //
        public void DecreaseHeight(string value)
        {
            float diff;

            if (value == "")
                diff = (float)HEIGHT_STEP * -1;
            else
                diff = ParseFloat(value, (float)HEIGHT_STEP * -1);

            AdjustTargetHeight(diff);
        }


        // CONTROL SWITCH //
        public void ControlSwitch()
        {
            switch(_mode)
            {
                case ACTIVE:
                    pidHoverCheck();
                    break;
                case START:
                    break;
                case DESCENT:
                    pidDescentCheck();
                    break;

            }
        }


        // GET FORWARD VELOCITY //
        public double GetDownwardVelocity()
        {
            return Vector3D.Dot(_cockpit.WorldMatrix.Down, _cockpit.GetShipVelocities().LinearVelocity);
        }


        // ACTIVATE HOVER //
        public void ActivateHover()
        {
            // TODO
        }


        // ACTIVATE DESCENT //
        public void ActivateDescent()
        {
            // TODO
        }
    }
}
