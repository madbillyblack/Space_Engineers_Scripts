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
        const string P_KEY = "P-Gain";
        const string I_KEY = "I-Gain";
        const string D_KEY = "D-Gain";

        PID _pid;
        public bool _hoverThrustersOn;
        public double _kP;
        public double _kI;
        public double _kD;


        // PID HOVER CHECK //
        public void pidHoverCheck()
        {
            if (!_hoverThrustersOn || _pid == null) return;

            double height;

            if(_cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out height))
            {
                double error = _hoverHeight - height;
                ControlThrusters((float)_pid.Control(error));
            }

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
            SetMainKey(MAIN_HEADER, HOVER_ON, "true");
            _pid = new PID(_kP, _kI, _kD, TIME_STEP);
            //Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }


        // HOVER THRUSTERS OFF //
        void HoverThrustersOff()
        {
            ControlThrusters(0);
            _hoverThrustersOn = false;
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
    }
}
