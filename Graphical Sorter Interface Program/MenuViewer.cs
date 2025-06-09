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
        const string PAGE_KEY = "CurrentPage";
        const string ID_KEY = "Menu ID";

        public class MenuViewer
        {
            public IMyTextSurface Surface {  get; set; }
            public MyIniHandler IniHandler { get; set; }
            public MenuPage MenuPage { get; set; }
            public int ScreenIndex { get; set; }
            public int Id { get; set; }
            public int ButtonCount {  get; set; }
            public int PageCount { get; set; }
            public int CurrentPage { get; set; }
            public GSorter GSorter { get; set; }
            public string Header { get; set; }

            public MenuViewer(MyIniHandler iniHandler, int id, int index)
            {
                IniHandler = iniHandler;
                Id = id;
                ScreenIndex = index;
                Header = SCREEN_HEADER + ScreenIndex;
                Surface = (IniHandler.Block as IMyTextSurfaceProvider).GetSurface(ScreenIndex);
                Surface.ContentType = ContentType.SCRIPT;
                InitSorter();
                InitPage();
            }

            void InitSorter()
            {
                if (_sorters.Count < 1)
                {
                    GSorter = null;
                }
                else
                {
                    string key = IniHandler.GetKey(Header, "Sorter", _sorters.Keys.FirstOrDefault());

                    if (!_sorters.ContainsKey(key))
                    {
                        key = _sorters.Keys.FirstOrDefault();
                        IniHandler.SetKey(Header, "Sorter", key);
                    }

                    GSorter = _sorters[key];
                    SetPageCount();
                }
            }

            void InitPage()
            {
                ButtonCount = ParseInt(IniHandler.GetKey(Header, BUTTON_KEY, "7"), 7);
                SetPageCount();
                // Index Pages from 1
                CurrentPage = ParseInt(IniHandler.GetKey(Header, PAGE_KEY, "1"), 1);

                if (CurrentPage > PageCount)
                    SetCurrentPage(1);

                SetMenuPage();
            }

            public void CycleSorter(bool cycleLast = false)
            {
                if (_sorters.Count < 2) return;

                List<string> keys = _sorters.Keys.ToList();
                int index = keys.IndexOf(GSorter.Tag);

                if (cycleLast)
                    index--;
                else
                    index++;

                if (index < 0)
                    index = keys.Count - 1;
                else if (index >= keys.Count)
                    index = 0;

                GSorter = _sorters[keys[index]];
                IniHandler.SetKey(Header, "Sorter", GSorter.Tag);
                SetPageCount();
            }

            public void CyclePages(bool cycleLast = false)
            {
                if (cycleLast)
                    CurrentPage--;
                else
                    CurrentPage++;

                if(CurrentPage < 1)
                    CurrentPage = PageCount;
                else if(CurrentPage > PageCount)
                    CurrentPage = 1;

                IniHandler.SetKey(Header, PAGE_KEY, CurrentPage.ToString());

                SetMenuPage();
            }

            void SetPageCount()
            {
                if(ButtonCount > 0)
                {
                    float pages = (GSorter.Filters.Count() + 2.0f) / ButtonCount;

                    PageCount = (int)Math.Ceiling(pages);
                }
                else { PageCount = -1; }
            }

            void SetCurrentPage(int page)
            {
                CurrentPage = page;
                IniHandler.SetKey(Header, PAGE_KEY, page.ToString());
            }


            void SetMenuPage()
            {
                MenuPage = new MenuPage(FiltersFromPageNumber(), GSorter.SorterBlock);
            }


            string[] FiltersFromPageNumber()
            {
                string[] filterArray = new string[ButtonCount];

                int startIndex =  (CurrentPage - 1) * ButtonCount;

                int endIndex;
                if(CurrentPage == PageCount)
                {
                    // If final page set last two buttons to "drain" and "bw"
                    endIndex = GSorter.Filters.Count() % ButtonCount;
                    filterArray[ButtonCount - 1] = "drain";
                    filterArray[ButtonCount - 2] = "bw";
                }
                else
                {
                    endIndex = ButtonCount;
                }

                for(int i = 0; i < endIndex; i++)
                {
                    filterArray[i] = GSorter.Filters[startIndex + i];
                }

                return filterArray;
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
                _menuViewers.Add(menuId, new MenuViewer(ini, menuId, 0));
            }
            else
            {
                if (ShowOnScreen(ini, 0, true))
                {
                    menuId = SelectMenuId(ini, 0);
                    _menuViewers.Add(menuId, new MenuViewer(ini, menuId, 0));
                }
                    
                for(int i = 1; i < surfaceCount; i++)
                {
                    if(ShowOnScreen(ini, i))
                    {
                        menuId = SelectMenuId(ini, i);
                        _menuViewers.Add(menuId, new MenuViewer(ini, menuId, i));
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


        public MenuViewer GetMenuViewer(string menuId)
        {
            if (_menuViewers.Count < 1)
            {
                _logger.LogError("NO MENU VIEWERS DETECTED\nPlease include " + VIEWER_TAG + "in the name of an LCD block that you wish to configure as a Menu Viewer.");
                return null;
            }

            int id;
            
            // If no viewer specified get default
            if(menuId == "")
                id = 0;
            else
                id = ParseInt(menuId, -1);

            if(id < 0)
            {
                _logger.LogError("Failed to parse Menu ID: '" + menuId + "'");
                return null;
            }

            if (!_menuViewers.ContainsKey(id))
            {
                _logger.LogError("Could not find Menu with ID: '" + menuId + "'");
                return null;
            }

            return _menuViewers[id];
        }
    }
}
