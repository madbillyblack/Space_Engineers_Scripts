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
        const float DV_SCAN = 10;
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
            if (!_scannerActive)
                return;
            // Print title and flasher
            string flasher;

            if (_lightOn)
                flasher = " - SCANNING -";
            else
                flasher = "";

            Echo("PLANET SCANNER" + flasher);

            if(_scanCamera == null)
            {
                Echo("  Inoperable: No Scan Camera Specified.\n");
            }
            else
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
        }


        // SCAN PLANET //
        void ScanPlanet(string planetName)
        {
            if(_scanCamera == null)
            {
                AddMessage("No Scan Camera Specified!");
                return;
            }
            else if(string.IsNullOrEmpty(planetName))
            {
                AddMessage("No PLANET NAME specified for SCAN!");
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
            MyDetectedEntityInfo planetInfo = _scanCamera.Raycast(_scanRange, 0, 0);

            if(planetInfo.IsEmpty() || planetInfo.Type.ToString().ToUpper() != "PLANET")
            {
                AddMessage("Scan Missed");
                return;
            }

            Vector3D? contact = planetInfo.HitPosition;

            if (contact == null)
            {
                AddMessage("Hit Position Error");
                return;
            }

            Vector3D samplePoint = (Vector3D)contact;
            Vector3D center = planetInfo.Position;
            double radius = Vector3D.Distance(samplePoint, center);


            if (_scanPlanet == _activePlanet)
                UpdatePlanetFromCast(planetInfo, center, (float) radius);
            else
                NewPlanetFromCast(planetInfo, center, (float)radius);

        }


        // NEW PLANET FROM CAST //
        void NewPlanetFromCast(MyDetectedEntityInfo planetInfo, Vector3D center, float radius)
        {

            string planetString = _scanPlanet + ";" + Vector3ToString(center) + ";" + radius.ToString("0.#") + ";GRAY;;;;;1";
            _planetList.Add(new Planet(planetString));

            DisplayScannedPlanet(planetInfo, true);
            DisableScanner();
            DataToLog();
        }


        // UPDATE PLANET FROM CAST //
        void UpdatePlanetFromCast(MyDetectedEntityInfo planetInfo, Vector3D center, float radius)
        {
            Planet planet = GetPlanet(_activePlanet);
            if(planet == null)
            {
                AddMessage("Rescanning Error for planet: " + _activePlanet);
                return;
            }

            // Account for old format error
            if (planet.SampleCount == 0)
                planet.SampleCount = 1;

            float oldRadius = planet.radius;
            float newRadius = ((oldRadius * planet.SampleCount) + radius) / (planet.SampleCount + 1);

            // Count new sample point
            planet.SampleCount++;

            float difference = (newRadius - oldRadius)/1000;
            
            DisplayScannedPlanet(planetInfo, false);
            AddMessage("Radius change of " + difference.ToString("0.#") + "km");
            DataToLog();
            DisableScanner();
        }


        // DISPLAY SCANNED PLANET //
        void DisplayScannedPlanet(MyDetectedEntityInfo planetInfo, bool newPlanet)
        {
            if(newPlanet)
                AddMessage("New Planet Logged:\n  " + _scanPlanet);
            else
                AddMessage("Planet Updated:\n  " + _scanPlanet);
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
