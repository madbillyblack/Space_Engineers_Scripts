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
        List<MapMenu> _mapMenus;

        public class MapMenu
        {
            public IMyShipController Controller;
            public IMyTextSurface Surface;
            public StarMap ActiveMap;
            public int CurrentMenu;
            
            public MapMenu(IMyShipController controller)
            {
                Controller = controller;

                string blockName = GetKey(controller, MENU_HEAD, "LCD Block", controller.CustomName);
                int index = ParseInt(GetKey(controller, MENU_HEAD, "LCD Index", "0"), 0);


            }

        }
    }
}
