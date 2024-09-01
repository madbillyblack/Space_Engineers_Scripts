using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
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

        IMyTextSurface _outbox;
        List<IMyCameraBlock> _cameras;
        string _castData;
        double _castRange;
        int _counter = 0;
        IMyInventory _inventory;
        List<IMyTerminalBlock> _blocks;
        List<ITerminalAction> _actions;

        MyIni _ini;

        IMySensorBlock _sensor;
        IMyCockpit _cockpit;

        // Broadcast variables
        int _runcount = 0;
        string _broadCastTag = "MDK IGC EXAMPLE 1";
        const string EXCLUDE = "EXCLUDE";
        IMyBroadcastListener _myBroadcastListener;
        IMyConveyorSorter _sorter;




        #region
        // PRORGRAM //
        public Program()
        {
            List<IMyCockpit> cockpits = new List<IMyCockpit>();


            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(cockpits);
            if(cockpits.Count > 0)
                _cockpit = cockpits.First();
            else
                _cockpit = null;

            _ini = GetIni(Me);
            _surface = Me.GetSurface(0);
            _surface.ContentType = ContentType.TEXT_AND_IMAGE;
            _outbox = GetDefaultSurface();
            

            List<IMyConveyorSorter> sorters = new List<IMyConveyorSorter>();
            GridTerminalSystem.GetBlocksOfType<IMyConveyorSorter>(sorters);
            if(sorters.Count > 0)
                _sorter = sorters.First();


            /*
            // Broadcast INIT
            Echo("Creator");
            _myBroadcastListener = IGC.RegisterBroadcastListener(_broadCastTag);
            _myBroadcastListener.SetMessageCallback(_broadCastTag);

            _blocks = new List<IMyTerminalBlock>();
            _actions = new List<ITerminalAction>();

            List<IMyTerminalBlock> tempBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(tempBlocks);
            foreach(IMyTerminalBlock tempBlock in tempBlocks)
            {
                if(!tempBlock.CustomName.ToUpper().Contains(EXCLUDE))
                    _blocks.Add(tempBlock);
            }



            
                        _cameras = new List<IMyCameraBlock>();
                        GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(_cameras);

                        _castData = "";
                        Runtime.UpdateFrequency = UpdateFrequency.Update1;

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
            */

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }


        // SAVE //
        public void Save(){}
        #endregion

        #region NEW MAIN
        public void Main(string argument, UpdateType updateSource)
        {
            //List<IMyTerminalAction> actions = new List<IMyTerminalAction>();
            //_surface.WriteText("");

            List<MyInventoryItemFilter> filters = new List<MyInventoryItemFilter>();


            if (_sorter != null)
                _sorter.GetFilterList(filters);


            if(argument == "ADD_ICE")
            {
                filters.Add(new MyInventoryItemFilter("MyObjectBuilder_Ore/Ice"));
                _sorter.SetFilter(MyConveyorSorterMode.Whitelist, filters);
            }
            else if(argument == "REMOVE_ICE")
            {
                _sorter.RemoveItem(new MyInventoryItemFilter("MyObjectBuilder_Ore/Ice"));
            }
            else if (argument == "ADD_ORE")
            {
                filters.Add(new MyInventoryItemFilter("MyObjectBuilder_Ore/(null)"));
                _sorter.SetFilter(MyConveyorSorterMode.Whitelist, filters);
            }
            else if (argument == "REMOVE_ORE")
            {
                _sorter.RemoveItem(new MyInventoryItemFilter("MyObjectBuilder_Ore/(null)"));
            }

            if (filters.Count < 1)
            {
                _surface.WriteText("No Filters");
                return;
            }

            string output = "";

            foreach(MyInventoryItemFilter filter in filters)
            {
                output += filter.ItemType.ToString() + "\n";
            }

            _surface.WriteText(output);
            //PrintBlockTypes();

            /*
            foreach (IMyTerminalBlock block in _blocks)
            {
                _surface.WriteText("--- " + block.CustomName + " ---\n", true);
                _surface.WriteText(block.DefinitionDisplayNameText + "\n" , true);
                _surface.WriteText(block.BlockDefinition.ToString() + "\n", true);
                _surface.WriteText(block.GetType().ToString() + "\n", true);


                _actions.Clear();

                block.GetActions(_actions);

                foreach (ITerminalAction action in _actions)
                    _surface.WriteText(" * " + action.ToString() + "\n", true);
            }



            _runcount++;
            Echo(_runcount.ToString() + ":" + updateSource.ToString());

            if (
                (updateSource & (UpdateType.Trigger | UpdateType.Terminal)) > 0
                || (updateSource & (UpdateType.Mod)) > 0
                || (updateSource & (UpdateType.Script)) > 0
                )
            {
                if (argument != "")
                {
                    IGC.SendBroadcastMessage(_broadCastTag, argument);
                    Echo("Sending message:\n" + argument);
                }
            }

            if ((updateSource & UpdateType.IGC) > 0)
            {
                while (_myBroadcastListener.HasPendingMessage)
                {
                    MyIGCMessage myIGCMessage = _myBroadcastListener.AcceptMessage();
                    if (myIGCMessage.Tag == _broadCastTag)
                    { // This is our tag
                        if (myIGCMessage.Data is string)
                        {
                            string str = myIGCMessage.Data.ToString();
                            Echo("Received IGC Public Message");
                            Echo("Tag=" + myIGCMessage.Tag);
                            Echo("Data=" + myIGCMessage.Data.ToString());
                            Echo("Source=" + myIGCMessage.Source.ToString("X"));

                            _surface.WriteText(str);
                        }
                        else // if(msg.Data is XXX)
                        {
                        }
                    }
                    else
                    {
                    }
                }
            }
            else if (_outbox != null)
            {
                
                string screentext = _outbox.GetText();
                Echo("OUT BOX:\n" + screentext);
                IGC.SendBroadcastMessage(_broadCastTag, screentext);
            }
            */
        }
        #endregion


        #region OLD MAIN
        // MAIN //

        /*
        public void Main(string argument, UpdateType updateSource)
        {
            if (_cockpit == null)
            {
                Echo("No Cockpit");
                return;
            }

            double elevation;
            if (_cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation))
                Echo("Elevation: " + elevation);
                //_surface.WriteText("Elevation: " + elevation);


            //PrintBlockTypes();
            //PrintEntityIDs();
            //PrintDetected();
            //PrintFubar();
            //PrintSubTypes();
            //PrintScreenSize();




            
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
                        
        }
    */
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

        // PRINT SUB TYPES //
        void PrintSubTypes()
        {
            string output = "";

            foreach (IMyTerminalBlock block in _blocks)
            {
                output += block.CustomName + "\n" + block.BlockDefinition.SubtypeName + "\n\n";
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

        void PrintFubar()
        {
            string fubar = _ini.Get("FUBAR", "SNAFU").ToString();

            _surface.WriteText("SITREP: " + fubar);
        }

        void PrintScreenSize()
        {
            string output = "";
            foreach (IMyTerminalBlock block in _blocks)
            {
                int screenCount;
                IMyTextSurfaceProvider screenBlock;

                try
                {
                    screenBlock = block as IMyTextSurfaceProvider;
                    screenCount = screenBlock.SurfaceCount;
                }
                catch
                {
                    screenCount = 0;
                    screenBlock = null;
                }

                if (screenCount > 0)
                {
                    output += block.CustomName + "\n";

                    for (int i = 0; i < screenCount; i++)
                    {
                        IMyTextSurface surface = screenBlock.GetSurface(i);
                        RectangleF viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);

                        output += i + ". " + viewport.Width + " x " + viewport.Height + "\n";
                    }

                    output += "\n";
                }
            }

            Echo(output);
            _surface.WriteText(output);
        }

        IMyTextSurface GetDefaultSurface()
        {
            try
            {
                List<IMyTextPanel> panels = new List<IMyTextPanel>();
                GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels);
                return panels.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}
