using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
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
using static IngameScript.Program;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const string BAY_TYPE = "BayType";
        const string BAY_HEAD = "USAP: Missile Bay";
        const string STATUS_KEY = "Status";
        const string RELOAD_KEY = "Reload Delay";
        const string DOOR_KEY = "Door Delay";
        const string AUTO_CLOSE = "Close Doors On Reload";
        const string PROGRAM_KEY = "Launch Program";
        const string SALVO_KEY = "Salvo Delay";
        const float DEF_RELOAD = 43; // Default Reload Time in seconds
        const float DEF_DOOR = 11; // Default Door Open/Close Time in seconds
        const float RECHECK_DELAY = 3;
        const float DEF_SALVO = 3;

        public LaunchSystem _launchSystem;
        public string _missileBayString;

        public class LaunchSystem
        {
            public string BayType { get; set; }
            public Dictionary<int, Bay> Bays { get; set; }
            public IMyProgrammableBlock Program {  get; set; }
            public float SalvoDelay {  get; set; }

            //private List<Bay> baysToCheck;
            public LaunchSystem(IMyProgrammableBlock launchProgram)
            {
                Program = launchProgram;
                Bays = new Dictionary<int, Bay>();
                //baysToCheck = new List<Bay>();

                BayType = _programIni.GetKey(BAY_HEAD, BAY_TYPE, "Bay");
                SalvoDelay = ParseFloat(_programIni.GetKey(BAY_HEAD, SALVO_KEY, DEF_SALVO.ToString()), DEF_SALVO);
                _programIni.EnsureComment(BAY_HEAD, BAY_TYPE, "How missile bay groups will be named: i.e. Bay, Silo, Tube, etc. Can include spaces.");
            }

            public void OpenBay(string bay)
            {
                int bayNumber = bayNumberFromString(bay);

                if (bayNumber > -1)
                    Bays[bayNumber].Open();
            }

            public void CloseBay(string bay)
            {
                int bayNumber = bayNumberFromString(bay);

                if (bayNumber > -1)
                    Bays[bayNumber].Close();
            }

            public void OpenAll()
            {
                foreach(int key in Bays.Keys) {  Bays[key].Open(); }
            }

            public void CloseAll()
            {
                foreach (int key in Bays.Keys) { Bays[key].Close(); }
            }

            public void ReloadAll()
            {
                //TODO
            }

            public void BayCheck(string bay)
            {
                int bayNumber = bayNumberFromString(bay);

                if (bayNumber > -1)
                    Bays[bayNumber].ManualCheck();
            }

            public void BayTimerCall(string bay)
            {
                int bayNumber = bayNumberFromString(bay);

                if(bayNumber > -1)
                    Bays[bayNumber].BayTimerCall();
            }

            private int bayNumberFromString(string bayString)
            {
                int number = ParseInt(bayString, -1);

                if (!Bays.ContainsKey(number))
                    number = -1;

                if (number < 0)
                    _log.LogError("Unrecognized bay number: \"" + bayString + "\"");

                return number;
            }
        }

        public class Bay
        {
            public int Number { get; set; }
            public string Name { get; set; }
            //public Dictionary<string, IMyProjector> Projectors { get; set; }
            public Dictionary<string, Loadout> Loadouts { get; set; }
            public List<IMyDoor> Doors { get; set; }
            public List<IMyShipWelder> Welders { get; set;}
            public List<IMyShipMergeBlock> MergeBlocks { get; set; }

            public IMyTimerBlock Timer {  get; set; }
            //public IMyTimerBlock FiringTimer {  get; set; }
            //public IMyTimerBlock ReloadTimer { get; set; }
            public bool Loaded { get; set; }
            public string MissileName { get; set; }
            public BayStatus Status { get; set; }

            public float DoorDelay { get; set; }
            public float ReloadDelay { get; set; }

            public MyIniHandler iniHandler;

            public Bay (int number, string name, IMyTimerBlock timer)
            {
                Number = number;
                Name = name;

                Timer = timer;
                iniHandler = new MyIniHandler(timer);
                Status = ParseStatus(iniHandler.GetKey(BAY_HEAD, STATUS_KEY, "UNSET"));
                ReloadDelay = ParseFloat(iniHandler.GetKey(BAY_HEAD, RELOAD_KEY, DEF_RELOAD.ToString()), DEF_RELOAD);

                Doors = new List<IMyDoor>();
                Welders = new List<IMyShipWelder>();
                MergeBlocks = new List<IMyShipMergeBlock>();
                Loadouts = new Dictionary<string,Loadout>();
            }

            /*
            public void AddFiringTimer(IMyTimerBlock timer)
            {
                FiringTimer = timer;
                iniHandler = new MyIniHandler(timer);

                Status = ParseStatus(iniHandler.GetKey(BAY_HEAD, STATUS_KEY, "OPEN"));
            }
            */
            public void Open()
            {
                if (Doors.Count < 1 || opened() || timerLockout()) { return; }

                setStatus(BayStatus.Opening);
                foreach (IMyDoor door in Doors)
                {
                    door.OpenDoor();
                }
                
                startCountDown(DoorDelay);
            }


            public void Close()
            {
                if (Doors.Count < 1 || !opened() || timerLockout()) { return; }

                setStatus(BayStatus.Closing);
                foreach (IMyDoor door in Doors)
                {
                    door.CloseDoor();
                }

                startCountDown(DoorDelay);
            }

            public void BayTimerCall()
            {
                switch (Status)
                { 
                    case BayStatus.Opening:
                        check();                            
                        break;
                    case BayStatus.Closing:
                        setStatus(BayStatus.Closed);
                        break;
                }

                //TODO
            }


            public void SelectLoadOut(string selection)
            {
                //TODO
            }

            public void ManualCheck()
            {
                if(Status != BayStatus.Unset && Status != BayStatus.Error)
                {
                    _log.LogWarning(Name + "\n- Checks only run on initial setup and errors.");
                    return;
                }

                if(Timer.IsCountingDown)
                {
                    _log.LogWarning(Name + "\n- Checks can't be run during timer countdown.");
                    return;
                }

                check();

                _log.LogInfo(Name + " is set to " + Status.ToString());
            }

            private void startCountDown(float delay)
            {
                Timer.TriggerDelay = delay;
                Timer.StartCountdown();
            }

            private void check(bool opening = false)
            {
                bool closed = !opened();
                bool empty = !loaded();

                if (opening && closed)
                {
                    startCountDown(RECHECK_DELAY);
                    return;
                }

                if(closed)
                    setStatus(BayStatus.Closed);
                else if(empty)
                    setStatus(BayStatus.Empty);
                else
                    setStatus(BayStatus.Ready);
            }

            private void setStatus(BayStatus status)
            {
                Status = status;
                iniHandler.SetKey(BAY_HEAD, STATUS_KEY, Status.ToString());
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

            private bool timerLockout()
            {
                if (Timer.IsCountingDown)
                {
                    _log.LogError(Name + "\n- Can't perform action during timer countdown.");
                    return true;
                }

                return false;
            }
        }

        public class Loadout
        {
            public IMyProjector Projector { get; set; }
            public float ReloadTime { get; set; }
            private MyIniHandler iniHandler;

            public Loadout(IMyProjector projector)
            {
                Projector = projector;
                iniHandler = new MyIniHandler(Projector);

                ReloadTime = ParseFloat(iniHandler.GetKey(BAY_HEAD, RELOAD_KEY, "10"), 10);
            }
        }


        public enum BayStatus
        {
            Opening, Ready, Closed, Closing, Empty, Reloading, RLClosing, RLOpening, Unset, Error
        }

        public static BayStatus ParseStatus(string status)
        {
            switch (status.ToUpper())
            {
                case "OPENING":
                    return BayStatus.Opening;
                case "READY":
                    return BayStatus.Ready;
                case "CLOSED":
                    return BayStatus.Closed;
                case "CLOSING":
                    return BayStatus.Closing;
                case "RELOADING":
                    return BayStatus.Reloading;
                case "RL_CLOSING":
                    return BayStatus.RLClosing;
                case "RL_OPENING":
                    return BayStatus.RLOpening;
                case "EMPTY":
                    return BayStatus.Empty;
                case "UNSET":
                    return BayStatus.Unset;
                default:
                    return BayStatus.Error;
            }
        }

        public Bay BayFromGroup(IMyBlockGroup group)
        {
            string[] nameParts = group.Name.Split(' ');
            string numPart = nameParts[nameParts.Length-1];

            int bayNumber = ParseInt(numPart, -1);
            if (bayNumber < 0)
            {
                _log.LogWarning("Can't get bay number from group:\n  " + group.Name);
                return null;
            }

            IMyTimerBlock timer = GetSameGridTimer(group);
            if(timer == null) { return null; }

            Bay bay = new Bay(bayNumber, group.Name, timer);

            /*
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
            */

            group.GetBlocksOfType<IMyDoor>(bay.Doors);
            if (bay.Doors.Count > 0)
                bay.DoorDelay = ParseFloat(bay.iniHandler.GetKey(BAY_HEAD, DOOR_KEY, DEF_DOOR.ToString()), DEF_DOOR);
            else
                bay.DoorDelay = 0;

            group.GetBlocksOfType<IMyShipWelder>(bay.Welders);
            group.GetBlocksOfType<IMyShipMergeBlock>(bay.MergeBlocks);

            List<IMyProjector> projectors = new List<IMyProjector>();
            group.GetBlocksOfType<IMyProjector>(projectors);

            if (projectors.Count > 0)
            {
                foreach (IMyProjector projector in projectors)
                {
                    string key = GetBracedInfo(projector.CustomName);

                    if (bay.Loadouts.ContainsKey(key))
                    {
                        _log.LogWarning("Group " + bay.Name + " contains multiple projectors with loadout {" + key + "}");
                        continue;
                    }

                    bay.Loadouts.Add(key, new Loadout(projector));
                }

                // If no properly tagged projectors, add the first
                if(bay.Loadouts.Count < 1)
                {
                    bay.Loadouts.Add("default", new Loadout(projectors[0]));
                }
            }

            return bay;
        }

        public void AssembleMissileBays()
        {
            Echo("Adding Missile Bays");

            IMyProgrammableBlock launchProgram = GetLaunchProgram();
            if (launchProgram == null){ return; }
            _launchSystem = new LaunchSystem(launchProgram);

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

        public IMyProgrammableBlock GetLaunchProgram()
        {
            List<IMyProgrammableBlock> programs = new List<IMyProgrammableBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(programs);

            // If this is the only program block stop looking
            if (programs.Count < 2) { return null; }

            string progamName = _programIni.GetKey(BAY_HEAD, PROGRAM_KEY, "").Trim();

            foreach(IMyProgrammableBlock program in programs)
            {
                string blockName = program.CustomName.Trim();

                if (progamName == "" && blockName.Contains("LAMP"))
                {
                    _programIni.SetKey(BAY_HEAD, PROGRAM_KEY, blockName);
                    return program;
                }

                if (blockName.ToUpper() == progamName.ToUpper())
                {
                    return program;
                }
            }            

            return null;
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
                    Echo("* " + bay.Name + " - " + bay.Status.ToString());// + "\n - Projectors: " + bay.Projectors.Count + "\n - Loaded: " + bay.loaded().ToString());
                }
            }
        }

        public IMyTimerBlock GetSameGridTimer(IMyBlockGroup group)
        {
            List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
            group.GetBlocksOfType<IMyTimerBlock>(timers);

            if (timers.Count < 1)
            {
                _log.LogError("Group \"" + group.Name + "\" contains no timers!");
                return null;
            }

            foreach (IMyTimerBlock timer in timers)
            {
                if (SameGridID(timer)) { return timer; }
            }

            _log.LogError("No timers from group \"" + group.Name + "\" found on the same grid.");
            return null;
        }
    }
}