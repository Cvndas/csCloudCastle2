using Server.src;
using System.Collections.Generic;
using CloudLib;

namespace Server.src.ServerModules;

internal abstract class HelperModule<MANAGER_TYPE>
{
    public HelperModule(int id, ConnectionResources resources, CancellationToken token)
    {
        _id = id;
        _connectionResources = resources;
        _token = token;
        _stream = resources.stream;
    }

    // --------- Properties --------- //


    // ------ Private Variables --------- //
    protected readonly ManagerModule<MANAGER_TYPE> manager;
    protected int _id;
    protected readonly CancellationToken _token;
    protected readonly ConnectionResources _connectionResources;
    protected readonly NetworkStream _stream;

    // -------- Methods ---------- //
    abstract protected void DisposeOfSelf();
    abstract protected void HelperJob();

    /// <summary>
    /// Must remove the user from the Dashboard Manager's list,
    /// and decrement the LOGGED_ON_USERS variable of the ServerManagement class.
    /// </summary>
    protected void NotifyServerOfDisconnection()
    {
        Debug.Assert(false, "unimplemented NotifyServerOfDisconnection()");
        // TODO 
    }
}