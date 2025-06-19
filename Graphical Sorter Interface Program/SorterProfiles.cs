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

            public static string WeaponList = " mr-20\n"
                                        + " mr-20mag\n"
                                        + " mr-50a\n"
                                        + " mr-50amag\n"
                                        + " mr-8p\n"
                                        + " mr-8pmag\n"
                                        + " mr-30e\n"
                                        + " mr-30emag\n"
                                        + " s-10\n"
                                        + " s-10mag\n"
                                        + " s-20a\n"
                                        + " s-20amag\n"
                                        + " s-10e\n"
                                        + " s-10emag\n"
                                        + " ro-1\n"
                                        + " pro-1\n"
                                        + " missile\n"
                                        + " flaregun\n"
                                        + " flareclip\n"
                                        + " 5.56x45mm";

            public static string ToolList = " anglegrinderitem\n"
                                        + " anglegrinder2item\n"
                                        + " anglegrinder3item\n"
                                        + " anglegrinder4item\n"
                                        + " welderitem\n"
                                        + " welder2item\n"
                                        + " welder3item\n"
                                        + " welder4item\n"
                                        + " handdrillitem\n"
                                        + " handdrill2item\n"
                                        + " handdrill3item\n"
                                        + " handdrill4item\n"
                                        + " hydrogenbottle\n"
                                        + " oxygenbottle";

            public static string MiscList = " medkit\n"
                                        + " powerkit\n"
                                        + " clangcola\n"
                                        + " cosmiccoffee\n"
                                        + " datapad\n"
                                        + " spacecredit\n"
                                        + " package\n"
                                        + " zonechip";

            public static readonly Dictionary<string, string[]> Lookup = new Dictionary<string, string[]>
            {
                // ORES
                {"ore/iron",new[]{"Ore/Iron","Fe","170,108,93"}},//"127,96,96"}},
                {"ore/nickel",new[]{"Ore/Nickel","Ni","145,144,124"}},
                {"ore/silicon",new[]{"Ore/Silicon","Si","87,88,83"}},
                {"ore/cobalt",new[]{"Ore/Cobalt","Co","96,194,255"}},//"73,162,194"
                {"ore/magnesium",new[]{"Ore/Magnesium","Mg","55,120,150"}},//"55,93,114"
                {"ore/silver",new[]{"Ore/Silver","Ag","201,199,176"}},
                {"ore/gold",new[]{"Ore/Gold","Au","168,147,1"}},
                {"ore/platinum",new[]{"Ore/Platinum","Pt","189,190,172"}},
                {"ore/uranium",new[]{"Ore/Uranium","U","73,66,60"}},
                {"ice",new[]{"Ore/Ice","Ice","75,110,130"}},
                {"stone",new[]{"Ore/Stone","Stone","127,127,127"}},
                {"scrap",new[]{"Ore/Scrap","Scrap","127,127,127"}},
                {"oldscrap",new[]{"Ingot/Scrap","OldScrap","127,96,72"}},
                {"organic",new[]{"Ore/Organic","Organic","127,164,88"}},

                // INGOTS
                {"ingot/iron",new[]{"Ingot/Iron","Fe","150,127,127"}},
                {"ingot/nickel",new[]{"Ingot/Nickel","Ni","106,119,125"}},
                {"ingot/silicon",new[]{"Ingot/Silicon","Si","55,70,89"}},
                {"ingot/cobalt",new[]{"Ingot/Cobalt","Co","120,111,114"}},
                {"ingot/magnesium",new[]{"Ingot/Magnesium","Mg","110,128,142"}},
                {"ingot/silver",new[]{"Ingot/Silver","Ag","136,159,173"}},
                {"ingot/gold",new[]{"Ingot/Gold","Au","168,147,1"}},
                {"ingot/platinum",new[]{"Ingot/Platinum","Pt","107,127,138"}},
                {"ingot/uranium",new[]{"Ingot/Uranium","U","59,65,65"}},
                {"gravel",new[]{"Ingot/Stone","Gravel","150,150,150"}},
                {"ptscrap",new[]{"Ingot/PrototechScrap","PT Scrap","168,147,1"}},

                // COMPONENTS
                {"bpglass",new[]{"Component/BulletproofGlass", "Glass","150,150,255"}},
                {"computer",new[]{"Component/Computer","CPU","72,84,56"}},
                {"construction",new[]{"Component/Construction","CstrComp","150,150,150"}},
                {"detector",new[]{"Component/Detector","Detect","127,127,127"}},
                {"display",new[]{"Component/Display","Display","127,127,127"}},
                {"explosives",new[]{"Component/Explosives","Explsv","127,64,64"}},
                {"girder",new[]{"Component/Girder","Girder","127,127,127"}},
                {"gravgen",new[]{"Component/GravityGenerator","Grav","150,150,150"}},
                {"interiorplate",new[]{"Component/InteriorPlate","Int.Plate","192,192,192"}},
                {"largetube",new[]{"Component/LargeTube","LTube","127,127,127"}},
                {"medical",new[]{"Component/Medical","Med.","255,192,192"}},
                {"metalgrid",new[]{"Component/MetalGrid","Grid","192,192,192"}},
                {"motor",new[]{"Component/Motor","Motor","150,150,150"}},
                {"powercell",new[]{"Component/PowerCell","PwrCell","150,150,150"}},
                {"radio",new[]{"Component/RadioCommunication","Radio","150,150,150"}},
                {"reactor",new[]{"Component/Reactor","Reactor","127,150,144"}},
                {"smalltube",new[]{"Component/SmallTube","STube","150,150,150"}},
                {"solarcell",new[]{"Component/SolarCell","Solar","150,150,255"}},
                {"steelplate",new[]{"Component/SteelPlate","St.Plate","127,127,127"}},
                {"superconductor",new[]{"Component/Superconductor","SprCnd","127,127,127"}},
                {"thruster",new[]{"Component/Thrust","Thrust","127,127,127"}},
                {"canvas",new[]{"Component/Canvas","Canvas","192,32,32"}},
                {"engineerplushie",new[]{"Component/EngineerPlushie","Plush","180,120,200"}},
                {"sabiroidplushie",new[]{"Component/SabiroidPlushie","SPlush","137,136,106"}},
                {"ptcircuitry",new[]{"Component/PrototechCircuitry","pCircuit","168,147,1"}},
                {"ptcapacitor",new[]{"Component/PrototechCapacitor","pCpctr","192,192,150"}},
                {"ptcooling",new[]{"Component/PrototechCoolingUnit","pCooling","192,192,150"}},
                {"ptframe",new[]{"Component/PrototechFrame","pFrame","192,192,150"}},
                {"ptmachinery",new[]{"Component/PrototechMachinery","pMach.","192,192,150"}},
                {"ptpanel",new[]{"Component/PrototechPanel","pPanel","192,192,150"}},
                {"ptpropulsion",new[]{"Component/PrototechPropulsionUnit","pProp.","192,192,150"}},

                // SHIP AMMO
                {"gatlingammo",new[]{"AmmoMagazine/NATO_25x184mm","25mm","120,170,120"}},
                {"autocannonmag",new[]{"AmmoMagazine/AutocannonClip","40mm","140,170,120"}},
                {"assaultcannonshell",new[]{"AmmoMagazine/MediumCalibreAmmo","55mm","150,150,150"}},
                {"artilleryshell",new[]{"AmmoMagazine/LargeCalibreAmmo","125mm","150,150,150"}},
                {"srailgunsabot",new[]{"AmmoMagazine/SmallRailgunAmmo","smRail","150,150,150"}},
                {"lrailgunsabot",new[]{"AmmoMagazine/LargeRailgunAmmo","lgRail","150,150,150"}},
                {"missile",new[]{"AmmoMagazine/Missile200mm","Missile","96,96,0"}},
                {"fireworksred",new[]{"AmmoMagazine/FireworksBoxRed","Fw:Red","255,64,64"}},
                {"fireworksyellow",new[]{"AmmoMagazine/FireworksBoxYellow","FwYellow","255,255,64"}},
                {"fireworksgreen",new[]{"AmmoMagazine/FireworksBoxGreen","Fw:Green","64,127,64"}},
                {"fireworksblue",new[]{"AmmoMagazine/FireworksBoxBlue","Fw:Blue","64,64,255"}},
                {"fireworkspink",new[]{"AmmoMagazine/FireworksBoxPink","Fw:Pink","255,96,127"}},
                {"fireworksrainbow",new[]{"AmmoMagazine/FireworksBoxRainbow","Fw:Multi","192,144,255"}},

                // PERSONAL AMMO
                {"5.56x45mm",new[]{"AmmoMagazine/NATO_5p56x45mm","5.56x45mm","84,84,84"}},
                {"mr-20mag",new[]{"AmmoMagazine/AutomaticRifleGun_Mag_20rd","mr-20","175,150,150"}},
                {"mr-50amag",new[]{"AmmoMagazine/RapidFireAutomaticRifleGun_Mag_50rd","mr-50a","160,160,150"}},
                {"mr-8pmag",new[]{"AmmoMagazine/PreciseAutomaticRifleGun_Mag_5rd","mr-8p","150,175,150"}},
                {"mr-30emag",new[]{"AmmoMagazine/UltimateAutomaticRifleGun_Mag_30rd","mr-30e","150,150,175"}},
                {"s-10mag",new[]{"AmmoMagazine/SemiAutoPistolMagazine","s-10","175,150,150"}},
                {"s-20amag",new[]{"AmmoMagazine/FullAutoPistolMagazine","s-20a","160,160,150"}},
                {"s-10emag",new[]{"AmmoMagazine/ElitePistolMagazine","s-10e","150,175,150"}},
                {"flareclip",new[]{"AmmoMagazine/FlareClip","flares","176,80,56"}},

                // WEAPONS
                {"mr-20",new[]{"PhysicalGunObject/AutomaticRifleItem","mr-20","175,150,150"}},
                {"mr-50a",new[]{"PhysicalGunObject/RapidFireAutomaticRifleItem","mr-50","160,160,150"}},
                {"mr-8p",new[]{"PhysicalGunObject/PreciseAutomaticRifleItem","mr-8p","150,175,150"}},
                {"mr-30e",new[]{"PhysicalGunObject/UltimateAutomaticRifleItem","mr-30e","150,150,175"}},
                {"s-10",new[]{"PhysicalGunObject/SemiAutoPistolItem","s-10","175,150,150"}},
                {"s-20a",new[]{"PhysicalGunObject/FullAutoPistolItem","s-20a","160,160,150"}},
                {"s-10e",new[]{"PhysicalGunObject/ElitePistolItem","s-10e","150,175,150"}},
                {"ro-1",new[]{"PhysicalGunObject/BasicHandHeldLauncherItem","ro-1","175,150,150"}},
                {"pro-1",new[]{"PhysicalGunObject/AdvancedHandHeldLauncherItem","pro-1","150,150,175"}},
                {"flaregun",new[]{"PhysicalGunObject/FlareGunItem","flaregun","176,80,56"}},

                // TOOLS
                {"anglegrinderitem",new[]{"PhysicalGunObject/AngleGrinderItem","","175,150,150"}},
                {"anglegrinder2item",new[]{"PhysicalGunObject/AngleGrinder2Item","","160,160,150"}},
                {"anglegrinder3item",new[]{"PhysicalGunObject/AngleGrinder3Item","","150,175,150"}},
                {"anglegrinder4item",new[]{"PhysicalGunObject/AngleGrinder4Item","","150,150,175"}},
                {"welderitem",new[]{"PhysicalGunObject/WelderItem","","175,150,150"}},//,"180,180,150" "176,144,69"
                {"welder2item",new[]{"PhysicalGunObject/Welder2Item","","160,160,150"}},//,"200,150,150" "185,69,78"
                {"welder3item",new[]{"PhysicalGunObject/Welder3Item","","150,175,150"}},//,"180,150,200" "161,76,179"  DF_LABEL = "179,237,255"
                {"welder4item",new[]{"PhysicalGunObject/Welder4Item","","150,150,175"}},//,"180,200,150" "126,156,58"
                {"handdrillitem",new[]{"PhysicalGunObject/HandDrillItem","","175,150,150"}},
                {"handdrill2item",new[]{"PhysicalGunObject/HandDrill2Item","","160,160,150"}},
                {"handdrill3item",new[]{"PhysicalGunObject/HandDrill3Item","","150,175,150"}},
                {"handdrill4item",new[]{"PhysicalGunObject/HandDrill4Item","","150,150,175"}},
                {"hydrogenbottle",new[]{"GasContainerObject/HydrogenBottle","H2","160,105,60"}},//,"255,140,80"}},
                {"oxygenbottle",new[]{"OxygenContainerObject/OxygenBottle","O2","100,200,237"}},//,"100,200,237"}},

                // MISC - TODO
                {"medkit",new[]{"ConsumableItem/Medkit","Medkit","255,192,192"}},
                {"powerkit",new[]{"ConsumableItem/Powerkit","PwrKit","150,150,150"}},
                {"clangcola",new[]{"ConsumableItem/ClangCola","CLANG!","192,72,72"}},
                {"cosmiccoffee",new[]{"ConsumableItem/CosmicCoffee","Coffee","120,90,60"}},
                {"datapad",new[]{"Datapad/Datapad","DataPad","150,150,150"}},
                {"spacecredit",new[]{"PhysicalObject/SpaceCredit","Credits","150,150,150"}},
                {"package",new[]{"Package/Package","Package","127,127,127"}},
                {"zonechip",new[]{"Component/ZoneChip","ZoneChip","64,127,100"}}
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
