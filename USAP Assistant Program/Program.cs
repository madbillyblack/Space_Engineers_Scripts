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
        const string FUEL_SUPPLY = "[IGT]";
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

        //Max distance (in meters) that program will check for timer
        const int REF_DIST = 12;

        //INVENTORY CONSTANTS:
        //-------------------------------------------------------------------
        const string SOURCE = "[SRC]";
        const string AMMO = "NATO_25x184mm";
        const int AMMO_VOLUME = 16;
        const string MISL = "Missile200mm";
        const int MISSILE_VOLUME = 60;
        const string FUEL = "Uranium";

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
        string _shipTag;

        string _refTag;
        public IMyTerminalBlock _refBlock;
        int _refDistance;

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
            if(argument == "")
			{
                manageCargo();
			}

            Echo(_statusMessage);
            /*
            MyIniParseResult result;
			if (!_ini.TryParse(Me.CustomData, out result))
				throw new Exception(result.ToString());

			_shipTag = _ini.Get(INI_HEAD, "shipTag").ToString();

			_refTag = _ini.Get(INI_HEAD, "referenceBlock").ToString();

			_combatShip = _ini.Get(INI_HEAD, "combatShip").ToBoolean();

			_miningShip = _ini.Get(INI_HEAD, "miningShip").ToBoolean();

			_largeGrid = _ini.Get(INI_HEAD, "largeGrid").ToBoolean();

			int errorCount = 0;

			if (_shipTag == "<Unset>")
			{
				Echo("SETUP: Please set shipTag parameter in Custom Data!\n");
				errorCount++;
			}

			if (_refTag == "<Unset>")
			{
				Echo("SETUP: Please set referenceBlock parameter in Custom Data!\n");
				errorCount++;
			}

			if (errorCount == 0)
			{
				if (argument != "")
				{
					string[] argArray = argument.Split(' ');
					string action = argArray[0];

					switch (action)
					{
						case ARGUMENT_A:
							Activate(TIMER_A);
							break;
						case ARGUMENT_B:
							Activate(TIMER_B);
							break;
						case "ResetCount":
							_loadCount = 0;
							displayLoadCount();
							break;
						case "SetCount":
							if (argArray.Length > 1)
							{
								string qty = argArray[1];
								_loadCount = int.Parse(qty);
								displayLoadCount();
							}
							break;
						default:
							Echo("Invalid Argument");
							break;
					}
				}
				else
				{
					if (_combatShip)
					{
						Reload();
					}

					if (_miningShip)
					{
						Unload();
					}
				}
			}
            */
        }


        // FUNCTIONS // ---------------------------------------------------------------------------------------------------------------------

        // ACTIVATE // - Finds and activates "Timer Block A" which is linked to detaching mechanisms. 
        public void Activate(string trigger)
        {
            List<IMyTerminalBlock> timers_list = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(trigger, timers_list);
            if (timers_list.Count > 0)
            {
                IMyTerminalBlock ref_block = GridTerminalSystem.GetBlockWithName(_refTag) as IMyTerminalBlock;
                double max_dist = REF_DIST;
                for (int i = 0; i < timers_list.Count; i++)
                {
                    var distance = Vector3D.Distance(ref_block.GetPosition(), timers_list[i].GetPosition());
                    IMyTerminalBlock timer = timers_list[i] as IMyTimerBlock;
                    if (distance < max_dist)
                    {
                        timer.GetActionWithName("TriggerNow").Apply(timer);
                    }
                }
            }

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
            List<IMyCargoContainer> fuelSupplies = new List<IMyCargoContainer>();
            List<IMyCargoContainer> oreContainers = new List<IMyCargoContainer>();

            foreach(IMyCargoContainer cargoBlock in cargoBlocks)
			{
                string name = cargoBlock.CustomName;
                
                if (name.Contains(AMMO_SUPPLY))
				{
                    ammoSupplies.Add(cargoBlock);
				}

                if(name.Contains(FUEL_SUPPLY))
				{
                    fuelSupplies.Add(cargoBlock);
				}

                if(name.Contains(DEST))
				{
                    oreContainers.Add(cargoBlock);
				}
			}

            foreach (IMyTerminalBlock block in _inventories)
			{
                string name = block.CustomName;

                if(name.Contains(MAG_TAG))
				{
                    Echo("Reload" + name);
                    Reload(block, ammoSupplies);
                }
                
                if(name.Contains(REACTOR))
				{
                    Echo("Refuel" + name);
				}

                if (name.Contains(PAYLOAD))
                {
                    Echo("Unload" + name);
                }
			}
        }


        // RELOAD // - Finds all inventories containing defined tag, and loads them with defined amounts of ammo.
        void Reload(IMyTerminalBlock magazine, List<IMyCargoContainer> supplyBlocks)
        {

            if (supplyBlocks.Count < 1 || !magazine.HasInventory)
            {
                return;
            }

            IMyInventory magInv = magazine.GetInventory(0);

            int gatQty = ParseInt(GetKey(magazine, INI_HEAD, "NATO_25x184mm", "0"),0);
            int missileQty = ParseInt(GetKey(magazine, INI_HEAD, "Missile200mm", "0"), 0);
            int artilleryQty = ParseInt(GetKey(magazine, INI_HEAD, "LargeCalibreAmmo", "0"), 0);
            int assaultQty = ParseInt(GetKey(magazine, INI_HEAD, "MediumCalibreAmmo", "0"), 0);
            int autoQty = ParseInt(GetKey(magazine, INI_HEAD, "AutocannonClip", "0"), 0);
            int railQty = ParseInt(GetKey(magazine, INI_HEAD, "LargeRailgunAmmo", "0"), 0);
            int miniRailQty = ParseInt(GetKey(magazine, INI_HEAD, "SmallRailgunAmmo", "0"), 0);

            foreach (IMyCargoContainer supply in supplyBlocks)
			{
                if(supply.HasInventory)
				{
                    IMyInventory supplyInv = supply.GetInventory(0);

                    ensureMinimumAmount(supplyInv, magInv, "NATO_25x184mm", gatQty);
                    ensureMinimumAmount(supplyInv, magInv, "Missile200mm", miniRailQty);
                    ensureMinimumAmount(supplyInv, magInv, "LargeCalibreAmmo", artilleryQty);
                    ensureMinimumAmount(supplyInv, magInv, "MediumCalibreAmmo", assaultQty);
                    ensureMinimumAmount(supplyInv, magInv, "AutocannonClip", autoQty);
                    ensureMinimumAmount(supplyInv, magInv, "LargeRailgunAmmo", railQty);
                    ensureMinimumAmount(supplyInv, magInv, "SmallRailgunAmmo", miniRailQty);
                }
            }
        }



        //Finds all reactors containing defined tag, and loads them with defined amounts of fuel. 
        void Refuel(string dest, int fuel_qty)
        {
            //Builds list of all source inventories. 
            List<IMyTerminalBlock> source_list = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(SOURCE, source_list);

            //Builds list of all destination inventories. 
            List<IMyTerminalBlock> dest_list = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(dest, dest_list);

            //Cycles through destination inventories. 
            for (int c = 0; c < dest_list.Count; c++)
            {
                IMyTerminalBlock destBlock = dest_list[c];
                if (destBlock.HasInventory)
                {
                    //Cycles through source inventories. 
                    for (int d = 0; d < source_list.Count; d++)
                    {
                        IMyTerminalBlock sourceBlock = source_list[d];
                        if (sourceBlock.HasInventory)
                        {
                            //Retrieve source and destination inventories. 
                            IMyInventory sourceInv = source_list[d].GetInventory(0);
                            IMyInventory destInv = destBlock.GetInventory(0);

                            //Load Selected Reactor With Uranium 
                            ensureMinimumAmount(sourceInv, destInv, FUEL, fuel_qty);
                        }

                    }
                }

            }
        }


        void Unload()
        {
            _loadCount += 1;
            Echo(_loadCount.ToString());
            List<IMyTerminalBlock> source_list = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(PAYLOAD, source_list);

            List<IMyTerminalBlock> dest_list = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(DEST, dest_list);

            for (int c = 0; c < source_list.Count; c++)
            {
                var source = source_list[c];
                if (source.HasInventory && source.CustomName.Contains(_shipTag))
                {
                    var sourceInv = source.GetInventory(0);
                    for (int d = 0; d < dest_list.Count; d++)
                    {
                        var dest = dest_list[d];
                        if (dest.HasInventory)
                        {
                            var destInv = dest.GetInventory(0);
                            if (!destInv.IsFull)
                            {
                                List<MyInventoryItem> items = new List<MyInventoryItem>();
                                sourceInv.GetItems(items);
                                for (int i = 0; i < items.Count; i++)
                                {
                                    MyInventoryItem item = items[i];
                                    Echo(item.Type.ToString());
                                    if (item.Type.ToString().Contains("MyObjectBuilder_Ore"))
                                    {
                                        sourceInv.TransferItemTo(destInv, 0, null, true, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            displayLoadCount();
        }


        void displayLoadCount()
        {
            IMyTerminalBlock ref_block = GridTerminalSystem.GetBlockWithName(_refTag);
            string message = "Load Count: " + _loadCount.ToString();
            ref_block.CustomData = message;
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

            _refDistance = ParseInt(GetKey(Me, INI_HEAD, "Ref_Distance", "12"), 12);

            _inventories = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);


            foreach(IMyTerminalBlock block in blocks)
			{
                string name = block.CustomName;
                if (block.HasInventory)
                {
                    if (name.Contains(PAYLOAD))
                    {
                        AddToInventories(block);
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
            if (!name.Contains(MAG_TAG + "]") && !name.Contains(":"))
                return;

            string tag = TagFromName(name);

            switch(tag.ToUpper())
			{
                case "GENERAL":
                    EnsureLoadout(block, new int[] { 5, 5, 5, 5, 5, 5, 5});
                    break;
                case GATLING:
                    EnsureLoadout(block, new int[] { 7, 0, 0, 0, 0, 0, 0 });
                    break;
                case MISSILE:
                    EnsureLoadout(block, new int[] { 0, 5, 0, 0, 0, 0, 0 });
                    break;
                case ARTILLERY:
                    EnsureLoadout(block, new int[] { 0, 0, 10, 0, 0, 0, 0 });
                    break;
                case ASSAULT:
                    EnsureLoadout(block, new int[] { 0, 0, 0, 10, 0, 0, 0 });
                    break;
                case AUTO:
                    EnsureLoadout(block, new int[] { 0, 0, 0, 0, 5, 0, 0 });
                    break;
                case RAIL:
                    EnsureLoadout(block, new int[] { 0, 0, 0, 0, 0, 4, 0 });
                    break;
                case MINI_RAIL:
                    EnsureLoadout(block, new int[] { 0, 0, 0, 0, 0, 0, 8 });
                    break;
			}
		}


        // ADD TO INVENTORIES //
        public void AddToInventories(IMyTerminalBlock block)
		{
            if(GetKey(block, INI_HEAD, "Grid_ID", _gridID) == _gridID)
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
                amounts = new int[] {0,0,0,0,0,0,0};

            EnsureKey(block, INI_HEAD, "NATO_25x184mm", amounts[0].ToString());
            EnsureKey(block, INI_HEAD, "Missile200mm", amounts[1].ToString());
            EnsureKey(block, INI_HEAD, "LargeCalibreAmmo", amounts[2].ToString());
            EnsureKey(block, INI_HEAD, "MediumCalibreAmmo", amounts[3].ToString());
            EnsureKey(block, INI_HEAD, "AutocannonClip", amounts[4].ToString());
            EnsureKey(block, INI_HEAD, "LargeRailgunAmmo", amounts[5].ToString());
            EnsureKey(block, INI_HEAD, "SmallRailgunAmmo", amounts[6].ToString());
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

        //---------------------------------------------------------------------------------//
        //ALL CODE BELOW THIS POINT WRITTEN BY PILOTERROR42//
        //---------------------------------------------------------------------------------//

        void ensureMinimumAmount(IMyInventory source, IMyInventory dest, string itemType, int num)
		{
            if (num < 1)
                return;

			while (!hasEnoughOfItem(dest, itemType, num))
			{
				int? index = indexOfItem(source, itemType);
				if (index == null)
					return;
				source.TransferItemTo(dest, (int)index, null, true, num - numberOfItemInContainer(dest, itemType));
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
