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
                                        + " ptscrap\n"
                                        + " gravel";

            public static string ComponentList = " bpglass\n"
                                        + " computer\n"
                                        + " construction\n"
                                        + " detector\n"
                                        + " display\n"
                                        + " explosives\n"
                                        + " girder\n"
                                        + " gravgen\n"
                                        + " interiorplate\n"
                                        + " largetube\n"
                                        + " medical\n"
                                        + " metalgrid\n"
                                        + " motor\n"
                                        + " powercell\n"
                                        + " radio\n"
                                        + " reactor\n"
                                        + " smalltube\n"
                                        + " solarcell\n"
                                        + " steelplate\n"
                                        + " superconductor\n"
                                        + " thruster\n"
                                        + " canvas\n"
                                        + " engineerplushie\n"
                                        + " sabiroidplushie\n"
                                        + " ptcircuitry\n"
                                        + " ptcapacitor\n"
                                        + " ptcooling\n"
                                        + " ptframe\n"
                                        + " ptmachinery\n"
                                        + " ptpanel\n"
                                        + " ptpropulsion\n";

            public static string AmmoList = " gatlingammo\n"
                                        + " autocannonmag\n"
                                        + " assaultcannonshell\n"
                                        + " artilleryshell\n"
                                        + " srailgunsabot\n"
                                        + " lrailgunsabot\n"
                                        + " missile\n"
                                        + " fireworksred\n"
                                        + " fireworksyellow\n"
                                        + " fireworksgreen\n"
                                        + " fireworksblue\n"
                                        + " fireworkspink\n"
                                        + " fireworksrainbow";

            public static string WeaponList = "TODO\n"
                                        + " \n"
                                        + " ";

            public static string MiscList = "TODO\n"
                                        + " \n"
                                        + " ";

            public static readonly Dictionary<string, string[]> Lookup = new Dictionary<string, string[]>
            {
                // ORES
                {"ore/iron",new[]{"Ore/Iron","Fe","127,96,96"}},
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
                {"oldscrap",new[]{"Ingot/Scrap","OldScrap"}},
                {"organic",new[]{"Ore/Organic","Organic"}},

                // INGOTS
                {"ingot/iron",new[]{"Ingot/Iron","Fe","192,127,127"}},
                {"ingot/nickel",new[]{"Ingot/Nickel","Ni"}},
                {"ingot/silicon",new[]{"Ingot/Silicon","Si"}},
                {"ingot/cobalt",new[]{"Ingot/Cobalt","Co"}},
                {"ingot/magnesium",new[]{"Ingot/Magnesium","Mg"}},
                {"ingot/silver",new[]{"Ingot/Silver","Ag"}},
                {"ingot/gold",new[]{"Ingot/Gold","Au"}},
                {"ingot/platinum",new[]{"Ingot/Platinum","Pt"}},
                {"ingot/uranium",new[]{"Ingot/Uranium","U"}},
                {"gravel",new[]{"Ingot/Stone","Gravel"}},
                {"ptscrap",new[]{"Ingot/PrototechScrap","PT Scrap"}},

                // COMPONENTS
                {"bpglass",new[]{"Component/BulletproofGlass", "Glass"}},
                {"computer",new[]{"Component/Computer","CPU"}},
                {"construction",new[]{"Component/Construction","CstrComp"}},
                {"detector",new[]{"Component/Detector","Detect"}},
                {"display",new[]{"Component/Display","Display"}},
                {"explosives",new[]{"Component/Explosives","Explsv"}},
                {"girder",new[]{"Component/Girder","Girder"}},
                {"gravgen",new[]{"Component/GravityGenerator","Grav"}},
                {"interiorplate",new[]{"Component/InteriorPlate","Int.Plate"}},
                {"largetube",new[]{"Component/LargeTube","LTube"}},
                {"medical",new[]{"Component/Medical","Med."}},
                {"metalgrid",new[]{"Component/MetalGrid","Grid"}},
                {"motor",new[]{"Component/Motor","Motor"}},
                {"powercell",new[]{"Component/PowerCell","PwrCell"}},
                {"radio",new[]{"Component/RadioCommunication","Radio"}},
                {"reactor",new[]{"Component/Reactor","Reactor"}},
                {"smalltube",new[]{"Component/SmallTube","STube"}},
                {"solarcell",new[]{"Component/SolarCell","Solar"}},
                {"steelplate",new[]{"Component/SteelPlate","St.Plate"}},
                {"superconductor",new[]{"Component/Superconductor","SprCnd"}},
                {"thruster",new[]{"Component/Thrust","Thrust"}},
                {"canvas",new[]{"Component/Canvas","Canvas"}},
                {"engineerplushie",new[]{"Component/EngineerPlushie","Plush"}},
                {"sabiroidplushie",new[]{"Component/SabiroidPlushie","SPlush"}},
                {"ptcircuitry",new[]{"Component/PrototechCircuitry","pCircuit"}},
                {"ptcapacitor",new[]{"Component/PrototechCapacitor","pCpctr"}},
                {"ptcooling",new[]{"Component/PrototechCoolingUnit","pCooling"}},
                {"ptframe",new[]{"Component/PrototechFrame","pFrame"}},
                {"ptmachinery",new[]{"Component/PrototechMachinery","pMach."}},
                {"ptpanel",new[]{"Component/PrototechPanel","pPanel"}},
                {"ptpropulsion",new[]{"Component/PrototechPropulsionUnit","pProp."}},

                // AMMO - TODO
                {"gatlingammo",new[]{"AmmoMagazine/NATO_25x184mm","25mm"}},
                {"autocannonmag",new[]{"AmmoMagazine/AutocannonClip","40mm"}},
                {"assaultcannonshell",new[]{"AmmoMagazine/MediumCalibreAmmo","55mm"}},
                {"artilleryshell",new[]{"AmmoMagazine/LargeCalibreAmmo","125mm"}},
                {"srailgunsabot",new[]{"AmmoMagazine/SmallRailgunAmmo","smRail"}},
                {"lrailgunsabot",new[]{"AmmoMagazine/LargeRailgunAmmo","lgRail"}},
                {"missile",new[]{"AmmoMagazine/Missile200mm","Missile"}},
                {"fireworksred",new[]{"AmmoMagazine/FireworksBoxRed","Fw:Red","255,64,64"}},
                {"fireworksyellow",new[]{"AmmoMagazine/FireworksBoxYellow","FwYellow","255,255,64"}},
                {"fireworksgreen",new[]{"AmmoMagazine/FireworksBoxGreen","Fw:Green","64,127,64"}},
                {"fireworksblue",new[]{"AmmoMagazine/FireworksBoxBlue","Fw:Blue","64,64,255"}},
                {"fireworkspink",new[]{"AmmoMagazine/FireworksBoxPink","Fw:Pink","255,96,127"}},
                {"fireworksrainbow",new[]{"AmmoMagazine/FireworksBoxRainbow", "Fw:Multi", "192,144,255"}}

                // WEAPONS - TODO

                // TOOLS - TODO

                // PROTOTECH - TODO

                // MISC - TODO
                //{"",new[]{"",""}},
            };

            public static string[] LookupItem(string item)
            {
                if (Lookup.ContainsKey(item))
                {
                    string [] arr = Lookup[item];
                    string last;

                    if (arr.Length < 3)
                        last = "";
                    else
                        last = arr[2];

                    return new string[] {"MyObjectBuilder_" + arr[0], arr[1], last};
                }

                return new string []{ item, "", ""};
            }
        }
    }
}
