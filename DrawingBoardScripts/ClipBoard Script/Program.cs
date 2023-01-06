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
        IMyTextSurface _surface;
        List<IMyCameraBlock> _cameras;
        string _castData;
        double _castRange;
        int _counter = 0;
        IMyInventory _inventory;
        List<IMyTerminalBlock> _blocks;

        IMySensorBlock _sensor;

        #region
        // PRORGRAM //
        public Program()
        {
            _surface = Me.GetSurface(0);
            _surface.ContentType = ContentType.TEXT_AND_IMAGE;

            _cameras = new List<IMyCameraBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(_cameras);

            _castData = "";
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            List<IMyTerminalBlock> inventoryBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName("Cargo", inventoryBlocks);

            _blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(_blocks);

            List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors);

            if (sensors.Count > 0)
                _sensor = sensors[0];
            else
                _sensor = null;


            if(inventoryBlocks.Count > 0 && inventoryBlocks[0].HasInventory)
            {
                _inventory = inventoryBlocks[0].GetInventory(0);
            }
            else
            {
                _inventory = null;
            }
        }


        // SAVE //
        public void Save(){}
        #endregion

        #region
        // MAIN //
        public void Main(string argument, UpdateType updateSource)
        {
            _surface.WriteText("");

            //PrintBlockTypes();
            //PrintEntityIDs();
            PrintDetected();





            /*
            if (_inventory == null)
                return;

                        List<MyInventoryItem> items = new List<MyInventoryItem>();
                        _inventory.GetItems(items);

                        if (items.Count < 1)
                        {
                            _surface.WriteText("<< EMPTY >>");
                            return;
                        }

                        foreach(MyInventoryItem item in items)
                        {
                            _surface.WriteText(item.Type.SubtypeId.ToString() + "\n", true);
                        }

                        _counter++;
                        // Clear surface from last run
                        _surface.WriteText(_counter.ToString() + "\n");

                        //PrintSpriteList();

                        if (_cameras.Count < 1)
                        { 
                            DisplayMessage("NO CAMERAS FOUND!");
                            return;
                        }

                        IMyCameraBlock camera = _cameras[0];
                        camera.EnableRaycast = true;

                        DisplayMessage(camera.CustomName);


                        double range = camera.RaycastDistanceLimit;
                        string rangeString;
                        if (range < 0)
                        {
                            rangeString = "INF";
                            _castRange = camera.AvailableScanRange;
                        }

                        else
                        {
                            rangeString = range.ToString("N1");
                            _castRange = range;
                        }

                        DisplayMessage("  Range: " + camera.AvailableScanRange.ToString("N1"));
                        DisplayMessage(" Time: " + camera.RaycastTimeMultiplier);


                        if(argument.ToUpper() == "CAST")
                        {
                            CastRay(camera);
                        }

                        DisplayMessage(_castData);
                        */
        }
        #endregion



        #region
        // PRINT BLOCK TYPES //
        void PrintBlockTypes()
        {
            string output = "";

            foreach(IMyTerminalBlock block in _blocks)
            {
                output += block.CustomName + "\n" + block.GetType().ToString() + "\n\n";
            }

            _surface.WriteText(output);
        }




        // CAST RAY //
        void CastRay(IMyCameraBlock camera)
        {
            UpdateCastData(camera.Raycast(_castRange, 0, 0));
        }


        void UpdateCastData(MyDetectedEntityInfo entity)
        {
            
            _castData = "TARGET INFO:\n";
            if (entity.IsEmpty())
            {
                _castData += "  Miss";
                return;
            }
                

           Vector3D? hitposition = entity.HitPosition;

            if (hitposition == null)
            {
                _castData = ("Ray Cast Error");
                return;
            }

           Vector3D center = entity.Position;
           double radius = Vector3D.Distance((Vector3D) hitposition, center);

            _castData += "  Name: " + entity.Name + "\n  Type: " + entity.Type.ToString() + "\n  Radius: " + radius.ToString("N1") +"\n  Center:\n    X:" + center.X + "\n    Y:" + center.Y + "\n    " + center.Z;
        }


        // PRINT SPRITE LIST //
        void PrintSpriteList()
        {
            List<string> sprites = new List<string>();
            _surface.GetSprites(sprites);

            _surface.WriteText("");
            foreach (string sprite in sprites)
                _surface.WriteText(sprite + "\n", true);
        }

        // DISPLAY MESSAGE //
        void DisplayMessage(string message)
        {
            Echo(message);
            _surface.WriteText(message + "\n", true);
        }

        void PrintEntityIDs()
        {
            string output = "";

            foreach (IMyTerminalBlock block in _blocks)
            {
                output += block.CustomName + "\n" + block.CubeGrid.EntityId.ToString() + "\n\n";
            }

            _surface.WriteText(output);
        }

        void PrintDetected()
        {
            string output;

            if (_sensor == null)
                output = "No Sensor Block Found!";
            else if (_sensor.IsActive)
                output = "DETECTED";
            else
                output = "Undetected";

            _surface.WriteText(output);                
        }
        #endregion
    }
}
