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
        //DEFINITIONS:
        //Values and strings inside quotes can be changed here.

        const string INI_HEAD = "USAP";
        const string PAYLOAD = "[MIN]";
        const string DEST = "[ORE]";
        const string MAG_TAG = "[MAG";
        const string REACTOR = "[PWR]";
        const string FUEL_SUPPLY = "[SRC]";
        const string AMMO_SUPPLY = "[WEP]";

        const string GATLING = "GATLING";
        const string MISSILE = "MISSILE";
        const string ARTILLERY = "ARTILLERY";
        const string ASSAULT = "ASSAULT";
        const string AUTO = "AUTO";
        const string RAIL = "RAIL";
        const string MINI_RAIL = "MINI-RAIL";

        //TIMER CONSTANTS:
        //-----------------------------------------------------------
        //Names of timers to activate:
        const string TIMER_A = "Door Timer";
        const string TIMER_B = "Timer B";

        //Program Run Argument that will activate timer
        const string ARGUMENT_A = "HangarDoor";
        const string ARGUMENT_B = "TimerB";

        //INVENTORY CONSTANTS:
        //-------------------------------------------------------------------
        const string SOURCE = "[SRC]";
        const string AMMO = "NATO_25x184mm";
        const int AMMO_VOLUME = 16;
        const string MISL = "Missile200mm";
        const int MISSILE_VOLUME = 60;
        const string FUEL = "Uranium";
        const string LOAD_TAG = "[LOAD]";

        /* DEFAULT_MAG
        ---> Default Load Out for Unspecified Mag Blocks*/
        const int DEFAULT_AMMO = 50;
        const int DEFAULT_MISSILE = 0;

        //Default Custom Data
        const string DEFAULT_DATA = "[USAP]\nshipTag=<Unset>\nreferenceBlock=<Unset>\ncombatShip=true\nminingShip=false\nlargeGrid=false\n---\n";

        //MyIni _ini = new MyIni();

        string _statusMessage;
        string _gridID;

        int _loadCount;
        bool _unloaded;
        bool _unloadPossible;
        IMyTerminalBlock _loadCounter;

        public IMyTerminalBlock _refBlock;

        List<IMyTerminalBlock> _inventories;
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

            /*
            if (!Me.CustomData.Contains(INI_HEAD))
            {
                String newData = Me.CustomData;
                newData = newData.Insert(0, DEFAULT_DATA);
                Me.CustomData = newData;
                Echo("SETUP: New Instance of Script. Please set parameters in Custom Data.\n");
            }
            */
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
                manageCargo();
            }
            else
			{
                switch(argument.ToUpper())
				{
                    case "REFRESH":
                        Build();
                        break;
                    case "UNLOAD":
                        // TODO - individual cargo commands
                        break;
                    case "RELOAD":
                        // TODO
                        break;
                    case "REFUEL":
                        // TODO
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
        void manageCargo()
        {
            if (_inventories.Count < 1)
                return;

            List<IMyCargoContainer> cargoBlocks = new List<IMyCargoContainer>();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(cargoBlocks);
            if (cargoBlocks.Count < 1)
            {
                _statusMessage = "No Inventories to interface with.";
                return;
            }

            List<IMyCargoContainer> ammoSupplies = new List<IMyCargoContainer>();
            List<IMyCargoContainer> oreContainers = new List<IMyCargoContainer>();

            foreach (IMyCargoContainer cargoBlock in cargoBlocks)
            {
                string name = cargoBlock.CustomName;

                if (name.Contains(AMMO_SUPPLY))
                {
                    ammoSupplies.Add(cargoBlock);
                }

                if (name.Contains(DEST))
                {
                    oreContainers.Add(cargoBlock);
                }
            }

            List<IMyReactor> tempList = new List<IMyReactor>();
            GridTerminalSystem.GetBlocksOfType<IMyReactor>(tempList);

            List<IMyReactor> fuelSupplies = new List<IMyReactor>();
            
            if(tempList.Count > 0)
			{
                foreach(IMyReactor reactor in tempList)
				{
                    if(reactor.CustomName.Contains(FUEL_SUPPLY))
					{
                        fuelSupplies.Add(reactor);
					}
				}
			}

            foreach (IMyTerminalBlock block in _inventories)
            {
                string name = block.CustomName;

                if (name.Contains(MAG_TAG))
                {
                    Echo("Reload: " + name);
                    Reload(block, ammoSupplies);
                }

                if (name.Contains(REACTOR))
                {
                    Echo("Refuel: " + name);
                    Refuel(block, fuelSupplies);
                }

                if (name.Contains(PAYLOAD))
                {
                    Echo("Unload: " + name);
                    Unload(block, oreContainers);
                }
            }
        }


        // RELOAD // - Finds all inventories containing defined tag, and loads them with defined amounts of ammo.
        void Reload(IMyTerminalBlock magazine, List<IMyCargoContainer> supplyBlocks)
        {
            // Convert Loadout Key into 2D array of format [Ammotype][AmmoQty]
            string[][] loadouts = StringTo2DArray(GetKey(magazine, INI_HEAD, "Loadout", ""), '\n', ':');

            if (supplyBlocks.Count < 1 || !magazine.HasInventory || loadouts.Length < 1)
            {
                return;
            }



            IMyInventory magInv = magazine.GetInventory(0);

            foreach (IMyCargoContainer supply in supplyBlocks)
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
        void Refuel(IMyTerminalBlock reactor, List<IMyReactor> fuelSupplies)//string dest, int fuel_qty)
        {
            if (fuelSupplies.Count < 1)
                return;

            IMyInventory destInv = reactor.GetInventory(0);
            int fuel_qty = ParseInt(GetKey(reactor, INI_HEAD, "Uranium", "50"), 50);

            foreach(IMyReactor supplyReactor in fuelSupplies)
			{
                IMyInventory sourceInv = supplyReactor.GetInventory(0);
                ensureMinimumAmount(sourceInv, destInv, FUEL, fuel_qty);
            }
            
        }


        // UNLOAD //
        void Unload(IMyTerminalBlock payload, List<IMyCargoContainer> oreContainers)
        {
            if (oreContainers.Count < 1)
                return;

            var sourceInv = payload.GetInventory(0);
            foreach(IMyCargoContainer container in oreContainers)
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
                                Echo(item.Type.ToString());
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
        void UpdateGridID(string arg)
        {
            string newID;
            if (arg != "")
                newID = arg;
            else
                newID = Me.CubeGrid.EntityId.ToString();

            SetKey(Me, INI_HEAD, "Grid_ID", newID);

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);

            foreach (IMyTerminalBlock block in blocks)
            {
                if (block.CustomData.Contains(_gridID))
                    SetKey(block, INI_HEAD, "Grid_ID", newID);
            }

            _gridID = newID;
            Build();
        }


        // INIT FUNCTIONS // --------------------------------------------------------------------------------------------------------------------------

        // BUILD //
        public void Build()
        {
            _statusMessage = "";
            _gridID = GetKey(Me, INI_HEAD, "Grid_ID", Me.CubeGrid.EntityId.ToString());
            _loadCounter = GetLoadCounter();
            _unloadPossible = false;

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

            _inventories = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);

            EnsureKey(Me, INI_HEAD, "Trigger_Door", "Door Timer");

            foreach (IMyTerminalBlock block in blocks)
            {
                string name = block.CustomName;
                if (block.HasInventory)
                {
                    if (name.Contains(PAYLOAD))
                    {
                        AddToInventories(block);
                        _unloadPossible = true;
                    }
                    else if (name.Contains(MAG_TAG))
                    {
                        SetMagAmounts(block);
                        AddToInventories(block);
                    }
                    else if (name.Contains(REACTOR))
                    {
                        EnsureKey(block, INI_HEAD, "Uranium", "50");
                        AddToInventories(block);
                    }
                }
            }

            _escapeThrusters = new List<IMyThrust>();
        }


        // SET MAG AMMOUNTS //
        public void SetMagAmounts(IMyTerminalBlock block)
        {
            string name = block.CustomName;
            if ((!name.Contains(MAG_TAG + "]") && !name.Contains(":")) || GetKey(block, INI_HEAD, "Grid_ID", _gridID) != _gridID)
                return;

            string tag = TagFromName(name);
            string loadout = "";

            switch (tag.ToUpper())
            {
                case "GENERAL":
                    //EnsureLoadout(block, new int[] { 5, 5, 5, 5, 5, 5, 5});
                    loadout = "NATO_25x184mm:1\n" +
                              "Missile200mm:1\n" +
                              "LargeCalibreAmmo:1\n" +
                              "MediumCalibreAmmo:1\n" +
                              "AutocannonClip:1\n" +
                              "LargeRailgunAmmo:1\n" +
                              "SmallRailgunAmmo:1";
                    break;
                case GATLING:
                    //EnsureLoadout(block, new int[] { 7, 0, 0, 0, 0, 0, 0 });
                    loadout = "NATO_25x184mm:7";
                    break;
                case MISSILE:
                    //EnsureLoadout(block, new int[] { 0, 5, 0, 0, 0, 0, 0 });
                    loadout = "Missile200mm:4";
                    break;
                case ARTILLERY:
                    //EnsureLoadout(block, new int[] { 0, 0, 10, 0, 0, 0, 0 });
                    loadout = "LargeCalibreAmmo:3";
                    break;
                case ASSAULT:
                    //EnsureLoadout(block, new int[] { 0, 0, 0, 10, 0, 0, 0 });
                    loadout = "MediumCalibreAmmo:2";
                    break;
                case AUTO:
                    //EnsureLoadout(block, new int[] { 0, 0, 0, 0, 5, 0, 0 });
                    loadout = "AutocannonClip:5";
                    break;
                case RAIL:
                    //EnsureLoadout(block, new int[] { 0, 0, 0, 0, 0, 4, 0 });
                    loadout = "LargeRailgunAmmo:1";
                    break;
                case MINI_RAIL:
                    //EnsureLoadout(block, new int[] { 0, 0, 0, 0, 0, 0, 8 });
                    loadout = "SmallRailgunAmmo:6";
                    break;
            }

            EnsureKey(block, INI_HEAD, "Loadout", loadout);
        }


        // ADD TO INVENTORIES //
        public void AddToInventories(IMyTerminalBlock block)
        {
            if (GetKey(block, INI_HEAD, "Grid_ID", _gridID) == _gridID)
            {
                _inventories.Add(block);
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

            _statusMessage = "No valid Load Counter block found.  Load counter should contain tag '" + LOAD_TAG
                            +"' in the name, and have contain any Custom Data parameters for other functions!";
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
                    return;
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
