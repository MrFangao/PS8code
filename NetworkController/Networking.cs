// PS7, networking prepared for PS8
// Student: Xinyu Sun,Yunzu Hou
// Date: 11/9/2023
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkUtil;

internal class ServerState
{
    public TcpListener Listener { get; }
    public Action<SocketState> OnClientConnected { get; }

    public ServerState(TcpListener listener, Action<SocketState> onClientConnected)
    {
        Listener = listener;
        OnClientConnected = onClientConnected;
    }
}
public static class Networking
{

    /////////////////////////////////////////////////////////////////////////////////////////
    // Server-Side Code
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Starts a TcpListener on the specified port and starts an event-loop to accept new clients.
    /// The event-loop is started with BeginAcceptSocket and uses AcceptNewClient as the callback.
    /// AcceptNewClient will continue the event-loop.
    /// </summary>
    /// <param name="toCall">The method to call when a new connection is made</param>
    /// <param name="port">The the port to listen on</param>

    public static TcpListener StartServer(Action<SocketState> toCall, int port)
    {
        //Initialize TCP listener, including IP and port.
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        try
        {
            //in case of unexcpeted error or bugs.
            listener.Start();
            ServerState serverState = new ServerState(listener, toCall);
            listener.BeginAcceptSocket(new AsyncCallback(AcceptNewClient), serverState);
        }
        catch
        {
            listener?.Stop();
            throw; // Throw the exception to indicate failure to the caller
        }

        return listener;
    }

    /// <summary>
    /// To be used as the callback for accepting a new client that was initiated by StartServer, and 
    /// continues an event-loop to accept additional clients.
    ///
    /// Uses EndAcceptSocket to finalize the connection and create a new SocketState. The SocketState's
    /// OnNetworkAction should be set to the delegate that was passed to StartServer.
    /// Then invokes the OnNetworkAction delegate with the new SocketState so the user can take action. 
    /// 
    /// If anything goes wrong during the connection process (such as the server being stopped externally), 
    /// the OnNetworkAction delegate should be invoked with a new SocketState with its ErrorOccurred flag set to true 
    /// and an appropriate message placed in its ErrorMessage field. The event-loop should not continue if
    /// an error occurs.
    ///
    /// If an error does not occur, after invoking OnNetworkAction with the new SocketState, an event-loop to accept 
    /// new clients should be continued by calling BeginAcceptSocket again with this method as the callback.
    /// </summary>
    /// <param name="ar">The object asynchronously passed via BeginAcceptSocket. It must contain a tuple with 
    /// 1) a delegate so the user can take action (a SocketState Action), and 2) the TcpListener</param>
    private static void AcceptNewClient(IAsyncResult ar)
    {
        if (ar.AsyncState is not ServerState serverState) 
        {
            throw new Exception("ar.AsyncState is not ServerState, located AcceptNewClient");
        }

        TcpListener listener = serverState.Listener;
        Action<SocketState> toCall = serverState.OnClientConnected;

        try
        {
            // Initialize clientSocket directly here, because if EndAcceptSocket throws an exception, we do not need this variable
            //直接在这里初始化clientSocket，因为如果EndAcceptSocket抛出异常，我们不需要这个变量
            Socket clientSocket = listener.EndAcceptSocket(ar);

            // Initialize the state here because if we do not enter the catch block, we will not need to set it to an error state
            //在这里初始化state，因为如果不进入catch块，我们将不需要将它设置为错误状态
            SocketState state = new SocketState(toCall, clientSocket);
            toCall(state);

            // If we arrive here, it indicates that no abnormalities have occurred and we can safely start listening for new connections again
            //如果我们到达这里，说明没有异常发生，可以安全地再次开始监听新的连接
            listener.BeginAcceptSocket(AcceptNewClient, serverState);
        }
        catch (Exception e)
        {
            // Create a SocketState with error information only when an exception occurs
            // 只有在出现异常时才创建一个带有错误信息的SocketState            
            SocketState state = new SocketState(toCall, e.Message);
            toCall(state);
            
            // 不需要重新开始监听新的连接，因为这可能表示监听器有问题
        }
    }


    /// <summary>
    /// Stops the given TcpListener.
    /// </summary>
    public static void StopServer(TcpListener listener)
    {
        if (listener == null)
        {
            throw new Exception();
        }
        listener.Stop(); //make sure listener is not null here.
    }

    /////////////////////////////////////////////////////////////////////////////////////////
    // Client-Side Code
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Begins the asynchronous process of connecting to a server via BeginConnect, 
    /// and using ConnectedCallback as the method to finalize the connection once it's made.
    /// 
    /// If anything goes wrong during the connection process, toCall should be invoked 
    /// with a new SocketState with its ErrorOccurred flag set to true and an appropriate message 
    /// placed in its ErrorMessage field. Depending on when the error occurs, this should happen either
    /// in this method or in ConnectedCallback.
    ///
    /// This connection process should timeout and produce an error (as discussed above) 
    /// if a connection can't be established within 3 seconds of starting BeginConnect.
    /// 
    /// </summary>
    /// <param name="toCall">The action to take once the connection is open or an error occurs</param>
    /// <param name="hostName">The server to connect to</param>
    /// <param name="port">The port on which the server is listening</param>
    public static void ConnectToServer(Action<SocketState> toCall, string hostName, int port)
    {
        // Establish the remote endpoint for the socket.
        IPHostEntry ipHostInfo;
        IPAddress ipAddress = IPAddress.None;

        // Determine if the server address is a URL or an IP
        try
        {
            ipHostInfo = Dns.GetHostEntry(hostName);
            bool foundIPV4 = false;
            foreach (IPAddress addr in ipHostInfo.AddressList)
                if (addr.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    foundIPV4 = true;
                    ipAddress = addr;
                    break;
                }
            // Didn't find any IPV4 addresses
            if (!foundIPV4)
            {
                SocketState errorMessage = new SocketState(toCall, "No IPV4 address found in the server");
                errorMessage.ErrorOccurred = true;
                toCall(errorMessage);
                return;
            }
        }
        catch (Exception)
        {
            // see if host name is a valid ipaddress
            try
            {
                ipAddress = IPAddress.Parse(hostName);
            }
            catch (Exception)
            {
                SocketState hostError = new SocketState(toCall, "Error with Name");
                hostError.ErrorOccurred = true;
                toCall(hostError);
                return;
            }
        }

        // Create a TCP/IP socket.
        Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        // Initialize instance of SocketState
        SocketState state = new SocketState(toCall, socket);

        // setup timer
        Timer? connectionTimer = null;
        connectionTimer = new Timer(
            callback: (obj) =>
            {
                // If the connection can not been established, close the socket and report an error.
                // 如果连接还没建立则关闭socket并报告错误
                if (!socket.Connected)
                {
                    state.ErrorOccurred = true;
                    state.ErrorMessage = "Connection timed out.";
                    socket.Close(); 
                    toCall(state); // 调用回调
                }
                //Cancel and release timer
                // 取消并释放定时器
                connectionTimer?.Dispose(); 
            },
            null,
            3000, //3000ms for 3sec
            Timeout.Infinite //timer will only be trigger once
        );

        //Start asynchronous connection
        // 开始异步连接
        try
        {
            socket.BeginConnect(ipAddress, port, (ar) =>
            {
                try
                {
                    //finish connection
                    // 完成连接
                    socket.EndConnect(ar);

                    // Disable timer. WE connected!
                    // 取消定时器，因为我们已经连接了
                    connectionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    connectionTimer?.Dispose();

                    // 如果已连接，继续进行后续步骤...
                    toCall(state);
                }
                catch (Exception e)
                {
                    //in case of connection error
                    // 处理连接异常
                    state.ErrorOccurred = true;
                    state.ErrorMessage = e.Message;
                    toCall(state);
                }
            }, state);
        }
        catch (Exception e)
        {
            //handling connection errorl
            // 处理连接尝试异常
            connectionTimer?.Dispose();
            state.ErrorOccurred = true;
            state.ErrorMessage = e.Message; //error message   
            toCall(state);
        }
    }
    /// <summary>
    /// To be used as the callback for finalizing a connection process that was initiated by ConnectToServer.
    ///
    /// Uses EndConnect to finalize the connection.
    /// 
    /// As stated in the ConnectToServer documentation, if an error occurs during the connection process,
    /// either this method or ConnectToServer should indicate the error appropriately.
    /// 
    /// If a connection is successfully established, invokes the toCall Action that was provided to ConnectToServer (above)
    /// with a new SocketState representing the new connection.
    /// 
    /// </summary>
    /// <param name="ar">The object asynchronously passed via BeginConnect</param>
    private static void ConnectedCallback(IAsyncResult ar)
    {
        try
        {
            //the asynchronous state object is not null
            if (ar.AsyncState != null)
            {
                // Casting the AsyncState back to a SocketState object
                SocketState state = (SocketState)ar.AsyncState;

                // Calling EndConnect to complete the asynchronous connection attempt
                state.TheSocket.EndConnect(ar);

                // If a delegate for handling network actions is set, invoke it with the new SocketState
                state.OnNetworkAction?.Invoke(state);
            }
        }
        catch (Exception ex)
        {
            if (ar.AsyncState != null)
            {
                SocketState state = (SocketState)ar.AsyncState;
                // Updating the state to reflect that an error has occurred
                state.ErrorOccurred = true;
                state.ErrorMessage = "Error connecting to server: " + ex.Message;

                state.OnNetworkAction?.Invoke(state);
            }
        }
    }


    /////////////////////////////////////////////////////////////////////////////////////////
    // Server and Client Common Code
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Begins the asynchronous process of receiving data via BeginReceive, using ReceiveCallback 
    /// as the callback to finalize the receive and store data once it has arrived.
    /// The object passed to ReceiveCallback via the AsyncResult should be the SocketState.
    /// 
    /// If anything goes wrong during the receive process, the SocketState's ErrorOccurred flag should 
    /// be set to true, and an appropriate message placed in ErrorMessage, then the SocketState's
    /// OnNetworkAction should be invoked. Depending on when the error occurs, this should happen either
    /// in this method or in ReceiveCallback.
    /// </summary>
    /// <param name="state">The SocketState to begin receiving</param>
    public static void GetData(SocketState state)
    {
        try
        {
            // Initiating the asynchronous receive operation using the socket within the provided SocketState.
            // The ReceiveCallback method is designated to be called once data starts arriving.

            state.TheSocket.BeginReceive(state.buffer, 0, SocketState.BufferSize, SocketFlags.None, ReceiveCallback, state);
        }
        catch (Exception error)
        {
            //error messages
            state.ErrorMessage = "Error receiving data: " + error.Message;
            state.ErrorOccurred = true;
            
            // Invoking the delegate to handle the error state.
            state.OnNetworkAction?.Invoke(state);
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a receive operation that was initiated by GetData.
    /// 
    /// Uses EndReceive to finalize the receive.
    ///
    /// As stated in the GetData documentation, if an error occurs during the receive process,
    /// either this method or GetData should indicate the error appropriately.
    /// 
    /// If data is successfully received:
    ///  (1) Read the characters as UTF8 and put them in the SocketState's unprocessed data buffer (its string builder).
    ///      This must be done in a thread-safe manner with respect to the SocketState methods that access or modify its 
    ///      string builder.
    ///  (2) Call the saved delegate (OnNetworkAction) allowing the user to deal with this data.
    /// </summary>
    /// <param name="ar"> 
    /// This contains the SocketState that is stored with the callback when the initial BeginReceive is called.
    /// </param>
    private static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            //the asynchronous state object is not null
            if (ar.AsyncState != null)
            {
                // Casting the AsyncState back to a SocketState object
                SocketState socketState = (SocketState)ar.AsyncState;
                //receive operation
                int byteReceived = socketState.TheSocket.EndReceive(ar);
                
                // Processing the received data
                if (byteReceived > 0)
                {
                    // Converting the received bytes to a string
                    string dateReceived = Encoding.UTF8.GetString(socketState.buffer, 0, byteReceived);

                    //thread-safe
                    lock (socketState.data)
                    {
                        socketState.data.Append(dateReceived);
                    }
                    socketState.OnNetworkAction?.Invoke(socketState);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Receive call back:" + e);
        }
    }

    /// <summary>
    /// Begin the asynchronous process of sending data via BeginSend, using SendCallback to finalize the send process.
    /// 
    /// If the socket is closed, does not attempt to send.
    /// 
    /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
    /// </summary>
    /// <param name="socket">The socket on which to send the data</param>
    /// <param name="data">The string to send</param>
    /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
    public static bool Send(Socket socket, string data)
    {
        //check socket status
        if (!socket.Connected || socket is null)
        {
            return false;
        }
        //  to bytes, using UTF-8 encoding
        byte[] ByteData = Encoding.UTF8.GetBytes(data);
        try
        {
            //try Initiating the asynchronous send operation
            socket.BeginSend(ByteData, 0, ByteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
            return true;
        }
        catch
        {
            //exceptions 
            socket.Close();
            return false;
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a send operation that was initiated by Send.
    ///
    /// Uses EndSend to finalize the send.
    /// 
    /// This method must not throw, even if an error occurred during the Send operation.
    /// </summary>
    /// <param name="ar">
    /// This is the Socket (not SocketState) that is stored with the callback when
    /// the initial BeginSend is called.
    /// </param>
    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            if (ar.AsyncState != null)
            {
                //try Initiating the asynchronous send operation
                Socket socket = (Socket)ar.AsyncState;
                int byteSend = socket.EndSend(ar);
            }
        }
        catch (Exception e)
        {
            //handling exception
            Console.WriteLine("here is error:" + e);
        }

    }


    /// <summary>
    /// Begin the asynchronous process of sending data via BeginSend, using SendAndCloseCallback to finalize the send process.
    /// This variant closes the socket in the callback once complete. This is useful for HTTP servers.
    /// 
    /// If the socket is closed, does not attempt to send.
    /// 
    /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
    /// </summary>
    /// <param name="socket">The socket on which to send the data</param>
    /// <param name="data">The string to send</param>
    /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
    public static bool SendAndClose(Socket socket, string data)
    {
        //check socket status
        if (!socket.Connected || socket is null)
        {
            return false;
        }
        
        //to bytes, using UTF-8 encoding
        byte[] ByteData = Encoding.UTF8.GetBytes(data);
        try
        {
            //try Initiating the asynchronous send operation
            socket.BeginSend(ByteData, 0, ByteData.Length, SocketFlags.None, new AsyncCallback(SendAndCloseCallback), socket);
            return true;
        }
        catch
        {
            //handling exception
            socket.Close();
            return false;
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a send operation that was initiated by SendAndClose.
    ///
    /// Uses EndSend to finalize the send, then closes the socket.
    /// 
    /// This method must not throw, even if an error occurred during the Send operation.
    /// 
    /// This method ensures that the socket is closed before returning.
    /// </summary>
    /// <param name="ar">
    /// This is the Socket (not SocketState) that is stored with the callback when
    /// the initial BeginSend is called.
    /// </param>
    private static void SendAndCloseCallback(IAsyncResult ar)
    {
        try
        {
            if (ar.AsyncState != null)
            {
                Socket socket = (Socket)ar.AsyncState;
                int byteSend = socket.EndSend(ar);
                socket.Close();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("here is error:" + e);
        }
    }
}
