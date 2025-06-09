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
        /*
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
                        _logger.LogWarning("Screen Index " + index + " is too large for block:\n" + Block.CustomName);
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
        }
        */
    }
}
