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
        const string MENU_HEAD = "Map Menu";
        const string MENU_TAG = "[Map Menu]";
        const string MENU_ID = "Menu ID";
        const string MENU_COLOR = "Menu Color Settings";
        const string MAP_KEY = "Current Map";
        const string PAGE_KEY = "Current Page";
        const string ALIGNMENT_KEY = "Alignment";
        const string DECAL_KEY = "Decals";
        const string BG_KEY = "Background Color";
        const string TITLE_KEY = "Title Color";
        const string BUTTON_KEY = "Button Color";
        const string LABEL_KEY = "Label Color";
        const string DATA_KEY = "Current Data Display";
        
        List<MapMenu> _mapMenus;

        // Menu Page variables
        const int MENU_PAGES = 6;
        public static int _menuPageLimit;

        
        // Button Illumination variables
        public static int _buttonCountDown;
        const int BUTTON_TIME = 3;


        // MAP MENU //
        public class MapMenu
        {
            public IMyTerminalBlock Block;
            public DataDisplay DataDisplay;
            public IMyTextSurface Surface;
            public int CurrentMapIndex;
            public int CurrentPage;
            public int IDNumber;
            public int ActiveButton;
            public RectangleF Viewport;
            public Color BackgroundColor;
            public Color TitleColor;
            public Color LabelColor;
            public Color ButtonColor;
            
            public string Alignment;
            public string Decals;
            
            // Constructor //
            public MapMenu(IMyTerminalBlock block)
            {
                EnsureKey(block, MENU_HEAD, MENU_ID, "");

                Block = block;
                ActiveButton = 0;
                
                int surfaceCount = GetSurfaceCount(block);
                int index;

                // If multi screen add INI Parameter Screen Index, otherwise assume index = 0.
                if (surfaceCount > 1)
                    index = ParseInt(GetKey(block, MENU_HEAD, "Screen Index", "0"), 0);
                else
                    index = 0;
                
                Alignment = GetMenuKey(ALIGNMENT_KEY, "TOP").ToUpper();
                Decals = GetMenuKey(DECAL_KEY, "").ToUpper();

                // Set currently available menu parameters
                CurrentPage = ParseInt(GetKey(block, MENU_HEAD, PAGE_KEY, "1"), 1);
                BackgroundColor = ParseColor(GetKey(block, MENU_COLOR, BG_KEY, "0,0,0"));
                TitleColor = ParseColor(GetKey(block, MENU_COLOR, TITLE_KEY, "160,160,0"));
                LabelColor = ParseColor(GetKey(block, MENU_COLOR, LABEL_KEY, "160,160,160"));
                ButtonColor = ParseColor(GetKey(block, MENU_COLOR, BUTTON_KEY, "0,160,160"));

                int mapIndex;

                if (_mapList.Count > 1)
                {
                    mapIndex = ParseInt(GetKey(block, MENU_HEAD, MAP_KEY, "0"), 0);
                    if (mapIndex >= _mapList.Count)
                        mapIndex = 0;
                }
                else
                    mapIndex = 0;

                CurrentMapIndex = mapIndex;

                if (surfaceCount > 0 && index < surfaceCount)
                {
                    Surface = (block as IMyTextSurfaceProvider).GetSurface(index);
                }
                else
                {
                    AddMessage("Menu Surface could not be retrieved from block " + block.CustomName);
                    Surface = null;
                }

                SetDataDisplay();
            }

            // Set ID //
            public void SetID(int idNumber)
            {
                IDNumber = idNumber;
                SetKey(Block, MENU_HEAD, MENU_ID, idNumber.ToString());
            }

            // Set Data Display //
            void SetDataDisplay()
            {
                if (_dataDisplays.Count < 1)
                    return;
                
                string dataKey = GetMenuKey(DATA_KEY, "0");
                int dataIndex = ParseInt(dataKey, 0);

                if (dataKey != "" && dataIndex < _dataDisplays.Count)
                    DataDisplay = GetDataDisplay(dataIndex);
                else
                    DataDisplay = null;
            }

            // Check for Display //
            bool NewDisplayAssignment()
            {
                if(DataDisplay == null && _dataDisplays.Count > 0)
                    return true;

                return false;
            }

            // Next Data Displa //
            public void NextDataDisplay()
            {
                bool newDisplay = NewDisplayAssignment();

                if (_dataDisplays.Count < 2)
                    return;

                int index;

                if (newDisplay)
                    index = 0;
                else
                {
                    index = DataDisplay.IDNumber + 1;
                    if (index >= _dataDisplays.Count)
                        index = 0;
                }
                    
                DataDisplay = GetDataDisplay(index);
                SetKey(Block, MENU_HEAD, DATA_KEY, index.ToString());
            }


            // Previous Data Display //
            public void PreviousDataDisplay()
            {
                bool newDisplay = NewDisplayAssignment();

                if (_dataDisplays.Count < 2)
                    return;

                int index;

                if(newDisplay)
                {
                    index = 0;
                }
                else
                {
                    index = DataDisplay.IDNumber - 1;
                    if (index < 0)
                        index = _dataDisplays.Count - 1;
                }
                    
                DataDisplay = GetDataDisplay(index);
                SetKey(Block, MENU_HEAD, DATA_KEY, index.ToString());
            }


            // Initialize Surface //
            public void InitializeSurface()
            {
                    PrepareTextSurfaceForSprites(Surface);
                    Viewport = new RectangleF((Surface.TextureSize - Surface.SurfaceSize) / 2f, Surface.SurfaceSize);
            }

            // Press Button //
            public void PressButton(int button)
            {
                ActiveButton = button;
                _buttonCountDown = BUTTON_TIME;
            }

            // Next Map //
            public void NextMap()
            {
                if (_mapList.Count < 2)
                    return;

                CurrentMapIndex++;

                if (CurrentMapIndex >= _mapList.Count)
                    CurrentMapIndex = 0;

                SetKey(Block, MENU_HEAD, MAP_KEY, CurrentMapIndex.ToString());
            }

            // Previous Map //
            public void PreviousMap()
            {
                if (_mapList.Count < 2)
                    return;

                CurrentMapIndex--;

                if (CurrentMapIndex < 0)
                    CurrentMapIndex = _mapList.Count - 1;

                SetKey(Block, MENU_HEAD, MAP_KEY, CurrentMapIndex.ToString());
            }

            // Get Menu Key //
            string GetMenuKey(string key, string defaultValue)
            {
                return GetKey(Block, MENU_HEAD, key, defaultValue);
            }

            // Next Data Page //
            public void NextDataPage()
            {
                if (DataDisplay == null)
                    return;

                DataDisplay.NextPage();
            }

            // Previous Data Page //
            public void PreviousDataPage()
            {
                if (DataDisplay == null)
                    return;

                DataDisplay.PreviousPage();
            }

            // Scroll Down //
            public void ScrollDown()
            {
                if (DataDisplay == null)
                    return;

                DataDisplay.ScrollDown();
            }

            // Scroll Up //
            public void ScrollUp()
            {
                if (DataDisplay == null)
                    return;

                DataDisplay.ScrollUp();
            }
        }


        // ASSIGN MENUS // - Get ship controllers tagged with MENU_TAG and add them to the map menu list.
        void AssignMenus()
        {
            _buttonCountDown = 0;
            _menuPageLimit = MENU_PAGES;

            if (_dataDisplays.Count < 1)
                _menuPageLimit--;

            List<IMyTerminalBlock> menuBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(MENU_TAG, menuBlocks);

            if(menuBlocks.Count > 0)
            {
                foreach(IMyTerminalBlock menuBlock in menuBlocks)
                {
                    // Check that controller has MENU_TAG and same Grid ID
                    if(GetKey(menuBlock, SHARED, "Grid_ID", _gridID) == _gridID)
                    {
                        MapMenu menu = new MapMenu(menuBlock);
                        if (menu != null && menu.Surface != null)
                        {
                            SetMenuID(menu);

                            //menu.IDNumber = _mapMenus.Count;
                            menu.InitializeSurface();
                            _mapMenus.Add(menu);
                        }
                        else
                        {
                            AddMessage("MENU SURFACE ERROR! - Could not add Menu for controller \n\"" + menuBlock.CustomName + "\"\n* Please check LCD Index in Custom Data for Controller.");
                        }
                    }
                }
            }

            DrawMenus();
        }

        /*
        // MENU FROM CONTROLLER // - Get Menu from provided block and populate menu parameters.
        MapMenu MenuFromController(IMyTerminalBlock block)
        {
            // Create Empty Menu
            MapMenu menu = new MapMenu(block);

            // Get name of block and index of screen where menu interface should be displayed
            //string blockName = GetKey(block, MENU_HEAD, "LCD Block", block.CustomName);
            int index = ParseInt(GetKey(block, MENU_HEAD, "LCD Index", "0"), 0);

            // Set currently available menu parameters
            menu.CurrentPage = ParseInt(GetKey(block, MENU_HEAD, PAGE_KEY, "1"), 1);
            menu.BackgroundColor = ParseColor(GetKey(block, MENU_HEAD, BG_KEY, "0,0,0"));
            menu.TitleColor = ParseColor(GetKey(block, MENU_HEAD, TITLE_KEY, "160,160,0"));
            menu.LabelColor = ParseColor(GetKey(block, MENU_HEAD, LABEL_KEY, "160,160,160"));
            menu.ButtonColor = ParseColor(GetKey(block, MENU_HEAD, BUTTON_KEY, "0,160,160"));

            int mapIndex;

            if (_mapList.Count > 1)
            {
                mapIndex = ParseInt(GetKey(block, MENU_HEAD, MAP_KEY, "0"), 0);
                if (mapIndex >= _mapList.Count)
                    mapIndex = 0;
            }
            else
                mapIndex = 0;

            menu.CurrentMapIndex = mapIndex;

     
            //IMyTerminalBlock lcdBlock = GridTerminalSystem.GetBlockWithName(blockName);
            //if (lcdBlock == null)
            //{
            //    lcdBlock = block;
            //    blockName = block.CustomName;
            //}

            int surfaceCount = GetSurfaceCount(block);

            if (surfaceCount > 0 && index < surfaceCount)
            {
                
                menu.Surface = (block as IMyTextSurfaceProvider).GetSurface(index);
                return menu;
            }
            else
            {
                AddMessage("Menu Surface could not be retrieved from block " + block.CustomName);
                return null;
            }
        }*/


        // GET MENU //
        MapMenu GetMenu(string arg)
        {
            if (_mapMenus.Count < 1)
                return null;

            int menuID;

            try
            {
                if (arg == "")
                    menuID = 0;
                else
                    menuID = ParseInt(arg.Split(' ')[0], 0);
            }
            catch
            {
                return null;
            }

            if (menuID == 0)
                return _mapMenus[0];

            foreach(MapMenu menu in _mapMenus)
            {
                if (menu.IDNumber == menuID)
                    return menu;
            }

            return null;
        }


        // NEXT MENU //
        void NextMenu(string arg, bool next)
        {
            MapMenu menu = GetMenu(arg);

            if(menu == null)
            {
                AddMessage("No Menu " + arg + " found!");
                return;
            }

            // Deactivate any lit buttons
            menu.ActiveButton = 0;

            if (next)
                menu.CurrentPage++;
            else
                menu.CurrentPage--;

            if (menu.CurrentPage > 6)
                menu.CurrentPage = 1;
            else if (menu.CurrentPage < 1)
                menu.CurrentPage = 6;

            SetKey(menu.Block, MENU_HEAD, PAGE_KEY, menu.CurrentPage.ToString());

            DrawMenu(menu);
        }


        // BUTTON TIMER //
        void ButtonTimer()
        {
            if (_buttonCountDown < 1)
                return;
            else if (_buttonCountDown == 1)
                ClearButtons();

            _buttonCountDown--;
        }


        // CLEAR BUTTONS //
        void ClearButtons()
        {
            if (_mapMenus.Count < 1)
                return;

            foreach(MapMenu menu in _mapMenus)
            {
                if(menu.ActiveButton > 0)
                {
                    menu.ActiveButton = 0;
                    DrawMenu(menu);
                }
            }
        }


        // SHOW MENU DATA //
        void ShowMenuData()
        {
            Echo("Menus: " + _mapMenus.Count);

            if (_mapMenus.Count < 1)
                return;

            foreach (MapMenu menu in _mapMenus)
            {
                string display;
                if (menu.DataDisplay != null)
                    display = menu.DataDisplay.IDNumber.ToString();
                else
                    display = "null";
                Echo(menu.Block.CustomName + "\n * Data Display: " + display + "\n");
            }
        }


        // SET MENU ID //
        void SetMenuID(MapMenu menu)
        {
            string idString = GetKey(menu.Block, MENU_HEAD, MENU_ID, "");
            int menuID;

            if (idString == "") // If no previous ID written to custom data, assign and check other block data for duplicates.
            {
                menuID = _mapMenus.Count + 1;
                menu.SetID(menuID);

                while (DuplicatedIDInData(menu))
                {
                    menuID++;
                    menu.SetID(menuID);
                }      
            }
            else // Use recorded value, but check current list for duplicates
            {
                menuID = ParseInt(idString, 1);
                menu.SetID(menuID);

                while (DuplicateIDInList(menu))
                {
                    menuID++;
                    menu.SetID(menuID);
                } 
            }
        }


        // DUPLICATE ID IN LIST // - Check assigned menus to make sure that menu ID number isn't already assigned.
        bool DuplicateIDInList(MapMenu menu)
        {
            if (_mapMenus.Count < 1)
                return false;

            foreach(MapMenu assignedMenu in _mapMenus)
            {
                if (menu.IDNumber == assignedMenu.IDNumber)
                    return true;
            }

            return false;
        }


        // DUPLICATE ID IN DATA //
        bool DuplicatedIDInData(MapMenu menu)
        {
            List<IMyTerminalBlock> menuBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(MENU_TAG, menuBlocks);

            if (menuBlocks.Count < 1)
                return false;

            foreach(IMyTerminalBlock block in menuBlocks)
            {
                string blockData = block.CustomData;
                if(blockData.Contains(MENU_HEAD) && blockData.Contains(MENU_ID) && block != menu.Block)
                {
                    MyIni blockIni = GetIni(block);
                    string idValue = blockIni.Get(MENU_HEAD, MENU_ID).ToString();

                    if(idValue == menu.IDNumber.ToString())
                        return true;
                }               
            }

            return false;
        }
    }
}
