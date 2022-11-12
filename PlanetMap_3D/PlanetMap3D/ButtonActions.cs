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
        // BUTTON PRESS //
        void ButtonPress(string buttonIndex, string menuIndex)
        {
            MapMenu menu = GetMenu(menuIndex);

            if(menu == null)
            {
                _statusMessage += "Invalid Button Call: " + buttonIndex + "," + menuIndex + "\n";
                return;
            }

            StarMap map = GetMap(menu.CurrentMapIndex);
            
            
            if(map == null)
            {
                _statusMessage += "Invalid Index for Menu " + menu.IDNumber + "!\n- Index:" + menu.CurrentMapIndex;
                return;
            }


            switch(buttonIndex)
            {
                case "1":
                    Action1(menu, map);
                    break;
                case "2":
                    Action2(menu, map);
                    break;
                case "3":
                    Action3(menu, map);
                    break;
                case "4":
                    Action4(menu, map);
                    break;
                case "5":
                    Action5(menu, map);
                    break;
                case "6":
                    Action6(menu, map);
                    break;
                case "7":
                    Action7(menu, map);
                    break;
                default:
                    _statusMessage += "No Such Button \"" + buttonIndex + "\"!\n";
                    break;
            }
        }


        // ACTION 1 //
        void Action1(MapMenu menu, StarMap map)
        {
            menu.PressButton(1);

            switch (menu.CurrentPage)
            {
                case 1:
                    CyclePlanets(map, false);
                    break;
                case 2:
                    map.Zoom(false);
                    break;
                case 3:
                    map.Rotate("LEFT");
                    break;
                case 4:
                    map.Move("LEFT");
                    break;
                case 5:
                    map.Track("LEFT");
                    break;
                case 6:
                    menu.PreviousDataPage();
                    break;
            }

            DrawMenu(menu);
        }


        // ACTION 2 //
        void Action2(MapMenu menu, StarMap map)
        {
            menu.PressButton(2);

            switch (menu.CurrentPage)
            {
                case 1:
                    CyclePlanets(map, true);
                    break;
                case 2:
                    map.Zoom(true);
                    break;
                case 3:
                    map.Rotate("RIGHT");
                    break;
                case 4:
                    map.Move("RIGHT");
                    break;
                case 5:
                    map.Track("RIGHT");
                    break;
                case 6:
                    menu.NextDataPage();
                    break;
            }


            DrawMenu(menu);
        }


        // ACTION 3 //
        void Action3(MapMenu menu, StarMap map)
        {
            menu.PressButton(3);

            switch (menu.CurrentPage)
            {
                case 1:
                    CycleWaypoints(map, false);
                    break;
                case 2:
                    map.AdjustRadius(true);
                    break;
                case 3:
                    map.Rotate("DOWN");
                    break;
                case 4:
                    map.Move("DOWN");
                    break;
                case 5:
                    map.Track("DOWN");
                    break;
                case 6:
                    menu.ScrollUp();
                    break;
            }

            DrawMenu(menu);
        }


        // ACTION 4 //
        void Action4(MapMenu menu, StarMap map)
        {
            menu.PressButton(4);

            switch (menu.CurrentPage)
            {
                case 1:
                    CycleWaypoints(map, true);
                    break;
                case 2:
                    map.AdjustRadius(false);
                    break;
                case 3:
                    map.Rotate("UP");
                    break;
                case 4:
                    map.Move("UP");
                    break;
                case 5:
                    map.Track("UP");
                    break;
                case 6:
                    menu.ScrollDown();
                    break;
            }

            DrawMenu(menu);
        }


        // ACTION 5 //
        void Action5(MapMenu menu, StarMap map)
        {
            menu.PressButton(5);

            switch (menu.CurrentPage)
            {
                case 1:
                    menu.PreviousMap();
                    break;
                case 2:
                    CycleMode(map, false);
                    break;
                case 3:
                    map.Spin("LEFT");
                    break;
                case 4:
                    map.Move("BACKWARD");
                    break;
                case 5:
                    map.Track("BACKWARD");
                    break;
                case 6:
                    menu.PreviousDataDisplay();
                    break;
            }

            DrawMenu(menu);
        }


        // ACTION 6 //
        void Action6(MapMenu menu, StarMap map)
        {
            menu.PressButton(6);

            switch (menu.CurrentPage)
            {
                case 1:
                    menu.NextMap();
                    break;
                case 2:
                    CycleMode(map, true);
                    break;
                case 3:
                    map.Spin("RIGHT");
                    break;
                case 4:
                    map.Move("FORWARD");
                    break;
                case 5:
                    map.Track("FORWARD");
                    break;
                case 6:
                    menu.NextDataDisplay();
                    break;
            }

            DrawMenu(menu);
        }


        // ACTION 7 //
        void Action7(MapMenu menu, StarMap map)
        {
            menu.PressButton(7);

            switch (menu.CurrentPage)
            {
                case 1:
                    cycleGPS(map);
                    break;
                case 2:
                    map.ShowInfo = setState(map.ShowInfo, 3); // Toggle Info Bars
                    break;
                case 3:
                    map.ShowInfo = setState(map.ShowInfo, 3); // Toggle Info Bars
                    break;
                case 4:
                    map.ShowInfo = setState(map.ShowInfo, 3); // Toggle Info Bars
                    break;
                case 5:
                    map.Stop();
                    break;
                case 6:
                    break;
            }

            DrawMenu(menu);
        }
    }
}
