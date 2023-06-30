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
        const string DF_COMTAG = "LFS Open Channel";
        const string COMMS_LABEL = "Communications Tag";

        string _broadcastTag;
        IMyBroadcastListener _listener;
        bool _commsEnabled;


        void BuildComms()
        {
            _commsEnabled = ParseBool(GetProgramKey("Comms Enabled", "False"));

            if(_commsEnabled)
                _broadcastTag = GetProgramKey(COMMS_LABEL, DF_COMTAG);
        }
    }
}
