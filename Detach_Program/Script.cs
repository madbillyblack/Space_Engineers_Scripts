void Main(string action)
{
    switch(action)
    {
        case "Detach":
            Detach();
            break;
        case "DropOff":
            DropOff();
            break;
    }
}

public void Detach()
//finds and activates "Timer Block A" which is linked to detaching mechanisms.
{
    List<IMyTerminalBlock> timers_list = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName("Timer Block A", timers_list);
    IMyTerminalBlock merge = GridTerminalSystem.GetBlockWithName("Cargo Merge Clamp MSF")
        as IMyShipMergeBlock;
    double max_dist = 15;
    for(int i=0; i<timers_list.Count; i++)
    {
        var distance = Vector3D.Distance(merge.GetPosition(), timers_list[i].GetPosition());
        IMyTerminalBlock timer = timers_list[i] as IMyTimerBlock;
        if (distance < max_dist)
        {
            timer.GetActionWithName("TriggerNow").Apply(timer);
        }
    }
}

public void DropOff()
//separates cargo pod from tug, while simultaniously locking cargo pod connector to receiving grid.
{
    IMyTerminalBlock merge = GridTerminalSystem.GetBlockWithName("Cargo Merge Clamp MSF")
    as IMyShipMergeBlock;

    List<IMyTerminalBlock> connectors = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName("Lower Connector", connectors);
    int min_block = -1;
    double min_dist = 0;
    for (int i=0; i < connectors.Count; i++)
    {
        var dist = Vector3D.Distance(merge.GetPosition(), connectors[i].GetPosition());
        if (dist < min_dist || min_block == -1)
        {
            min_block = i;
            min_dist = dist;
        }
    }
    IMyTerminalBlock connector = connectors[min_block] as IMyShipConnector;
    connector.ApplyAction("SwitchLock");
    merge.ApplyAction("OnOff");   
}