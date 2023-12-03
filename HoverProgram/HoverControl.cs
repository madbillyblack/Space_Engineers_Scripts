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
        const string START = "starting";
        const string ACTIVE = "hover";
        const string LAND = "landing";
        const string INACTIVE = "off";

        const string P_KEY = "P-Gain";
        const string I_KEY = "I-Gain";
        const string D_KEY = "D-Gain";

        PID _pid;
        PID _parkingPid;
        public bool _hoverThrustersOn;
        public double _kP;
        public double _kI;
        public double _kD;
        string _mode;

        const string ON = "OnOff_On";
        const string OFF = "OnOff_Off";


        // GET CURRENT HEIGHT //
        public double GetCurrentHeight()
        {
            double height;

            if(_scanningEnabled)
                return _scanCamera.DetectHeight();

            if (_cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out height))
                return height;

            return _hoverHeight;
        }


        // PID HOVER CHECK //
        public void PidHoverCheck()
        {
            if (!_hoverThrustersOn) return;

            double height = GetCurrentHeight();

            double error = _hoverHeight - height;


            if (error < - DESCENT_MOD)
                PidDescentCheck(_descentSpeed);
            else
                ControlThrusters((float)_pid.Control(error));

        }


        // PID DESCENT CHECK //
        public void PidDescentCheck(double descentSpeed)
        {
            double descent = GetDownwardVelocity();

            double error = descent - descentSpeed;
            ControlThrusters((float)_pid.Control(error));
        }


        // PID START CHECK //
        public void PidStartCheck()
        {
            double height = GetCurrentHeight();
            double error = _hoverHeight - height;

            ControlThrusters((float)_parkingPid.Control(error));

            if(height > _hoverHeight)
                NormalizeHover();
        }


        // PID LAND CHECK //
        public void PidLandCheck()
        {
            PidDescentCheck(LANDING_SPEED);

            if (GetCurrentHeight() <= _hoverHeight || IsParked())
                TurnOff();
        }


        // CONTROL THRUSTERS //
        void ControlThrusters(float input)
        {
            if (!_hasHoverThrusters) return;

            foreach (IMyThrust thruster in _hoverThrusters)
                thruster.ThrustOverridePercentage = input;

            if (!_hasDownThrusters) return;
            
            foreach (IMyThrust downThruster in _downThrusters)
                downThruster.ThrustOverridePercentage = -input;
        }


        // HOVER THRUSTERS ON //
        void HoverThrustersOn()
        {
            if (!_hasHoverThrusters)
                return;

            _hoverThrustersOn = true;
            //_cockpit.DampenersOverride = false;

            SetMainKey(HEADER, HOVER_ON, "true");

            //_pid = new PID(_kP, _kI, _kD, TIME_STEP);
            //Runtime.UpdateFrequency = UpdateFrequency.Update10;
            ActivateLandingGear(false);


            foreach (IMyThrust thruster in _hoverThrusters)
                thruster.GetActionWithName(ON).Apply(thruster);

            SetTickRate(TICK_RATE);
        }


        // HOVER THRUSTERS OFF //
        void HoverThrustersOff()
        {
            ControlThrusters(0);
            _hoverThrustersOn = false;
            //_cockpit.DampenersOverride = true;
            SetMainKey(HEADER, HOVER_ON, "false");

            foreach (IMyThrust thruster in _hoverThrusters)
                thruster.GetActionWithName(OFF).Apply(thruster);                
        }


        // TOGGLE HOVER THRUSTERS //
        void ToggleHoverThrusters()
        {
           

            if (_mode == INACTIVE || _mode == LAND)
            {
                StartHover();
            }
            else
                StopHover();
        }


        // ADD HOVER CONTROL //
        void AddHoverControl()
        {
            _hoverThrustersOn = ParseBool(GetMainKey(HEADER, HOVER_ON, "false"));
            _mode = GetMainKey(HEADER, MODE, ACTIVE).ToLower();

            _pid = new PID(_kP, _kI, _kD, TIME_STEP);
            _parkingPid = new PID(_kP * PARKING_MOD, _kI * PARKING_MOD, _kD * PARKING_MOD, TIME_STEP);

            switch(_mode)
            {
                case ACTIVE:
                    NormalizeHover();
                    break;
                case START:
                    StartHover();
                    break;
                case LAND:
                    StopHover();
                    break;
            }
            /*
            if (_hoverThrustersOn)
                _pid = new PID(_kP, _kI, _kD, TIME_STEP);
            else
                _pid = null;
            */
        }


        // SET TARGET HEIGHT //
        public void SetTargetHeight(string value)
        {
            float newTarget = ParseFloat(value, 2.5f);

            if (newTarget < 0)
                newTarget *= -1;

            _hoverHeight = newTarget;
            SetMainKey(HEADER, HOVER_KEY, newTarget.ToString());
            
        }


        // ADJUST TARGET HEIGHT //
        public void AdjustTargetHeight(double heightStep)
        {
            _hoverHeight += heightStep;

            if(_hoverHeight < 0)
                _hoverHeight = 0;

            SetMainKey(HEADER, HOVER_KEY, _hoverHeight.ToString());
            SetScanRange();
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


        // CONTROL HOVER //
        public void ControlHover()
        {
            switch(_mode)
            {
                case ACTIVE:
                    PidHoverCheck();
                    break;
                case START:
                    PidStartCheck();
                    break;
                case LAND:
                    PidLandCheck();
                    break;
                default:
                    Echo("-- HOVER DEACTIVATED --");
                    break;
            }
        }


        // GET FORWARD VELOCITY //
        public double GetDownwardVelocity()
        {
            return Vector3D.Dot(_cockpit.WorldMatrix.Down, _cockpit.GetShipVelocities().LinearVelocity);
        }


        // START HOVER // - Initialize Hover from parked position
        public void StartHover()
        {
            double parkingMod = _hoverHeight * 0.005;
            _parkingPid = new PID(_kP * parkingMod, _kI * parkingMod, _kD * parkingMod, TIME_STEP);
            _mode = START;
            SetMainKey(HEADER, MODE, START);
            SetAutoLock(false);
            HoverThrustersOn();
        }


        // STOP HOVER // - Initialize the landing/park sequence
        public void StopHover()
        {
            _mode = LAND;
            SetMainKey(HEADER, MODE, LAND);
            SetAutoLock(true);
        }


        // NORMALIZE HOVER // - Switch the craft to its main hover mode
        public void NormalizeHover()
        {
            _mode = ACTIVE;
            SetMainKey(HEADER, MODE, ACTIVE);
        }


        // SET PARK HEIGHT //
        public void SetParkHeight()
        {
            _parkHeight = GetCurrentHeight();
            SetMainKey(HEADER, PARK_HEIGHT, _parkHeight.ToString());
        }


        // IS PARKED //
        public bool IsParked()
        {
            if(_landingGear.Count > 0)
            {
                foreach (IMyLandingGear landingGear in _landingGear)
                {
                    if (landingGear.IsLocked)
                        return true;
                }
            }

            return false;
        }


        // TURN OFF //
        public void TurnOff()
        {
            _mode = INACTIVE;
            SetMainKey(HEADER, MODE, INACTIVE);

            HoverThrustersOff();
            Runtime.UpdateFrequency = UpdateFrequency.None;
        }
    }
}
