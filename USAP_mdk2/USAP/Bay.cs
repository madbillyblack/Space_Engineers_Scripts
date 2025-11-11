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
        const string BAY_TYPE = "BayType";

        public LaunchSystem _launchSystem;
        public string _missileBayString;

        public class LaunchSystem
        {
            public string BayType { get; set; }
            public Dictionary<int, Bay> Bays { get; set; }
            public LaunchSystem()
            {
                Bays = new Dictionary<int, Bay>();

                BayType = _programIniHandler.GetKey(INI_HEAD, BAY_TYPE, "Bay");
                _programIniHandler.EnsureComment(INI_HEAD, BAY_TYPE, "How missile bay groups will be named: i.e. Bay, Silo, Tube, etc. Can include spaces.");
            }

            public void OpenAll()
            {
                //TODO
            }

            public void CloseAll()
            {
                //TODO
            }

            public void ReloadAll()
            {
                //TODO
            }
        }

        public class Bay
        {
            public int Number { get; set; }
            public string Name { get; set; }
            public List<IMyDoor> Doors { get; set; }
            public List<IMyShipWelder> Welders { get; set;}
            public IMyTimerBlock FiringTimer {  get; set; }
            public IMyTimerBlock ReloadTimer { get; set; }
            public bool Loaded { get; set; }
            public string MissileName { get; set; }
            public BayStatus Status { get; set; }

            public MyIniHandler iniHandler;

            public Bay (int number, string name)
            {
                Number = number;
                Name = name;

                Doors = new List<IMyDoor>();
                Welders = new List<IMyShipWelder>();
            }

            public void AddFiringTimer(IMyTimerBlock timer)
            {
                FiringTimer = timer;
                iniHandler = new MyIniHandler(timer);
            }
        }

        public enum  BayStatus
        {
            Opening, Open, Closed, Closing, Reloading
        }

        public Bay BayFromGroup(IMyBlockGroup group)
        {
            string[] nameParts = group.Name.Split(' ');
            string numPart = nameParts[nameParts.Length-1];

            int bayNumber = ParseInt(numPart, -1);
            if (bayNumber < 0)
            {
                //_statusMessage += "WARNING: Could not get bay number from group name " + group.Name + "\n";
                return null;
            }

            Bay bay = new Bay(bayNumber, group.Name);

            // Get Timers
            List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
            group.GetBlocksOfType<IMyTimerBlock>(timers);

            if(timers.Count < 1)
            {
                _statusMessage += "ERROR: Group " + group.Name + " contains no timers!\n";
                return null;
            }

            _statusMessage += "Group: " + group.Name + "\n";

            foreach (IMyTimerBlock timer in timers)
            {
                
                string timerName = timer.CustomName.ToUpper();

                if (timerName.Contains(FIRE_TAG))
                {
                    bay.AddFiringTimer(timer);
                }
                else if (timerName.Contains(RELOAD_TAG))
                {
                    bay.ReloadTimer = timer;
                }
            }

            if(bay.FiringTimer == null)
            {
                _statusMessage += "ERROR: Group " + group.Name + " contains no timers with the tag " + FIRE_TAG + "\n";
                return null;
            }

            group.GetBlocksOfType<IMyDoor>(bay.Doors);

            List<IMyShipWelder> welders = new List<IMyShipWelder>();
            group.GetBlocksOfType<IMyShipWelder>(bay.Welders);

            return bay;
        }

        public void AssembleMissileBays()
        {
            Echo("Adding Missile Bays");

            _launchSystem = new LaunchSystem();
            List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);
            if (groups.Count < 1)
                return;

            foreach (IMyBlockGroup group in groups)
            {
                if (group.Name.Contains(_launchSystem.BayType))
                {
                    Bay bay = BayFromGroup(group);

                    if (bay == null)
                        continue;

                    int key = bay.Number;
                    if (_launchSystem.Bays.ContainsKey(key))
                    {
                        _statusMessage += "ERROR: Cannot add " + group.Name + ". Launch systems already contains key " + key + "\n";  
                    }
                    else
                    {
                        _launchSystem.Bays.Add(key, bay);
                    }
                }
            }
        }

        public void ShowMissileBayData()
        {
            int count = _launchSystem.Bays.Count;
            Echo("Missile Bay Count: " + count);

            if (count > 0)
            {
                foreach (int key in _launchSystem.Bays.Keys)
                {
                    Echo("* " + _launchSystem.Bays[key].Name);
                }
            }
        }
    }
}