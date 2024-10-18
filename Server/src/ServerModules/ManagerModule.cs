using CloudLib;
using Server.src;
using System.Collections.Generic;


namespace Server.src.ServerModules;

/// <summary>
///  ManagerModules do not run on their own threads. they are merely objects which are used to 
///  manage a set number of helpers, each of which does run on its own thread. 
/// </summary>
/// <typeparam name="HELPER_TYPE"></typeparam>
internal abstract class ManagerModule<HELPER_TYPE>
{
    // +++++++++++ ABSTRACT ++++++++++++++++ //
    abstract protected int Capacity { get; init; }
    abstract protected void AssignHelper(ConnectionResources connectionResources);
    // +++++++++++++++++++++++++++++++++++++ //






    // --------------- CONSTRUCTOR ----------------- //
    internal protected ManagerModule(int id)
    {
        _id = id;
        _idToGiveHelper = 1;

        CR_activeHelperThreads = new List<Thread>(Capacity);
        LOCK_activeHelperThreads = new object();

        CR_clientList = new List<ConnectionResources>(Capacity);
        LOCK_ClientList = new object();
    }
    // --------------------------------------------- //






    // --------- Properties --------- //
    protected int Id { get { return _id; } }
    // ------------------------------ //






    // ------ Private Variables --------- //
    protected int _id;
    protected int _idToGiveHelper;


    // <<<<<<<<<<< CRITICAL >>>>>>>>>>>> // 
    protected readonly List<Thread> CR_activeHelperThreads;
    protected readonly object LOCK_activeHelperThreads;
    // _________________________________ // 


    // <<<<<<<<<<< CRITICAL >>>>>>>>>>>> // 
    protected readonly List<ConnectionResources> CR_clientList;
    protected readonly object LOCK_ClientList;
    // _________________________________ // 

    // ----------------------------------------- // 





    // -------- Methods ---------- //

    /// <summary>
    /// To be called by a Dashboard Helper
    /// </summary>
    public (int userCount, int capacity) GetCapacityStatus(){
        int activeHelperCount;
        lock(LOCK_activeHelperThreads){
            activeHelperCount = CR_activeHelperThreads.Count;
        }
        return (activeHelperCount, Capacity);
    }

    /// <summary>
    /// To be called by a Dashboard Helper
    /// Returns true upon success
    /// Returns false if the server was full.
    /// </summary>
    public bool AssignClient(ConnectionResources connectionResources)
    {
        lock (LOCK_activeHelperThreads) {
            if (CR_activeHelperThreads.Count == Capacity){
                return false;
            }
            if (CR_activeHelperThreads.Count > Capacity){
                throw new Exception("ManagerModule " + Id + " had more active helpers than capacity allowed");
            }

            AssignHelper(connectionResources);
            _idToGiveHelper += 1;
            return true;
        }
    }

    /// <summary>
    ///  Must dispose of all its helpers too
    /// </summary>
    protected void DisposeOfManagement()
    {
        Debug.Assert(false, "unimplemented DisposeOfManagement()");
    }

    /// <summary>
    /// Create a new worker and put it to work
    /// </summary>

}