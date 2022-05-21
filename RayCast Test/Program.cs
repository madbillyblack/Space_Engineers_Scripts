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
    partial class Program : MyGridProgram
    {
        const double SCAN_RANGE = 20;
        const string LCD_NAME = "LCD Panel";

        const float MIN_ANGLE = -45f;
        const float MAX_ANGLE = 45f;

        float _x = MIN_ANGLE;
        float _y = MIN_ANGLE;
        const float STEP = 1f;


        IMyTextPanel lcd;
        List<IMyCameraBlock> cams;
        List<MyDetectedEntityInfo> foundObjects;
        MyDetectedEntityInfo info;

        public Program()
        {
            cams = new List<IMyCameraBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(cams);
            if(cams.Count > 0)
            {
                foreach (IMyCameraBlock camera in cams)
                    camera.EnableRaycast = true;
            }


            info = new MyDetectedEntityInfo();
            foundObjects = new List<MyDetectedEntityInfo>();

            lcd = GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;
            if(lcd != null)
                lcd.ContentType = ContentType.TEXT_AND_IMAGE;


            if (Me.CustomData.ToLower().Contains("fast"))
                Runtime.UpdateFrequency = UpdateFrequency.Update1;
            else
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save(){}

        public void Main(string argument, UpdateType updateSource)
        {
            if (lcd == null)
                Echo("NO LCD PANEL FOUND!");

            Echo("Camera Count: " + cams.Count);
            Echo("Scan Angle\n  X:" + _x + "\n  Y:" + _y);

            if (lcd == null || cams.Count < 1)
                return;

            foreach(IMyCameraBlock camera in cams)
            {
                if(cams[0].CanScan(SCAN_RANGE))
                {
                    info = camera.Raycast(SCAN_RANGE, _y, _x);

                    if(!info.IsEmpty())
                    {
                        if(info.EntityId != Me.CubeGrid.EntityId)
                        {
                            bool inList = false;

                            if(foundObjects.Count > 0)
                            {
                                for(int k = 0; k< foundObjects.Count; k++)
                                {
                                    if(foundObjects[k].EntityId == info.EntityId)
                                    {
                                        inList = true;
                                        foundObjects.RemoveAt(k);
                                        foundObjects.Add(info);

                                    }
                                }
                            }

                            if (!inList)
                                foundObjects.Add(info);

                        }
                    }

                    _x += STEP;

                    if(_x > MAX_ANGLE)
                    {
                        _x = MIN_ANGLE;
                        _y += STEP;
                    }

                    if(_y > MAX_ANGLE)
                    {
                        _x = MIN_ANGLE;
                        _y = MIN_ANGLE;
                    }
                }
            }

            Display();
        }

        void Display()
        {
            lcd.WriteText("Scan Range = " + SCAN_RANGE);
            lcd.WriteText("\n\rAvailable Scan Range = " + cams[0].AvailableScanRange + " Meters", true);
            lcd.WriteText("\n\rTime Until Scan = " + cams[0].TimeUntilScan(SCAN_RANGE)/1000 + " Seconds", true);
            lcd.WriteText("\n\rX Angle = " + _x, true);
            lcd.WriteText("\n\rY Angle = " + _y, true);
            lcd.WriteText("\n\n\rFound Objects:", true);

            if(foundObjects.Count > 0)
            {
                foreach(MyDetectedEntityInfo foundObject in foundObjects)
                {
                    lcd.WriteText("\n\rObject Type = " + foundObject.Type, true);
                    lcd.WriteText("\n\rObject Name =" + foundObject.Name, true);
                    lcd.WriteText("\n\rObject Position = " + foundObject.Position, true);
                }
            }
        }
    }
}
