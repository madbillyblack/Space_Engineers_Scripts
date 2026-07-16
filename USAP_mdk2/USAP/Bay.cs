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
//using static System.Collections.Specialized.BitVector32;

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
        const string DEF_TOGGLE = "[TOGGLE]";
        const string TOGGLE_KEY = "Toggle Block Tag";
        const string MAIN_BC_KEY = "Main Broadcaster";
        const string TARGET_KEY = "Target Mode";
        const string CAMERA_CMD = "mode_camera";
        const string TURRET_CMD = "mode_turret";
        const string BEAM_CMD = "mode_beamride";
        const float DEF_RELOAD = 43; // Default Reload Time in seconds
        const float DEF_DOOR = 11; // Default Door Open/Close Time in seconds
        const float RECHECK_DELAY = 3;
        const float DEF_SALVO = 3;

        // Bay Broadcast Message Constants
        const int FIRE = 1;
        const int LOADING = 2;
        const int LOADED = 3;
        const int CLOSING = 4;
        const int CLOSED = 5;
        const int OPENING = 6;
        const int OPENED = 7;
        const int ERR = 8; // Error Message number
        const int CAM_MODE = 6;
        const int TURRET_MODE = 7;
        const int BEAM_RIDE = 8;


        static LaunchSystem _launchSystem;
        static string _toggleTag;

        class LaunchSystem
        {
            public string BayType { get; set; }
            public SortedDictionary<int, Bay> Bays { get; set; }
            public IMyProgrammableBlock LaunchProgram {  get; set; }
            public IMyBroadcastController Broadcaster { get; set; }
            public float SalvoDelay {  get; set; }
            public string BayData {  get; set; }
            public TargetMode Mode { get; set; }

            //private List<Bay> baysToCheck;
            public LaunchSystem(IMyProgrammableBlock launchProgram)
            {
                LaunchProgram = launchProgram;
                Bays = new SortedDictionary<int, Bay>();
                //baysToCheck = new List<Bay>();

                BayType = _programIni.GetKey(BAY_HEAD, BAY_TYPE, "Bay");
                _toggleTag = _programIni.GetKey(BAY_HEAD, TOGGLE_KEY, DEF_TOGGLE);
                SalvoDelay = ParseFloat(_programIni.GetKey(BAY_HEAD, SALVO_KEY, DEF_SALVO.ToString()), DEF_SALVO);
                _programIni.EnsureComment(BAY_HEAD, BAY_TYPE, "How missile bay groups will be named: i.e. Bay, Silo, Tube, etc. Can include spaces.");

                initTargetMode();                
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

            /* Fires a vailable missiles up to the specified quantity */
            public void FireCount(string count)
            {
                int requested = ParseInt(count, -1);
                if(requested < 1)
                {
                    _log.Error("Invalid count argument: \"" + count + "\"");
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
                    _log.Warning(String.Format("Only able to fire {0} of {1} missiles.", fired, requested));
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

            public void ToggleBay(string bay)
            {
                int bayNumber = bayNumberFromString(bay);

                if (bayNumber > -1)
                {
                    Bays[bayNumber].Toggle();
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

            public void ToggleAll()
            {
                foreach (int key in Bays.Keys) { Bays[key].Toggle(); }

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

            public void CameraMode()
            {
                Mode = TargetMode.Camera;
                LaunchProgram.TryRun(CAMERA_CMD);
                _programIni.SetKey(BAY_HEAD, TARGET_KEY, TargetMode.Camera.ToString());
                broadcast(CAM_MODE);
            }

            public void TurretMode()
            {
                Mode = TargetMode.Turret;
                LaunchProgram.TryRun(TURRET_CMD);
                _programIni.SetKey(BAY_HEAD, TARGET_KEY, TargetMode.Turret.ToString());
                broadcast(TURRET_MODE);
            }

            public void BeamRide()
            {
                Mode = TargetMode.BeamRide;
                LaunchProgram.TryRun(BEAM_CMD);
                _programIni.SetKey(BAY_HEAD, TARGET_KEY, TargetMode.BeamRide.ToString());
                broadcast(BEAM_RIDE);
            }

            public void CycleMode()
            {
                switch (Mode)
                {
                    case TargetMode.Camera:
                        TurretMode();
                        break;
                    case TargetMode.Turret:
                        BeamRide();
                        break;
                    default:
                        CameraMode();
                        break;
                }
            }

            private void broadcast(int msgNumber)
            {
                if(Broadcaster == null) { return; }

                string message = String.Format("Transmit Message {0}", msgNumber.ToString());
                Broadcaster.GetActionWithName(message).Apply(Broadcaster);
            }

            private void fireSalvo(List<Bay> bays)
            {
                if (bays.Count < 1) 
                {
                    _log.Error("No bays in Salvo");
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

                _log.Error("Can't fire. No bays ready.");
            }

            private int bayNumberFromString(string bayString)
            {
                int number = ParseInt(bayString, -1);

                if (!Bays.ContainsKey(number))
                    number = -1;

                if (number < 0)
                    _log.Error("Unrecognized bay number: \"" + bayString + "\"");

                return number;
            }

            private void initTargetMode()
            {
                Mode = ParseTargetMode(_programIni.GetKey(BAY_HEAD, TARGET_KEY, TargetMode.BeamRide.ToString()));

                if (Mode == TargetMode.Camera)
                    LaunchProgram.TryRun(CAMERA_CMD);
                else if (Mode == TargetMode.Turret)
                    LaunchProgram.TryRun(TURRET_CMD);
                else
                    LaunchProgram.TryRun(BEAM_CMD);
            }
        }

        class Bay
        {
            public int Number { get; set; }
            public string Name { get; set; }
            public string ActiveLoadout { get; set; }
            public float DoorDelay { get; set; }
            public IMyTimerBlock Timer { get; set; }
            public IMyBroadcastController Broadcaster { get; set; }
            
            public BayStatus Status { get; set; }

            public MyIniHandler IniHandler;

            public Dictionary<string, Loadout> Loadouts { get; set; }
            public List<IMyDoor> Doors { get; set; }
            public List<IMyShipWelder> Welders { get; set;}
            public List<IMyShipMergeBlock> MergeBlocks { get; set; }
            public List<IMyShipConnector> Connectors { get; set; }

            public List<ToggleBlock> ToggleBlocks { get; set; }// Optional blocks that are powered on when ready to fire
            private bool canBroadcast;
            public bool HasToggle;


            public Bay (int number, string name, IMyTimerBlock timer, IMyBroadcastController broadcaster)
            {
                Number = number;
                Name = name;

                Timer = timer;
                IniHandler = new MyIniHandler(timer);
                Status = ParseStatus(IniHandler.GetKey(BAY_HEAD, STATUS_KEY, "UNSET"));

                Broadcaster = broadcaster;
                canBroadcast = broadcaster != null;

                Doors = new List<IMyDoor>();
                Welders = new List<IMyShipWelder>();
                MergeBlocks = new List<IMyShipMergeBlock>();
                Loadouts = new Dictionary<string, Loadout>();
                ToggleBlocks = new List<ToggleBlock>();
            }

            public void Fire()
            {
                if(_launchSystem.LaunchProgram == null) { return; }

                if (IsReady() || Status == BayStatus.Queued)
                {
                    setStatus(BayStatus.Firing);
                    string command = "fire --range " + Number + " " +Number;
                    _launchSystem.LaunchProgram.TryRun(command);
                    startCountDown(RECHECK_DELAY);

                    sendMessage(FIRE);
                }
                else
                {
                    _log.Error(Name + " - is not ready.");
                    sendMessage(ERR);
                }
            }


            public void Reload()
            {
                if(timerLockout()){ return; }
                if (Status == BayStatus.Ready || Status == BayStatus.Loaded)
                {
                    _log.Error(Name + " is already loaded.");
                    sendMessage(ERR);
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
                sendMessage(OPENING);
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
                sendMessage(CLOSING);
            }

            public void Toggle()
            {
                if (opened())
                    Close();
                else
                    Open();
            }

            public void BayTimerCall()
            {
                switch (Status)
                { 
                    case BayStatus.Firing:
                        check();                            
                        break;
                    case BayStatus.Queued:
                        Fire();
                        break;
                    case BayStatus.Opening:
                        check();
                        if (opened()) { sendMessage(OPENED); }
                        else { sendMessage(ERR); }
                        break;
                    case BayStatus.Closing:
                        if (loaded()) { setStatus(BayStatus.Loaded); }
                        else { setStatus(BayStatus.Empty); }
                        sendMessage(CLOSED);
                        break;
                    case BayStatus.Reloading:
                        activateReload(false);
                        break;
                    case BayStatus.RLQueued:
                        activateReload(true);
                        break;
                }
            }


            public void SelectLoadOut(string selection)
            {
                //TODO
            }

            public void ManualCheck(bool force)
            {
                if(!force && Status != BayStatus.Unset && Status != BayStatus.Error)
                {
                    _log.Warning(Name + "\n- Checks only run on initial setup and errors.");
                    return;
                }

                if(IsCounting())
                {
                    _log.Warning(Name + "\n- Checks can't be run during timer countdown.");
                    return;
                }

                check();

                _log.Info(Name + " is set to " + Status.ToString());
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

                if(Status == BayStatus.Ready)
                    activateToggleBlock(true);
                else
                    activateToggleBlock(false);
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
                    _log.Error(Name + "\n- Can't perform action during timer countdown.");
                    return true;
                }

                return false;
            }

            private void activateReload(bool setActive)
            {
                Loadout loadout = GetActiveLoadout();
                if (loadout == null || loadout.Projector == null)
                {
                    _log.Error(Name + ": Could not get active loadout");
                    return;
                }

                if (Welders.Count < 1 & !setActive)
                {
                    _log.Error(Name + ": No Welders available!");
                    return;
                }

                string action;
                if (setActive)
                {
                    action = "OnOff_On";
                    setStatus(BayStatus.Reloading);
                    startCountDown(loadout.ReloadTime);
                    sendMessage(LOADING);
                }
                else
                {
                    action = "OnOff_Off";
                    check();

                    if(loaded())
                        sendMessage(LOADED);
                    else
                        sendMessage(ERR);
                }

                IMyProjector projector = loadout.Projector;
                projector.GetActionWithName(action).Apply(projector);

                foreach (IMyShipWelder welder in Welders)
                {
                    welder.GetActionWithName(action).Apply(welder);
                }
            }

            private void sendMessage(int msgNumber)
            {
                if (canBroadcast)
                {
                    string message = String.Format("Transmit Message {0}", msgNumber.ToString());
                    Broadcaster.GetActionWithName(message).Apply(Broadcaster);
                }
            }

            private void activateToggleBlock(bool setActive)
            {
                if (!HasToggle) { return; }

                foreach(ToggleBlock toggle in ToggleBlocks)
                {
                    toggle.Activate(setActive);
                }
            }
        }

        class Loadout
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

        class ToggleBlock
        {
            private IMyTerminalBlock block;
            private bool inverted;
            private MyIniHandler iniHandler;

            public ToggleBlock(IMyTerminalBlock block)
            {
                this.block = block;
                iniHandler = new MyIniHandler(this.block);
                inverted = ParseBool(iniHandler.GetKey(BAY_HEAD, "Inverted", "false"));
            }

            public void Activate(bool setActive)
            {
                bool turnOn;
                if (inverted)
                    turnOn = !setActive;
                else
                    turnOn = setActive;

                string action;
                if (turnOn)
                    action = "OnOff_On";
                else
                    action = "OnOff_Off";

                block.GetActionWithName(action).Apply(block);
            }
        }


        enum BayStatus
        {
            Opening, Open, Ready, Closing, Empty, Reloading, RLClosing, RLOpening, Unset, Error, Firing, Queued, RLQueued, Loaded, Loading
        }


        static BayStatus ParseStatus(string status)
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

        enum TargetMode
        {
            Camera, Turret, BeamRide
        }

        static TargetMode ParseTargetMode(string mode)
        {
            switch (mode.ToUpper().Trim())
            {
                case "CAMERA":
                    return TargetMode.Camera;
                case "TURRET":
                    return TargetMode.Turret;
                case "BEAMRIDE":
                default:
                    return TargetMode.BeamRide;

            }
        }



        Bay BayFromGroup(IMyBlockGroup group)
        {
            string[] nameParts = group.Name.Split(' ');
            string numPart = nameParts[nameParts.Length-1];

            int bayNumber = ParseInt(numPart, -1);
            if (bayNumber < 0)
            {
                _log.Warning("Can't get bay number from group:\n  " + group.Name);
                return null;
            }

            IMyTimerBlock timer = GetSameGridTimer(group);
            if(timer == null) { return null; }

            IMyBroadcastController broadcaster = GetSameGridBroadcaster(group);

            Bay bay = new Bay(bayNumber, group.Name, timer, broadcaster);

            AddDoors(bay, group);
            AddWelders(bay, group);
            AddMergeBlocks(bay, group);
            AddConnectors(bay, group);
            AddLoadouts(bay, group);
            AddToggles(bay, group);

            return bay;
        }

        

        void AssembleMissileBays()
        {
            //Echo("Adding Missile Bays");

            IMyProgrammableBlock launchProgram = GetLaunchProgram();
            _launchSystem = new LaunchSystem(launchProgram);

            if (launchProgram == null) {
                _toggleTag = DEF_TOGGLE;
                return;
            }

            AddMainBroadcaster();
            


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
                        _log.Error("Cannot add " + group.Name + ". Launch systems already contains key " + key);  
                    }
                    else
                    {
                        _launchSystem.Bays.Add(key, bay);
                    }
                }
            }

            _launchSystem.UpdateBayData();
        }

        IMyProgrammableBlock GetLaunchProgram()
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

        void AddMainBroadcaster()
        {
            string bcName = _programIni.GetKey(BAY_HEAD, MAIN_BC_KEY, "");

            if(bcName == "")
            {
                bcName = GetDefaultBCName();
                if(bcName == "") { return; }
            }

            _launchSystem.Broadcaster = GridTerminalSystem.GetBlockWithName(bcName) as IMyBroadcastController;
        }


        string GetDefaultBCName()
        {
            List<IMyBroadcastController> broadcasters = new List<IMyBroadcastController>();
            GridTerminalSystem.GetBlocksOfType<IMyBroadcastController>(broadcasters);

            foreach(IMyBroadcastController broadcaster in broadcasters)
            {
                if(broadcaster.CustomName.Contains(INI_HEAD) && SameGridID(broadcaster))
                    return broadcaster.CustomName;
            }

            return "";
        }


        void AddDoors(Bay bay, IMyBlockGroup group)
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

        void AddWelders(Bay bay, IMyBlockGroup group)
        {
            List<IMyShipWelder> welders = new List<IMyShipWelder>();
            group.GetBlocksOfType<IMyShipWelder>(welders);

            foreach(IMyShipWelder welder in welders)
            {
                if(SameGridID(welder))
                    bay.Welders.Add(welder);
            }
        }

        void AddMergeBlocks(Bay bay, IMyBlockGroup group)
        {
            List<IMyShipMergeBlock> mergeBlocks = new List<IMyShipMergeBlock>();
            group.GetBlocksOfType<IMyShipMergeBlock>(mergeBlocks);

            foreach(IMyShipMergeBlock mergeBlock in mergeBlocks)
            {
                if(SameGridID(mergeBlock))
                    bay.MergeBlocks.Add(mergeBlock);
            }
        }

        void AddConnectors(Bay bay, IMyBlockGroup group)
        {
            List<IMyShipConnector> connectors = new List<IMyShipConnector>();
            group.GetBlocksOfType<IMyShipConnector>(connectors);

            foreach(IMyShipConnector connector in connectors)
            {
                if (SameGridID(connector))
                    bay.Connectors.Add(connector);
            }
        }

        void AddLoadouts(Bay bay, IMyBlockGroup group)
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
                        _log.Warning("Group " + bay.Name + " contains multiple projectors with loadout {" + key + "}");
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


        void AddToggles(Bay bay, IMyBlockGroup group)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            group.GetBlocksOfType<IMyTerminalBlock>(blocks);

            foreach (IMyTerminalBlock block in blocks)
            {
                if(SameGridID(block) && block.CustomName.Contains(_toggleTag))
                {
                    ToggleBlock toggle = new ToggleBlock(block);
                    bay.ToggleBlocks.Add(toggle);
                    bay.HasToggle = true;
                }
            }
        }


        IMyTimerBlock GetSameGridTimer(IMyBlockGroup group)
        {
            List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
            group.GetBlocksOfType<IMyTimerBlock>(timers);

            if (timers.Count < 1)
            {
                _log.Error("Group \"" + group.Name + "\" contains no timers!");
                return null;
            }

            foreach (IMyTimerBlock timer in timers)
            {
                // Exclude timers with TOGGLE_TAG (main timer should always be active)
                if (SameGridID(timer) &! timer.CustomName.Contains(_toggleTag)) { return timer; }
            }

            _log.Error("No timers from group \"" + group.Name + "\" found on the same grid.");
            return null;
        }

        IMyBroadcastController GetSameGridBroadcaster(IMyBlockGroup group)
        {
            List<IMyBroadcastController> broadcasters = new List<IMyBroadcastController>();
            group.GetBlocksOfType<IMyBroadcastController>(broadcasters);

            foreach (IMyBroadcastController broadcaster in broadcasters)
            {
                if (SameGridID(broadcaster))
                {
                    return broadcaster;
                }
            }

            return null;
        }

        static int[] RangeFromString(string range)
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
                _log.Error("Invalid range argument:\n \"" + range + "\"");
            }

            return minMax;
        }

        static bool WithinRange(int[] range, int valueToCheck)
        {
            return valueToCheck >= range[0] && valueToCheck <= range[1];
        }


        void ShowMissileBayData()
        {
            if(_launchSystem != null)
                Echo(_launchSystem.BayData);
        }
    }
}