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
        List<MapMenu> _mapMenus;
        
        public static int _buttonCountDown;
        const int BUTTON_TIME = 3;


        // MAP MENU //
        public class MapMenu
        {
            public IMyShipController Controller;
            public IMyTextSurface Surface;
            public int CurrentMapIndex;
            public int CurrentPage;
            public int IDNumber;
            public int ActiveButton;
            public RectangleF Viewport;
            public Color Color1;
            public Color Color2;

            
            public MapMenu(IMyShipController controller)
            {
                Controller = controller;
                ActiveButton = 0;
            }

            public void InitializeSurface()
            {
                PrepareTextSurfaceForSprites(Surface);
                Viewport = new RectangleF((Surface.TextureSize - Surface.SurfaceSize) / 2f, Surface.SurfaceSize);
            }

            public void PressButton(int button)
            {
                ActiveButton = button;
                _buttonCountDown = BUTTON_TIME;
            }

            public void NextMap()
            {
                if (_mapList.Count < 2)
                    return;

                CurrentMapIndex++;

                if (CurrentMapIndex >= _mapList.Count)
                    CurrentMapIndex = 0;
            }


            public void PreviousMap()
            {
                if (_mapList.Count < 2)
                    return;

                CurrentMapIndex--;

                if (CurrentMapIndex < 0)
                    CurrentMapIndex = _mapList.Count - 1;
            }
        }


        // ASSIGN MENUS // - Get ship controllers tagged with MENU_TAG and add them to the map menu list.
        void AssignMenus()
        {
            _buttonCountDown = 0;

            List<IMyShipController> controllers = new List<IMyShipController>();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(controllers);

            if(controllers.Count > 0)
            {
                foreach(IMyShipController controller in controllers)
                {
                    // Check that controller has MENU_TAG and same Grid ID
                    if(controller.CustomName.Contains(MENU_TAG) && GetKey(controller, SHARED, "Grid_ID", "") == _gridID)
                    {
                        MapMenu menu = MenuFromController(controller);
                        if (menu != null)
                        {
                            menu.IDNumber = _mapMenus.Count;
                            menu.InitializeSurface();
                            DrawMenu(menu);
                            _mapMenus.Add(menu);
                        }
                    }
                }
            }
        }


        // MENU FROM CONTROLLER // - Get Menu from provided block and populate menu parameters.
        MapMenu MenuFromController(IMyShipController block)
        {
            // Create Empty Menu
            MapMenu menu = new MapMenu(block);

            // Get name of block and index of screen where menu interface should be displayed
            string blockName = GetKey(block, MENU_HEAD, "LCD Block", block.CustomName);
            int index = ParseInt(GetKey(block, MENU_HEAD, "LCD Index", "0"), 0);

            // Set currently available menu parameters
            menu.CurrentPage = ParseInt(GetKey(block, MENU_HEAD, "Current Page", "1"), 1);
            menu.Color1 = ParseColor(GetKey(block, MENU_HEAD, "Color 1", "0,0,0"));
            menu.Color2 = ParseColor(GetKey(block, MENU_HEAD, "Color 2", "0,127,0"));

            int mapIndex;

            if (_mapList.Count > 1)
            {
                mapIndex = ParseInt(GetKey(block, MENU_HEAD, "Current Map", "0"), 0);
                if (mapIndex >= _mapList.Count)
                    mapIndex = 0;
            }
            else
                mapIndex = 0;

            menu.CurrentMapIndex = mapIndex;


            try
            {
                IMyTerminalBlock lcdBlock = GridTerminalSystem.GetBlockWithName(blockName);
                menu.Surface = (lcdBlock as IMyTextSurfaceProvider).GetSurface(index);

                return menu;
            }
            catch
            {
                _statusMessage += "Menu Surface could not be retrieved from block " + blockName + "\n";
                return null;
            }
        }


        // GET MENU //
        MapMenu GetMenu(string arg)
        {
            try
            {
                int index;

                if (arg == "")
                    index = 0;
                else
                    index = ParseInt(arg.Split(' ')[0], 0);

                return _mapMenus[index];
            }
            catch
            {
                return null;
            }
        }


        // NEXT MENU //
        void NextMenu(string arg, bool next)
        {
            MapMenu menu = GetMenu(arg);

            if(menu == null)
            {
                _statusMessage += "No Menu " + arg + " found!\n";
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
    }
}
