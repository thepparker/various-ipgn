namespace ipgnSteamPugInterface
{
    using System;
    using System.Collections;
    using System.Text;
    using System.Net;
    using System.Net.Sockets;
    using System.Windows.Forms;
    using System.Threading;

    class ipgnBotPugInterface
    {
        Socket ipgnPugInterfaceSocket;
        CPUGData pugStatus;

        private const string ConnectionFailed = "Unable to connect to the pug bot";
        private const string ConnectionSuccess = "Connected to the pug bot";
        private const string ConnectionClosed = "Connection closed by the pug bot, WTF?";

        public bool connectedToBot;

        public static IPEndPoint botIPEndPoint;
        public static string botPassword;

        public ipgnBotPugInterface()
        {
            ipgnPugInterfaceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            pugStatus = new CPUGData();
        }

        /* Socket setup and control, including reading/receiving/connecting
         * This is essential for connecting to the pug bot, however this method is not set in stone.
         * I think perhaps a UDP listen socket on both this bot and the pug bot may be better than a
         * constantly open TCP socket on the localhost, though no bandwidth cost will be induced either way
         * and TCP is substantially more reliable.
         */

        public bool connectToPugBot(IPEndPoint pugBotAddress, string interfacePassword)
        {
            botIPEndPoint = pugBotAddress;
            botPassword = interfacePassword;

            try
            {
                ipgnPugInterfaceSocket.Connect(pugBotAddress);
            }
            catch (SocketException)
            {
                onSocketError(ConnectionFailed);
                onConnectionSuccess(false);
                return false;
            }

            onConnectionSuccess(true);
            //ipgnPugInterfaceSocket.Blocking = false; -- we don't need non blocking, we're using async methods

            //send start notice to bot with password
            sendBotCommand("STARTUP!" + interfacePassword);

            //start listening
            receiveBotCommand();

            return true;
        }

        private void onSocketError(string error)
        {
            //if (String.Compare(error, ConnectionClosed) == 0)
            //    connectToPugBot(botIPEndPoint, botPassword);
            
            Program.logToWindow("Socket had error: " + error);
        }
        private void onConnectionSuccess(bool connected)
        {
            if (connected)
            {
                Program.logToWindow("Successfully established socket connection to the pug bot");
                connectedToBot = true;
            }
            else
            {
                Program.logToWindow("No connection to the pug bot");
                connectedToBot = false;
            }
        }

        public void sendBotCommand(string command)
        {
            if (connectedToBot)
            {
                byte[] commandPacket = ASCIIEncoding.ASCII.GetBytes(command);

                ipgnPugInterfaceSocket.BeginSend(commandPacket, 0, commandPacket.Length, SocketFlags.None, new AsyncCallback(sendBotCallback), this);
            }
            else
            {
                Program.logToWindow("Unable to send data - lost socket connection. Attempting to reconnect");

                connectToPugBot(botIPEndPoint, botPassword);
                Thread.Sleep(4000);
                if (!connectedToBot)
                {
                    MessageBox.Show("Can't reconnect. Closing");
                    Application.Exit();
                }
                byte[] commandPacket = ASCIIEncoding.ASCII.GetBytes(command);
                ipgnPugInterfaceSocket.BeginSend(commandPacket, 0, commandPacket.Length, SocketFlags.None, new AsyncCallback(sendBotCallback), this);
            }
        }

        void sendBotCallback(IAsyncResult asyncResult)
        {
            ipgnPugInterfaceSocket.EndSend(asyncResult);
        }

        public void receiveBotCommand()
        {
            receiveState currentState = new receiveState();
            currentState.IsNewData = false;
            currentState.dataReceived = new byte[256];
            
            ipgnPugInterfaceSocket.BeginReceive(currentState.dataReceived, 0, currentState.bufferSizeReadable, 
                SocketFlags.None, new AsyncCallback(receiveCallback), currentState);
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
            else
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
        }
    }

    //This acts as a data pack, which we pass through the receiveCallback and the incomingData processor numerous times
    public class receiveState
    {
        public const int bufferSize = 256;
        public int bufferSizeReadable = bufferSize;
        public byte[] dataReceived = new byte[bufferSize];
        public StringBuilder receivedString = new StringBuilder();

        public int bytesRead;
        public bool IsNewData;
    }


    /* Our main data container for the pug bot interface. This will contain whether the pug is in progress,
     * the number of players, the players joined through steam, their steamid, map vote, etc.
     * Need to keep in mind, however, we don't want to replicate the pug bot's functions entirely, we want to
     * emulate them
     */
    public class CPUGData 
    {
        //bools
        public bool inProgress;
        public bool mapVoting;
        public bool detailed;

        //ints
        public int numPlayers;
        public int serverPort;

        //strings
        public string serverIP;
        public string serverPassword;
        public string adminLogin;
        public string[] mapVotes;
    }
}
