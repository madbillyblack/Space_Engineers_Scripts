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
        public void MainSwitch(string arg)
        {
            _lastCommand = arg;
            string cmd;
            string data;
            string[] args = arg.Split(' ');

            if(args.Length > 1)
            {
                cmd = args[0].Trim();  
                data = args[1].Trim();
            }
            else
            {
                cmd = arg;
                data = "";
            }

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
                case "EMERGENCY_STOP":
                case "ALERT":
                    EmergencyStop();
                    break;
                case "BEACON_ON":
                case "ACTIVATE_BEACON":
                    ActivateBeacons(true);
                    break;
                case "BEACON_OFF":
                case "DEACTIVATE_BEACON":
                    ActivateBeacons(false);
                    break;
                case "RESET":
                    ResetRig();
                    break;
                case "REFRESH":
                    Build();
                    break;
                case "SET_START_POINT":
                    SetStartPoint();
                    break;
                case "LOWER":
                    LowerRig();
                    break;
                case "SET_GRID_ID":
                    SetGridID(data);
                    break;
                default:
                    _statusMessage = "UNKOWN COMMAND: \"" + arg + "\"";
                    break;
            }
        }


        // CYCLE CALL // - Function called by sensor when drill arm reaches end of movement
        public void CycleCall()
        {
            _rotors.Reverse();

            SetCycleCount(_cycleCount + 1);

            if (_phase == CYCLE)
                RunCycle();
            else if (_phase == RETRACT)
                RetractCheck();
            else
                StopRig();

            DisplayData();
        }


        // CYCLE CHECK // - Basic Check for Cycle Call
        public void RunCycle()
        {
            if (_HorzPistons.MinPos() > 9.95f)
                RetractRig();
            else if (_horzCount > 0)
                _HorzPistons.AdjustMaximum(_horzStep / _horzCount);
        }


        // RETRACT CHECK // - Check for when drill arm is retracting
        public void RetractCheck()
        {
            if(_HorzPistons.MaxPos() < 0.05f)
            {
                CycleRig();
                LowerRig();
            }
        }


        // CYCLE RIG // - Start basic cycle action
        public void CycleRig()
        {
            _phase = CYCLE;
            SetMainKey(MAIN_HEADER, PHASE, CYCLE);

            if(_horzCount > 0)
                _HorzPistons.SetVelocity(_pistonSpeed / _horzCount);

            if (_vertCount > 0)
                _VertPistons.SetVelocity(_pistonSpeed / _vertCount);

            if (_baseCount > 0)
                _BasePistons.SetVelocity(-_pistonSpeed / _baseCount);
        }


        // RETRACT RIG // - Start horizontal retraction action
        public void RetractRig()
        {
            _phase = RETRACT;
            SetMainKey(MAIN_HEADER, PHASE, RETRACT);

            if(_horzCount > 0)
            {
                _HorzPistons.SetMaximum(0);
                _HorzPistons.SetVelocity(-_pistonSpeed / _horzCount);
            }
        }


        /* LOWER RIG // - Move rig lower 1 step by way of either Base or Vert Pistons
                        Stops Rig if already at minimum position */
        public void LowerRig()
        {
            if (_baseCount > 0 && _BasePistons.MinPos() > 0)
                _BasePistons.AdjustMinimum(-_vertStep /_baseCount);
            else if (_vertCount > 0 && _VertPistons.MaxPos() < 10)
                _VertPistons.AdjustMaximum(_vertStep / _vertCount);
            else
            {
                _statusMessage += "Finished Sequence\n";
                StopRig();
            }
        }


        // START RIG //
        public void StartRig()
        {
            if (GetMainKey(MAIN_HEADER, PHASE, CYCLE) == CYCLE)
                CycleRig();
            else
                RetractRig();

            ActivateDrills(true);
            ActivateLights(true);
            ActivateBeacons(false);
            _rotors.StartRotors();
        }


        // STOP RIG //
        public void StopRig()
        {
            ActivateDrills(false);
            ActivateLights(false);
            _rotors.StopRotors();
        }


        // EMERGENCY STOP // - Stop rig and light the beacons. Gondor calls for aid!
        public void EmergencyStop()
        {
            StopRig();
            ActivateBeacons(true);
        }


        // RESET RIG // - Return pistons to their original configuration
        public void ResetRig()
        {
            StopRig();
            SetCycleCount(0);

            // Set start position for Base pistons and move toward that point
            _BasePistons.SetMaximum(_baseStart + 0.5f);
            _BasePistons.SetMinimum(_baseStart);
            _BasePistons.SetVelocity(PISTON_SPEED);

            // Retract Horz & Vert Pistons
            _HorzPistons.SetMaximum(0);
            _VertPistons.SetMaximum(0);
            _HorzPistons.SetVelocity(-PISTON_SPEED);
            _VertPistons.SetVelocity(-PISTON_SPEED);

            DisplayData();
        }
    }
}
