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
        public class SorterMenu
        {
            public MyIniHandler IniHandler { get; set; }
            public IMyTerminalBlock Block { get; set; }
            public string Tag {  get; set; }
            public IMyTextSurface Surface { get; set; }
            public int PageCount { get; set; }
            public int CurrentPage { get; set; }
            public int ButtonsPerPage { get; set; }

            public Dictionary<int, MenuPage> Pages { get; set; }

            public SorterMenu(IMyTerminalBlock block, string tag, GSorter sorter)
            {
                Block = block;
                Tag = tag;
                IniHandler = new MyIniHandler(Block);

                AddSurface();
                SetButtonCount();
                AddPages(sorter);
                
            }

            private void AddSurface()
            {
                int index;
                int surfaceCount = (Block as IMyTextSurfaceProvider).SurfaceCount;

                if (surfaceCount == 1)
                {
                    index = 0;
                }
                else
                {
                    index = ParseInt(IniHandler.GetKey(MENU_HEADER, SCREEN_KEY, "0"), 0);
                    if(index >= surfaceCount)
                    {
                        _statusMessage += "\nWARNING: Screen Index " + index + " is to large for block:\n  "
                                            + Block.CustomName +"\n";
                        index = 0;
                    }
                }

                Surface = (Block as IMyTextSurfaceProvider).GetSurface(index);
                Surface.ContentType = ContentType.SCRIPT;
            }

            private void SetButtonCount()
            {
                int defaultCount;
                string tagType = Tag.Split('_')[0].Trim().ToUpper();

                if (tagType == "ORE" || tagType == "IGT")
                    defaultCount = 8;
                else
                    defaultCount = 7;

                ButtonsPerPage = ParseInt(IniHandler.GetKey(MENU_HEADER, BUTTON_KEY, defaultCount.ToString()), defaultCount);
            }



            private void AddPages(GSorter sorter)
            {

            }


            private void AddButtonsToPage(MenuPage page, MenuButton[] buttons)
            {

            }
        }


        public class MenuPage
        {
            public Dictionary<int, MenuButton> Buttons { get; set; }

            public MenuPage(string[] filters)
            {
                Buttons = new Dictionary<int, MenuButton>();

                if(filters.Length > 0)
                {
                    for (int i = 0; i < filters.Length; i++)
                    {
                        if (string.IsNullOrEmpty(filters[i])) { continue; }

                        Buttons.Add(i, new MenuButton(filters[i]));
                    }
                }
            }
        }


        public class MenuButton
        {
            public string Filter { get; set; }
            public bool Active { get; set; }
            public enum ButtonType { ITEM, BW_LIST, DRAIN }

            public MenuButton(string filter)
            {
                // If Filter Is not contained in the lookup, use entire provided filter.
                // For custom/missing item types
                if(!SorterProfiles.Lookup.ContainsKey(filter))
                    Filter = filter;
                else Filter = SorterProfiles.Lookup[filter];
            }
        }

        /*
        public void AddMenus()
        {
            if(_sorters.Count < 1) return;

            foreach(string key in _sorters.Keys)
            {
                GSorter sorter = _sorters[key];

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.SearchBlocksOfName("SRT_" + key, blocks);       

                foreach (IMyTerminalBlock block in blocks)
                {
                    if((block as IMyTextSurfaceProvider).SurfaceCount > 0)
                        

                    
                }
            }
        }
        */
    }
}
