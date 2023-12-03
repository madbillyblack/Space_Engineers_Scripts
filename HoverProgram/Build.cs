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
        public string _statusMessage, _lastCommand;

        public bool _hasHoverThrusters;
        public bool _hasDownThrusters;

        public static double _hoverHeight = 2.5;
        public double _parkHeight = 1;
        public double _descentSpeed = 10;

        List<IMyThrust> _hoverThrusters;
        List<IMyThrust> _downThrusters;
        List<IMyLandingGear> _landingGear;

        public static IMyCockpit _cockpit;


        // BUILD //
        public void Build()
        {
            _statusMessage = "";
            _data = "";
            AddBreather();
            SetMainIni();
            SetCockpit();
            AddThrusters();
            AddLandingGear();
            AddScanCamera();
            SetGains();
            GetHeightFromIni();
            AddHoverControl();
            SetTickRate(TICK_RATE);
            AddDisplays();
        }


        public void SetTickRate(int tickRate)
        { 
            if(_mode == INACTIVE)
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }

            switch(tickRate)
            {
                case 1:
                    Runtime.UpdateFrequency = UpdateFrequency.Update1;
                    break;
                case 10:
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    break;
                case 100:
                    Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    break;
                default:
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    break;
            }
        }


        // ADD THRUSTERS //
        public void AddThrusters()
        {
            // Initialize lists
            _hoverThrusters = new List<IMyThrust>();
            _downThrusters = new List<IMyThrust>();
            List<IMyThrust> thrusters = new List<IMyThrust>();

            // Get thrusters from Hover Group
            try
            {
                IMyBlockGroup hoverGroup = GridTerminalSystem.GetBlockGroupWithName(HOVER_GROUP);
                hoverGroup.GetBlocksOfType<IMyThrust>(thrusters);
            }
            catch
            {
                _statusMessage += "No Block Group \"" + HOVER_GROUP +"\" Defined!\n\n";
                return;
            }

            // Add thrusters with same Grid ID to hover list
            foreach (IMyThrust thruster in thrusters)
            {
                if (SameGridID(thruster))
                {
                    if(thruster.GridThrustDirection == Vector3I.Down)
                        _hoverThrusters.Add(thruster);
                    else if(thruster.GridThrustDirection == Vector3I.Up)
                        _downThrusters.Add(thruster);
                }
                    
            }

            _hasHoverThrusters = _hoverThrusters.Count > 0;
            if (!_hasHoverThrusters)
                _statusMessage += "NO HOVER THRUSTERS FOUND!\n\n";

            _hasDownThrusters = _downThrusters.Count > 0;
        }


        // SET COCKPIT //
        public void SetCockpit()
        {
            _cockpit = null;

            List<IMyCockpit> cockpits = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType(cockpits);

            if(cockpits.Count() < 1)
            {
                _statusMessage += "No cockpits found on grid!\n\n";
                return;
            }

            foreach(IMyCockpit cockpit in cockpits)
            {
                if(cockpit.CustomName.Contains(COCKPIT_TAG) && SameGridID(cockpit))
                {
                    _cockpit = cockpit;
                    return;
                }
            }

            _statusMessage += "No cockpits with tag \"" + COCKPIT_TAG + "\" and matching Grid ID found!\n\n";
        }


        // GET HEIGHT FROM INI //
        public void GetHeightFromIni()
        {
            _hoverHeight = ParseFloat(GetMainKey(HEADER, HOVER_KEY, _hoverHeight.ToString()), (float) _hoverHeight);
            _parkHeight = ParseFloat(GetMainKey(HEADER, PARK_HEIGHT, _parkHeight.ToString()), (float) _parkHeight);
            SetScanRange();
        }


        // SET GAINS //
        public void SetGains()
        {
            _kP = float.Parse(GetMainKey(HEADER, P_KEY, KP.ToString()));
            _kI = float.Parse(GetMainKey(HEADER, I_KEY, KI.ToString()));
            _kD = float.Parse(GetMainKey(HEADER, D_KEY, KD.ToString()));
        }


        // ACTIVATE LANDING GEAR //
        public void ActivateLandingGear(bool lockGear)
        {
            if(_landingGear.Count < 1) return;

            foreach(IMyLandingGear landingGear in _landingGear)
            {
                if(lockGear)
                    landingGear.Lock();
                else
                    landingGear.Unlock();
            }
        }


        // SET AUTO LOCK //
        public void SetAutoLock(bool autoLock)
        {
            if (_landingGear.Count < 1) return;

            foreach (IMyLandingGear landingGear in _landingGear)
                landingGear.AutoLock = autoLock;
        }

        // ADD LANDING GEAR //
        public void AddLandingGear()
        {
            _landingGear = new List<IMyLandingGear>();

            List<IMyLandingGear> tempGearList = new List<IMyLandingGear>();
            GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(tempGearList);

            if (tempGearList.Count < 1) return;

            foreach(IMyLandingGear landingGear in tempGearList)
            {
                if(landingGear.CustomName.Contains(MAIN_TAG) && SameGridID(landingGear))
                    _landingGear.Add(landingGear);
            }
        }


        // ADD BREATHER //
        public void AddBreather()
        {
            _cycleCount = 0;
            _currentBreath = _breather [0];
        }



    }
}
