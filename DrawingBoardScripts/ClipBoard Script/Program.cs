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
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        IMyTextSurface _surface;
        List<IMyCameraBlock> _cameras;
        string _castData;
        double _castRange;
        int _counter = 0;

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
        }


        // SAVE //
        public void Save(){}
        #endregion

        #region
        // MAIN //
        public void Main(string argument, UpdateType updateSource)
        {
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
        #endregion



        #region
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
        #endregion
    }
}
