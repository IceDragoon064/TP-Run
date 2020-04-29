using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NETWORK_ENGINE
{

    public class NetworkCore : MonoBehaviour
    {
        public string IpAddress;
        public int Port;
        public bool IsConnected = false;
        public bool IsServer = false;
        public bool IsClient = false;
        public bool CanJoin = true;
        public int LocalPlayerId = -1;        //-1 = server
        public bool CurrentlyConnecting = false;

        public Dictionary<int, NetworkConnection> Connections;    //list of connections. NetworkConnection is a class from another script that represents a player. It is assigned a player id based on the ConCounter.
        public Dictionary<int, NetworkID> NetObjs;

        public int ObjectCounter = 0;         //These counters are used for assigning net ids and player ids
        public int ConCounter = 0;            //Connection Counter - Incremented after a connection
        public int MaxConnections = 100;
        public GameObject[] SpawnPrefab;      //Spawnable objects array - Prefabs of dynamically addable objects

        public Socket TCP_Listener;           //Only using TCP for now.
        public Socket UDP_Listener;
        public float MasterTimer = .05f;
        Coroutine ListeningThread;            //Coroutine made ann instance of a variable so it can be turned off.
        public string MasterMessage;

        public GameObject NetworkPlayerManager;//This will be the first thing that is spawned when you first connect. Need a game object to send a message.

        //Locks
        public object _conLock = new object();
        public object _objLock = new object();
        public object _masterMessage = new object();

        // Use this for initialization
        void Start()
        {

            IsServer = false;
            IsClient = false;
            IsConnected = false;
            CurrentlyConnecting = false;
            //ipAddress = "127.0.0.1";//Local host
            if (IpAddress == "")
            {
                IpAddress = "127.0.0.1";//Local host
            }
            if (Port == 0)
            {
                Port = 9001;
            }
            Connections = new Dictionary<int, NetworkConnection>();
            NetObjs = new Dictionary<int, NetworkID>();
        }


        /// <summary>
        /// Server Functions
        /// StartServer -> Initialize Listener and Slow Update
        ///     - WIll spawn the first prefab as a "NetworkPlayerManager"
        /// Listen -> Will bind to a port and allow clients to join.
        /// </summary>
        public void StartServer()
        {
            if (!IsConnected)
            {
                IsServer = true;
                IsClient = false;
                IsConnected = true;
                ListeningThread = StartCoroutine(Listen());    //Listener
                StartCoroutine(SlowUpdate());                  //Business Logic Coroutine
            }
        }

        public void StopListening()
        {
            if (IsServer && CanJoin)
            {

                CurrentlyConnecting = false;
                StopCoroutine(ListeningThread);
                TCP_Listener.Close();
            }
        }

        public IEnumerator Listen()
        {

            //If we are listening then we are the server.
            IsServer = true;
            IsConnected = true;
            IsClient = false;
            LocalPlayerId = -1; //For server the localplayer id will be -1.
                                //Initialize port to listen to

            IPAddress ip = (IPAddress.Any);               //Find any IP address on the machine and use that IP address
            IPEndPoint endP = new IPEndPoint(ip, Port);
            //We could do UDP in some cases but for now we will do TCP
            TCP_Listener = new Socket(ip.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            //Now I have a socket listener.
            TCP_Listener.Bind(endP);
            TCP_Listener.Listen(MaxConnections);
            Debug.Log("We are now listening");
            while (CanJoin)
            {
                CurrentlyConnecting = false;
                //Normally Accept will be a blocking function, but since there is no thread dedicated to this, it will stop the game from working unless you allow it to continue and use a yield and callback.
                TCP_Listener.BeginAccept(new System.AsyncCallback(this.ListenCallBack), TCP_Listener);          //wait and listen for a new client coming in. AsyncCallback will listen for a client and invoke a callback once it finds one. Pass listener to listencallback
                yield return new WaitUntil(() => CurrentlyConnecting);                                          //Goes to ListenCallBack function
                //Waiting here - using coroutine since we are not using a thread
                CurrentlyConnecting = false;
                if (Connections.ContainsKey(ConCounter - 1) == false)    //If the dictionary does not have the connection that was just set up, continue, there was probably some error.
                {
                    //Connection was not fully established.
                    continue;
                }
                //First thing that happens after a player connection
                Connections[ConCounter - 1].Send(Encoding.ASCII.GetBytes("PlayerID#" + Connections[ConCounter - 1].PlayerId + "\n"));    //server knows the player id, but the client does not know what player id 
                //Start Server side listening for client messages.                                                                       //they are so we send a package containing the player id in an array of bytes.
                StartCoroutine(Connections[ConCounter - 1].TCPRecv());  //allows server to receive information from the socket. It's in the connection class.
                //Udpate all current network objects                   
                foreach (KeyValuePair<int, NetworkID> entry in NetObjs)
                {//This will create a custom create string for each existing object in the game.
                 //This is sent to a connecting client so they get all of the current objects and their current states, network id, and type/owner.

                    string MSG = "CREATE#" + entry.Value.Type + "#" + entry.Value.Owner +
                   "#" + entry.Value.NetId + "#" + entry.Value.transform.position.x.ToString("n2") +
                   "#" + entry.Value.transform.position.y.ToString("n2") + "#"
                   + entry.Value.transform.position.z.ToString("n2") + "\n";
                    Connections[ConCounter - 1].Send(Encoding.ASCII.GetBytes(MSG));
                }
                //Create NetworkPlayerManager - Allows you to transfer information related to the game.
                NetCreateObject(-1, ConCounter - 1, Vector3.zero);         //NetCreateObject(type of object, object connecting, Vector3.zero);
                yield return new WaitForSeconds(.1f);
                //loop back to start listening and yield.
            }
        }
        public void ListenCallBack(System.IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;   //listener = TCP_listener 
            Socket handler = listener.EndAccept(ar);   //accept connection
            NetworkConnection temp = new NetworkConnection();
            temp.TCPCon = handler;   //socket we just received
            temp.PlayerId = ConCounter;
            ConCounter++;
            temp.MyCore = this;      //can quicklt access the network core
            lock (_conLock)
            {
                Connections.Add(temp.PlayerId, temp);     //adds this new connection with the player id as the key to the dictionary holding all of the different connections.
                Debug.Log("There are now " + Connections.Count +        //debug message to know how many are logged in
                    " player(s) connected.");
            }
            CurrentlyConnecting = true;                   //Will end the yield statement in the list function
        }

        public void CloseGame()
        {
            if (IsServer && IsConnected && CanJoin)
            {
                CanJoin = false;
                StopCoroutine(ListeningThread);
            }
        }

        /// <summary>
        /// Client Functions 
        /// Start Client - Will join with a server specified
        /// at IpAddress and Port.
        /// </summary>

        public void StartClient()
        {
            if (!IsConnected)
            {
                IsServer = false;
                IsClient = false;
                IsConnected = false;
                CurrentlyConnecting = false;
                StartCoroutine(ConnectingClient());
            }
        }
        public IEnumerator ConnectingClient()
        {
            IsClient = false;
            IsServer = false;
            //Setup our socket
            IPAddress ip = (IPAddress.Parse(IpAddress));       //IP addressof server. You can use DNS here if you want to use some domain.
            IPEndPoint endP = new IPEndPoint(ip, Port);
            Socket clientSocket = new Socket(ip.AddressFamily, SocketType.Stream,
                ProtocolType.Tcp);
            //Connect client
            clientSocket.BeginConnect(endP, ConnectingCallback, clientSocket);   //Start ConnectingCallback, pass in clientSocket.
            Debug.Log("Trying to wait for server...");
            //Wait for the client to connect
            yield return new WaitUntil(() => CurrentlyConnecting);
            StartCoroutine(Connections[0].TCPRecv());  //It is 0 on the client because we only have 1 socket. Will let the client start listening to the server.
            StartCoroutine(SlowUpdate());  //This will allow the client to send messages to the server.
        }
        public void ConnectingCallback(System.IAsyncResult ar)
        {
            //Client will use the con list (but only have one entry).
            NetworkConnection temp = new NetworkConnection();
            temp.TCPCon = (Socket)ar.AsyncState;
            temp.TCPCon.EndConnect(ar);//This finishes the TCP connection (DOES NOT DISCONNECT)
            CurrentlyConnecting = true;
            IsConnected = true;
            IsClient = true;
            temp.MyCore = this;

            Connections.Add(0, temp);
        }
        /// <summary>
        /// Disconnect functions
        /// Leave game 
        /// Disconnect
        /// OnClientDisconnect -> is virtual so you can override it
        /// </summary>
        public void Disconnect(int badConnection)   //badConnection is 0 if its a client because they only have 1 connection.
        {
            Debug.Log("Trying to disconnect: " + badConnection);

            if (IsClient)
            {
                if (Connections.ContainsKey(badConnection))
                {
                    NetworkConnection badCon = Connections[badConnection];
                    try
                    {
                        badCon.TCPCon.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    { }
                    //but for now we will close it.
                    try
                    {
                        badCon.TCPCon.Close();
                    }
                    catch
                    {

                    }
                }
                this.IsClient = false;
                this.IsServer = false;
                this.IsConnected = false;
                this.LocalPlayerId = -10;
                foreach (KeyValuePair<int, NetworkID> obj in NetObjs)    //destroy objects that are pointed to by the dictionary in the scene
                {
                    Destroy(obj.Value.gameObject);
                }
                NetObjs.Clear();            //clear dictionary
                Connections.Clear();
            }
            if (IsServer)
            {
                try
                {
                    if (Connections.ContainsKey(badConnection))
                    {
                        NetworkConnection badCon = Connections[badConnection];
                        badCon.TCPCon.Shutdown(SocketShutdown.Both);
                        badCon.TCPCon.Close();



                    }
                }
                catch (System.Net.Sockets.SocketException)
                {
                    Debug.Log("Connection " + badConnection + " is already Closed!  Removing Objects.");
                }
                catch (System.ObjectDisposedException)
                {
                    Debug.Log("Socket already shutdown: ");
                }
                catch
                {
                    //In case anything else goes wrong.
                    Debug.Log("Warning - Error caught in the generic catch!");
                }
                //Delete All other players objects....
                OnClientDisc(badConnection);
                Connections.Remove(badConnection);
            }
        }
        public virtual void OnClientDisc(int badConnection)   //virtual so I can modify this function. Create a class that inherits from network core and override this function.
        {
            if (IsServer)               //currently a generic destroy all objects owned by the disconnecting player, but can be changed to have other functionality based on the game.
            {
                Debug.Log("Here!");
                //Remove Connection from server

                List<int> badObjs = new List<int>();
                foreach (KeyValuePair<int, NetworkID> obj in NetObjs)      //all objects
                {
                    if (obj.Value.Owner == badConnection)   //if object's owner is the disconnecting player, it will be added to the bad objects list
                    {
                        if(obj.Value.CompareTag("Player"))
                        {
                            Debug.Log("Found player disconnecting");
                            GameCharacter discChar = obj.Value.GetComponent<GameCharacter>();
                            if (discChar.inventory.tpCarried > 0)
                            {
                                Debug.Log(obj.Value.GetComponent<GameCharacter>().inventory.tpCarried);
                            }
                        }
                        badObjs.Add(obj.Key);
                        //I have to add the key to a temp list and delete
                        //it outside of this for loop
                    }
                }
                //Now I can remove the netObjs from the dictionary.
                for (int i = 0; i < badObjs.Count; i++)
                {
                    NetDestroyObject(badObjs[i]);
                }
            }
        }
        public void LeaveGame()
        {
            if (IsClient && IsConnected)
            {
                try
                {
                    lock (_conLock)
                    {
                        Debug.Log("Sending Disconnect!");
                        Connections[0].IsDisconnecting = true;

                        Connections[0].Send(Encoding.ASCII.
                                         GetBytes(
                                         "DISCON#" + Connections[0].PlayerId.ToString() + "\n")
                                         );

                    }
                }
                catch (System.NullReferenceException)
                {
                    //Client double-tapped disconnect.
                    //Ignore.
                }
                StartCoroutine(WaitForDisc());
            }
            if (IsServer && IsConnected)
            {
                Debug.Log("A");

                foreach (KeyValuePair<int, NetworkConnection> obj in Connections)
                {
                    lock (_conLock)
                    {
                        Debug.Log("Sending Disconnect!");
                        Connections[obj.Key].Send(Encoding.ASCII.       //sending disconnect code to the NetworkConnection class
                                         GetBytes(
                                         "DISCON#-1\n")
                                         );
                        Connections[obj.Key].IsDisconnecting = true;
                    }
                }
                Debug.Log("B");
                IsServer = false;
                try
                {
                    foreach (KeyValuePair<int, NetworkID> obj in NetObjs)
                    {
                        Destroy(obj.Value.gameObject);
                    }
                }
                catch (System.NullReferenceException)
                {
                    //Objects already destroyed.
                }
                try
                {
                    foreach (KeyValuePair<int, NetworkConnection> entry in Connections)
                    {
                        Disconnect(entry.Key);
                    }
                }
                catch (System.NullReferenceException)
                {
                    Debug.Log("Inside Disonnect error!");
                    //connections already destroyed.
                }
                Debug.Log("C");
                IsConnected = false;
                IsClient = false;
                CurrentlyConnecting = false;
                CanJoin = true;
                try
                {
                    NetObjs.Clear();
                    Connections.Clear();
                    StopCoroutine(ListeningThread);
                    TCP_Listener.Close();

                }
                catch (System.NullReferenceException)
                {
                    Debug.Log("Inside error.");
                    NetObjs = new Dictionary<int, NetworkID>();
                    Connections = new Dictionary<int, NetworkConnection>();
                }
                //StopAllCoroutines();
                Debug.Log("D");
            }
        }
        IEnumerator WaitForDisc()
        {
            if (IsClient)
            {
                yield return new WaitUntil(() => Connections[0].DidDisconnect);
                Disconnect(0);
            }
            yield return new WaitForSeconds(.1f);
        }
        public void OnApplicationQuit()
        {
            LeaveGame();
        }
        /// <summary>
        /// Object functions
        /// NetCreateObject -> creates an object across the network
        /// NetDestroyObject -> Destroys an object across the network
        /// </summary>
        public GameObject NetCreateObject(int type, int ownMe, Vector3 initPos)    //Network Object spawner
        {
            if (IsServer)
            {
                GameObject temp;
                lock (_objLock)
                {
                    if (type != -1)
                    {
                        temp = GameObject.Instantiate(SpawnPrefab[type], initPos, Quaternion.identity); //Instantiated object on the server
                    }
                    else
                    {
                        temp = GameObject.Instantiate(NetworkPlayerManager, initPos, Quaternion.identity);
                    }
                    temp.GetComponent<NetworkID>().Owner = ownMe;    //player id of the connection who owns that object
                    temp.GetComponent<NetworkID>().NetId = ObjectCounter;
                    temp.GetComponent<NetworkID>().Type = type;
                    NetObjs[ObjectCounter] = temp.GetComponent<NetworkID>();
                    ObjectCounter++;
                    string MSG = "CREATE#" + type + "#" + ownMe +               //sending create message to player network connections
                    "#" + (ObjectCounter - 1) + "#" + initPos.x.ToString("n2") + "#" +
                    initPos.y.ToString("n2") + "#" + initPos.z.ToString("n2") + "\n";
                    lock (_masterMessage)
                    {
                        MasterMessage += MSG;
                    }
                    foreach (NetworkComponent n in temp.GetComponents<NetworkComponent>())
                    {
                        //Force update to all clients.
                        n.IsDirty = true;
                    }
                }
                return temp;
            }
            else
            {
                return null;
            }

        }
        public void NetDestroyObject(int netIDBad)
        {
            try
            {
                if (NetObjs.ContainsKey(netIDBad))
                {
                    Destroy(NetObjs[netIDBad].gameObject);
                    NetObjs.Remove(netIDBad);
                }
            }
            catch
            {
                //Already been destroyed.
            }
            string msg = "DELETE#" + netIDBad + "\n";
            lock (_masterMessage)
            {
                MasterMessage += msg;
            }

        }


        /// <summary>
        /// Support functions
        /// Slow Update()
        /// SetIP address
        /// SetPort
        /// </summary>

        public IEnumerator SlowUpdate()
        {
            while (true)
            {
                //Compose Master Message

                foreach (KeyValuePair<int, NetworkID> id in NetObjs)
                {
                    lock (_masterMessage)
                    {
                        //Add their message to the masterMessage (the one we send)
                        lock (id.Value._lock)
                        {
                            MasterMessage += id.Value.GameObjectMessages + "\n";   //Master message is updated with all of the object messages that are added in NetworkID
                            //Clear Game Objects messages.
                            id.Value.GameObjectMessages = "";
                        }

                    }

                }

                //Send Master Message
                List<int> bad = new List<int>();
                if (MasterMessage != "")
                {
                    foreach (KeyValuePair<int, NetworkConnection> item in Connections)
                    {
                        try
                        {
                            //This will send all of the information to the client (or to the server if on a client).
                            item.Value.Send(Encoding.ASCII.GetBytes(MasterMessage));
                        }
                        catch
                        {
                            bad.Add(item.Key);
                        }
                    }
                    lock (_masterMessage)
                    {
                        MasterMessage = "";//delete old values.
                    }
                    lock (_conLock)
                    {
                        foreach (int i in bad)
                        {
                            this.Disconnect(i);
                        }
                    }
                }
                yield return new WaitForSeconds(MasterTimer);
            }
        }

        public void SetIp(string ip)
        {
            IpAddress = ip;
        }
        public void SetPort(string p)
        {
            Port = int.Parse(p);
        }



        // Update is called once per frame
        void Update()
        {

        }
    }
}
