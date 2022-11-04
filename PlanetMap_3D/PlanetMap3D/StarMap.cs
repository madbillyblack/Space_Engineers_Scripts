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
		List<StarMap> _mapList;

		public class StarMap
		{
			public Vector3 center;
			public string mode;
			public int altitude;
			public int azimuth;
			public int rotationalRadius;
			public int focalLength;
			public int azSpeed; // Rotational velocity of Azimuth
			public int number;
			public int index;
			public int dX;
			public int dY;
			public int dZ;
			public int dAz;
			public int gpsState;
			public IMyTextSurface drawingSurface;
			public RectangleF viewport;
			public IMyTerminalBlock block;
			public bool showNames;
			public bool showShip;
			public bool showInfo;
			public int planetIndex;
			public int waypointIndex;
			public string activePlanetName;
			public string activeWaypointName;
			public string gpsMode;
			public Planet activePlanet;
			public Waypoint activeWaypoint;

			public StarMap()
			{
				this.azSpeed = 0;
				this.planetIndex = 0;
				this.waypointIndex = -1;
			}

			public void yaw(int angle)
			{
				if (this.mode.ToUpper() == "PLANET" || this.mode.ToUpper() == "CHASE" || this.mode.ToUpper() == "ORBIT")
				{
					_statusMessage = "Yaw controls locked in PLANET, CHASE & ORBIT modes.";
					return;
				}
				this.azimuth = DegreeAdd(this.azimuth, angle);
			}

			public void pitch(int angle)
			{
				if (this.mode.ToUpper() != "PLANET" || this.mode.ToUpper() == "ORBIT")
				{
					int newAngle = DegreeAdd(this.altitude, angle);

					if (newAngle > MAX_PITCH)
					{
						newAngle = MAX_PITCH;
					}
					else if (newAngle < -MAX_PITCH)
					{
						newAngle = -MAX_PITCH;
					}

					this.altitude = newAngle;
				}
				else
				{
					_statusMessage = "Pitch controls locked in PLANET & ORBIT modes.";
				}
			}

			public string gpsStateToMode()
			{
				switch (this.gpsState)
				{
					case 0:
						this.gpsMode = "OFF";
						break;
					case 1:
						this.gpsMode = "NORMAL";
						break;
					case 2:
						this.gpsMode = "SHOW_ACTIVE";
						break;
					default:
						this.gpsMode = "ERROR";
						break;
				}

				return this.gpsMode;
			}
		}
	}
}
