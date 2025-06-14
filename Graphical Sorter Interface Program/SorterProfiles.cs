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

            public static readonly Dictionary<string, string[]> Lookup = new Dictionary<string, string[]>
            {
                // ORES
                {"ore/iron",new[]{"Ore/Iron","Fe"}},
                {"ore/nickel",new[]{"Ore/Nickel","Ni" }},
                {"ore/silicon",new[]{"Ore/Silicon","Si"}},
                {"ore/cobalt",new[]{"Ore/Cobalt","Co" }},
                {"ore/magnesium",new[]{"Ore/Magnesium","Mg"}},
                {"ore/silver",new[]{"Ore/Silver","Ag"}},
                {"ore/gold",new[]{"Ore/Gold","Au"}},
                {"ore/platinum",new[]{"Ore/Platinum","Pt"}},
                {"ore/uranium",new[]{"Ore/Uranium","U" }},
                {"ice",new[]{"Ore/Ice","Ice"}},
                {"stone",new[]{"Ore/Stone","Stone"}},
                {"scrap",new[]{"Ore/Scrap","Scrap"}},
                {"oldscrap",new[]{"Ingot/Scrap","Old"}},
                {"organic",new[]{"Ore/Organic","Organic"}},

                // INGOTS
                {"ingot/iron",new[]{"Ingot/Iron","Fe"}},
                {"ingot/nickel",new[]{"Ingot/Nickel","Ni"}},
                {"ingot/silicon",new[]{"Ingot/Silicon","Si"}},
                {"ingot/cobalt",new[]{"Ingot/Cobalt","Co"}},
                {"ingot/magnesium",new[]{"Ingot/Magnesium","Mg"}},
                {"ingot/silver",new[]{"Ingot/Silver","Ag"}},
                {"ingot/gold",new[]{"Ingot/Gold","Au"}},
                {"ingot/platinum",new[]{"Ingot/Platinum","Pt"}},
                {"ingot/uranium",new[]{"Ingot/Uranium","U"}},
                {"gravel",new[]{"Ingot/Stone","Gravel"}},
                {"prototechscrap",new[]{"Ingot/PrototechScrap","pScrap"}}

                // COMPONENTS - TODO

                // AMMO - TODO

                // WEAPONS - TODO

                // TOOLS - TODO

                // MISC - TODO
            };

            public static string[] LookupItem(string item)
            {
                if (Lookup.ContainsKey(item))
                {
                    string [] arr = Lookup[item];
                    return new string[] {"MyObjectBuilder_" + arr[0], arr[1]};
                }

                return new string []{ item, "" };
            }
        }
    }
}
