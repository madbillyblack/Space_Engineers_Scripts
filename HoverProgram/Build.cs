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

        public double _hoverHeight = 2.5;

        List<IMyThrust> _hoverThrusters;
        List<IMyLandingGear> _landingGear;

        IMyCockpit _cockpit;


        // BUILD //
        public void Build()
        {
            _statusMessage = "";
            SetMainIni();
            SetCockpit();
            AddThrusters();
            AddLandingGear();
            SetGains();
            GetHeightFromIni();
            AddHoverControl();
        }


        public void SetTickRate(int tickRate)
        { 
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
                _hoverThrusters.Add(thruster);
            }

            _hasHoverThrusters = _hoverThrusters.Count > 0;
            if (!_hasHoverThrusters)
                _statusMessage += "NO HOVER THRUSTERS FOUND!\n\n";
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
            _hoverHeight = ParseFloat(GetMainKey(MAIN_HEADER, HOVER_KEY, _hoverHeight.ToString()), (float) _hoverHeight);
        }


        // SET GAINS //
        public void SetGains()
        {
            _kP = float.Parse(GetMainKey(MAIN_HEADER, P_KEY, KP.ToString()));
            _kI = float.Parse(GetMainKey(MAIN_HEADER, I_KEY, KI.ToString()));
            _kD = float.Parse(GetMainKey(MAIN_HEADER, D_KEY, KD.ToString()));
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
    }
}
