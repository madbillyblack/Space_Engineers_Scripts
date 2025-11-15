using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

            private List<Bay> baysToCheck;
            public LaunchSystem()
            {
                Bays = new Dictionary<int, Bay>();
                baysToCheck = new List<Bay>();

                BayType = _programIni.GetKey(INI_HEAD, BAY_TYPE, "Bay");
                _programIni.EnsureComment(INI_HEAD, BAY_TYPE, "How missile bay groups will be named: i.e. Bay, Silo, Tube, etc. Can include spaces.");
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
            public Dictionary<string, IMyProjector> Projectors { get; set; }
            public List<IMyDoor> Doors { get; set; }
            public List<IMyShipWelder> Welders { get; set;}
            public List<IMyShipMergeBlock> MergeBlocks { get; set; }
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
                MergeBlocks = new List<IMyShipMergeBlock>();
                Projectors = new Dictionary<string, IMyProjector>();
            }

            public void AddFiringTimer(IMyTimerBlock timer)
            {
                FiringTimer = timer;
                iniHandler = new MyIniHandler(timer);
            }

            public void Open()
            {
                //TODO
            }


            public void Close()
            {
                //TODO
            }

            public void SelectLoadOut(string selection)
            {
                //TODO
            }


            private bool opened()
            {
                if (Doors.Count < 1) return true;

                foreach (IMyDoor door in Doors)
                {
                    if(door.OpenRatio < 1)
                        return false;
                }

                return true;
            }


            private bool loaded()
            {
                if (MergeBlocks.Count > 0)
                {
                    foreach (IMyShipMergeBlock block in MergeBlocks)
                    {
                        if (block.IsConnected)
                            return true;
                    }
                }

                return false;
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
                _log.LogWarning("Could not get bay number from group name " + group.Name);
                return null;
            }

            Bay bay = new Bay(bayNumber, group.Name);

            // Get Timers
            List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
            group.GetBlocksOfType<IMyTimerBlock>(timers);

            if(timers.Count < 1)
            {
                _log.LogError("Group \" + group.Name + \" contains no timers!");
                return null;
            }

            _log.LogInfo("Group: " + group.Name);

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
                _log.LogError("Group " + group.Name + " contains no timers with the tag " + FIRE_TAG);
                return null;
            }

            group.GetBlocksOfType<IMyDoor>(bay.Doors);
            group.GetBlocksOfType<IMyShipWelder>(bay.Welders);
            group.GetBlocksOfType<IMyShipMergeBlock>(bay.MergeBlocks);

            List<IMyProjector> projectors = new List<IMyProjector>();
            group.GetBlocksOfType<IMyProjector>(projectors);

            if (projectors.Count > 0)
            {
                foreach (IMyProjector projector in projectors)
                {
                    string key = GetBracedInfo(projector.CustomName);

                    if (bay.Projectors.ContainsKey(key))
                    {
                        _log.LogWarning("Group " + bay.Name + " contains multiple projectors with loadout {" + key + "}");
                        continue;
                    }

                    bay.Projectors.Add(key, projector);
                }

                // If no properly tagged projectors, add the first
                if(bay.Projectors.Count < 1)
                {
                    bay.Projectors.Add("default", projectors[0]);
                }
            }

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
                        _log.LogError("Cannot add " + group.Name + ". Launch systems already contains key " + key);  
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
                    Bay bay = _launchSystem.Bays[key];
                    Echo("* " + bay.Name);// + "\n - Projectors: " + bay.Projectors.Count + "\n - Loaded: " + bay.loaded().ToString());
                }
            }
        }
    }
}