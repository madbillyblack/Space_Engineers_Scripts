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
using System.Text.RegularExpressions;
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
        const string LOADOUT_KEY = "Active Loadout";
        const float DEF_RELOAD = 43; // Default Reload Time in seconds
        const float DEF_DOOR = 11; // Default Door Open/Close Time in seconds
        const float RECHECK_DELAY = 3;
        const float DEF_SALVO = 3;

        public static LaunchSystem _launchSystem;
        public string _missileBayString;

        public class LaunchSystem
        {
            public string BayType { get; set; }
            public SortedDictionary<int, Bay> Bays { get; set; }
            public IMyProgrammableBlock Program {  get; set; }
            public float SalvoDelay {  get; set; }
            public bool StaggerReload { get; set; }

            public string BayData {  get; set; }

            //private List<Bay> baysToCheck;
            public LaunchSystem(IMyProgrammableBlock launchProgram)
            {
                Program = launchProgram;
                Bays = new SortedDictionary<int, Bay>();
                //baysToCheck = new List<Bay>();

                BayType = _programIni.GetKey(BAY_HEAD, BAY_TYPE, "Bay");
                SalvoDelay = ParseFloat(_programIni.GetKey(BAY_HEAD, SALVO_KEY, DEF_SALVO.ToString()), DEF_SALVO);
                _programIni.EnsureComment(BAY_HEAD, BAY_TYPE, "How missile bay groups will be named: i.e. Bay, Silo, Tube, etc. Can include spaces.");
            }

            public void Fire(string bay)
            {
                if(bay == "")
                {
                    fireNext();
                    return;
                }

                int bayNumber = bayNumberFromString(bay);

                if (bayNumber > -1)
                {
                    Bays[bayNumber].Fire();
                    UpdateBayData();
                }
            }


            public void FireRange(string range)
            {
                List<Bay> salvo = new List<Bay>();
                int [] vals = RangeFromString(range);

                foreach (int key in Bays.Keys)
                {
                    Bay bay = Bays[key];
                    if (WithinRange(vals, bay.Number) && bay.IsReady())
                        salvo.Add(bay);
                }

                fireSalvo(salvo);
                UpdateBayData();
            }


            public void FireCount(string count)
            {
                int requested = ParseInt(count, -1);
                if(requested < 1)
                {
                    _log.LogError("Invalid count argument: \"" + count + "\"");
                    return;
                }

                List<Bay> salvo = new List<Bay>();
                int toFire = requested;

                foreach (int key in Bays.Keys)
                {
                    Bay bay = (Bay) Bays[key];
                    if(toFire > 0 && bay.IsReady())
                    {
                        salvo.Add(bay);
                        toFire--;
                    }
                }

                if(toFire > 0)
                {
                    int fired = requested - toFire;
                    _log.LogWarning(String.Format("Only able to fire {0} of {1} missiles.", fired, requested));
                }

                fireSalvo(salvo);
                UpdateBayData();
            }

            public void Reload(string bay)
            {

                int bayNumber = bayNumberFromString(bay);

                if (bayNumber > -1)
                {
                    Bays[bayNumber].Reload();
                    UpdateBayData();
                }
            }

            public void ReloadAll()
            {
                float delay = 0.1f;

                foreach (int key in Bays.Keys)
                {
                    Bay bay = Bays[key];

                    if(!bay.IsCounting() && !bay.IsReady() && bay.Status != BayStatus.Loaded)
                    {
                        Loadout loadout = bay.GetActiveLoadout();
                        if(loadout == null) { continue; }

                        bay.QueueToReload(delay);
                        delay += loadout.ReloadTime;
                    } 
                }

                UpdateBayData();
            }

            public void OpenBay(string bay)
            {
                int bayNumber = bayNumberFromString(bay);

                if (bayNumber > -1)
                {
                    Bays[bayNumber].Open();
                    UpdateBayData();
                }
            }

            public void CloseBay(string bay)
            {
                int bayNumber = bayNumberFromString(bay);

                if (bayNumber > -1)
                {
                    Bays[bayNumber].Close();
                    UpdateBayData();
                }
            }

            public void OpenAll()
            {
                foreach(int key in Bays.Keys) {  Bays[key].Open(); }

                UpdateBayData();
            }

            public void CloseAll()
            {
                foreach (int key in Bays.Keys) { Bays[key].Close(); }

                UpdateBayData();
            }

            public void BayCheck(string arg)
            {
                string[] args = arg.Split(' ');
                string bay = args[0];
                bool force = (args.Length > 1 && args[1].Trim().ToUpper() == "FORCE");

                int bayNumber = bayNumberFromString(bay);

                if (bayNumber > -1)
                {
                    Bays[bayNumber].ManualCheck(force);
                    UpdateBayData();
                }
            }

            public void BayTimerCall(string bay)
            {
                int bayNumber = bayNumberFromString(bay);

                if(bayNumber > -1)
                {
                    Bays[bayNumber].BayTimerCall();
                    UpdateBayData();
                }
            }

            public void UpdateBayData()
            {
                int count = _launchSystem.Bays.Count;
                BayData = "Missile Bay Count: " + count;

                if (count > 0)
                {
                    foreach (int key in _launchSystem.Bays.Keys)
                    {
                        Bay bay = _launchSystem.Bays[key];
                        BayData += "\n* " + bay.Name + " - " + bay.Status.ToString();
                    }
                }
            }


            private void fireSalvo(List<Bay> bays)
            {
                if (bays.Count < 1) 
                {
                    _log.LogError("No bays in Salvo");
                    return;
                }

                float delay = 0.1f;

                foreach(Bay bay in bays)
                {
                    bay.QueueToFire(delay);
                    delay += SalvoDelay;
                }
            }

            private void fireNext()
            {
                foreach(int key in Bays.Keys)
                {
                    Bay bay = Bays[key];
                    if (bay.IsReady())
                    {
                        bay.Fire();
                        return;
                    }  
                }

                _log.LogError("Can't fire. No bays ready.");
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
            public List<IMyShipConnector> Connectors { get; set; }

            public IMyTimerBlock Timer {  get; set; }
            //public IMyTimerBlock FiringTimer {  get; set; }
            //public IMyTimerBlock ReloadTimer { get; set; }
            //public bool CloseDoorsOnReload { get; set; }
            public string ActiveLoadout { get; set; }
            public BayStatus Status { get; set; }

            public float DoorDelay { get; set; }
            //public float ReloadDelay { get; set; }

            public MyIniHandler IniHandler;

            public Bay (int number, string name, IMyTimerBlock timer)
            {
                Number = number;
                Name = name;

                Timer = timer;
                IniHandler = new MyIniHandler(timer);
                Status = ParseStatus(IniHandler.GetKey(BAY_HEAD, STATUS_KEY, "UNSET"));
                //ReloadDelay = ParseFloat(IniHandler.GetKey(BAY_HEAD, RELOAD_KEY, DEF_RELOAD.ToString()), DEF_RELOAD);



                Doors = new List<IMyDoor>();
                Welders = new List<IMyShipWelder>();
                MergeBlocks = new List<IMyShipMergeBlock>();
                Loadouts = new Dictionary<string,Loadout>();
            }

            public void Fire()
            {
                if(_launchSystem.Program == null) { return; }

                if (IsReady() || Status == BayStatus.Queued)
                {
                    setStatus(BayStatus.Firing);
                    string command = "fire --range " + Number + " " +Number;
                    _launchSystem.Program.TryRun(command);
                    startCountDown(RECHECK_DELAY);
                }
                else
                {
                    _log.LogError(Name + " - is not ready.");
                }
            }


            public void Reload()
            {
                if(timerLockout()){ return; }
                if (Status == BayStatus.Ready || Status == BayStatus.Loaded)
                {
                    _log.LogError(Name + " is already loaded.");
                    return;
                }

                activateReload(true);
            }

            public void QueueToFire(float delay)
            {
                if (IsReady())
                {
                    setStatus(BayStatus.Queued);
                    startCountDown(delay);
                }
            }

            public void QueueToReload(float delay)
            {
                Status = BayStatus.RLQueued;
                startCountDown(delay);
            }

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
                    case BayStatus.Firing:
                        check();                            
                        break;
                    case BayStatus.Queued:
                        Fire();
                        break;
                    case BayStatus.Closing:
                        if (loaded())
                            setStatus(BayStatus.Loaded);
                        else
                            setStatus(BayStatus.Empty);
                        break;
                    case BayStatus.Reloading:
                        activateReload(false);
                        break;
                    case BayStatus.RLQueued:
                        activateReload(true);
                        break;
                }

                //TODO
            }


            public void SelectLoadOut(string selection)
            {
                //TODO
            }

            public void ManualCheck(bool force)
            {
                if(!force && Status != BayStatus.Unset && Status != BayStatus.Error)
                {
                    _log.LogWarning(Name + "\n- Checks only run on initial setup and errors.");
                    return;
                }

                if(IsCounting())
                {
                    _log.LogWarning(Name + "\n- Checks can't be run during timer countdown.");
                    return;
                }

                check();

                _log.LogInfo(Name + " is set to " + Status.ToString());
            }

            public bool IsReady()
            {
                return Status == BayStatus.Ready;
            }

            public bool HasDoors()
            {
                return Doors.Count > 0;
            }

            public bool IsCounting()
            {
                return Timer.IsCountingDown;
            }

            public Loadout GetActiveLoadout()
            {
                if (Loadouts.Count < 1 || string.IsNullOrEmpty(ActiveLoadout)) { return null; }
                return Loadouts[ActiveLoadout];
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

                if(closed && empty)
                    setStatus(BayStatus.Empty);
                else if(closed)
                    setStatus(BayStatus.Loaded);
                else if(empty)
                    setStatus(BayStatus.Open);
                else
                    setStatus(BayStatus.Ready);
            }

            private void setStatus(BayStatus status)
            {
                Status = status;
                IniHandler.SetKey(BAY_HEAD, STATUS_KEY, Status.ToString());
            }

            private bool opened()
            {
                if (!HasDoors()) return true;

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
                if (IsCounting())
                {
                    _log.LogError(Name + "\n- Can't perform action during timer countdown.");
                    return true;
                }

                return false;
            }

            private void activateReload(bool setActive)
            {
                Loadout loadout = GetActiveLoadout();
                if (loadout == null || loadout.Projector == null)
                {
                    _log.LogError(Name + ": Could not get active loadout");
                    return;
                }

                if (Welders.Count < 1 & !setActive)
                {
                    _log.LogError(Name + ": No Welders available!");
                    return;
                }

                string action;
                if (setActive)
                {
                    action = "OnOff_On";
                    setStatus(BayStatus.Reloading);
                    startCountDown(loadout.ReloadTime);
                }
                else
                {
                    action = "OnOff_Off";
                    check();
                }

                IMyProjector projector = loadout.Projector;
                projector.GetActionWithName(action).Apply(projector);

                foreach (IMyShipWelder welder in Welders)
                {
                    welder.GetActionWithName(action).Apply(welder);
                }
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

                ReloadTime = ParseFloat(iniHandler.GetKey(BAY_HEAD, RELOAD_KEY, DEF_RELOAD.ToString()), 43);
            }
        }


        public enum BayStatus
        {
            Opening, Open, Ready, Closing, Empty, Reloading, RLClosing, RLOpening, Unset, Error, Firing, Queued, RLQueued, Loaded, Loading
        }


        public static BayStatus ParseStatus(string status)
        {
            switch (status.ToUpper())
            {
                case "OPENING":
                    return BayStatus.Opening;
                case "READY":
                    return BayStatus.Ready;
                case "OPEN":
                    return BayStatus.Open;
                case "CLOSING":
                    return BayStatus.Closing;
                case "RELOADING":
                    return BayStatus.Reloading;
                case "RLCLOSING":
                    return BayStatus.RLClosing;
                case "RLOPENING":
                    return BayStatus.RLOpening;
                case "RLQUEUED":
                    return BayStatus.RLQueued;
                case "LOADING":
                    return BayStatus.Loading; // Applies to loading while closed
                case "EMPTY":
                    return BayStatus.Empty;
                case "UNSET":
                    return BayStatus.Unset;
                case "FIRING":
                    return BayStatus.Firing;
                case "QUEUED":
                    return BayStatus.Queued;
                case "LOADED":
                    return BayStatus.Loaded;
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

            AddDoors(bay, group);
            AddWelders(bay, group);
            AddMergeBlocks(bay, group);
            AddConnectors(bay, group);
            AddLoadouts(bay, group);

            return bay;
        }

        public void AssembleMissileBays()
        {
            Echo("Adding Missile Bays");

            IMyProgrammableBlock launchProgram = GetLaunchProgram();
            _launchSystem = new LaunchSystem(launchProgram);

            if (launchProgram == null) { return; }

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

            _launchSystem.UpdateBayData();
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

        public void AddDoors(Bay bay, IMyBlockGroup group)
        {
            List<IMyDoor> doors = new List<IMyDoor>();
            group.GetBlocksOfType<IMyDoor>(doors);

            foreach (IMyDoor door in doors)
            {
                if (SameGridID(door))
                    bay.Doors.Add(door);
            }

            if (bay.HasDoors())
                bay.DoorDelay = ParseFloat(bay.IniHandler.GetKey(BAY_HEAD, DOOR_KEY, DEF_DOOR.ToString()), DEF_DOOR);
            else
                bay.DoorDelay = 0;
        }

        public void AddWelders(Bay bay, IMyBlockGroup group)
        {
            List<IMyShipWelder> welders = new List<IMyShipWelder>();
            group.GetBlocksOfType<IMyShipWelder>(welders);

            foreach(IMyShipWelder welder in welders)
            {
                if(SameGridID(welder))
                    bay.Welders.Add(welder);
            }
        }

        public void AddMergeBlocks(Bay bay, IMyBlockGroup group)
        {
            List<IMyShipMergeBlock> mergeBlocks = new List<IMyShipMergeBlock>();
            group.GetBlocksOfType<IMyShipMergeBlock>(mergeBlocks);

            foreach(IMyShipMergeBlock mergeBlock in mergeBlocks)
            {
                if(SameGridID(mergeBlock))
                    bay.MergeBlocks.Add(mergeBlock);
            }
        }

        public void AddConnectors(Bay bay, IMyBlockGroup group)
        {
            List<IMyShipConnector> connectors = new List<IMyShipConnector>();
            group.GetBlocksOfType<IMyShipConnector>(connectors);

            foreach(IMyShipConnector connector in connectors)
            {
                if (SameGridID(connector))
                    bay.Connectors.Add(connector);
            }
        }

        public void AddLoadouts(Bay bay, IMyBlockGroup group)
        {
            List<IMyProjector> projectors = new List<IMyProjector>();
            group.GetBlocksOfType<IMyProjector>(projectors);

            if (projectors.Count > 0)
            {
                foreach (IMyProjector projector in projectors)
                {
                    string key = GetBracedInfo(projector.CustomName);
                    if (key == "") { continue; }

                    if (bay.Loadouts.ContainsKey(key))
                    {
                        _log.LogWarning("Group " + bay.Name + " contains multiple projectors with loadout {" + key + "}");
                        continue;
                    }

                    bay.Loadouts.Add(key, new Loadout(projector));
                }

                // If no properly tagged projectors, add the first
                if (bay.Loadouts.Count < 1)
                {
                    bay.Loadouts.Add("default", new Loadout(projectors[0]));
                }

                // Check for previously saved loadout to set as active or select first loadout if new setup
                if(bay.Loadouts.Count > 0)
                {
                    bay.ActiveLoadout = bay.IniHandler.GetKey(BAY_HEAD, LOADOUT_KEY, bay.Loadouts.Keys.First());
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


        public static int[] RangeFromString(string range)
        {
            int [] minMax = { -1, -1 };

            string[] args = range.Split('-');

            if(args.Length == 2)
            {
                minMax[0] = ParseInt(args[0].Trim(), -1);
                minMax[1] = ParseInt(args[1].Trim(), -1);
            }
            else
            {
                _log.LogError("Invalid range argument:\n \"" + range + "\"");
            }

            return minMax;
        }

        public static bool WithinRange(int[] range, int valueToCheck)
        {
            return valueToCheck >= range[0] && valueToCheck <= range[1];
        }


        public void ShowMissileBayData()
        {
            if(_launchSystem != null)
                Echo(_launchSystem.BayData);
        }
    }
}