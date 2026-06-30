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
        class DestInventory
        {

            public DestType Type;
            public Dictionary<string, MyItemType> ItemTypes;
            public Dictionary<string, int> ItemAmmounts;
            public MyIniHandler IniHandler;
            public IMyInventory Inventory;
            public IMyTerminalBlock Block;

            public DestInventory(DestType type, IMyTerminalBlock block)
            {
                Type = type;
                Block = block;
                IniHandler = new MyIniHandler(Block);
                Inventory = Block.GetInventory();
                ItemTypes = new Dictionary<string, MyItemType>();
                ItemAmmounts = new Dictionary<string, int>();
            }

            public void SetLoadout()
            {
                List<MyItemType> itemTypes = new List<MyItemType>();
                Inventory.GetAcceptedItems(itemTypes);

                string [] keyList = IniHandler.GetKey(INI_HEAD, LOADOUT, "").Split('\n');

                if(keyList.Length < 1) { return; }

                foreach (string keyItem in keyList)
                {
                    AddTypeAndCount(keyItem, itemTypes);
                }
            }

            public bool Refill(List<IMyInventory> sources)
            {
                if(ItemTypes.Keys.Count < 1) {
                    _surface.WriteText("WARNING: No item keys loaded for " + Block.CustomName, true);
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

                            _surface.WriteText("  Untransferred: " + missingCount + "\n", true);
                            /*
                            // Check if transfer was incomplete due to lack of space
                            if (missingCount > 0 & !Inventory.CanItemsBeAdded(missingCount, itemType))
                            {
                                _surface.WriteText("-- WARNING: The requested amount for item " + key + "\n in " + Block.CustomName + " exceeds capacity!\n");
                                missingCount = 0;
                            }
                            */
                        }
                    }

                    if (missingCount > 0) { unstocked++; }
                }
/*
                        if (missingCount > 0)
                    {
                        foreach (IMyInventory source in sources)
                        {
                            if(!restocked)
                            {
                                restocked = RestockItem(itemType, targetCount, source);

                                int missingCount = targetCount - Inventory.GetItemAmount(itemType).ToIntSafe();

                                if (missingCount > 0)
                                {
                                    if(Inventory.CanItemsBeAdded(missingCount, itemType))
                                    {
                                        // Had some but not enough of the needed item.
                                        restocked = false;
                                    }
                                    else
                                    {
                                        _surface.WriteText("-- WARNING: The requested amount for item " + key + "\n in " + Block.CustomName + " exceeds capacity!\n");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        restocked = true;
                    }

                    if (!restocked)
                        unstocked++;
                }
            */
                // If all inventories fully stocked, return true
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
                        if(currentCount > 0)
                        {
                            currentCount = UnloadItem(item, destination, currentCount);
                        }
                    }

                    // If untransferred items set return to false, and continue transfers.
                    if(currentCount > 0)
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
                    _surface.WriteText(transferred + "x " + GetItemName(item.Type.ToString()) + "transferred from " + Block.CustomName +"\n", true);

                    return currentAmount;
                }
                else
                {
                    return targetCount;
                }
            }


            private int RestockItem(MyItemType itemType, IMyInventory source, int targetCount)
            {
                _surface.WriteText("Requested: " + targetCount + "x " + GetItemName(itemType.ToString()) + " in " + Block.CustomName + "\n", true);
                MyInventoryItem? item = source.FindItem(itemType);
                if (item != null && Inventory.TransferItemFrom(source, (MyInventoryItem) item, targetCount))
                {
                    int currentAmount = Inventory.GetItemAmount(itemType).ToIntSafe();                

                    return currentAmount;
                }
                else
                {
                    _surface.WriteText("Unable to transfer requested item to " + Block.CustomName + "\n", true);
                    return Inventory.GetItemAmount(itemType).ToIntSafe();
                }


                /*
                    int currentCount = Inventory.GetItemAmount(itemType).ToIntSafe();
   
                int amountToTransfer = targetCount - currentCount;
                if (amountToTransfer < 1) { return true; } // Item already stocked

                MyInventoryItem? item = source.FindItem(itemType);
                if(item != null)
                {
                    return Inventory.TransferItemFrom(source, (MyInventoryItem) item, amountToTransfer);
                }

                return false;
                */
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

        public enum DestType { MAGAZINE, CONSTRUCTION, REACTOR, ICEBOX, ROCKBOX }

        public static string GetItemName(string longName)
        {
            int index = longName.IndexOf('/');
            return longName.Substring(index + 1);
        }
    }
}
