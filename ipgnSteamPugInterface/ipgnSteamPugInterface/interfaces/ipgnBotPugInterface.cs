namespace ipgnSteamPugInterface
{
    using System;
    using System.Collections;
    using System.Text;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Timers;
    using System.Windows.Forms;
    using Steam4NET;

    class ipgnBotPugInterface
    {
        public Socket ipgnPugInterfaceSocket;
        public CPUGData pugStatus;
        System.Timers.Timer ipgnPugReconTimer;

        sendState sndState;

        private const string ConnectionFailed = "Unable to connect to the pug bot";
        private const string ConnectionSuccess = "Connected to the pug bot";
        private const string ConnectionClosed = "Connection closed by remote host - could mean a server error";

        public bool connectedToBot;

        public static IPEndPoint botIPEndPoint;
        public static string botPassword;

        private static ipgnBotSteamInterface ipgnSteamInterface;

        public ipgnBotPugInterface()
        {
            ipgnPugInterfaceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            pugStatus = new CPUGData();
            sndState = new sendState();
        }

        //void to handle the passing of the steam interface, so we can use it inside this object
        public void ipgnSteamInterfacePass(ipgnBotSteamInterface interfaceHandle)
        {
            ipgnSteamInterface = interfaceHandle;
        }

        /* Socket setup and control, including reading/receiving/connecting
         * This is essential for connecting to the pug bot, however this method is not set in stone.
         * I think perhaps a UDP listen socket on both this bot and the pug bot may be better than a
         * constantly open TCP socket on the localhost, though no bandwidth cost will be induced either way
         * and TCP is substantially more reliable.
         */

        public void connectToPugBot(IPEndPoint pugBotAddress, string interfacePassword)
        {
            connectState conState = new connectState();

            botIPEndPoint = pugBotAddress;
            botPassword = interfacePassword;

            conState.botPassword = interfacePassword;
            conState.botIPEndPoint = pugBotAddress;

            if (!ipgnPugInterfaceSocket.Connected)
                ipgnPugInterfaceSocket.BeginConnect(pugBotAddress, new AsyncCallback(connectCallback), conState);
        }

        private void connectCallback(IAsyncResult aResult)
        {
            connectState conState = (connectState)aResult.AsyncState;
            try
            {
                ipgnPugInterfaceSocket.EndConnect(aResult);
                onConnectionSuccess(true);

                string authString = "STARTUP!" + conState.botPassword;

                //send start notice to bot with password
                sendBotCommand(authString);

                if ((sndState.sendMsg != null) && (sndState.sendMsg != authString))
                {
                    sendBotCommand(sndState.sendMsg);
                }

                //start listening
                receiveBotCommand();
            }
            catch (SocketException)
            {
                onSocketError(ConnectionFailed);
            }
        }

        private void disconnectCallback(IAsyncResult aResult)
        {
            Program.logToWindow("********** Disconnected from bot. Attempting to reconnect");

            connectedToBot = false;

            ipgnPugReconTimer = new System.Timers.Timer();
            ipgnPugReconTimer.Elapsed += new ElapsedEventHandler(ipgnPugInterfaceReconnect);
            ipgnPugReconTimer.Interval = 10000;
            ipgnPugReconTimer.Start();
        }

        private int ipgnPugConnectAttempts = 0;

        private void onSocketError(string error)
        {
            Program.logToWindow("********** Socket had error: " + error);
            if (ipgnPugInterfaceSocket.Connected)
                ipgnPugInterfaceSocket.BeginDisconnect(true, new AsyncCallback(disconnectCallback), this);
            else
            {
                ipgnPugConnectAttempts++;
                if (ipgnPugConnectAttempts > 50)
                {
                    MessageBox.Show("Something is dangerously wrong. Infinite loop on socket connect");
                    Application.Exit();
                }
                connectToPugBot(botIPEndPoint, botPassword);
                Program.logToWindow("********** Trying to connect. Attempt: " + ipgnPugConnectAttempts);
            }
        }

        private void onConnectionSuccess(bool connected)
        {
            if (connected)
            {
                Program.logToWindow("********** Successfully established socket connection to the pug bot");
                connectedToBot = true;
            }
            else
            {
                Program.logToWindow("********** No connection to the pug bot");
                connectedToBot = false;
            }
        }

        public void sendBotCommand(string command)
        {
            byte[] commandPacket = ASCIIEncoding.ASCII.GetBytes(command);

            sndState.sendMsg = command;

            if (ipgnPugInterfaceSocket.Connected)
            {
                ipgnPugInterfaceSocket.BeginSend(commandPacket, 0, commandPacket.Length, SocketFlags.None, new AsyncCallback(sendBotCallback), this);
            }
            else
            {
                Program.logToWindow("********** Unable to send data - lost socket connection. Attempting to reconnect");
                onSocketError(ConnectionClosed);
            }
        }

        void sendBotCallback(IAsyncResult asyncResult)
        {
            try
            {
                ipgnPugInterfaceSocket.EndSend(asyncResult);
                sndState.sendMsg = null;
            }
            catch (SocketException)
            {
                onSocketError(ConnectionClosed);
            }
        }

        public void receiveBotCommand()
        {
            try
            {
                receiveState currentState = new receiveState();
                currentState.IsNewData = false;
                currentState.dataReceived = new byte[256];

                ipgnPugInterfaceSocket.BeginReceive(currentState.dataReceived, 0, currentState.bufferSizeReadable,
                    SocketFlags.None, new AsyncCallback(receiveCallback), currentState);
            }
            catch (SocketException)
            {
                onSocketError(ConnectionClosed);
            }
        }

        void receiveCallback(IAsyncResult asyncResult)
        {
            receiveState currentState = (receiveState)asyncResult.AsyncState;
            bool success = false;
            try
            {
                int bytesRead = ipgnPugInterfaceSocket.EndReceive(asyncResult);

                success = true;

                if (bytesRead > 0)
                {
                    currentState.IsNewData = true;
                    currentState.bytesRead = bytesRead;

                    Program.logToWindow("Receive callback triggered. More than 0 bytes, therefore new data");
                }
                else
                    currentState.IsNewData = false;
            }
            catch (SocketException)
            {
                onSocketError(ConnectionClosed);
            }

            if (success)
                processIncomingData(currentState);
        }

        void processIncomingData(receiveState currentState)
        {
            Program.logToWindow("Processing received data. New data: " + currentState.IsNewData 
                + " Bytes read: " + currentState.bytesRead);

            if (currentState.IsNewData) //we have new data
            {
                currentState.receivedString.Append(Encoding.ASCII.GetString(currentState.dataReceived,
                    0, currentState.bytesRead));

                string currentMessage = currentState.receivedString.ToString();
                Program.logToWindow("We have new data. Current message: " + currentMessage);

                //If the amount of data read is >= the max buffer size, we should check for more 
                if (currentState.bytesRead >= currentState.bufferSizeReadable)
                    ipgnPugInterfaceSocket.BeginReceive(currentState.dataReceived, 0, currentState.bufferSizeReadable,
                        SocketFlags.None, new AsyncCallback(receiveCallback), currentState);
                else
                {
                    reactToBot(currentMessage, pugStatus);
                    receiveBotCommand(); //start listening again
                }
            }
            else if (currentState.bytesRead > 0)
            {
                //Do something with the data we've got, because there's no more coming immediately
                Program.logToWindow("Received a command or a response from the pug bot");

                string receivedMessage = currentState.receivedString.ToString();

                Program.logToWindow("Received data: " + receivedMessage);

                //parse this command and react, sending the current status object with it
                reactToBot(receivedMessage, pugStatus);

                //go back to listening again
                receiveBotCommand();
            }
            else
            {
                //let's presume it was closed... because this is what happens when it's closed
                onSocketError(ConnectionClosed);
            }
        }

        void ipgnPugInterfaceReconnect(object source, ElapsedEventArgs e)
        {
            this.connectToPugBot(botIPEndPoint, botPassword);
            Thread.Sleep(500);
            if (ipgnPugInterfaceSocket.Connected)
            {
                ipgnPugReconTimer.Stop();
                Program.logToWindow("********** Successfully reconnected");
            }
        }

        /* End the socket control. Now we setup a function for processing the command send between the
        * bots. The main program will need to be included in this control somehow, because it controls both
        * the steam interface and the pug interface, and we need both for this bot to function properly.
        * I think perhaps it may be better to initialize the socket inside the steam interface class, rather
        * than having the main program control both. Though this could just be due to bad implementation ;)
        */

        public void reactToBot(string botMessage, CPUGData pugStatus)
        {
            //hardcode reactions here. ie, details, endpug, start/end mapvote, fun stuff
            //Program.ipgnSteamInterface.sendMessage("STEAM_0:1:111", "asdf", false);
        }

        public bool addPlayer(CSteamID steamID, string name)
        {
            for (int i = 0; i < pugStatus.players.GetLength(0); i++)
            {
                if (pugStatus.players[i, 0] == null)
                {
                    pugStatus.players[i, 0] = steamID.ToString();
                    pugStatus.players[i, 1] = name;

                    Program.logToFile("Added " + name + " (" + steamID + ") to the pug");

                    this.sendBotCommand("ADDPLAYER!" + name + "@" + steamID.ToString());

                    pugStatus.numPlayers += 1;

                    return true;
                }
            }
            return false;
        }

        public bool removePlayer(CSteamID steamID)
        {
            int playerIndex = GetPlayerIndex(steamID);

            if (playerIndex >= 0)
            {
                //player is in the pug
                string name = pugStatus.players[playerIndex, 1];

                pugStatus.players[playerIndex, 0] = null;
                pugStatus.players[playerIndex, 1] = null;

                Program.logToFile("Removed " + name + " (" + steamID + ") from the pug");

                this.sendBotCommand("REMOVEPLAYER!" + name + "@" + steamID.ToString());

                pugStatus.numPlayers -= 1;

                return true;
            }

            return false;
        }

        public int GetPlayerIndex(CSteamID steamID)
        {
            for (int i = 0; i < pugStatus.players.GetLength(0); i++)
            {
                Program.logToWindow("Checking row " + i + " for SteamID " + steamID.ToString() + ". Array values: ID: \n"
                    + pugStatus.players[i, 0] + " Name: " + pugStatus.players[i, 1]);

                if (pugStatus.players[i, 0] == steamID.ToString())
                {
                    return i;
                }
            }

            return -1;
        }
    }

    //This acts as a data pack, which we pass through the receiveCallback and the incoming data processor numerous times
    public class receiveState
    {
        public const int bufferSize = 256;
        public int bufferSizeReadable = bufferSize;
        public byte[] dataReceived = new byte[bufferSize];
        public StringBuilder receivedString = new StringBuilder();

        public int bytesRead;
        public bool IsNewData;
    }

    public class connectState
    {
        public string botPassword;
        public IPEndPoint botIPEndPoint;
    }

    public class sendState
    {
        public string sendMsg;
    }

    /* Our main data container for the pug bot interface. This will contain whether the pug is in progress,
     * the number of players, the players joined through steam, their steamid, map vote, etc.
     * Need to keep in mind, however, we don't want to replicate the pug bot's functions entirely, we want to
     * emulate it
     */
    public class CPUGData 
    {
        //bools
        public bool inProgress;
        public bool mapVoting;
        public bool detailed;
        public bool lookingForPlayers;
        public bool isPug = true;

        //ints
        public int numPlayers = 0;
        public int serverPort;
        public int redScore;
        public int blueScore;
        public int maxPlayers = 12;

        //strings
        public string serverIP;
        public string serverPassword;
        public string adminLogin;
        public string winMap;
        public string ircplayers;

        //arrays
        public string[,] mapVotes = new string[12, 2];
        public string[,] players = new string[12, 2];
    }
}
