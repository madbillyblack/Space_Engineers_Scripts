using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Policy;
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
        public class MenuPage
        {
            public Dictionary<int, MenuButton> Buttons { get; set; }

            public MenuPage(string[] filters, IMyConveyorSorter sorter, Color iconColor)
            {
                Buttons = new Dictionary<int, MenuButton>();
                List<MyInventoryItemFilter> currentFilters = new List<MyInventoryItemFilter>();
                sorter.GetFilterList(currentFilters);

                if (filters.Length > 0)
                {
                    for (int i = 0; i < filters.Length; i++)
                    {
                        string filter = filters[i];
                        if (String.IsNullOrEmpty(filter))
                            filter = "";

                        // If Filter Is not contained in the lookup, use entire provided filter.
                        // For custom/missing item types
                        string[] lookup = SorterProfiles.LookupItem(filter);

                        Color color;

                        if (string.IsNullOrEmpty(lookup[2]))
                            color = iconColor;
                        else
                            color = ParseColor(lookup[2]);

                        MenuButton button = new MenuButton(lookup[0], sorter, i+1, lookup[1], color);
                        button.Initialize(currentFilters);

                        Buttons.Add(i + 1, button);
                    }
                }
            }
        }


        public class MenuButton
        {
            public int Id { get; set; }
            public string Filter { get; set; }
            public string Label { get; set; }
            public bool Active { get; set; }
            public enum ButtonType { ITEM, BW_LIST, DRAIN, EMPTY }
            public ButtonType Type { get; set; }
            public IMyConveyorSorter Sorter { get; set; }
            public Color Color { get; set; }


            public MenuButton(string filter, IMyConveyorSorter sorter, int buttonNumber, string label, Color color)
            {
                Id = buttonNumber;
                Sorter = sorter;
                Filter = filter;
                Label = label;
                Color = color;

                switch (Filter)
                {
                    case "":
                        Type = ButtonType.EMPTY;
                        break;
                    case "bw":
                        Type = ButtonType.BW_LIST;
                        break;
                    case "drain":
                        Type = ButtonType.DRAIN;
                        break;
                    default:
                        Type = ButtonType.ITEM;
                        break;
                }

                /*
                // If Filter Is not contained in the lookup, use entire provided filter.
                // For custom/missing item types
                string [] lookup = SorterProfiles.LookupItem(filter);
                Filter = lookup[0];
                Label = lookup[1];
                */
            }

            public void Initialize(List<MyInventoryItemFilter> filterList)
            {
                switch (Type)
                {
                    case ButtonType.BW_LIST:
                        Active = Sorter.Mode == MyConveyorSorterMode.Whitelist;
                        break;
                    case ButtonType.DRAIN:
                        Active = Sorter.DrainAll;
                        break;
                    case ButtonType.EMPTY:
                        Active = false;
                        break;
                    case ButtonType.ITEM:
                        Active = IsFilterActive(filterList);
                        break;
                }
                
            }

            bool IsFilterActive(List<MyInventoryItemFilter> filterList)
            {

                //string item = SorterProfiles.LookupItem(Filter);

                try
                {
                    MyDefinitionId defId = MyDefinitionId.Parse(Filter);
                    MyInventoryItemFilter itemFilter = new MyInventoryItemFilter(defId);

                    return filterList.Contains(itemFilter);
                }
                catch
                {
                    
                    _logger.LogError("Could not parse filter " + Filter);
                    return false;
                }
            }

            public void ToggleDrainAll()
            {
                if (Sorter.DrainAll)
                {
                    Sorter.DrainAll = false;
                    Active = false;
                }
                else
                {
                    Sorter.DrainAll = true;
                    Active = true;
                }
            }

            public void ToggleWhiteList()
            {
                List<MyInventoryItemFilter> filters = new List<MyInventoryItemFilter>();
                Sorter.GetFilterList(filters);

                if(Sorter.Mode == MyConveyorSorterMode.Whitelist)
                {
                    Sorter.SetFilter(MyConveyorSorterMode.Blacklist, filters);
                    Active = false;
                }
                else
                {
                    Sorter.SetFilter(MyConveyorSorterMode.Whitelist, filters);
                    Active = true;
                }
            }

            public void ToggleItem()
            {
                List<MyInventoryItemFilter> filters = new List<MyInventoryItemFilter>();
                Sorter.GetFilterList(filters);

                //string item = SorterProfiles.LookupItem(Filter);
                MyDefinitionId defId = MyDefinitionId.Parse(Filter);
                MyInventoryItemFilter itemFilter = new MyInventoryItemFilter(defId);

                if (filters.Contains(itemFilter))
                {
                    Sorter.RemoveItem(itemFilter);
                    Active = false;
                }
                else
                {
                    Sorter.AddItem(itemFilter);
                    Active = true;
                }
            }
        }
    }
}
