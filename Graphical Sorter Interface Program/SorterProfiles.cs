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
using VRage.Scripting;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public static class SorterProfiles
        {
            public static string OreList = " ore/iron\n"
                                        + " ore/nickel\n"
                                        + " ore/silicon\n"
                                        + " ore/cobalt\n"
                                        + " ore/magnesium\n"
                                        + " ore/silver\n"
                                        + " ore/gold\n"
                                        + " ore/uranium\n"
                                        + " ore/platinum\n"
                                        + " ice\n"
                                        + " stone\n"
                                        + " scrap\n"
                                        + " oldscrap";

            public static string IngotList = " ingot/iron\n"
                                        + " ingot/nickel\n"
                                        + " ingot/silicon\n"
                                        + " ingot/cobalt\n"
                                        + " ingot/magnesium\n"
                                        + " ingot/silver\n"
                                        + " ingot/gold\n"
                                        + " ingot/uranium\n"
                                        + " ingot/platinum\n"
                                        + " gravel";

            public static string ComponentList = "TODO";

            public static string AmmoList = "TODO";

            public static string WeaponList = "TODO";

            public static string MiscList = "TODO";

            public static readonly Dictionary<string, string> Lookup = new Dictionary<string, string>
            {
                // ORES
                { "ore/iron","Ore/Iron" },
                { "ore/nickel","Ore/Nickel" },
                { "ore/silicon","Ore/Silicon" },
                { "ore/cobalt","Ore/Cobalt" },
                { "ore/magnesium","Ore/Magnesium" },
                { "ore/silver","Ore/Silver" },
                { "ore/gold","Ore/Gold" },
                { "ore/platinum","Ore/Platinum" },
                { "ore/uranium","Ore/Uranium" },
                { "ice","Ore/Ice"},
                { "stone","Ore/Stone" },
                { "scrap","Ore/Scrap" },
                { "oldscrap","Ingot/Scrap"},
                { "organic","Ore/Organic" },

                // INGOTS
                { "ingot/iron","Ingot/Iron" },
                { "ingot/nickel","Ingot/Nickel" },
                { "ingot/silicon","Ingot/Silicon" },
                { "ingot/cobalt","Ingot/Cobalt" },
                { "ingot/magnesium","Ingot/Magnesium" },
                { "ingot/silver","Ingot/Silver" },
                { "ingot/gold","Ingot/Gold" },
                { "ingot/platinum","Ingot/Platinum" },
                { "ingot/uranium","Ingot/Uranium" },
                { "gravel", "Ingot/Stone" },
                { "prototechscrap", "Ingot/PrototechScrap"}

                // COMPONENTS - TODO

                // AMMO - TODO

                // WEAPONS - TODO

                // TOOLS - TODO

                // MISC - TODO
            };

            public static string LookupItem(string item)
            {
                if (Lookup.ContainsKey(item))
                {
                    //_logger.LogInfo("Retrieved item filter: " + Lookup[item]);
                    return "MyObjectBuilder_" + Lookup[item];
                }
                    
                return item;
            }
        }
    }
}
