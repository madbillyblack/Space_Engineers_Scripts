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
                if(ItemTypes.Keys.Count < 1) { return true; }

                int unstocked = 0;

                foreach (string key in ItemTypes.Keys)
                {
                    int targetCount = ItemAmmounts[key];
                    MyItemType itemType = ItemTypes[key];
                    bool restocked = false;

                    if (targetCount > 0)
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

                // If all inventories fully stocked, return true
                return unstocked < 1;
            }

            private bool RestockItem(MyItemType itemType, int targetAmmount, IMyInventory source)
            {
                int currentCount = Inventory.GetItemAmount(itemType).ToIntSafe();
   
                int amountToTransfer = targetAmmount - currentCount;
                if (amountToTransfer < 1) { return true; } // Item already stocked

                MyInventoryItem? item = source.FindItem(itemType);
                if(item != null)
                {
                    return Inventory.TransferItemFrom(source, (MyInventoryItem) item, amountToTransfer);
                }

                return false;
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
    }



}
