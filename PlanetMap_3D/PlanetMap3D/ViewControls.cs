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

		// ZOOM // Changes Focal Length of Maps. true => Zoom In / false => Zoom Out
		void Zoom(StarMap map, bool zoomIn)
        {
			int doF = map.FocalLength;
			float newScale;

			if (zoomIn)
			{
				newScale = doF * ZOOM_STEP;
			}
			else
			{
				newScale = doF / ZOOM_STEP;
			}


			if (newScale > ZOOM_MAX)
			{
				doF = ZOOM_MAX;
			}
			else if (newScale < 1)
			{
				doF = 1;
			}
			else
			{
				doF = (int)newScale;
			}

			map.FocalLength = doF;
		}

		void ZoomMaps(List<StarMap> maps, string arg)
		{
			if (NoMaps(maps))
				return;

			bool zoomIn = arg == "IN";

			foreach (StarMap map in maps)
				Zoom(map, zoomIn);

		}


		// ADJUST RADIUS //
		void AdjustRadius(StarMap map, bool increase)
        {
			int radius = map.RotationalRadius;

			if (increase)
			{
				radius *= 2;
			}
			else
			{
				radius /= 2;
			}

			if (radius < map.FocalLength)
			{
				radius = map.FocalLength;
			}
			else if (radius > MAX_VALUE)
			{
				radius = MAX_VALUE;
			}

			map.RotationalRadius = radius;
		}

		void AdjustRadiusForList(List<StarMap> maps, bool increase)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				AdjustRadius(map, increase);

			}
		}


		// MOVE MAP //
		void MoveMap(StarMap map, string direction)
        {
			float step = (float)MOVE_STEP;
			float x = 0;
			float y = 0;
			float z = 0;

			switch (direction)
			{
				case "LEFT":
					x = step;
					break;
				case "RIGHT":
					x = -step;
					break;
				case "UP":
					y = step;
					break;
				case "DOWN":
					y = -step;
					break;
				case "FORWARD":
					z = step;
					break;
				case "BACKWARD":
					z = -step;
					break;
			}
			Vector3 moveVector = new Vector3(x, y, z);

			if (map.Mode == "FREE" || map.Mode == "WORLD")
			{
				map.Center += rotateMovement(moveVector, map);
			}
			else
			{
				_statusMessage = "Translation controls only available in FREE & WORLD modes.";
			}
		}

		void MoveMaps(List<StarMap> maps, string direction)
		{
			if (NoMaps(maps))
				return;



			foreach (StarMap map in maps)
			{
				MoveMap(map, direction);
			}
		}


		// TRACK CENTER //		Adjust translational speed of map.
		void TrackMap(StarMap map, string direction)
        {
			switch (direction)
			{
				case "LEFT":
					map.dX += MOVE_STEP;
					break;
				case "RIGHT":
					map.dX -= MOVE_STEP;
					break;
				case "UP":
					map.dY += MOVE_STEP;
					break;
				case "DOWN":
					map.dY -= MOVE_STEP;
					break;
				case "FORWARD":
					map.dZ += MOVE_STEP;
					break;
				case "BACKWARD":
					map.dZ -= MOVE_STEP;
					break;
				default:
					_statusMessage = "Error with Track Command";
					break;
			}
		}
		void TrackMaps(List<StarMap> maps, string direction)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				TrackMap(map, direction);
			}
		}


		// ROTATE MAPS //
		void RotateMap(StarMap map, string direction)
        {
			switch (direction)
			{
				case "LEFT":
					map.yaw(ANGLE_STEP);
					break;
				case "RIGHT":
					map.yaw(-ANGLE_STEP);
					break;
				case "UP":
					map.pitch(-ANGLE_STEP);
					break;
				case "DOWN":
					map.pitch(ANGLE_STEP);
					break;
			}
		}

		void RotateMaps(List<StarMap> maps, string direction)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				RotateMap(map, direction);
			}
		}


		// SPIN MAPS //		Adjust azimuth speed of maps.
		void SpinMap(StarMap map, string direction, int deltaAz)
        {
			if (direction == "RIGHT")
				deltaAz *= -1;

			map.dAz += deltaAz;
		}

		void SpinMaps(List<StarMap> maps, string direction, int deltaAz)
		{
			if (NoMaps(maps))
				return;

			if (direction == "RIGHT")
				deltaAz *= -1;

			foreach (StarMap map in maps)
			{
				SpinMap(map, direction, deltaAz);
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
			DefaultView(map);
			map.Center = _myPos;
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

				SelectPlanet(_nearestPlanet, map);
			}

			Vector3 planetPos = map.ActivePlanet.position;

			map.Center = (_myPos + planetPos) / 2;
			map.Altitude = 0;
			Vector3 orbit = _myPos - planetPos;
			map.Azimuth = (int)ToDegrees((float)Math.Abs(Math.Atan2(orbit.Z, orbit.X) + (float)Math.PI * 0.75f)); //  )

			//Get largest component distance between ship and planet.
			//double span = Math.Sqrt(orbit.LengthSquared() - Math.Pow(orbit.Y,2));
			float span = orbit.Length();

			double newRadius = 1.25f * map.FocalLength * span / map.Viewport.Height;

			if (newRadius > MAX_VALUE || newRadius < 0)
			{
				newRadius = MAX_VALUE;
				double newZoom = 0.8f * map.Viewport.Height * (MAX_VALUE / span);
				map.FocalLength = (int)newZoom;
			}

			map.RotationalRadius = (int)newRadius;
		}


		// PLANET MODE //
		void PlanetMode(StarMap map)
		{
			map.FocalLength = DV_FOCAL;
			map.dAz = 0;
			map.dX = 0;
			map.dY = 0;
			map.dZ = 0;

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

			map.RotationalRadius = DV_RADIUS;
			map.Mode = "PLANET";
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


		// DEFAULT VIEW //
		void DefaultView(StarMap map)
		{
			map.Mode = "FREE";

			map.Center = new Vector3(0, 0, 0);
			map.FocalLength = DV_FOCAL;

			if (map.Viewport.Width > 500)
			{
				map.FocalLength *= 3;
			}

			map.RotationalRadius = DV_RADIUS;
			map.Azimuth = 0;
			map.Altitude = DV_ALTITUDE;
		}


		// MAPS TO DEFAULT //
		void MapsToDefault(List<StarMap> maps)
		{
			if (maps.Count < 1)
				return;

			foreach (StarMap map in maps)
			{
				DefaultView(map);
			}
		}


		// BRIGHTEN MAP //
		void BrightenMap(StarMap map, bool brighten)
        {
			if(brighten)
            {
				map.BrightnessMod += BRIGHTNESS_STEP;
				if (map.BrightnessMod > BRIGHTNESS_LIMIT)
					map.BrightnessMod = BRIGHTNESS_LIMIT;
            }
			else
            {
				map.BrightnessMod -= BRIGHTNESS_STEP;

				// Don't let brightness fall below level of single step.
				if (map.BrightnessMod < BRIGHTNESS_STEP)
					map.BrightnessMod = BRIGHTNESS_STEP;
            }

			SetListKey(map.Block, MAP_HEADER, "Brightness", map.BrightnessMod.ToString(), "1", map.Index);
        }

		void BrightenMaps(List<StarMap> maps, bool brighten)
        {
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
				BrightenMap(map, brighten);
        }
	}
}
