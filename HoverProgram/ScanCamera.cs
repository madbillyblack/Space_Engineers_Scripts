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
        public static ScanCamera _scanCamera;
        public bool _scanningEnabled;

        public class ScanCamera
        {
            public IMyCameraBlock Block;
            List<MyDetectedEntityInfo> FoundObjects;
            //MyDetectedEntityInfo Info;
            public double ScanRange {  get; set; }

            public ScanCamera(IMyCameraBlock camera)
            {
                Block = camera;
                Block.EnableRaycast = true;
                FoundObjects = new List<MyDetectedEntityInfo>();
                //Info = new MyDetectedEntityInfo();
            }

            public double DetectHeight()
            {
                try
                {
                    if(Block.CanScan(ScanRange))
                    {
                        Vector3? hitPoint = Block.Raycast(ScanRange, 0, 0).HitPosition;

                        double dist = Vector3.Distance((Vector3) hitPoint, Block.GetPosition());

                        return dist * GravityCos();
                    }
                    return ScanRange;
                }
                catch
                {
                    return ScanRange;
                }
            }
        }

        public void AddScanCamera()
        {
            List<IMyCameraBlock> cameras = new List<IMyCameraBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(cameras);

            if(cameras.Count > 0 )
            {
                foreach(IMyCameraBlock camera in cameras)
                {
                    if(camera.CustomName.Contains(MAIN_TAG) && SameGridID(camera))
                    {
                        _scanCamera = new ScanCamera(camera);
                        _scanningEnabled = true;
                        return;
                    }
                }
            }

            _scanCamera = null;
            _scanningEnabled = false;
        }


        // SET SCAN RANGE //
        public static void SetScanRange()
        {
            if (_scanCamera == null)
                return;

            _scanCamera.ScanRange = _hoverHeight * SCAN_MOD;
            if (_scanCamera.ScanRange > SCAN_LIMIT)
                _scanCamera.ScanRange = SCAN_LIMIT;
        }


        // GRAVITY SIN //
        public static double GravityCos()
        {  
            // Get angle between cockpit and gravity vector (in rads)
            double angle = Vector3D.Angle(_cockpit.GetNaturalGravity(), _cockpit.WorldMatrix.Down);
            return Math.Cos(angle);
        }
    }
}
