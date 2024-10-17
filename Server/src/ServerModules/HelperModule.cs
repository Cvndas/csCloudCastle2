using Server.src;
using System.Collections.Generic;
using CloudLib;

namespace Server.src.ServerModules;

internal abstract class HelperModule 
{
    // --------- Properties --------- //
    abstract protected int Id{get;}
    abstract protected ConnectionResources ConnResources{get;}
    abstract protected NetworkStream Stream{get;}
    abstract protected CancellationToken EndHelper{get;}


    // -------- Methods ---------- //
    abstract protected void DisposeOfSelf();
    abstract protected void HelperJob();
    
    /// <summary>
    /// Must remove the user from the Dashboard Manager's list,
    /// and decrement the LOGGED_ON_USERS variable of the ServerManagement class.
    /// </summary>
    protected void NotifyServerOfDisconnection(){
        Debug.Assert(false, "unimplemented NotifyServerOfDisconnection()");
        // TODO 
    }
}