﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters;
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
using VRageRender;

namespace IngameScript
{
    partial class Program
    {
        public static Dictionary<int, MenuViewer> _menuViewers;
        const string VIEWER_TAG = "[GSIP]";
        const string MENU_HEADER = "GSIP - Menu";
        const string SCREEN_HEADER = "GSIP Screen ";
        const string SCREEN_KEY = "ScreenIndex";
        const string BUTTON_KEY = "ButtonsPerPage";
        const string PAGE_KEY = "CurrentPage";
        const string ID_KEY = "Menu ID";

        const string CENTER = "CENTER";
        const string BOTTOM = "BOTTOM";
        const string TOP = "TOP";


        public class MenuViewer
        {
            public IMyTextSurface Surface {  get; set; }
            public RectangleF Viewport { get; set; }
            public bool BigScreen { get; set; }
            public bool WideScreen { get; set; }
            public string Alignment { get; set; }
            public MyIniHandler IniHandler { get; set; }
            public MenuPage MenuPage { get; set; }
            public int ScreenIndex { get; set; }
            public int Id { get; set; }
            public int ButtonCount {  get; set; }
            public int PageCount { get; set; }
            public int CurrentPage { get; set; }
            public GSorter GSorter { get; set; }
            public string Header { get; set; }
            public bool ColorMode { get; set; }

            public MenuViewer(MyIniHandler iniHandler, int id, int index)
            {
                IniHandler = iniHandler;
                Id = id;
                ScreenIndex = index;
                Header = SCREEN_HEADER + ScreenIndex;
                ColorMode = ParseBool(IniHandler.GetKey(Header, "ColorMode", "true"));

                Surface = (IniHandler.Block as IMyTextSurfaceProvider).GetSurface(ScreenIndex);
                Viewport = new RectangleF((Surface.TextureSize - Surface.SurfaceSize) / 2f, Surface.SurfaceSize);
                BigScreen = Viewport.Width > 500;

                PrepareTextSurfaceForSprites(Surface);

                InitSorter();
                InitPage();
                SetAlignment();
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
                WideScreen = Viewport.Width >= Viewport.Height * 3 || ButtonCount < 6;
                SetPageCount();
                // Index Pages from 1
                CurrentPage = ParseInt(IniHandler.GetKey(Header, PAGE_KEY, "1"), 1);

                if (CurrentPage > PageCount)
                    SetCurrentPage(1);

                SetMenuPage();
            }

            void SetAlignment()
            {
                Alignment = IniHandler.GetKey(Header, "Alignment", CENTER).ToUpper();
            }

            public void PressButton(int buttonNumber)
            {
                if(buttonNumber > ButtonCount)
                {
                    _logger.LogError("Invalid Button " + buttonNumber + " for Menu " + Id);
                    return;
                }

                MenuButton button = MenuPage.Buttons[buttonNumber];

                switch(button.Type)
                {
                    case MenuButton.ButtonType.EMPTY:
                        break;                        
                    case MenuButton.ButtonType.DRAIN:
                        button.ToggleDrainAll();
                        break;
                    case MenuButton.ButtonType.BW_LIST:
                        button.ToggleWhiteList();
                        break;
                    case MenuButton.ButtonType.ITEM:
                        button.ToggleItem();
                        break;

                }

                DrawSurface();
            }

            public void ToggleColor()
            {
                if(ColorMode)
                    ColorMode = false;
                else
                    ColorMode = true;

                IniHandler.SetKey(Header, "ColorMode", ColorMode.ToString());
                SetMenuPage();
                DrawSurface();
            }

            /*
            public void CycleSorter(bool cycleLast = false)
            {
                if (_sorters.Count < 2) return;

                //SetCurrentPage(1);

                List<string> keys = _sorters.Keys.ToList();
                keys = keys.OrderBy(x => x).ToList();

                int index = keys.IndexOf(GSorter.Tag);

                if (cycleLast)
                    index--;
                else
                    index++;

                if (index < 0)
                    index = keys.Count - 1;
                else if (index >= keys.Count)
                    index = 0;

                SetSorter(_sorters[keys[index]]);                
            }*/

            public void CycleSorter(string type, bool cycleLast = false)
            {
                if (_sorters.Count < 2) return;

                List<string> keys;

                if(type == "")
                {
                    keys = _sorters.Keys.ToList();
                }
                else
                {
                    keys = new List<string>();
                    foreach (string key in _sorters.Keys)
                    {
                        if (key.Contains(type))
                            keys.Add(key);
                    }
                }

                if(keys.Count < 1)
                {
                    _logger.LogWarning("No Sorters of type " + type + " found.");
                    return;
                }

                if(cycleLast)
                    keys = keys.OrderByDescending(x => x).ToList();
                else
                    keys =keys.OrderBy(x => x).ToList();

                int index = keys.IndexOf(GSorter.Tag);

                if(index < 0) // If current sorter of different type, set to first
                    index = 0;
                else
                    index++;

                if (index >= keys.Count)
                    index = 0;

                SetSorter(_sorters[keys[index]]);
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
                DrawSurface();
            }

            public void UpdateActive()
            {
                SetMenuPage();
                DrawSurface();
            }

            #region Private Utilities
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
                MenuPage = new MenuPage(FiltersFromPageNumber(), GSorter.SorterBlock, GSorter.LabelColor * 0.9f, ColorMode);
            }

            void SetSorter(GSorter sorter)
            {
                GSorter = sorter;
                IniHandler.SetKey(Header, "Sorter", GSorter.Tag);
                SetCurrentPage(1);
                SetPageCount();
                SetMenuPage();
                DrawSurface();
            }

            string[] FiltersFromPageNumber()
            {
                string[] filterArray = new string[ButtonCount];

                int startIndex =  (CurrentPage - 1) * ButtonCount;
                int endIndex;
                int remainder = GSorter.Filters.Count() % ButtonCount;
                bool oneShort = remainder == ButtonCount - 1;

                if (CurrentPage == PageCount)
                {
                    // If final page set last two buttons to "drain" and "bw"
                    endIndex = remainder;
                    filterArray[ButtonCount - 1] = "drain";
                    filterArray[ButtonCount - 2] = "bw";

                    if(oneShort) return filterArray;
                }
                else
                {
                    if (oneShort && CurrentPage == PageCount - 1)
                        endIndex = remainder;
                    else
                        endIndex = ButtonCount;
                }

                _dataScreen.WriteText("Index: " + startIndex + "\n");
                for (int i = 0; i < endIndex; i++)
                {
                    _dataScreen.WriteText("[" + i + "]", true);
                    filterArray[i] = GSorter.Filters[startIndex + i];
                    _dataScreen.WriteText(filterArray[i] + "\n", true);
                }

                return filterArray;
            }
            #endregion
            #region Draw Functions
            // DRAW SURFACE //
            public void DrawSurface()
            {
                _frame = Surface.DrawFrame();

                Vector2 center = Viewport.Center;
                float height = Viewport.Height;
                float width = Viewport.Width;


                int rowCount;
                float cellWidth, buttonHeight, titleSize;

                float fontSize = 0.5f;

                if (BigScreen)
                    fontSize *= 1.5f;

                titleSize = fontSize;

                if (height == width)
                    fontSize *= 2;

                if (WideScreen)
                {
                    rowCount = ButtonCount;
                    //cellWidth = width * 0.142857f;
                    buttonHeight = height * 0.5f;
                }
                else
                {
                    rowCount = (int)Math.Ceiling(ButtonCount * 0.5);
                    //cellWidth = (width * 0.25f);
                    buttonHeight = (height * 0.225f);
                }

                cellWidth = width / rowCount;

                if (buttonHeight > cellWidth)
                    buttonHeight = cellWidth - 4;

                // Background
                Vector2 position = center - new Vector2(width * 0.5f, 0);
                DrawTexture(SQUARE, position, new Vector2(width, height), 0, ColorMode ? GSorter.BackgroundColor : _defBgColor);

                // Grid
                float gridHeight = width > height ? width : height;
                DrawTexture("Grid", position, new Vector2(gridHeight, gridHeight), 0, GSorter.LabelColor);

                // Set Starting Top Edge
                Vector2 topLeft;
                switch (Alignment)
                {
                    case TOP:
                        topLeft = center - new Vector2(width * 0.5f, height * 0.5f);
                        break;
                    case BOTTOM:
                        if (WideScreen)
                            topLeft = center - new Vector2(width * 0.5f, height * -0.5f + buttonHeight * 2);
                        else
                            topLeft = center - new Vector2(width * 0.5f, height * -0.5f + buttonHeight * 4);
                        break;
                    case CENTER:
                    default:
                        if (WideScreen)
                            topLeft = center - new Vector2(width * 0.5f, buttonHeight);
                        else
                            topLeft = center - new Vector2(width * 0.5f, buttonHeight * 2);
                        break;
                }

                DrawTitles(topLeft, width, titleSize);

                if (WideScreen)
                    DrawSingleRow(topLeft, cellWidth, buttonHeight, fontSize);
                else
                    DrawDoubleRow(topLeft, cellWidth, buttonHeight, rowCount, fontSize);

                _frame.Dispose();
            }

            
            void DrawTitles(Vector2 topLeft, float width, float fontSize)
            {
                Color buttonColor = ColorMode ? GSorter.ButtonColor : _defButtonColor;

                // Title Bar
                Vector2 position = topLeft + new Vector2(0, fontSize * 15);
                DrawTexture(SQUARE, position, new Vector2(width, fontSize * 40), 0, buttonColor * 1.1f);

                // Menu Title
                position = topLeft + new Vector2(10, 0);
                string title = "Menu " + Id;
                DrawText(title, position, fontSize, TextAlignment.LEFT, GSorter.LabelColor);

                // Current Sorter
                position = topLeft + new Vector2(width * 0.5f, 0);
                string sortTitle = "Sorter: " + GSorter.Tag;
                DrawText(sortTitle, position, fontSize, TextAlignment.CENTER, GSorter.TitleColor);

                // Pages
                position = topLeft + new Vector2(width - 10, 0);
                string pageTitle = "Page " + CurrentPage + " of " + PageCount;
                DrawText(pageTitle, position, fontSize, TextAlignment.RIGHT, GSorter.LabelColor);
            }


            void DrawSingleRow(Vector2 position, float cellWidth, float buttonHeight, float fontSize)
            {
                // Button Backgrounds
                Vector2 pos = position + new Vector2((cellWidth - buttonHeight) * 0.5f, buttonHeight * 1.175f);
                //Vector2 buttonScale = new Vector2(buttonHeight, buttonHeight);

                List<int> keys = MenuPage.Buttons.Keys.ToList();

                foreach (int i in keys)
                {
                    MenuButton button = MenuPage.Buttons[i];

                    DrawButton(button, pos, buttonHeight, fontSize);

                    pos += new Vector2(cellWidth, 0);
                }
            }


            void DrawDoubleRow(Vector2 position, float cellWidth, float buttonHeight, int rowCount, float fontSize)
            {
                Vector2 pos = position + new Vector2((cellWidth - buttonHeight) * 0.5f, buttonHeight * 1.33f);
                //Vector2 buttonScale = new Vector2(buttonHeight, buttonHeight);

                List<int> keys = MenuPage.Buttons.Keys.ToList();

                //			try{
                for (int i = 0; i < rowCount; i++)
                {
                    MenuButton button = MenuPage.Buttons[keys[i]];

                    DrawButton(button, pos, buttonHeight, fontSize);

                    pos += new Vector2(cellWidth, 0);
                }

                float heightMod;

                if (Viewport.Width == Viewport.Height)
                    heightMod = 3.1f;
                else
                    heightMod = 3.3f;

                pos = position + new Vector2(0, buttonHeight * heightMod);

                // check if the button count is even, offset bottom row if so.
                if (MenuPage.Buttons.Count % 2 > 0)
                    pos += new Vector2(cellWidth - buttonHeight * 0.5f, 0);
                else
                    pos += new Vector2((cellWidth - buttonHeight) * 0.5f, 0); // Parentheses matter!


                //			try {
                for (int j = rowCount; j < MenuPage.Buttons.Count; j++)
                {
                    MenuButton button = MenuPage.Buttons[keys[j]];

                    DrawButton(button, pos, buttonHeight, fontSize);

                    pos += new Vector2(cellWidth, 0);
                }
            }

            void DrawButton(MenuButton button, Vector2 startPosition, float scale, float fontSize)
            {
                if (button.Type == MenuButton.ButtonType.EMPTY) return;

                Vector2 position = startPosition;
                Color highlightColor;
                Color buttonColor = ColorMode ? GSorter.ButtonColor : _defButtonColor;
                bool whiteList = GSorter.SorterBlock.Mode == MyConveyorSorterMode.Whitelist;

                if (button.Active)
                {
                    if (whiteList)
                        highlightColor = Color.LightGray;
                    else
                        highlightColor = Color.Black;
                }
                else
                {
                    if (button.Type == MenuButton.ButtonType.BW_LIST)
                        highlightColor = Color.Black;
                    else
                        highlightColor = buttonColor * 1.1f;
                }

                DrawTexture(SQUARE, position, new Vector2(scale, scale), 0, highlightColor);

                switch (button.Type)
                {
                    case MenuButton.ButtonType.ITEM:
                        DrawItem(button, position, scale, fontSize, whiteList, buttonColor);
                        break;
                    case MenuButton.ButtonType.BW_LIST:
                        DrawBwToggle(position, scale, fontSize, whiteList);
                        break;
                    case MenuButton.ButtonType.DRAIN:
                        DrawDrain(position, scale, fontSize, button.Active, buttonColor);
                        break;
                }

                // Number Label
                position = startPosition + new Vector2(scale * 0.5f, scale * 0.5f - fontSize * 5);//0.45f);
                DrawText(button.Id.ToString(), position, fontSize * 1.125f, TextAlignment.CENTER, GSorter.LabelColor);
            }

            void DrawItem(MenuButton button, Vector2 startPosition, float scale, float fontSize, bool whiteList, Color buttonColor)
            {
                Vector2 position = startPosition + new Vector2(scale * 0.05f, 0);

                DrawTexture(SQUARE, position, new Vector2(scale * 0.9f, scale * 0.9f), 0, buttonColor);

                Color itemColor, labelColor;

                // Set Colors based if the item should pass through the filter
                if (button.Active && whiteList || !button.Active && !whiteList)
                {
                    itemColor = button.Color;
                    labelColor = GSorter.TitleColor;
                }  
                else
                {
                    itemColor = buttonColor * 0.85f;
                    labelColor = GSorter.LabelColor;
                }

                Vector2 vertOffset;
                if (String.IsNullOrEmpty(button.Label))
                    vertOffset = Vector2.Zero;
                else
                    vertOffset = new Vector2(0, scale * 0.035f);

                DrawTexture(button.Filter, position + vertOffset, new Vector2(scale * 0.9f, scale * 0.9f), 0, itemColor);//ButtonColor * 1.75f);

                // Scale Down Longer Text labels, and move further right
                float offsetMod,fontMod;
                if(button.Label.Length > 6)
                {
                    offsetMod = 0.95f;
                    fontMod = 0.6f;
                }
                else
                {
                    offsetMod = 0.9f;
                    fontMod = 0.67f;
                }

                position = startPosition + new Vector2(scale * offsetMod, scale * -0.45f);
                DrawText(button.Label, position, fontSize * fontMod, TextAlignment.RIGHT, labelColor);
            }

            void DrawDrain(Vector2 startPosition, float scale, float fontSize, bool active, Color buttonColor)
            {
                Vector2 position = startPosition + new Vector2(scale * 0.05f, 0);

                DrawTexture(SQUARE, position, new Vector2(scale * 0.9f, scale * 0.9f), 0, buttonColor);
                DrawTexture("AH_PullUp", position, new Vector2(scale * 0.9f, scale * -0.9f), 0, buttonColor);

                Color labelColor;
                if (active)
                    labelColor = GSorter.TitleColor;
                else
                    labelColor = GSorter.LabelColor;

                position = startPosition + new Vector2(scale * 0.9f, scale * -0.4f);
                DrawText("Drain All", position, fontSize * 0.6f, TextAlignment.RIGHT, labelColor);
            }

            void DrawBwToggle(Vector2 startPosition, float scale, float fontSize, bool whiteList)
            {
                string topText;
                Color textColor, buttonColor;

                if (whiteList)
                {
                    topText = "WHITE";
                    buttonColor = Color.LightGray;
                    textColor = Color.Black;
                }
                else
                {
                    topText = "BLACK";
                    buttonColor = Color.Black;
                    textColor = Color.LightGray;
                }

                Vector2 position = startPosition + new Vector2(scale * 0.1f, 0);
                DrawTexture(SQUARE, position, new Vector2(scale * 0.8f, scale * 0.8f), 0, textColor);

                position += new Vector2(scale * 0.05f, 0);
                DrawTexture(SQUARE, position, new Vector2(scale * 0.7f, scale * 0.7f), 0, buttonColor);

                // Top Text
                position = startPosition + new Vector2(scale * 0.5f, scale * -0.25f);
                DrawText(topText, position, fontSize* 0.5f, TextAlignment.CENTER, textColor, "Monospace");

                // Bottom Text
                position += new Vector2(0, scale * 0.3f);
                DrawText("LIST", position, fontSize * 0.5f, TextAlignment.CENTER, textColor, "Monospace");
            }
            #endregion
        }





        public void AddMenuViewers()
        {
            _menuViewers = new Dictionary<int, MenuViewer>();

            if (_sorters.Count < 1)
            {
                _logger.LogWarning("No Sorters Added - Please rename sorters to include [SRT] tag or variant.");
                _logger.LogInfo("If Sorters already tagged, check that Grid ID in Custom Data of Sorter Block matches Grid ID in Custom Data of Progam Block");
                return;
            }

            List<IMyTerminalBlock> menuBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(VIEWER_TAG, menuBlocks);

            if (menuBlocks.Count < 1)
            {
                _logger.LogWarning("No Menus Added - Please rename sorters to include [GSIP] tag.");
                _logger.LogInfo("If Menus already tagged, check that Grid ID in Custom Data of Menu Block matches Grid ID in Custom Data of Progam Block");
                return;
            }

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


        public void DrawAllMenus()
        {
            if (_menuViewers.Count < 1) return;

            foreach (int key in _menuViewers.Keys) {
                _menuViewers[key].DrawSurface();
            }
        }
    }
}
