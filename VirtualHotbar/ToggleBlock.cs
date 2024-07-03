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
        const string NORMAL = "NORMAL";
        const string ACTUATOR = "ACTUATOR";
        const string VENT = "VENT";
        const string DOOR = "DOOR";
        const string SENSOR = "SENSOR";
        const string TANK = "TANK";
        const string MAG_PLATE = "MAG_PLATE";
        const string EJECTOR = "EJECTOR";
        const string BATTERY = "BATTERY";
        const string CONTROL = "CONTROL";
        const string THRUST = "THRUST";
        const string TRANSPONDER = "TRANSPONDER";

        // TOGGLE BLOCK //
        public class ToggleBlock
        {
            public IMyTerminalBlock Block;
            public string ToggleType;
            public string BlockType;
            public bool IsInverted;
            public float ToggleValue;

            public ToggleBlock(IMyTerminalBlock block, string toggleData)
            {
                Block = block;
                IsInverted = false;
                //ToggleValue = toggleValue;

                string data = toggleData.ToUpper().Trim();
                string[] blockData = Block.GetType().ToString().Split('.');
                BlockType = blockData[blockData.Length - 1].Trim();

                //_statusMessage += block.CustomName + ": " + BlockType + ".\n";

                switch (data)
                {
                    case "":
                        MakeNormal();
                        break;
                    case "OFF":
                        MakeNormal(true);
                        break;
                    case "PRESSURIZED":
                    case "DEPRESSURIZED":
                        MakeVent(data);
                        break;
                    case "OPEN":
                    case "CLOSED":
                        MakeDoor(data);
                        break;
                    case "DETECTED":
                    case "NOT DETECTED":
                        MakeSensor(data);
                        break;
                    case "LOCKED":
                    case "AUTOLOCK":
                        MakeMagPlate(data);
                        break;
                    case "RECHARGE":
                    case "CHARGED":
                        MakeBattery(data);
                        break;
                    case "THROW OUT":
                    case "COLLECT ALL":
                        MakeEjector(data);
                        break;
                    case "STOCKPILE":
                    case "FULL":
                        MakeTank(data);
                        break;
                    case "BUSY":
                        MakeControl();
                        break;
                    case "FREE":
                        MakeControl(true);
                        break;
                    default:
                        if (BlockType == "MyThrust")
                            MakeThrust(data);
                        else if (BlockType == "MyTransponderBlock")
                            MakeTransponder(data);
                        else
                            MakeActuator(data);
                        break;

                }
            }

            // MAKE NORMAL
            void MakeNormal(bool invert = false)
            {
                ToggleType = NORMAL;
                IsInverted = invert;
            }
            
            // MAKE CONTROL
            void MakeControl(bool invert = false)
            {
                switch(BlockType)
                {
                    case "MyRemoteControl":
                    case "MySearchlight":
                    case "MyTurretControlBlock":
                    case "MyLargeInteriorTurret":
                    case "MyLargeGatlingTurret":
                    case "MyLargeMissileTurret":
                        ToggleType = CONTROL;
                        IsInverted = invert;
                        break;
                    default:
                        MakeNormal();
                        break;
                }
            }

            // CONTROL STATE
            bool ControlState()
            {
                bool state;

                switch(BlockType)
                {
                    case "MyLargeInteriorTurret":
                    case "MyLargeGatlingTurret":
                    case "MyLargeMissileTurret":
                    case "MySearchlight":
                        state = (Block as IMyLargeTurretBase).IsUnderControl;
                        break;
/*                    case "MyTurretControlBlock":
#pragma warning disable ProhibitedMemberRule // Prohibited Type Or Member
                        state = (Block as IMyTurretControlBlock).IsUnderControl;
#pragma warning restore ProhibitedMemberRule // Prohibited Type Or Member
                        break;*/
                    case "MyRemoteControl":
                        state = (Block as IMyRemoteControl).IsUnderControl;
                        break;
                    default:
                        state = false;
                        break;
                }

                return state;
            }

            // MAKE THRUSTER
            void MakeThrust(string data)
            {
                if(BlockType != "MyThrust")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = THRUST;

                if (data.StartsWith(">") && data.Length > 1)
                {
                    ToggleValue = ParseFloat(data.Substring(1), 0);
                }
                else if (data.StartsWith("<") && data.Length > 1)
                {
                    ToggleValue = ParseFloat(data.Substring(1), 0);
                    IsInverted = true;
                }
                else
                {
                    MakeNormal();
                }
            }

            // THRUSTER STATE
            bool ThrusterState()
            {
                IMyThrust thruster = Block as IMyThrust;

                if (IsInverted)
                    return thruster.ThrustOverride < ToggleValue;
                else
                    return thruster.ThrustOverride > ToggleValue;
            }

            // MAKE ACTUATOR
            void MakeActuator(string data)
            {
                if (BlockType != "MyExtendedPistonBase" && BlockType != "MyMotorStator" && BlockType != "MyMotorAdvancedStator")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = ACTUATOR;

                if (data.StartsWith(">") && data.Length > 1)
                {
                    ToggleValue = ParseFloat(data.Substring(1), 0);
                }
                else if (data.StartsWith("<") && data.Length > 1)
                {
                    ToggleValue = ParseFloat(data.Substring(1), 0);
                    IsInverted = true;
                }
                else
                {
                    MakeNormal();
                }
            }


            // ACTUATOR STATE //
            bool ActuatorState()
            {
                bool state = false;

                if (IsInverted)
                {
                    if (BlockType == "MyMotorStator" || BlockType == "MyMotorAdvancedStator")
                        state = ToHalfCircle(ToDegrees((Block as IMyMotorStator).Angle)) <= ToggleValue;
                    else if (BlockType == "MyExtendedPistonBase")
                        state = (Block as IMyPistonBase).CurrentPosition <= ToggleValue;
                }
                else
                {
                    if (BlockType == "MyMotorStator" || BlockType == "MyMotorAdvancedStator")
                        state = ToHalfCircle(ToDegrees((Block as IMyMotorStator).Angle)) >= ToggleValue;
                    else if (BlockType == "MyExtendedPistonBase")
                        state = (Block as IMyPistonBase).CurrentPosition >= ToggleValue;
                }

                return state;
            }

            // MAKE VENT
            void MakeVent(string data)
            {
                if (BlockType != "MyAirVent")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = VENT;

                if (data == "DEPRESSURIZED")
                    IsInverted = true;
            }

            // VENT STATE
            bool VentState()
            {
                bool state = (Block as IMyAirVent).GetOxygenLevel() >= THRESHHOLD;

                if (IsInverted)
                    state = !state;

                return state;
            }

            // MAKE DOOR
            void MakeDoor(string data)
            {
                if (BlockType != "MyDoor" && BlockType != "MyAirtightHangarDoor" && BlockType != "MyAirtightSlideDoor")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = DOOR;

                if (data == "CLOSED")
                    IsInverted = true;
            }

            // DOOR STATUS
            bool DoorState()
            {
                bool state = (Block as IMyDoor).OpenRatio > 0;

                if (IsInverted)
                    state = !state;

                return state;
            }

            // MAKE SENSOR
            void MakeSensor(string data)
            {
                if (BlockType != "MySensorBlock")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = SENSOR;

                if (data == "NOT DETECTED")
                    IsInverted = true;
            }

            // SENSOR STATE
            bool SensorState()
            {
                bool state = (Block as IMySensorBlock).IsActive;

                if (IsInverted)
                    state = !state;

                return state;
            }

            // MAKE EJECTOR
            void MakeEjector(string data)
            {
                if (BlockType != "MyShipConnector")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = EJECTOR;

                if (data == "COLLECT ALL")
                    IsInverted = true;
            }

            // EJECTOR STATE
            bool EjectorState()
            {
                IMyShipConnector ejector = Block as IMyShipConnector;

                if (IsInverted)
                    return ejector.CollectAll;
                else
                    return ejector.ThrowOut;
            }

            // MAKE MAG PLATE
            void MakeMagPlate(string data)
            {
                if (BlockType != "MyLandingGear" && BlockType != "MyShipConnector")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = MAG_PLATE;

                if (data == "AUTOLOCK")
                {
                    if (BlockType == "MyShipConnector")
                        _statusMessage += "Warning: \"AUTOLOCK\" is not a valid parameter for Connector Blocks!";
                    else
                        IsInverted = true;
                }
            }

            // MAG PLATE STATE
            bool MagPlateState()
            {
                if (BlockType == "MyShipConnector")
                {
                    IMyShipConnector connector = Block as IMyShipConnector;
                    return connector.Status == MyShipConnectorStatus.Connected;
                }
                else
                {
                    IMyLandingGear magPlate = Block as IMyLandingGear;


                    if (IsInverted)
                        return magPlate.AutoLock;
                    else
                        return magPlate.IsLocked;
                }
            }

            // MAKE BATTERY
            void MakeBattery(string data)
            {
                if (BlockType != "MyBatteryBlock" && BlockType != "MyJumpDrive")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = BATTERY;

                if (data == "CHARGED") // Treat Charged Batter/Jump Drive as Inverted case
                    IsInverted = true;
            }

            // BATTERY STATE
            bool BatteryState()
            {
                if (BlockType == "MyBatteryBlock")
                {
                    IMyBatteryBlock battery = Block as IMyBatteryBlock;

                    if (IsInverted)
                    {
                        float charge = battery.CurrentStoredPower / battery.MaxStoredPower;
                        return charge > THRESHHOLD;
                    }
                    else
                    {
                        return battery.ChargeMode == ChargeMode.Recharge;
                    }

                }
                else if (BlockType == "MyJumpDrive")
                {
                    IMyJumpDrive jumpDrive = Block as IMyJumpDrive;

                    if (IsInverted)
                    {
                        float jump = jumpDrive.CurrentStoredPower / jumpDrive.MaxStoredPower;
                        return jump > THRESHHOLD;
                    }
                    else
                    {
                        return jumpDrive.Recharge;
                    }
                }

                return false;
            }

            // MAKE TANK
            void MakeTank(string data)
            {
                if (BlockType != "MyGasTank")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = TANK;

                if (data == "FULL") // Treat full tank as inverted case
                    IsInverted = true;
            }

            // TANK STATE
            bool TankState()
            {
                IMyGasTank tank = Block as IMyGasTank;

                if (IsInverted) // Light if full
                    return tank.FilledRatio > THRESHHOLD;
                else
                    return tank.Stockpile;
            }


            // MAKE TRANSPONDER
            public void MakeTransponder(string data)
            {
                if (BlockType != "MyTransponderBlock")
                {
                    MakeNormal();
                    _statusMessage += "INVALID BLOCK TYPE:\n" + BlockType + "\n";
                    return;
                }

                ToggleType = TRANSPONDER;
                ToggleValue = ParseInt(data,0);
                _statusMessage += "TOGGLE VALUE = " + ToggleValue + "\n";
            }


            // TRANSPONDER STATE
            bool TransponderState()
            {
                IMyTransponder transponder = Block as IMyTransponder;

                return (transponder.Channel == (int)ToggleValue);
            }


            // IS ACTIVE
            public bool IsActive()
            {
                bool active;

                switch (ToggleType)
                {
                    case NORMAL:
                        active = Block.IsWorking;
                        break;
                    case ACTUATOR:
                        active = ActuatorState();
                        break;
                    case VENT:
                        active = VentState();
                        break;
                    case DOOR:
                        active = DoorState();
                        break;
                    case SENSOR:
                        active = SensorState();
                        break;
                    case TANK:
                        active = TankState();
                        break;
                    case MAG_PLATE:
                        active = MagPlateState();
                        break;
                    case EJECTOR:
                        active = EjectorState();
                        break;
                    case BATTERY:
                        active = BatteryState();
                        break;
                    case CONTROL:
                        active = ControlState();
                        break;
                    case THRUST:
                        active = ThrusterState();
                        break;
                    case TRANSPONDER:
                        active = TransponderState();
                        break;
                    default:
                        active = IsInverted;
                        break;
                }

                if (IsInverted)
                    active = !active;

                return active;
            }
        }
    }
}
