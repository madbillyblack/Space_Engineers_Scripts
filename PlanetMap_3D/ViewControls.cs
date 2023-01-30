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
		// View Control Constants
		const int ANGLE_STEP = 5; // Basic angle in degrees of step rotations.
		const int MAX_PITCH = 90; // Maximum (+/-) value of map pitch. [Not recommended above 90]
		const int MOVE_STEP = 5000; // Step size for translation (move) commands.
		const float ZOOM_STEP = 1.5f; // Factor By which map is zoomed in and out (multiplied).
		const int ZOOM_MAX = 1000000000; // Max value for Focal Length
		const float BRIGHTNESS_STEP = 0.25f;


		// ZOOM MAPS //
		void ZoomMaps(List<StarMap> maps, string direction)
		{
			if (NoMaps(maps))
				return;

			bool zoomIn = direction == "IN";

			foreach (StarMap map in maps)
				map.Zoom(zoomIn);

		}


		// ADJUST RADII //
		void AdjustRadii(List<StarMap> maps, bool increase)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				map.AdjustRadius(increase);
			}
		}


		// MOVE MAPS //
		void MoveMaps(List<StarMap> maps, string direction)
		{
			if (NoMaps(maps))
				return;



			foreach (StarMap map in maps)
			{
				map.Move(direction);
			}
		}


		// TRACK MAPS //
		void TrackMaps(List<StarMap> maps, string direction)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				map.Track(direction);
			}
		}


		// ROTATE MAPS //
		void RotateMaps(List<StarMap> maps, string direction)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				map.Rotate(direction);
			}
		}


		// SPIN MAPS //		Adjust azimuth speed of maps.
		void SpinMaps(List<StarMap> maps, string direction)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				map.Spin(direction);
			}
		}


		// STOP MAPS //		Halt all translational and azimuthal speed in maps.
		void StopMaps(List<StarMap> maps)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				map.Stop();
			}
		}


		// MAPS TO SHIP //		Centers maps on ship
		void MapsToShip(List<StarMap> maps)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				map.Center = _myPos;
			}
		}


		// CENTER WORLD //	   Updates Map Center to the Average of all charted Planets
		void CenterWorld(StarMap map)
		{
			map.Altitude = -15;
			map.Azimuth = 45;
			map.FocalLength = 256;
			map.RotationalRadius = 4194304;
			Vector3 worldCenter = new Vector3(0, 0, 0);

			if (_planets)
			{
				foreach (Planet planet in _planetList)
				{
					worldCenter += planet.position;
				}

				worldCenter /= _planetList.Count;
			}

			map.Center = worldCenter;
		}


		// CENTER SHIP //
		void CenterShip(StarMap map)
		{
			map.DefaultView();
			map.Center = _myPos;

			map.SetMapKey(CENTER_KEY, Vector3ToString(map.Center));
		}


		// ALIGN SHIP //
		void AlignShip(StarMap map)
		{
			Vector3 heading = _refBlock.WorldMatrix.Forward;
			int newAz = DegreeAdd((int)ToDegrees((float)Math.Atan2(heading.Z, heading.X)), -90);

			int newAlt = (int)ToDegrees((float)Math.Asin(heading.Y));// -25;
			if (newAlt < -90)
			{
				newAlt = DegreeAdd(newAlt, 180);
				newAz = DegreeAdd(newAz, 180);
			}

			map.Altitude = newAlt;
			map.Azimuth = newAz;
			map.Center = _myPos;
		}


		// ALIGN ORBIT //
		void AlignOrbit(StarMap map)
		{
			if (_planetList.Count < 1)
			{
				return;
			}

			if (map.ActivePlanet == null)
			{
				if (_nearestPlanet == null)
				{
					Echo("No Nearest Planet Set!");
					return;
				}

				//SelectPlanet(_nearestPlanet, map);
				map.SetActivePlanet(_nearestPlanet);
			}

			Vector3 planetPos = map.ActivePlanet.position;

			map.Center = (_myPos + planetPos) *0.75f;
			//map.Center = _myPos;

			map.Altitude = 0;
			Vector3 orbit = _myPos - planetPos;
			map.Azimuth = (int)ToDegrees((float)Math.Abs(Math.Atan2(orbit.Z, orbit.X) + PI * 0.75f)); //  )

			//Get largest component distance between ship and planet.
			//double span = Math.Sqrt(orbit.LengthSquared() - Math.Pow(orbit.Y,2));
			float span = orbit.Length();

			map.FocalLength = DV_FOCAL;
			double newRadius = 1.25f * map.FocalLength * span / map.Viewport.Height * map.ZoomMod;

			if (newRadius > MAX_VALUE || newRadius < 0)
			{
				newRadius = MAX_VALUE;
				double newZoom = 0.8f * map.Viewport.Height * (MAX_VALUE / span);
				map.FocalLength = (int)newZoom;
			}

			map.RotationalRadius = (int)newRadius;

			//map.UpdateBasicParameters();
		}


		// PLANET MODE //
		void PlanetMode(StarMap map)
		{
			map.Mode = "PLANET";
			map.RotationalRadius = DV_RADIUS;
			map.FocalLength = DV_FOCAL;

			if (map.Viewport.Width > 500)
			{
				map.FocalLength *= 4;
			}

			if (_planets)
			{
				SortByNearest(_planetList);
				map.ActivePlanet = _planetList[0];
				ShipToPlanet(map);

				if (map.ActivePlanet.radius < 30000)
				{
					map.FocalLength *= 4;
				}
			}

			map.Stop();

			SetKey(map.Block, map.Header, ZOOM_KEY, map.FocalLength.ToString());
			SetKey(map.Block, map.Header, RADIUS_KEY, map.RotationalRadius.ToString());
			SetKey(map.Block, map.Header, PLANET_KEY, map.ActivePlanetName);
		}


		// SET MAP MODE //
		void SetMapMode(StarMap map, string mapMode)
		{
			if (mapMode == "WORLD")
			{
				CenterWorld(map);
			}
			else
			{
				CenterShip(map);
			}

			if (mapMode == "PLANET")
			{
				PlanetMode(map);
			}
			else if (mapMode == "ORBIT")
			{
				AlignOrbit(map);
			}
			else if (mapMode == "CHASE")
			{
				AlignShip(map);
			}

			map.Mode = mapMode;
			map.SetMapKey(MODE_KEY, mapMode);
		}


		// CHANGE MODE //
		void ChangeMode(string mapMode, List<StarMap> maps)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				SetMapMode(map, mapMode);
			}
		}


		// CYCLE MODE //
		void CycleMode(StarMap map, bool cycleUp)
        {
			_activePlanet = "";
			string[] modes = { "FREE", "SHIP", "CHASE", "PLANET", "ORBIT", "WORLD" };
			int length = modes.Length;

			int modeIndex = 0;
			for (int i = 0; i < length; i++)
			{
				// Find Current Map Mode
				if (map.Mode.ToUpper() == modes[i])
				{
					modeIndex = i;
				}
			}

			// Cycle Mode Up/Down by 1
			if (cycleUp)
			{
				modeIndex++;
			}
			else
			{
				modeIndex--;
			}

			if (modeIndex >= length)
			{
				modeIndex = 0;
			}
			else if (modeIndex < 0)
			{
				modeIndex = length - 1;
			}

			SetMapMode(map, modes[modeIndex]);
		}

		void CycleModeForList(List<StarMap> maps, bool cycleUp)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				CycleMode(map, cycleUp);
			}
		}


		// SHIP TO PLANET //   Aligns the map so that the ship appears above the center of the planet.
		void ShipToPlanet(StarMap map)
		{
			if (_planets)
			{
				Planet planet = _nearestPlanet;

				Vector3 shipVector = _myPos - planet.position;
				float magnitude = Vector3.Distance(_myPos, planet.position);

				float azAngle = (float)Math.Atan2(shipVector.Z, shipVector.X);
				float altAngle = (float)Math.Asin(shipVector.Y / magnitude);

				map.Center = planet.position;
				map.Azimuth = DegreeAdd((int)ToDegrees(azAngle), 90);
				map.Altitude = (int)ToDegrees(-altAngle);
			}
		}


		// MAPS TO DEFAULT //
		void MapsToDefault(List<StarMap> maps)
		{
			if (maps.Count < 1)
				return;

			foreach (StarMap map in maps)
			{
				map.DefaultView();
			}
		}


		// BRIGHTEN MAPS //
		void BrightenMaps(List<StarMap> maps, bool increaseBrightness)
        {
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
				map.Brighten(increaseBrightness);
        }
	}
}
