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
        const string SCAN_TAG = "[Scan Cam]";
        const float DV_SCAN = 100;
        const string RANGE_KEY = "Scan Range";
    
        IMyCameraBlock _scanCamera;
        static bool _scannerActive;
        float _scanRange; // In meters
        string _scanPlanet; // Name of planet that's being scanned
        




        // SET SCAN CAMERA //
        void SetScanCamera()
        {
            _scannerActive = false;

            List<IMyCameraBlock> cameras = new List<IMyCameraBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(cameras);

            if (cameras.Count < 1)
            {
                _scanCamera = null;
                return;
            }

            foreach(IMyCameraBlock camera in cameras)
            {
                if(camera.CustomName.Contains(SCAN_TAG) && GetKey(camera, SHARED, GRID_KEY, _gridID) == _gridID)
                {
                    _scanCamera = camera;
                    _scanRange = ParseFloat(GetKey(camera, PROGRAM_HEAD, RANGE_KEY, DV_SCAN.ToString()), DV_SCAN) * 1000;

                    return;
                }
            }

            _scanRange = 0;
            _scanCamera = null;
        }


        // DISPLAY SCAN DATA //
        void DisplayScanData()
        {
            // Print title and flasher
            string flasher;

            if (_scannerActive && _lightOn)
                flasher = " - SCANNING -";
            else
                flasher = "";

            Echo("PLANET SCANNER" + flasher);

            if(_scanCamera == null)
            {
                Echo("  Inoperable: No Scan Camera Specified.\n");
            }
            else if(_scannerActive)
            {
                string planetToScan;

                if (_scanPlanet == _activePlanet)
                    planetToScan = "* Rescanning Planet:\n    ";
                else
                    planetToScan = "* Scanning New Planet:\n    ";

                Echo(planetToScan + _scanPlanet);
                Echo("* " + _scanCamera.CustomName
                    + "\n  - Range: " + (_scanRange / 1000).ToString("N1") + "km");

                string countdown;
                int milliseconds = _scanCamera.TimeUntilScan(_scanRange);

                TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);
                if (milliseconds < 1)
                {
                    // Make READY flash opposite of SCANNING
                    if (_lightOn)
                        countdown = "  -";
                    else
                        countdown = "  - READY";
                }
                else
                    countdown = "  - Ready in: " + time.ToString(@"hh\:mm\:ss");//should print time in format "00:00:00"

                Echo(countdown + "\n");
            }
            else
            {
                Echo("  Inactive\n");
            }
        }


        // SCAN PLANET //
        void ScanPlanet(string planetName)
        {
            if(_scanCamera == null)
            {
                _statusMessage += "No Scan Camera Specified!";
                return;
            }
            else if(string.IsNullOrEmpty(planetName))
            {
                _statusMessage += "No PLANET NAME specified for SCAN!";
            }
            else if(_scannerActive)
            {
                CastRay(planetName);
            }
            else
            {
                _scannerActive = true;
                _scanPlanet = planetName;
                _scanCamera.EnableRaycast = true;

                Planet planet = GetPlanet(planetName);

                if (planet != null)
                    _activePlanet = planet.name;
            }
        }


        // CAST RAY //
        void CastRay(string planetName)
        {
            MyDetectedEntityInfo entity = _scanCamera.Raycast(_scanRange, 0, 0);

            if(entity.IsEmpty() || entity.Type.ToString().ToUpper() != "PLANET")
            {

            }
            else if(_scanPlanet == _activePlanet)
            {
                UpdatePlanetFromCast(entity);
            }
            else
            {
                NewPlanetFromCast(entity);
            }
        }


        // NEW PLANET FROM CAST //
        void NewPlanetFromCast(MyDetectedEntityInfo planetInfo)
        {
            DisplayScannedPlanet(planetInfo, true);

            Vector3D? contact = planetInfo.HitPosition;

            if (contact == null)
            {
                _statusMessage += "Hip Position Error\n";
                return;
            }

            Vector3D samplePoint = (Vector3D)contact;
            Vector3D center = planetInfo.Position;

            double radius = Vector3D.Distance(samplePoint, center);
            string planetString = _scanPlanet + ";" + Vector3ToString(center) + ";" + ((int)radius).ToString() + ";GRAY;;;;;0";

            _planetList.Add(new Planet(planetString));

            DisableScanner();
            DataToLog();
        }


        // UPDATE PLANET FROM CAST //
        void UpdatePlanetFromCast(MyDetectedEntityInfo planetInfo)
        {
            
            DisplayScannedPlanet(planetInfo, false);

            // TODO - Update Planet Parameters

            DataToLog();
            DisableScanner();
        }


        // DISPLAY SCANNED PLANET //
        void DisplayScannedPlanet(MyDetectedEntityInfo planetInfo, bool newPlanet)
        {
            if(newPlanet)
                _statusMessage += "New Planet Logged:\n  " + _scanPlanet;
            else
                _statusMessage += "Planet Updated:\n  " + _scanPlanet;
        }


        // DISABLE SCANNER //
        void DisableScanner()
        {
            _scannerActive = false;
            _scanPlanet = "";
            _scanCamera.EnableRaycast = false;
        }


        // SET SCAN DISTANCE //
        void SetScanRange(string arg)
        {
            _scanRange = ParseFloat(arg, DV_SCAN) * 1000;
            
            if (_scanRange < 0)
                _scanRange *= -1;
        }
    }
}
