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
        readonly Dictionary<int, string> _menuTitle = new Dictionary<int, string>()
        {
           {1,"Systems"},
           {2,"Zoom"},
           {3,"Rotation"},
           {4,"Translation"},
           {5,"Tracking"},
           {6,"Data Display"}
        };

        readonly Dictionary<int, string> _labelA = new Dictionary<int, string>()
        {
           {1,"Map"},
           {2,"Zoom"},
           {3,""},
           {4,""},
           {5,""},
           {6,""}
        };

        readonly Dictionary<int, string> _labelB = new Dictionary<int, string>()
        {
           {1,"Planet"},
           {2,"Radius"},
           {3,""},
           {4,""},
           {5,""},
           {6,""}
        };

        readonly Dictionary<int, string> _labelC = new Dictionary<int, string>()
        {
           {1,"Waypoint"},
           {2,"Map Mode"},
           {3,""},
           {4,""},
           {5,""},
           {6,""}
        };

        readonly Dictionary<int, string> _labelD = new Dictionary<int, string>()
        {
           {1,"Display"},
           {2,"Info"},
           {3,"Info"},
           {4,"Info"},
           {5,""},
           {6,""}
        };

        readonly Dictionary<int, string> _cmd1 = new Dictionary<int, string>()
        {
           {1,"◀"},
           {2,"-"},
           {3,"◀"},
           {4,"◀"},
           {5,"◀◀"},
           {6,"◀"}
        };

        readonly Dictionary<int, string> _cmd2 = new Dictionary<int, string>()
        {
           {1,"▶"},
           {2,"+"},
           {3,"▶"},
           {4,"▶"},
           {5,"▶▶"},
           {6,"▶"}
        };

        readonly Dictionary<int, string> _cmd3 = new Dictionary<int, string>()
        {
           {1,"◀"},
           {2,"+"},
           {3,"▼"},
           {4,"▼"},
           {5,"\\▼/"},
           {6,"▼"}
        };

        readonly Dictionary<int, string> _cmd4 = new Dictionary<int, string>()
        {
           {1,"▶"},
           {2,"-"},
           {3,"▲"},
           {4,"▲"},
           {5,"/▲\\"},
           {6,"▲"}
        };

        readonly Dictionary<int, string> _cmd5 = new Dictionary<int, string>()
        {
           {1,""},
           {2,"◀"},
           {3,"◀◀"},
           {4,"-"},
           {5,"--"},
           {6,""}
        };

        readonly Dictionary<int, string> _cmd6 = new Dictionary<int, string>()
        {
           {1,""},
           {2,""},
           {3,""},
           {4,"+"},
           {5,"++"},
           {6,""}
        };

        readonly Dictionary<int, string> _cmd7 = new Dictionary<int, string>()
        {
           {1,"-/o"},
           {2,"-/o"},
           {3,"-/o"},
           {4,"-/o"},
           {5,"STOP"},
           {6,""}
        };
    }
}
