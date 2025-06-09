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
        public class MenuPage
        {
            public Dictionary<int, MenuButton> Buttons { get; set; }

            public MenuPage(string[] filters, IMyConveyorSorter sorter)
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

                        MenuButton button = new MenuButton(filter, sorter);
                        button.Initialize(currentFilters);

                        Buttons.Add(i, button);
                    }
                }
            }
        }


        public class MenuButton
        {
            public string Filter { get; set; }
            public bool Active { get; set; }
            public enum ButtonType { ITEM, BW_LIST, DRAIN, EMPTY }
            public ButtonType Type { get; set; }

            public IMyConveyorSorter Sorter { get; set; }

            public MenuButton(string filter, IMyConveyorSorter sorter)
            {
                Sorter = sorter;

                switch (filter)
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


                // If Filter Is not contained in the lookup, use entire provided filter.
                // For custom/missing item types
                Filter = SorterProfiles.LookupItem(filter);
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

                string item = SorterProfiles.LookupItem(Filter);

                try
                {
                    MyDefinitionId defId = MyDefinitionId.Parse(item);
                    MyInventoryItemFilter itemFilter = new MyInventoryItemFilter(defId);

                    return filterList.Contains(itemFilter);
                }
                catch
                {
                    
                    _logger.LogError("Could not parse filter " + item);
                    return false;
                }
            }
        }
    }
}
