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
        public Dictionary<int, MenuViewer> _menuViewers;
        const string VIEWER_TAG = "[GSIP_MENU]";
        const string MENU_HEADER = "GSIP - Menu";
        const string SCREEN_HEADER = "GSIP Screen ";
        const string SCREEN_KEY = "ScreenIndex";
        const string BUTTON_KEY = "ButtonsPerPage";
        const string ID_KEY = "Menu ID";

        public class MenuViewer
        {
            public IMyTextSurface Surface {  get; set; }
            public MyIniHandler IniHandler { get; set; }
            public int ScreenIndex { get; set; }
            public int Id { get; set; }
            public int ButtonCount {  get; set; }
            public int CurrentPage { get; set; }
            public GSorter GSorter { get; set; }

            public MenuViewer(MyIniHandler iniHandler, int index)
            {
                IniHandler = iniHandler;
                ScreenIndex = index;
                Surface = (IniHandler.Block as IMyTextSurfaceProvider).GetSurface(ScreenIndex);
                Surface.ContentType = ContentType.SCRIPT;
                InitSorter();
            }

            void InitSorter()
            {
                if (_sorters.Count < 1)
                {
                    GSorter = null;
                }
                else
                {
                    string key = IniHandler.GetKey(SCREEN_HEADER + ScreenIndex, "Sorter", _sorters.Keys.FirstOrDefault());

                    if (!_sorters.ContainsKey(key))
                    {
                        key = _sorters.Keys.FirstOrDefault();
                        IniHandler.SetKey(SCREEN_HEADER + ScreenIndex, "Sorter", key);
                    }

                    GSorter = _sorters[key];
                }
            }

            void CycleSorter(bool cycleLast = false)
            {
                if (_sorters.Count < 2) return;

                List<string> keys = _sorters.Keys.ToList();
                int index = keys.IndexOf(GSorter.Tag);

                if (cycleLast)
                {
                    index--;
                    if (index < 0)
                        index = keys.Count - 1;
                }
                else
                {
                    index++;
                    if (index >= keys.Count)
                        index = 0;
                }

                GSorter = _sorters[keys[index]];
                IniHandler.SetKey(SCREEN_HEADER + ScreenIndex, "Sorter", GSorter.Tag);
            }
        }


        public void AddMenuViewers()
        {
            _menuViewers = new Dictionary<int,MenuViewer>();

            List<IMyTerminalBlock> menuBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(VIEWER_TAG, menuBlocks);

            if (menuBlocks.Count < 1) return;

            foreach (IMyTerminalBlock block in menuBlocks)
                AddMenusFromBlock(block);
        }

        public void AddMenusFromBlock(IMyTerminalBlock block)
        {
            int surfaceCount = (block as IMyTextSurfaceProvider).SurfaceCount;

            if (surfaceCount < 1 || !SameGridID(block)) return;

            MyIniHandler ini = new MyIniHandler(block);

            int menuId;

            if(surfaceCount == 1)
            {
                menuId = SelectMenuId(ini, 0);
                _menuViewers.Add(menuId, new MenuViewer(ini, 0));
            }
            else
            {
                if (ShowOnScreen(ini, 0, true))
                {
                    menuId = SelectMenuId(ini, 0);
                    _menuViewers.Add(menuId, new MenuViewer(ini, 0));
                }
                    

                for(int i = 1; i < surfaceCount; i++)
                {
                    if(ShowOnScreen(ini, i))
                    {
                        menuId = SelectMenuId(ini, i);
                        _menuViewers.Add(menuId, new MenuViewer(ini, i));
                    }  
                }
            }
        }

        public bool ShowOnScreen(MyIniHandler ini, int index, bool isMainScreen = false)
        {
            return ParseBool(ini.GetKey(MENU_HEADER, "Show on screen " + index, isMainScreen.ToString()));
        }


        public int SelectMenuId(MyIniHandler ini, int screenIndex)
        {
            int defaultVal = _menuViewers.Count;

            int menuId = ParseInt(ini.GetKey(SCREEN_HEADER + screenIndex, ID_KEY, defaultVal.ToString()), defaultVal);

            while (_menuViewers.ContainsKey(menuId))
            {
                menuId++;
            }

            return menuId;
        }
    }
}
