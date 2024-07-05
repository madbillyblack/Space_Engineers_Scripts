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
        const string DISPLAY_KEY = "Display as";
        public static string _turretString;

        public static List<DisplayTurret> _turrets;

        public class DisplayTurret
        {
            public string DisplayName { get; set; }
            MyIniHandler IniHandler {  get; set; }
            public string BlockName { get; set; }


            public IMyLargeTurretBase Turret {  get; set; }
            public IMyTurretControlBlock TurretController { get; set; }

            public DisplayTurret(string displayName, MyIniHandler iniHandler, IMyLargeTurretBase turret)
            {
                DisplayName = displayName;
                IniHandler = iniHandler;
                Turret = turret;
                TurretController = null;
                BlockName = Turret.CustomName;
            }

            public DisplayTurret(string displayName, MyIniHandler iniHandler, IMyTurretControlBlock turretController)
            {
                DisplayName = displayName;
                IniHandler = iniHandler;
                Turret = null;
                TurretController = turretController;
                BlockName = TurretController.CustomName;
            }

            public string GetStatus()
            {
                string status = "";
                bool IsWorking = (TurretController != null && TurretController.IsWorking) || (Turret != null && Turret.IsWorking);
                bool IsTargeting = (TurretController != null && TurretController.HasTarget) || (Turret != null && Turret.HasTarget);

                if (IsWorking && IsTargeting)
                    status = "ACTIVE";
                else if (IsWorking)
                    status = "Idle";
                else
                    status = "Disabled";

                return status;
            }
        }

        public void AssignDisplayTurrets()
        {
            _turrets = new List<DisplayTurret>();

            List<IMyLargeTurretBase> turrets = new List<IMyLargeTurretBase>();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(turrets);

            if (turrets.Count > 0)
            {
                foreach(IMyLargeTurretBase turret in turrets)
                {
                    AssignDisplayTurret(turret);
                }
            }

            List<IMyTurretControlBlock> controllers = new List<IMyTurretControlBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTurretControlBlock>(controllers);

            if (controllers.Count > 0)
            {
                foreach(IMyTurretControlBlock controller in controllers)
                {
                    AssignDisplayTurret(controller, true);
                }
            }

            if(_turrets.Count > 0)
                _turrets.Sort((x, y) => x.DisplayName.CompareTo(y.DisplayName));
        }

        
        public void AssignDisplayTurret(IMyTerminalBlock block, bool isController = false)
        {
            MyIniHandler iniHandler = new MyIniHandler(block);

            if (!iniHandler.HasSameGridId()) return;

            string name = iniHandler.GetKey(INI_HEAD, DISPLAY_KEY, block.CustomName);

            switch(name.Trim().ToUpper())
            {
                case null:
                case "":
                case "FALSE":
                case "OFF":
                    return;
                default:
                    if (isController)
                        _turrets.Add(new DisplayTurret(name, iniHandler, block as IMyTurretControlBlock));
                    else
                        _turrets.Add(new DisplayTurret(name, iniHandler, block as IMyLargeTurretBase));
                    break;
            }
        }


        public static void UpdateTurretString()
        {
            if (_turrets.Count < 1) return;

            _turretString = "";

            foreach(DisplayTurret turret in _turrets) {
                _turretString += turret.DisplayName + ": " + turret.GetStatus() + "\n";

            }
        }
    }
}
