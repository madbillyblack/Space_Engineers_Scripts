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
        const string MENU_HEADER = "GSIP - Menu";
        const string SCREEN_KEY = "ScreenIndex";

        public class SorterMenu
        {
            public MyIniHandler IniHandler {  get; set; }
            public IMyTerminalBlock Block {  get; set; }
            public IMyTextSurface Surface { get; set; }
            public int PageCount { get; set; }
            public int CurrentPage { get; set; }

            public Dictionary<int, MenuPage> Pages { get; set; }

            public SorterMenu(IMyTerminalBlock block)
            {
                Block = block;
                IniHandler = new MyIniHandler(Block);

                AddSurface();
            }

            private void AddSurface()
            {
                int index;

                if((Block as IMyTextSurfaceProvider).SurfaceCount == 1)
                {
                    index = 0;
                }
                else
                {
                    index = ParseInt(IniHandler.GetKey(MENU_HEADER, SCREEN_KEY, "0"), 0);
                }

                Surface = (Block as IMyTextSurfaceProvider).GetSurface(index);
                Surface.ContentType = ContentType.SCRIPT;
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

            public MenuButton(string filter)
            {
                // If Filter Is not contained in the lookup, use entire provided filter.
                // For custom/missing item types
                if(!SorterProfiles.Lookup.ContainsKey(filter))
                    Filter = filter;
                else Filter = SorterProfiles.Lookup[filter];
            }
        }
    }
}
