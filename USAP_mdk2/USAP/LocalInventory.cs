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
        class LocalInventory
        {
            public InventoryType Type;
            public Dictionary<string, MyItemType> ItemTypes;
            public Dictionary<string, int> ItemAmmounts;
            public MyIniHandler IniHandler;
            public IMyInventory Inventory;
            public IMyTerminalBlock Block;

            public LocalInventory(InventoryType type, IMyTerminalBlock block)
            {
                Type = type;
                Block = block;
                IniHandler = new MyIniHandler(Block);
                Inventory = Block.GetInventory();
                ItemTypes = new Dictionary<string, MyItemType>();
                ItemAmmounts = new Dictionary<string, int>();
                SetLoadout();
            }

            private void SetLoadout()
            {
                List<MyItemType> itemTypes = new List<MyItemType>();
                Inventory.GetAcceptedItems(itemTypes);

                string[] keyList = IniHandler.GetKey(INI_HEAD, LOADOUT, "").Split('\n');

                if (keyList.Length < 1) { return; }

                foreach (string keyItem in keyList)
                {
                    AddTypeAndCount(keyItem, itemTypes);
                }
            }

            public bool Refill(List<IMyInventory> sources)
            {
                if (ItemTypes.Keys.Count < 1)
                {
                    _log.LogWarning("No item keys loaded for " + Block.CustomName);
                    return true;
                }

                int unstocked = 0;

                foreach (string key in ItemTypes.Keys)
                {
                    int targetCount = ItemAmmounts[key];
                    MyItemType itemType = ItemTypes[key];

                    int missingCount = targetCount - Inventory.GetItemAmount(itemType).ToIntSafe();

                    foreach (IMyInventory source in sources)
                    {
                        if (missingCount > 0)
                        {
                            missingCount = targetCount - RestockItem(itemType, source, missingCount);
                        }
                    }

                    if (missingCount > 0) { unstocked++; }
                }

                return unstocked < 1;
            }

            public bool Unload(List<IMyInventory> destinations)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                Inventory.GetItems(items);

                bool noLeftovers = true;

                foreach (MyInventoryItem item in items)
                {
                    int currentCount = Inventory.GetItemAmount(item.Type).ToIntSafe();

                    foreach (IMyInventory destination in destinations)
                    {
                        if (currentCount > 0)
                        {
                            currentCount = UnloadItem(item, destination, currentCount);
                        }
                    }

                    // If untransferred items set return to false, and continue transfers.
                    if (currentCount > 0)
                        noLeftovers = false;
                }

                return noLeftovers;
            }

            // Transfers specific item to inventory. Returns remaining amount to transfer
            public int UnloadItem(MyInventoryItem item, IMyInventory destination, int targetCount)
            {
                if (Inventory.TransferItemTo(destination, item, targetCount))
                {
                    int currentAmount = Inventory.GetItemAmount(item.Type).ToIntSafe();
                    int transferred = targetCount - currentAmount;
                    //_log.LogInfo(transferred + "x " + GetItemName(item.Type.ToString()) + "transferred from " + Block.CustomName);

                    return currentAmount;
                }
                else
                {
                    _log.LogWarning("Unable to transfer requested " + GetItemName(item.Type.ToString()) + " from " + Block.CustomName);
                    return targetCount;
                }
            }


            private int RestockItem(MyItemType itemType, IMyInventory source, int targetCount)
            {
                //_log.LogInfo("Requested: " + targetCount + "x " + GetItemName(itemType.ToString()) + " in " + Block.CustomName);
                MyInventoryItem? item = source.FindItem(itemType);
                if (item != null && Inventory.TransferItemFrom(source, (MyInventoryItem)item, targetCount))
                {
                    int currentAmount = Inventory.GetItemAmount(itemType).ToIntSafe();

                    return currentAmount;
                }
                else
                {
                    _log.LogWarning("Unable to transfer requested item " + GetItemName(itemType.ToString()) + " to " + Block.CustomName);
                    return Inventory.GetItemAmount(itemType).ToIntSafe();
                }
            }


            // Add item type and count based on string from Loadout list.
            private void AddTypeAndCount(string input, List<MyItemType> itemTypes)
            {
                string[] data = input.Split(':');

                if (data.Length < 2) { return; }

                string key = data[0];

                try
                {
                    MyItemType itemType = ItemTypeFromString(key, itemTypes);

                    ItemTypes.Add(key, itemType);

                    int count;
                    if (int.TryParse(data[1], out count))
                        ItemAmmounts.Add(key, count);
                    else
                        ItemAmmounts.Add(key, 0);
                }
                catch { return; }
            }


            private MyItemType ItemTypeFromString(string itemString, List<MyItemType> itemTypes)
            {
                string itemName = itemString.ToLower();

                foreach (MyItemType itemType in itemTypes)
                {
                    if (itemType.ToString().ToLower().Contains(itemName))
                        return itemType;
                }
                throw new Exception("Could not locate item in type list");
            }
        }


        #region Global Functions
        // ADD TO INVENTORIES //
        public void AddToInventories(IMyTerminalBlock block)
        {
            string name = block.CustomName;

            if (name.Contains(PAYLOAD))
            {
                //_unloadPossible = true;
                //_miningCargos.Add(block);
                _miningCargos.Add(new LocalInventory(InventoryType.ROCKBOX, block));
            }
            else if (name.Contains(MAG_TAG))
            {
                SetMagAmounts(block);
                //_magazines.Add(block);
                _magazines.Add(new LocalInventory(InventoryType.MAGAZINE, block));
            }
            else if (name.Contains(COMP_TAG))
            {
                EnsureKey(block, INI_HEAD, "Sync_To_Profiles", "True");
                EnsureKey(block, INI_HEAD, LOADOUT, DEFAULT_PROFILE);
                _hasComponentCargo = true;
                //_constructionCargos.Add(block);
                _constructionCargos.Add(new LocalInventory(InventoryType.CONSTRUCTION, block));
            }
            else if (name.Contains(REACTOR) && block.BlockDefinition.TypeIdString.ToLower().Contains("reactor"))
            {
                EnsureKey(block, INI_HEAD, LOADOUT, "Ingot/Uranium:100");
                //_reactors.Add(block);
                _reactors.Add(new LocalInventory(InventoryType.REACTOR, block));
            }
            else if (name.Contains(GAS_TAG) && block.BlockDefinition.TypeIdString.ToLower().Contains("oxygengenerator"))
            {
                EnsureKey(block, INI_HEAD, LOADOUT, "Ore/Ice:2702");
                //_o2Generators.Add(block);
                _o2Generators.Add(new LocalInventory(InventoryType.ICEBOX, block));
            }
        }


        // RESTOCK //
        void Restock(List<LocalInventory> destInventories, string sourceTag) //void Restock(List<IMyTerminalBlock> destBlocks, string sourceTag)
        {
            if (destInventories.Count < 1)
                return;
            /*
            List<IMyTerminalBlock> sourceBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(sourceTag, sourceBlocks);
            if (sourceBlocks.Count < 1)
                return;
            */

            List<IMyInventory> sources = GetRemoteInventories(sourceTag);
            _log.LogInfo("Reloading from inventories with tag " + sourceTag);
            _log.LogInfo("Source Count: " + sources.Count);

            foreach (LocalInventory dest in destInventories)
            {
                Echo("Resupply: " + dest.Block.CustomName);
                dest.Refill(sources);
                //Reload(destBlock, sourceBlocks);
            }
        }

        void Unload(List<LocalInventory> localInventories, string destTag)
        {
            if (localInventories.Count < 1) return;

            List<IMyInventory> destinations = GetRemoteInventories(destTag);
            _log.LogInfo("Unloading to inventories with tag " + destTag);
            _log.LogInfo("Destination Count: " + destinations.Count);

            foreach (LocalInventory source in localInventories)
            {
                Echo("Unloading: " +  source.Block.CustomName);
                source.Unload(destinations);
            }
        }
        #endregion

        #region Utility Functions
        public enum InventoryType { MAGAZINE, CONSTRUCTION, REACTOR, ICEBOX, ROCKBOX }

        public List<IMyInventory> GetRemoteInventories(string tag)
        {
            List<IMyInventory> sources = new List<IMyInventory>();
            List<IMyTerminalBlock> sourceBlocks = new List<IMyTerminalBlock>();

            GridTerminalSystem.SearchBlocksOfName(tag, sourceBlocks);

            if (sourceBlocks.Count < 1) { return sources; }

            foreach (IMyTerminalBlock block in sourceBlocks)
            {
                _log.LogInfo("Adding inventory from " + block.CustomName);
                if (block.HasInventory)
                    sources.Add(block.GetInventory());
                else
                    _log.LogWarning(block.CustomName + " has no valid inventories.");
            }

            return sources;
        }

        public static string GetItemName(string longName)
        {
            int index = longName.IndexOf('/');
            return longName.Substring(index + 1);
        }
        #endregion
    }
}
