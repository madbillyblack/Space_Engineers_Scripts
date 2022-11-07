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
                    Zoom(map, false);
                    break;
                case 3:
                    RotateMap(map, "LEFT");
                    break;
                case 4:
                    MoveMap(map, "LEFT");
                    break;
                case 5:
                    TrackMap(map, "LEFT");
                    break;
                case 6:
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
                    Zoom(map, true);
                    break;
                case 3:
                    RotateMap(map, "RIGHT");
                    break;
                case 4:
                    MoveMap(map, "RIGHT");
                    break;
                case 5:
                    TrackMap(map, "RIGHT");
                    break;
                case 6:
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
                    AdjustRadius(map, true);
                    break;
                case 3:
                    RotateMap(map, "DOWN");
                    break;
                case 4:
                    MoveMap(map, "DOWN");
                    break;
                case 5:
                    TrackMap(map, "DOWN");
                    break;
                case 6:
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
                    AdjustRadius(map, false);
                    break;
                case 3:
                    RotateMap(map, "UP");
                    break;
                case 4:
                    MoveMap(map, "UP");
                    break;
                case 5:
                    TrackMap(map, "UP");
                    break;
                case 6:
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
                    SpinMap(map, "LEFT", ANGLE_STEP / 2);
                    break;
                case 4:
                    MoveMap(map, "BACKWARD");
                    break;
                case 5:
                    TrackMap(map, "BACKWARD");
                    break;
                case 6:
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
                    SpinMap(map, "RIGHT", ANGLE_STEP / 2);
                    break;
                case 4:
                    MoveMap(map, "FORWARD");
                    break;
                case 5:
                    TrackMap(map, "FORWARD");
                    break;
                case 6:
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
                    map.showInfo = setState(map.showInfo, 3); // Toggle Info Bars
                    break;
                case 3:
                    map.showInfo = setState(map.showInfo, 3); // Toggle Info Bars
                    break;
                case 4:
                    map.showInfo = setState(map.showInfo, 3); // Toggle Info Bars
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
