using CloudLib;
using Server.src;
using System.Collections.Generic;


namespace Server.src.ServerModules;

internal abstract class ManagerModule<T>
{
    // --------- Properties --------- //
    abstract protected int Id { get; }
    /// <summary>
    /// Applies to both the helper queue and client queue.
    /// </summary>
    abstract protected int Capacity { get; init;}
    abstract protected List<T> HelperList { get; init;}
    abstract protected List<ConnectionResources> CR_ClientList { get; init;}

    abstract protected object Lock_ClientQueue{get; init;}


    // -------- Methods ---------- //
    abstract protected void AddClientToQueue();
    abstract protected void AddHelperToQueue();

    /// <summary>
    ///  Must dispose of all its helpers too
    /// </summary>
    protected void DisposeOfManagement(){
        Debug.Assert(false, "unimplemented DisposeOfManagement()");
    }

    /// <summary>
    /// Must assign clients from 
    /// </summary>
    abstract protected void ManagementJob();

    protected bool HasRoomForNewClient()
    {
        if (HelperList.Count < Capacity) {
            return true;
        }
        else {
            return false;
        }
    }
}