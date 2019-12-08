const String OUTER_DOOR = "Landing Lock Door (Outer) CDR";
const String INNER_DOOR = "Landing Lock Door (Inner) CDR";
const String LOCK_VENT = "Landing Lock Vent CDR";
const String LOCK_TIMER = "Lock Timer CDR";
const String SOUND_BLOCK = "Lock Alarm CDR";
const String LOCK_LIGHT = "Corner Light 01 (Landing Lock Inner) CDR";
const float STAGE1 = 2;
const float STAGE2 = 3;

int _lockState;

public Program()
{
    if (Storage.Length > 0)
    {
        _lockState = int.Parse(Storage);
    }
    else
    {
        _lockState = 0;
    }
}


public void Save() 
{
    Storage = _lockState.ToString();
}


public void Main(String arg) 
{ 
    switch(arg) 
    { 
        case "OpenLock": 
            OpenLock(); 
            break; 
        case "CloseLock": 
            CloseLock(); 
            break;
        case "LockTimer":
             switch(_lockState)   
            {   
                case 1:   
                    InnerDoorLock();   
                    break;   
                case 2:   
                    OuterDoorLock();   
                    break;   
                case 3:   
                    LockOpened();   
                    break;   
                case 4:   
                    LockClosed();   
                    break;
            }
            break;  
/* 
        case "DockSeal": 
            DockSeal(); 
            break; 
        case "Undock": 
            UnDock(); 
            break; 
*/          
    } 
}


//Seal Inner Door, depressurize lock, start opening timer.
public void OpenLock()
{
    //Set Lock State for timer switch action
    _lockState = 1;

    //Populate Lock components
    IMyTerminalBlock light = GridTerminalSystem.GetBlockWithName(LOCK_LIGHT) as IMyInteriorLight;
    IMyTerminalBlock alarm = GridTerminalSystem.GetBlockWithName(SOUND_BLOCK) as IMySoundBlock;
    IMyTerminalBlock vent = GridTerminalSystem.GetBlockWithName(LOCK_VENT) as IMyAirVent;
    IMyTerminalBlock timer = GridTerminalSystem.GetBlockWithName(LOCK_TIMER) as IMyTimerBlock;
    IMyTerminalBlock innerDoor = GridTerminalSystem.GetBlockWithName(INNER_DOOR) as IMyDoor;
    
    innerDoor.GetActionWithName("Open_Off").Apply(innerDoor);
    vent.GetActionWithName("Depressurize_On").Apply(vent);
    timer.SetValueFloat("TriggerDelay", STAGE1);
    timer.GetActionWithName("Start").Apply(timer);
    light.GetActionWithName("IncreaseBlink Interval").Apply(light);
    alarm.GetActionWithName("PlaySound").Apply(alarm);  
}


//Seal Outer Door, pressurize lock, start closing timer.
public void CloseLock()
{ 
    //Set Lock State for timer switch action 
    _lockState = 2; 
 
    //Populate Lock components 
    IMyTerminalBlock light = GridTerminalSystem.GetBlockWithName(LOCK_LIGHT) as IMyInteriorLight; 
    IMyTerminalBlock alarm = GridTerminalSystem.GetBlockWithName(SOUND_BLOCK) as IMySoundBlock; 
    IMyTerminalBlock vent = GridTerminalSystem.GetBlockWithName(LOCK_VENT) as IMyAirVent; 
    IMyTerminalBlock timer = GridTerminalSystem.GetBlockWithName(LOCK_TIMER) as IMyTimerBlock; 
    IMyTerminalBlock outerDoor = GridTerminalSystem.GetBlockWithName(OUTER_DOOR) as IMyDoor; 
     
    outerDoor.GetActionWithName("Open_Off").Apply(outerDoor); 
    vent.GetActionWithName("Depressurize_Off").Apply(vent); 
    timer.SetValueFloat("TriggerDelay", STAGE1); 
    timer.GetActionWithName("Start").Apply(timer); 
    light.GetActionWithName("IncreaseBlink Interval").Apply(light); 
    alarm.GetActionWithName("PlaySound").Apply(alarm);   
}


//Locks inner door, continues lock opening timer.
public void InnerDoorLock()
{
    _lockState = 3;
 
    //Populate Lock components  
    IMyTerminalBlock timer = GridTerminalSystem.GetBlockWithName(LOCK_TIMER) as IMyTimerBlock;  
    IMyTerminalBlock innerDoor = GridTerminalSystem.GetBlockWithName(INNER_DOOR) as IMyDoor;

    innerDoor.GetActionWithName("OnOff_Off").Apply(innerDoor);   
    timer.SetValueFloat("TriggerDelay", STAGE2);
    timer.GetActionWithName("Start").Apply(timer);
}


//Locks outer door, continues lock closing timer. 
public void OuterDoorLock()
{ 
    _lockState = 4; 
  
    //Populate Lock components
    IMyTerminalBlock timer = GridTerminalSystem.GetBlockWithName(LOCK_TIMER) as IMyTimerBlock;
    IMyTerminalBlock outerDoor = GridTerminalSystem.GetBlockWithName(OUTER_DOOR) as IMyDoor;   
    
    outerDoor.GetActionWithName("OnOff_Off").Apply(outerDoor); 
    timer.SetValueFloat("TriggerDelay", STAGE2); 
    timer.GetActionWithName("Start").Apply(timer);    
}


//Finalizes lock opening sequence.
public void LockOpened() 
{   
    //Populate Lock components    
    IMyTerminalBlock light = GridTerminalSystem.GetBlockWithName(LOCK_LIGHT) as IMyInteriorLight;
    IMyTerminalBlock outerDoor = GridTerminalSystem.GetBlockWithName(OUTER_DOOR) as IMyDoor;
    IMyTerminalBlock alarm = GridTerminalSystem.GetBlockWithName(SOUND_BLOCK) as IMySoundBlock;    

    outerDoor.GetActionWithName("OnOff_On").Apply(outerDoor);
    outerDoor.GetActionWithName("Open_On").Apply(outerDoor);
    light.GetActionWithName("DecreaseBlink Interval").Apply(light);
    alarm.GetActionWithName("StopSound").Apply(alarm);           
}


//Finalizes lock closing sequence. 
public void LockClosed()  
{    
    //Populate Lock components     
    IMyTerminalBlock light = GridTerminalSystem.GetBlockWithName(LOCK_LIGHT) as IMyInteriorLight;
    IMyTerminalBlock innerDoor = GridTerminalSystem.GetBlockWithName(INNER_DOOR) as IMyDoor;
    IMyTerminalBlock alarm = GridTerminalSystem.GetBlockWithName(SOUND_BLOCK) as IMySoundBlock;     
    

    innerDoor.GetActionWithName("OnOff_On").Apply(innerDoor); 
    innerDoor.GetActionWithName("Open_On").Apply(innerDoor); 
    light.GetActionWithName("DecreaseBlink Interval").Apply(light);
    alarm.GetActionWithName("StopSound").Apply(alarm);          
}


//Chooses Timer Action based on state variable.  
/*
public void TimerAction()
{  
    switch(_lockState)  
    {  
        case 1:  
            InnerDoorLock();  
            break;  
        case 2:  
            OuterDoorLock();  
            break;  
        case 3:  
            LockOpened();  
            break;  
        case 4:  
            LockClosed();  
            break;  
    }  
}  
*/

