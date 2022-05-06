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
        const string LOAD_TAG = "[LOAD]";

        string _statusMessage;
        string _gridID;

        int _loadCount;
        bool _unloaded;
        bool _unloadPossible;
        bool _hasComponentCargo;
        IMyTerminalBlock _loadCounter;

        public IMyTerminalBlock _refBlock;

        //List<IMyTerminalBlock> _inventories;
        List<IMyTerminalBlock> _magazines;
        List<IMyTerminalBlock> _reactors;
        List<IMyTerminalBlock> _miningCargos;
        List<IMyTerminalBlock> _constructionCargos;
        List<IMyTerminalBlock> _o2Generators;
        List<IMyThrust> _escapeThrusters;
        
        // INIT // ----------------------------------------------------------------------------------------------------------------------------------------
        public Program()
        {
            if (Storage.Length > 0)
            {
                try
                {
                    _loadCount = int.Parse(Storage);
                }
                catch
                {
                    _loadCount = 0;
                }
            }
            else
            {
                _loadCount = 0;
            }

            Build();
        }

        public void Save()
        {
            Storage = _loadCount.ToString();
        }


        // MAIN // -------------------------------------------------------------------------------------------------------------------------------------
        public void Main(string argument, UpdateType updateSource)
        {
            _unloaded = false;

            if (argument == "")
            {
                ManageCargo();
            }
            else
			{
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
                        Unstock(_miningCargos, ORE_DEST);
                        Unstock(_constructionCargos, COMP_SUPPLY);
                        break;
                    case "RELOAD":
                        Restock(_magazines, AMMO_SUPPLY);
                        break;
                    case "REFUEL":
                        Restock(_reactors, FUEL_SUPPLY);
                        Restock(_o2Generators, ICE_SUPPLY);
                        break;
                    case "RESUPPLY":
                        Unstock(_constructionCargos, COMP_SUPPLY);
                        Restock(_constructionCargos, COMP_SUPPLY);
                        break;
                    case "ESCAPE_THRUSTERS_ON":
                        // TODO - Escape Thruster functions
                        break;
                    case "ESCAPE_THRUSTERS_OFF":
                        // TODO
                        break;
                    case "TOGGLE_ESCAPE_THRUSTERS":
                        // TODO
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
                    default:
                        TriggerCall(argument);
                        break;
				}
			}

            Echo(_statusMessage);

            // If cargo successfully unloaded, increment load count.
            if (_unloaded)
			{
                _loadCount++;
                Echo("Load Count: " + _loadCount);
			}
            displayLoadCount();
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
            Unstock(_miningCargos, ORE_DEST);

            Restock(_magazines, AMMO_SUPPLY);
            Restock(_reactors, FUEL_SUPPLY);
            Restock(_o2Generators, ICE_SUPPLY);

            Unstock(_constructionCargos, COMP_SUPPLY);
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
                Reload(destBlock, sourceBlocks);
        }


        // UNSTOCK //
        void Unstock(List<IMyTerminalBlock> sourceBlocks, string destTag)
        {
            if (sourceBlocks.Count < 1)
                return;

            List<IMyTerminalBlock> destBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(destTag, destBlocks);

            foreach(IMyTerminalBlock sourceBlock in sourceBlocks)
            {
                Unload(sourceBlock, destBlocks);
            }
        }


        // UNLOAD //
        void Unload(IMyTerminalBlock payload, List<IMyTerminalBlock> destBlocks)
        {
             if (destBlocks.Count < 1)
                return;

            var sourceInv = payload.GetInventory(0);
            foreach(IMyCargoContainer container in destBlocks)
            {
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
                                if (item.Type.ToString().Contains("MyObjectBuilder_Ore"))
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
            if (_loadCounter == null || !_unloadPossible)
                return;

            if(_loadCounter.CustomData != "" && !_loadCounter.CustomData.Contains("Load Count:"))
			{
                _statusMessage = "Conflicting Custom Data in Load Counter! Please check block's name and Custom Data!";
                return;
			}

            string message = "Load Count: " + _loadCount.ToString();
            _loadCounter.CustomData = message;
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
            _loadCounter = GetLoadCounter();
            _unloadPossible = false;
            _hasComponentCargo = false;

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

            // Make sure that any construction cargos have a profile in custom data.
            if(_hasComponentCargo)
            {
                EnsureProfiles();
            }
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
                        profileList += " ," + profiles[j];
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
            _statusMessage = tag;

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


        // GET LOAD COUNTER //
        IMyTerminalBlock GetLoadCounter()
        {
            List<IMyTerminalBlock> counters = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(LOAD_TAG, counters);

            foreach (IMyTerminalBlock block in counters)
			{
                if(!block.CustomData.Contains(INI_HEAD))
				{
                    return block;
				}
			}

            /*_statusMessage = "No valid Load Counter block found.  Load counter should contain tag '" + LOAD_TAG
                            +"' in the name, and have contain any Custom Data parameters for other functions!";*/
            return null;
        }


        // TOOL FUNCTIONS // ---------------------------------------------------------------------------------------------------------------------------

        // PARSE INT //
        static int ParseInt(string arg, int defaultVal)
        {
            int number;
            if (int.TryParse(arg, out number))
                return number;
            else
                return defaultVal;
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
