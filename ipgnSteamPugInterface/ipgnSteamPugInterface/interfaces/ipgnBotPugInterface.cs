namespace ipgnSteamPugInterface
{
    using System;
    using System.Collections;
    using System.Text;
    using System.Net;
    using System.Net.Sockets;

    class ipgnBotPugInterface
    {
        Socket ipgnPugInterfaceSocket;

        private const string ConnectionFailed = "Unable to connect to the pug bot";
        private const string ConnectionSuccess = "Connected to the pug bot";
        private const string ConnectionClosed = "Connection closed by the pug bot, WTF?";

        public bool connectedToBot;

        public ipgnBotPugInterface()
        {
            ipgnPugInterfaceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public bool connectToPugBot(IPEndPoint pugBotAddress, string interfacePassword)
        {
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
            //ipgnPugInterfaceSocket.Blocking = false;

            //send start notice
            sendBotCommand("STARTUP!" + interfacePassword);

            //start listening
            receiveBotCommand();

            return true;
        }

        private void onSocketError(string error)
        {
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

                //TODO: Write automatic reconnect method
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
                    reactToBot(currentMessage);
                    receiveBotCommand(); //start listening again
                }
            }
            else
            {
                //Do something with the data we've got, because there's no more coming immediately
                Program.logToWindow("Received a command or a response from the pug bot");

                string receivedMessage = currentState.receivedString.ToString();

                Program.logToWindow("Received data: " + receivedMessage);

                //parse this command and react
                reactToBot(receivedMessage);

                //go back to listening again
                receiveBotCommand();
            }
        }

        public void reactToBot(string botMessage)
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
}
