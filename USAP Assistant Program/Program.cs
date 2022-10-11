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
        // USER CONSTANTS:
        const string ORE_DEST = "[ORE]"; // Destination for Ore Drop Offs
        const string FUEL_SUPPLY = "[SRC]"; // Source inventory tag for refueling.
        const string AMMO_SUPPLY = "[WEP]"; // Source inventory tag for rearming.
        const string COMP_SUPPLY = "[CMP]"; // Source inventory tag for re-stocking components.
        const string GAS_TAG = "[H2O]"; // Destination inventory tag for re-stocking ice.
        const string ICE_SUPPLY = "[ORE]"; // Destination inventory tag for re-stocking ice.
        const string LOAD_TAG = "Load Counter"; // Tag for block that displays load count
        const string THRUST_DISPLAY = "Thrust Display"; // Tag for block that displays escape thruster override percentage.
        const int RUN_CAP = 10;

        // CONTROLLER CONSTANTS:
        const double TIME_STEP = 1.0 / 6.0;
        const double KP = 1;
        const double KI = 0;
        const double KD = 0;

        //DEFINITIONS:

        const string INI_HEAD = "USAP";
        const string SHARED = "Shared Data";
        const string PAYLOAD = "[MIN]";
        const string MAG_TAG = "[MAG";
        const string REACTOR = "[PWR]";
        const string COMP_TAG = "[CST]";
        
        const string GATLING = "GATLING";
        const string MISSILE = "MISSILE";
        const string ARTILLERY = "ARTILLERY";
        const string ASSAULT = "ASSAULT";
        const string AUTO = "AUTO";
        const string RAIL = "RAIL";
        const string MINI_RAIL = "MINI-RAIL";
        const string LOADOUT = "Loadout";
        const string DEFAULT_PROFILE =  "BulletproofGlass:0\n" +
                                        "Computer:0\n" +
                                        "Construction:0\n" +
                                        "Detector:0\n" +
                                        "Display:0\n" +
                                        "Explosives:0\n" +
                                        "Girder:0\n" +
                                        "GravityGenerator:0\n" +
                                        "InteriorPlate:0\n" +
                                        "LargeTube:0\n" +
                                        "Medical:0\n" +
                                        "MetalGrid:0\n" +
                                        "Motor:0\n" +
                                        "PowerCell:0\n" +
                                        "RadioCommunication:0\n" +
                                        "Reactor:0\n" +
                                        "SmallTube:0\n" +
                                        "SolarCell:0\n" +
                                        "SteelPlate:0\n" +
                                        "Superconductor:0\n" +
                                        "Thrust:0";
        const string DEFAULT_BASIC =    "BulletproofGlass:50\n" +
                                        "Computer:250\n" +
                                        "Construction:1500\n" +
                                        "Detector:0\n" +
                                        "Display:25\n" +
                                        "Explosives:0\n" +
                                        "Girder:25\n" +
                                        "GravityGenerator:0\n" +
                                        "InteriorPlate:250\n" +
                                        "LargeTube:25\n" +
                                        "Medical:0\n" +
                                        "MetalGrid:150\n" +
                                        "Motor:100\n" +
                                        "PowerCell:25\n" +
                                        "RadioCommunication:0\n" +
                                        "Reactor:0\n" +
                                        "SmallTube:500\n" +
                                        "SolarCell:0\n" +
                                        "SteelPlate:1500\n" +
                                        "Superconductor:0\n" +
                                        "Thrust:0";
        const string DEFAULT_ADVANCED = "BulletproofGlass:50\n" +
                                        "Computer:750\n" +
                                        "Construction:1000\n" +
                                        "Detector:25\n" +
                                        "Display:25\n" +
                                        "Explosives:0\n" +
                                        "Girder:25\n" +
                                        "GravityGenerator:4\n" +
                                        "InteriorPlate:125\n" +
                                        "LargeTube:25\n" +
                                        "Medical:0\n" +
                                        "MetalGrid:25\n" +
                                        "Motor:100\n" +
                                        "PowerCell:25\n" +
                                        "RadioCommunication:10\n" +
                                        "Reactor:100\n" +
                                        "SmallTube:250\n" +
                                        "SolarCell:50\n" +
                                        "SteelPlate:1000\n" +
                                        "Superconductor:150\n" +
                                        "Thrust:75";

        //INVENTORY CONSTANTS:
        //-------------------------------------------------------------------

        const string AMMO = "NATO_25x184mm";
        const string MISL = "Missile200mm";
        const string FUEL = "Uranium";
        
        const int SAFETY_HEIGHT = 1000;

        string _statusMessage;
        string _gridID;

        int _loadCount;
        int _runningNumber;
        bool _unloaded;
        bool _unloadPossible;
        bool _hasComponentCargo;
        bool _escapeThrustersOn;
        IMyTerminalBlock _loadCounter;
        IMyTextSurface _countSurface;
        IMyTextSurface _thrustSurface;

        // Escape Thruster variables
        IMyShipController _cockpit;
        float _maxSpeed = 100;
        PID _pid;


        public IMyTerminalBlock _refBlock;

        //List<IMyTerminalBlock> _inventories;
        List<IMyTerminalBlock> _magazines;
        List<IMyTerminalBlock> _reactors;
        List<IMyTerminalBlock> _miningCargos;
        List<IMyTerminalBlock> _constructionCargos;
        List<IMyTerminalBlock> _o2Generators;

        string _escapeTag;
        List<IMyThrust> _escapeThrusters;
        
        // INIT // ----------------------------------------------------------------------------------------------------------------------------------------
        public Program()
        {
            if (Storage.Length > 0)
            {
                string[] storageData = Storage.Split(';');

                try
                {
                    _loadCount = int.Parse(storageData[0]);
                }
                catch
                {
                    _loadCount = 0;
                }

                try
                {
                    _escapeThrustersOn = ParseBool(storageData[1]);
                }
                catch
                {
                    _escapeThrustersOn = false;
                }
            }
            else
            {
                _loadCount = 0;
                _escapeThrustersOn = false;
            }

            Build();
        }

        public void Save()
        {
            string loadCount = _loadCount.ToString();
            string escapeActive = _escapeThrustersOn.ToString();
            Storage = loadCount + ";" + escapeActive;
        }


        // MAIN // -------------------------------------------------------------------------------------------------------------------------------------
        public void Main(string argument, UpdateType updateSource)
        {
            _unloaded = false;

            Echo("... " + _runningNumber);
            _runningNumber++;

            if(!string.IsNullOrEmpty(argument))
			{
                Echo("CMD: " + argument);

                string[] args = argument.Split(' ');
                string arg = args[0].ToUpper();

                string cmdArg = "";
                if(args.Length > 1)
                {
                    for(int i = 1; i < args.Length; i++)
                    {
                        cmdArg += args[i] + " ";
                    }

                    cmdArg = cmdArg.Trim();
                }

                switch(arg)
				{
                    case "REFRESH":
                        Build();
                        break;
                    case "UNLOAD":
                        Unstock(_miningCargos, ORE_DEST, false);
                        Unstock(_constructionCargos, COMP_SUPPLY, true);
                        break;
                    case "RELOAD":
                        Restock(_magazines, AMMO_SUPPLY);
                        break;
                    case "REFUEL":
                        Restock(_reactors, FUEL_SUPPLY);
                        Restock(_o2Generators, ICE_SUPPLY);
                        break;
                    case "RESUPPLY":
                        Unstock(_constructionCargos, COMP_SUPPLY, true);
                        Restock(_constructionCargos, COMP_SUPPLY);
                        break;
                    case "ESCAPE_THRUSTERS_ON":
                        EscapeThrustersOn();
                        break;
                    case "ESCAPE_THRUSTERS_OFF":
                        EscapeThrustersOff();
                        break;
                    case "TOGGLE_ESCAPE_THRUSTERS":
                        if (_escapeThrustersOn)
                            EscapeThrustersOff();
                        else
                            EscapeThrustersOn();
                        break;
                    case "SELECT_PROFILE":
                        SelectProfile(cmdArg);
                        break;
                    case "UPDATE_PROFILES":
                        UpdateProfiles();
                        break;
                    case "SET_GRID_ID":
                        SetGridID(cmdArg);
                        break;
                    case "ADD_PREFIX":
                        AddTags(cmdArg, true);
                        break;
                    case "ADD_SUFFIX":
                        AddTags(cmdArg, false);
                        break;
                    case "DELETE_PREFIX":
                        RemoveTags(cmdArg, true);
                        break;
                    case "DELETE_SUFFIX":
                        RemoveTags(cmdArg, false);
                        break;
                    case "REPLACE_PREFIX":
                        ReplaceTags(args, true);
                        break;
                    case "REPLACE_SUFFIX":
                        ReplaceTags(args, false);
                        break;
                    case "SWAP_TO_PREFIX":
                        SwapTags(cmdArg, true);
                        break;
                    case "SWAP_TO_SUFFIX":
                        SwapTags(cmdArg, false);
                        break;
                    case "SET_LOAD_COUNT":
                        SetLoadCount(cmdArg);
                        break;
                    case "RESET_LOAD_COUNT":
                        SetLoadCount("0");
                        break;
                    default:
                        TriggerCall(argument);
                        break;
				}
			}
            else if (!_escapeThrustersOn)
            {
                Echo("NO ARGUMENT");
            }

            Echo("STATUS: " + _statusMessage);

            // If cargo successfully unloaded, increment load count.
            if (_unloaded)
			{
                _loadCount++;
                Echo("Load Count: " + _loadCount);
			}
            displayLoadCount();

            if (_escapeThrustersOn)// && ((updateSource & UpdateType.Update10) != -0))
            {
                double velocity = GetForwardVelocity();
                double error = _maxSpeed - velocity;
                double control = _pid.Control(error);

                ThrottleThrusters((float)control);

                if (_runningNumber > RUN_CAP)
                {
                    _runningNumber = 0;
                    SafetyCheck();
                    CheckGravity();
                }
            }
        }


        // FUNCTIONS // ---------------------------------------------------------------------------------------------------------------------

        // ACTIVATE // - Finds and activates "Timer Block A" which is linked to detaching mechanisms. 
        public void Activate(string trigger)
        {
            List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(timers);
            if (timers.Count < 1 || _refBlock == null)
                return;

            List<IMyTimerBlock> triggerTimers = new List<IMyTimerBlock>();
            foreach(IMyTimerBlock timer in timers)
			{
                if (timer.CustomName.Contains(trigger))
                    triggerTimers.Add(timer);
			}
            if (triggerTimers.Count < 1)
			{
                _statusMessage = "No Timers of name " + trigger + " found.";
                return;
            }

            IMyTimerBlock timerToTrigger = triggerTimers[0];
            var distance = Vector3D.Distance(_refBlock.GetPosition(), timerToTrigger.GetPosition());

            // Check to see if any other triggers are closer than the one in the list. If they are, make them the dominant trigger.
            foreach (IMyTimerBlock triggerTimer in triggerTimers)
            {
                var newDistance = Vector3D.Distance(_refBlock.GetPosition(), triggerTimer.GetPosition());
                if (newDistance < distance)
                {
                    distance = newDistance;
                    timerToTrigger = triggerTimer;
                }
            }

            timerToTrigger.GetActionWithName("TriggerNow").Apply(timerToTrigger);
            Echo("Activate: " + timerToTrigger.CustomName);
        }


        // TRIGGER CALL //
        public void TriggerCall(string arg)
		{
            if(arg.ToUpper().StartsWith("TRIGGER_"))
			{

                // Format the argument into a trigger key to read from program block's INI.
                string[] args = arg.Split('_');
                if(args.Length < 2)
				{
                    _statusMessage = "Invalid Trigger Command: " + arg;
                    return;
				}
                string triggerKey = "Trigger_" + args[1];

                // Get a timer name/tag from the INI and try to activate the nearest timer with that name.
                string timerName = GetKey(Me, INI_HEAD, triggerKey , "Timer Block");
                Activate(timerName);
                
                return;
			}

            _statusMessage = "UNRECOGNIZED COMMAND: " + arg;
        }


        // MANAGE CARGO //
        void ManageCargo()
        {
            Unstock(_miningCargos, ORE_DEST, false);

            Restock(_magazines, AMMO_SUPPLY);
            Restock(_reactors, FUEL_SUPPLY);
            Restock(_o2Generators, ICE_SUPPLY);

            Unstock(_constructionCargos, COMP_SUPPLY, true);
            Restock(_constructionCargos, COMP_SUPPLY);
        }


        // RELOAD // - Finds all inventories containing defined tag, and loads them with defined amounts of ammo.
        void Reload(IMyTerminalBlock destination, List<IMyTerminalBlock> supplyBlocks)
        {
            // Convert Loadout Key into 2D array of format [Ammotype][AmmoQty]
            string[][] loadouts = StringTo2DArray(GetKey(destination, INI_HEAD, LOADOUT, ""), '\n', ':');

            if (supplyBlocks.Count < 1 || !destination.HasInventory || loadouts.Length < 1)
            {
                return;
            }

            IMyInventory magInv = destination.GetInventory(0);

            foreach (IMyTerminalBlock supply in supplyBlocks)
            {
                if (supply.HasInventory)
                {
                    IMyInventory supplyInv = supply.GetInventory(0);

                    for (int i = 0; i < loadouts.Length; i++)
                    {
                        ensureMinimumAmount(supplyInv, magInv, loadouts[i][0], ParseInt(loadouts[i][1], 1));
                    }
                }
            }
        }


        // REFUEL //Finds all reactors containing defined tag, and loads them with defined amounts of fuel. 
        void Refuel()
        {
            if (_reactors.Count < 1)
                return;

            List<IMyTerminalBlock> fuelSupplies = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(FUEL_SUPPLY, fuelSupplies);

            if (fuelSupplies.Count < 1)
                return;

            foreach(IMyTerminalBlock reactor in _reactors)
            {
                IMyInventory destInv = reactor.GetInventory(0);
                int fuel_qty = ParseInt(GetKey(reactor, INI_HEAD, "Uranium", "50"), 50);

                foreach (IMyReactor fuelSupply in fuelSupplies)
                {
                    IMyInventory sourceInv = fuelSupply.GetInventory(0);
                    ensureMinimumAmount(sourceInv, destInv, FUEL, fuel_qty);
                }
            }
        }


        // RESTOCK //
        void Restock(List<IMyTerminalBlock> destBlocks, string sourceTag)
        {
            if (destBlocks.Count < 1)
                return;

            List<IMyTerminalBlock> sourceBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(sourceTag, sourceBlocks);
            if (sourceBlocks.Count < 1)
                return;

            foreach (IMyTerminalBlock destBlock in destBlocks)
            {
                Echo("Resupply: " + destBlock.CustomName);
                Reload(destBlock, sourceBlocks);
            } 
        }


        // UNSTOCK //
        void Unstock(List<IMyTerminalBlock> sourceBlocks, string destTag, bool includeComponents)
        {
            if (sourceBlocks.Count < 1)
                return;

            List<IMyTerminalBlock> destBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(destTag, destBlocks);

            foreach(IMyTerminalBlock sourceBlock in sourceBlocks)
            {
                Unload(sourceBlock, destBlocks, includeComponents);
            }
        }


        // UNLOAD //
        void Unload(IMyTerminalBlock payload, List<IMyTerminalBlock> destBlocks, bool includeComponents)
        {
             if (destBlocks.Count < 1)
                return;

            Echo("Unloading " + payload.CustomName);

            var sourceInv = payload.GetInventory(0);
            foreach(IMyTerminalBlock container in destBlocks)
            {
                Echo("Destination: " + container.CustomName);
                if (container.HasInventory)
                {
                    var destInv = container.GetInventory(0);
                    if (!destInv.IsFull)
                    {
                        List<MyInventoryItem> items = new List<MyInventoryItem>();
                        sourceInv.GetItems(items);
                        if(items.Count > 0)
						{
                            foreach (MyInventoryItem item in items)
                            {
                                //Echo(item.Type.ToString());
                                if (item.Type.ToString().Contains("MyObjectBuilder_Ore") || (includeComponents && item.Type.ToString().Contains("MyObjectBuilder_Component")))
                                {
                                    sourceInv.TransferItemTo(destInv, 0, null, true, null);
                                    _unloaded = true;
                                }
                            }
                        }
                    }
                }
            }
        }


        // DISPLAY LOAD COUNT //
        void displayLoadCount()
        {
            if (_countSurface == null || !_unloadPossible)
                return;
 
            _countSurface.WriteText("Load Count: " + _loadCount.ToString());
        }


        // DISPLAY THRUST //


        // SET LOAD COUNT //
        void SetLoadCount(string arg)
        {
            int value = ParseInt(arg, 0);

            if(value < 0)
            {
                Echo("INVALID LOAD COUNT VALUE: " + arg);
                return;
            }

            _loadCount = value;
            displayLoadCount();
        }


        // ESCAPE THRUSTERS ON //
        void EscapeThrustersOn()
        {
            if (_escapeThrusters.Count < 1)
                return;

            _escapeThrustersOn = true;
            _pid = new PID(KP, KI, KD, TIME_STEP);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }


        // ESCAPE THRUSTERS OFF //
        void EscapeThrustersOff()
        {
            ThrottleThrusters(0);
            _escapeThrustersOn = false;
            _runningNumber = 0;
            Runtime.UpdateFrequency = UpdateFrequency.None;
        }


        // ASSIGN COCKPIT //
        void AssignCockpit()
        {
            List<IMyShipController> controllers = new List<IMyShipController>();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(controllers);
            
            if(controllers.Count < 1)
            {
                _statusMessage += "NO CONTROLLERS FOUND!\n";
                return;
            }

            string name = GetKey(Me, INI_HEAD, "Cockpit", "");

            foreach (IMyShipController controller in controllers)
            {
                if(controller.CustomName == name)
                {
                    _cockpit = controller;
                    return;
                }
            }

            // If no perfect match found, choose first controller in list.
            _cockpit = controllers[0];
            SetKey(Me, INI_HEAD, "Cockpit", _cockpit.CustomName);
        }


        // THROTTLE THRUSTERS //
        void ThrottleThrusters(float input)
        {
            if (_escapeThrusters.Count < 1)
                return;

            foreach(IMyThrust thruster in _escapeThrusters)
            {
                thruster.ThrustOverridePercentage = input;
            }

            DisplayThrust(_escapeThrusters[0].ThrustOverridePercentage);
        }


        // CHECK GRAVITY //
        void CheckGravity()
        {
            if (_cockpit.GetNaturalGravity().Length() < 0.04)
            {
                EscapeThrustersOff();
                _statusMessage += "GRAVITY WELL VACATED\nThrusters Disengaged\n";
            }   
        }


        // SAFETY CHECK //
        void SafetyCheck()
        {
            double altitude;
            if(_cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude))
            {
                if (altitude < SAFETY_HEIGHT)
                {
                    double speed = _cockpit.GetShipVelocities().LinearVelocity.Length();
                   
                    if(speed > 0)
                    {
                        Vector3D gravity = _cockpit.GetNaturalGravity();

                        //Get cosine of angle between heading and gravity vector
                        double cos = Vector3D.Dot(_cockpit.WorldMatrix.Forward, gravity) / gravity.Length();

                        if(cos > 0.707) // If angle is within 45 degrees of gravity vector, disengage escape thrusters
                        {
                            EscapeThrustersOff();
                            _statusMessage += "SAFETY THRUSTER DISENGAGE!\n";
                        }
                    }
                }  
            }   
        }


        // INI FUNCTIONS -----------------------------------------------------------------------------------------------------------------------------------

        // ENSURE KEY // Check to see if INI key exists, and if it doesn't write with default value.
        static void EnsureKey(IMyTerminalBlock block, string header, string key, string defaultVal)
        {
            //if (!block.CustomData.Contains(header) || !block.CustomData.Contains(key))
            MyIni ini = GetIni(block);
            if (!ini.ContainsKey(header, key))
                SetKey(block, header, key, defaultVal);
        }


        // GET KEY // Gets ini value from block.  Returns default argument if doesn't exist.
        static string GetKey(IMyTerminalBlock block, string header, string key, string defaultVal)
        {
            EnsureKey(block, header, key, defaultVal);
            MyIni blockIni = GetIni(block);
            return blockIni.Get(header, key).ToString();
        }


        // SET KEY // Update ini key for block, and write back to custom data.
        static void SetKey(IMyTerminalBlock block, string header, string key, string arg)
        {
            MyIni blockIni = GetIni(block);
            blockIni.Set(header, key, arg);
            block.CustomData = blockIni.ToString();
        }


        // GET INI // Get entire INI object from specified block.
        static MyIni GetIni(IMyTerminalBlock block)
        {
            MyIni iniOuti = new MyIni();

            MyIniParseResult result;
            if (!iniOuti.TryParse(block.CustomData, out result))
            {
                block.CustomData = "---\n" + block.CustomData;
                if (!iniOuti.TryParse(block.CustomData, out result))
                    throw new Exception(result.ToString());
            }

            return iniOuti;
        }


        // SET GRID ID // Updates Grid ID parameter for all designated blocks in Grid, then rebuilds the grid.
        void SetGridID(string arg)
        {
            string gridID;
            if (arg != "")
                gridID = arg;
            else
                gridID = Me.CubeGrid.EntityId.ToString();

            SetKey(Me, SHARED, "Grid_ID", gridID);
            _gridID = gridID;

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);

            foreach (IMyTerminalBlock block in blocks)
            {
                if (block.CustomData.Contains(SHARED))
                    SetKey(block, SHARED, "Grid_ID", gridID);
            }

            Build();
        }


        // INIT FUNCTIONS // --------------------------------------------------------------------------------------------------------------------------

        // BUILD //
        public void Build()
        {
            _statusMessage = "";
            _gridID = GetKey(Me, SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString());
            _unloadPossible = false;
            _hasComponentCargo = false;
            _runningNumber = 0;

            // Establish user defined reference block.  If none, set program block as reference.
            string refTag = GetKey(Me, INI_HEAD, "Reference", Me.CustomName);
            try
            {
                _refBlock = GridTerminalSystem.GetBlockWithName(refTag);
                Echo(_refBlock.CustomName);
            }
            catch
            {
                _refBlock = Me;
            }

            _escapeThrusters = new List<IMyThrust>();


            // Ensure Default Trigger Call Parameter
            EnsureKey(Me, INI_HEAD, "Trigger_Door", "Door Timer");

            // Create inventory lists
            _magazines = new List<IMyTerminalBlock>();
            _reactors = new List<IMyTerminalBlock>();
            _constructionCargos = new List<IMyTerminalBlock>();
            _miningCargos = new List<IMyTerminalBlock>();
            _o2Generators = new List<IMyTerminalBlock>();

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);

            foreach (IMyTerminalBlock block in blocks)
            {
                if (block.HasInventory && GetKey(block, SHARED, "Grid_ID", _gridID) == _gridID)
                    AddToInventories(block);
            }

            if (_unloadPossible)
                AssignLoadCounter();

            // Make sure that any construction cargos have a profile in custom data.
            if(_hasComponentCargo)
            {
                EnsureProfiles();
            }

            AssignThrusters();

            if (_escapeThrusters.Count > 0)
            {
                AssignCockpit();
                _maxSpeed = ParseFloat(GetKey(Me, INI_HEAD, "Max_Speed", "100"), 100);
            }

            if (_escapeThrustersOn)
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
            else
                Runtime.UpdateFrequency = UpdateFrequency.None;
        }


        // SET MAG AMMOUNTS //
        public void SetMagAmounts(IMyTerminalBlock block)
        {
            string name = block.CustomName;
            if ((!name.Contains(MAG_TAG + "]") && !name.Contains(":")) || GetKey(block, SHARED, "Grid_ID", _gridID) != _gridID)
                return;

            string tag = TagFromName(name);
            string loadout = "";

            switch (tag.ToUpper())
            {
                case "GENERAL":
                    loadout = "NATO_25x184mm:1\n" +
                              "Missile200mm:1\n" +
                              "LargeCalibreAmmo:1\n" +
                              "MediumCalibreAmmo:1\n" +
                              "AutocannonClip:1\n" +
                              "LargeRailgunAmmo:1\n" +
                              "SmallRailgunAmmo:1";
                    break;
                case GATLING:
                    loadout = "NATO_25x184mm:7";
                    break;
                case MISSILE:
                    loadout = "Missile200mm:4";
                    break;
                case ARTILLERY:
                    loadout = "LargeCalibreAmmo:3";
                    break;
                case ASSAULT:
                    loadout = "MediumCalibreAmmo:2";
                    break;
                case AUTO:
                    loadout = "AutocannonClip:5";
                    break;
                case RAIL:
                    loadout = "LargeRailgunAmmo:1";
                    break;
                case MINI_RAIL:
                    loadout = "SmallRailgunAmmo:6";
                    break;
            }

            EnsureKey(block, INI_HEAD, LOADOUT, loadout);
        }


        // ENSURE PROFILES // Ensure that program block has construction profiles
        public void EnsureProfiles()
        {
            string [] profiles = GetKey(Me, INI_HEAD, "Profiles", "Basic, Advanced").Split(',');

            if (profiles.Length < 1)
                return;

            foreach(string profile in profiles)
            {
                string header = "Profile: " + profile.Trim();

                // Decide the default loadout profile if none already exists.
                string loadout;
                switch (profile.Trim().ToUpper())
                {
                    case "BASIC":
                        loadout = DEFAULT_BASIC;
                        break;
                    case "ADVANCED":
                        loadout = DEFAULT_ADVANCED;
                        break;
                    default:
                        loadout = DEFAULT_PROFILE;
                        break;
                }

                EnsureKey(Me, header, LOADOUT, loadout);
            }

            UpdateProfiles();
        }


        // SELECT PROFILE // - Internal function for choosing profile
        public void SelectProfile(string profileName)
        {
            // Search for profile in 
            if (profileName == "" || !Me.CustomData.ToLower().Contains(profileName.ToLower() + "]"))
            {
                _statusMessage = "No Profile named \"" + profileName + "\" found!";
                return;
            }

            SetActiveProfile(profileName);
            UpdateProfiles();
            Unstock(_constructionCargos, COMP_SUPPLY, true);
            Restock(_constructionCargos, COMP_SUPPLY);
        }


        // GET ACTIVE PROFILE // - Returns Active Profile as 2D array
        public string [][] GetActiveProfile()
        {
            string[] profiles = GetKey(Me, INI_HEAD, "Profiles", DEFAULT_PROFILE).Split(',');

            if (profiles.Length > 0)
            {
                string activeProfile = "Profile: " + profiles[0];
                return StringTo2DArray(GetKey(Me, activeProfile, LOADOUT, DEFAULT_BASIC), '\n', ':');
            }
            else
            {
                return StringTo2DArray(DEFAULT_BASIC, '\n', ':');
            }  
        }


        // SET ACTIVE PROFILE // - Designate Active Profile by making it the first entry in the Profile Key List.
        public void SetActiveProfile(string profileName)
        {
            string[] profiles = GetKey(Me, INI_HEAD, "Profiles", "").Split(',');
            if (profiles.Length < 2)
                return;

            for(int i = 1; i < profiles.Length; i++)
            {
                if(profiles[i].Trim().ToLower() == profileName.ToLower())
                {
                    // Switch the old and new active profiles
                    profiles[i] = profiles[0];
                    profiles[0] = profileName;

                    // Rebuild the Profile Key List.
                    string profileList = profiles[0];
                    for(int j = 1; j < profiles.Length; j++)
                    {
                        profileList += "," + profiles[j];
                    }

                    SetKey(Me, INI_HEAD, "Profiles", profileList);
                    return;
                }
            }
        }


        // UPDATE PROFILES // - Set profiles of all component cargoes, based on profile template saved in program block.
                            // Profiles are proportional to large grid small cargo container: 15,625 L
        public void UpdateProfiles()
        {
            if (_constructionCargos.Count < 1 || !_hasComponentCargo)
                return;

            string[][] profileData = GetActiveProfile();

            foreach (IMyTerminalBlock block in _constructionCargos)
            {
                if (block.CustomName.Contains(COMP_TAG) && ParseBool(GetKey(block, INI_HEAD, "Sync_To_Profiles", "false")))
                {
                    //Get Ratio of container's volume to reference container size
                    float volumeRatio = (float)block.GetInventory(0).MaxVolume / 15.625f;
                    string loadout = "";

                    for (int c = 0; c < profileData.GetLength(0); c++)
                    {
                        double amount = Math.Floor((float) ParseInt(profileData[c][1], 0) * volumeRatio);
                        string number = ((int)amount).ToString();

                        loadout += profileData[c][0] + ":" + number + "\n";
                    }

                    SetKey(block, INI_HEAD, LOADOUT, loadout);
                }
            }  
        }


        // CONNECTED TO COMPONENT SUPPLY // - True if ship is connected to station with designated COMP SUPPLY inventories
        public bool ConnectedToComponentSupply()
        {
            List<IMyTerminalBlock> compSupplies = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(COMP_SUPPLY, compSupplies);

            if (compSupplies.Count > 0)
                return true;
            else
                return false;
        }

        // ADD TO INVENTORIES //
        public void AddToInventories(IMyTerminalBlock block)
        {
            string name = block.CustomName;

            if (name.Contains(PAYLOAD))
            {
                _unloadPossible = true;
                _miningCargos.Add(block);
            }
            else if (name.Contains(MAG_TAG))
            {
                SetMagAmounts(block);
                _magazines.Add(block);
            }
            else if (name.Contains(COMP_TAG))
            {
                EnsureKey(block, INI_HEAD, "Sync_To_Profiles", "True");
                EnsureKey(block, INI_HEAD, LOADOUT, DEFAULT_PROFILE);
                _hasComponentCargo = true;
                _constructionCargos.Add(block);
            }
            else if (name.Contains(REACTOR) && block.BlockDefinition.TypeIdString.ToLower().Contains("reactor"))
            {
                EnsureKey(block, INI_HEAD, LOADOUT, "Ingot/Uranium:100");
                _reactors.Add(block);
            }
            else if (name.Contains(GAS_TAG) && block.BlockDefinition.TypeIdString.ToLower().Contains("oxygengenerator"))
            {
                EnsureKey(block, INI_HEAD, LOADOUT, "Ore/Ice:2702");
                _o2Generators.Add(block);
            }
        }


        // TAG FROM NAME //  Gets specific tag from MAG name.
        public string TagFromName(string name)
        {
            string tag = "";
            if (name.Contains(MAG_TAG + "]"))
                return "GENERAL";

            int start = name.IndexOf(MAG_TAG) + MAG_TAG.Length + 1; //Start index of tag substring - includes colon
            Echo("Start: " + start);
            tag = name.Substring(start);
            Echo(tag);
            int length = tag.IndexOf("]");
            Echo("Length: " + length);
            tag = tag.Substring(0, length);//Length of tag
            Echo(tag);
            //_statusMessage = tag;

            return tag;
        }


        // ENSURE LOADOUT // -  Ensures that inventory block has loadout parameters
        public void EnsureLoadout(IMyTerminalBlock block, int[] amounts)
        {
            if (amounts.Length < 7)
                amounts = new int[] { 0, 0, 0, 0, 0, 0, 0 };

            EnsureKey(block, INI_HEAD, "NATO_25x184mm", amounts[0].ToString());
            EnsureKey(block, INI_HEAD, "Missile200mm", amounts[1].ToString());
            EnsureKey(block, INI_HEAD, "LargeCalibreAmmo", amounts[2].ToString());
            EnsureKey(block, INI_HEAD, "MediumCalibreAmmo", amounts[3].ToString());
            EnsureKey(block, INI_HEAD, "AutocannonClip", amounts[4].ToString());
            EnsureKey(block, INI_HEAD, "LargeRailgunAmmo", amounts[5].ToString());
            EnsureKey(block, INI_HEAD, "SmallRailgunAmmo", amounts[6].ToString());
        }


        // ASSIGN LOAD COUNTER //
        void AssignLoadCounter()
        {
            List<IMyTerminalBlock> counters = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(LOAD_TAG, counters);

            if (counters.Count < 1)
                return;

            foreach (IMyTerminalBlock block in counters)
            {
                if (GetKey(block, SHARED, "Grid_ID", _gridID) == _gridID)
                {
                    IMyTextSurfaceProvider counter = block as IMyTextSurfaceProvider;
                    int surfaceCount = counter.SurfaceCount;
                    int index = ParseInt(GetKey(block, INI_HEAD, "Counter_Index", "0"), 0);

                    if (surfaceCount < 1)
                    {
                        _statusMessage += "DESIGNATED LOAD COUNTER is INVALID: Contains no Display Surfaces!\n";
                        return;
                    }
                    else if (index >= surfaceCount)
                    {
                        index = surfaceCount - 1;
                        SetKey(block, INI_HEAD, "Counter_Index", index.ToString());
                        _statusMessage += "INVALID SCREEN INDEX for block " + block.CustomName + "\n-->Index reset to " + index + "\n";
                    }
                    else if (index < 0)
                    {
                        index = 0;
                        SetKey(block, INI_HEAD, "Counter_Index", "0");
                        _statusMessage += "INVALID SCREEN INDEX for block " + block.CustomName + "\n-->Index reset to 0\n";
                    }

                    if (index > -1 && index < surfaceCount)
                    {
                        _loadCounter = counter as IMyTerminalBlock;
                        _countSurface = counter.GetSurface(index);
                        _countSurface.ContentType = ContentType.TEXT_AND_IMAGE;
                        displayLoadCount();
                        return;
                    }
                }
            }
        }


        // ASSIGN THRUSTERS //
        void AssignThrusters()
        {
            _escapeThrusters = new List<IMyThrust>();
            _escapeTag = GetKey(Me, INI_HEAD, "Escape_Thrusters", "");

            if (_escapeTag == "")
                return;

            IMyBlockGroup escapeGroup = GridTerminalSystem.GetBlockGroupWithName(_escapeTag);
            
            if(escapeGroup == null)
            {
                _statusMessage += "NO GROUP WITH NAME \"" + _escapeTag + "\" FOUND!\n";
                return;
            }

            escapeGroup.GetBlocksOfType<IMyThrust>(_escapeThrusters);
            _statusMessage += "ESCAPE THRUSTERS: " + _escapeTag + "\nThruster Count: " + _escapeThrusters.Count + "\n";
            AssignThrustDisplay();
        }


        // GET FORWARD VELOCITY //
        public double GetForwardVelocity()
        {
            return Vector3D.Dot(_cockpit.WorldMatrix.Forward, _cockpit.GetShipVelocities().LinearVelocity);
        }


        // ASSIGN THRUST DISPLAY //
        void AssignThrustDisplay()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(THRUST_DISPLAY, blocks);

            if (blocks.Count < 1)
                return;

            foreach (IMyTerminalBlock block in blocks)
            {
                if (GetKey(block, SHARED, "Grid_ID", _gridID) == _gridID)
                {
                    IMyTextSurfaceProvider displayBlock = block as IMyTextSurfaceProvider;
                    int surfaceCount = displayBlock.SurfaceCount;
                    int index = ParseInt(GetKey(block, INI_HEAD, "Thrust_Display_Index", "0"), 0);

                    if (surfaceCount < 1)
                    {
                        _statusMessage += "DESIGNATED LOAD COUNTER is INVALID: Contains no Display Surfaces!\n";
                        return;
                    }
                    else if (index >= surfaceCount)
                    {
                        index = surfaceCount - 1;
                        SetKey(block, INI_HEAD, "Thrust_Display_Index", index.ToString());
                        _statusMessage += "INVALID SCREEN INDEX for block " + block.CustomName + "\n-->Index reset to " + index + "\n";
                    }
                    else if (index < 0)
                    {
                        index = 0;
                        SetKey(block, INI_HEAD, "Thrust_Display_Index", "0");
                        _statusMessage += "INVALID SCREEN INDEX for block " + block.CustomName + "\n-->Index reset to 0\n";
                    }

                    if (index > -1 && index < surfaceCount)
                    {
                        _thrustSurface = displayBlock.GetSurface(index);
                        _thrustSurface.ContentType = ContentType.TEXT_AND_IMAGE;
                        DisplayThrust(0);
                        return;
                    }
                }
            }
        }

        // DISPLAY THRUST //
        void DisplayThrust(float power)
        {


            int value = (int) (power * 100);
            string output = value + "%";

            if (value == 0)
                output = "OFF";

            output = "Auto-Throttle: " + output;

            Echo(output);

            if (_thrustSurface == null)
                return;

            _thrustSurface.WriteText(output);
        }


        // TOOL FUNCTIONS // ---------------------------------------------------------------------------------------------------------------------------

        // PARSE INT //
        static int ParseInt(string arg, int defaultValue)
        {
            int number;
            if (int.TryParse(arg, out number))
                return number;
            else
                return defaultValue;
        }


        // PARSE FLOAT //
        static float ParseFloat(string arg, float defaultValue)
        {
            float number;
            if (float.TryParse(arg, out number))
                return number;
            else
                return defaultValue;
        }


        // PARSE BOOL //
        static bool ParseBool(string val)
        {
            string uVal = val.ToUpper();
            if (uVal == "TRUE" || uVal == "T" || uVal == "1")
            {
                return true;
            }

            return false;
        }


        // BORROWED FUNCTIONS // -------------------------------------------------------------------------------------------------------------------------
        public string[][] StringTo2DArray(string source, char separatorOuter, char separatorInner)
        {
            return source
                   .Split(separatorOuter)
                   .Select(x => x.Split(separatorInner))
                   .ToArray();
        }


        //---------------------------------------------------------------------------------//
        //ALL CODE BELOW THIS POINT WRITTEN BY PILOTERROR42//
        //---------------------------------------------------------------------------------//

        void ensureMinimumAmount(IMyInventory source, IMyInventory dest, string itemType, int num)
        {
            if (num < 1)
                return;

            int initialSupply = numberOfItemInContainer(dest, itemType);

            while (!hasEnoughOfItem(dest, itemType, num))
            {
                int? index = indexOfItem(source, itemType);
                if (index == null)
                    return;

                source.TransferItemTo(dest, (int)index, null, true, num - numberOfItemInContainer(dest, itemType));

                // If there's still the same number in the container after attempting to transfer, STOP.
                if (numberOfItemInContainer(dest, itemType) == initialSupply)
                {
                    _statusMessage = "WARNING: Failed to transfer item of type " + itemType + "!";
                    return;
                }
            }
        }


        bool hasEnoughOfItem(IMyInventory inventoryToSearch, string itemName, int minAmount)
        {
            return numberOfItemInContainer(inventoryToSearch, itemName) >= minAmount;
        }


        int numberOfItemInContainer(IMyInventory inventoryToSearch, string itemName)
        {
            int total = 0;
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            inventoryToSearch.GetItems(items);

            for (int c = 0; c < items.Count; c++)
            {
                if (items[c].Type.ToString().Contains(itemName))
                {
                    total += (int)(items[c].Amount);
                }
            }
            return total;
        }


        Nullable<int> indexOfItem(IMyInventory source, string item)
        {
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            source.GetItems(items);
            for (int c = 0; c < items.Count; c++)
            {
                if (items[c].Type.ToString().Contains(item))
                {
                    return c;
                }
            }
            return null;
        }
    }
}
