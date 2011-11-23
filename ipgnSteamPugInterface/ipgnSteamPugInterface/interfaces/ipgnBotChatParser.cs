namespace ipgnSteamPugInterface
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Text;
    using Steam4NET;

    class ipgnBotChatParser
    {
        CPUGData pugStatus;

        public bool IsGroupMsg;

        public CSteamID ChatRoom;
        public string ChatRoomName;

        public CSteamID Sender;
        public string SenderName;

        public CSteamID Receiver;
        public string ReceiverName;

        public string Message;
        public DateTime MessageTime;
        public EChatEntryType MessageType;

        public string replyMessage;

        //declare handle for pug interface
        private ipgnBotPugInterface ipgnPugInterface;

        //passed from program -> steam interface -> here
        public void ipgnPugInterfacePass(ipgnBotPugInterface interfaceHandle)
        {
            ipgnPugInterface = interfaceHandle;
            pugStatus = ipgnPugInterface.pugStatus;
        }

        public void parseMessage(string msg)
        {
            string logMessage;
            
            if (this.IsGroupMsg)
                logMessage = (this.SenderName + " (" + this.ChatRoomName + "): " + msg);
            else
                logMessage = (this.SenderName + " (PRIVATE): " + msg);

            Program.logToWindow(logMessage);

            if (msg == "!status")
            {
                if (pugStatus.detailed)
                {
                    this.replyMessage = "Waiting for the pug to start on " + pugStatus.winMap;
                }
                else if (pugStatus.inProgress)
                {
                    this.replyMessage = "The game is currently in progress on " + pugStatus.winMap + ". Approximately <timer> timeleft. Scores: Red: "
                        + pugStatus.redScore + " Blue: " + pugStatus.blueScore;
                }
                else if (pugStatus.lookingForPlayers)
                {
                    this.replyMessage = "Currently looking for more players " + slotsRemaining();
                }
                else if (pugStatus.mapVoting)
                {
                    this.replyMessage = "Currently in map voting";
                }
                else
                    this.replyMessage = "No pug in progress. Type !pug to start one";
            }
            else if ((msg == "!join") || (msg == "!j") || (msg == "!add"))
            {
                this.replyMessage = "You are now in the pug! (just kidding)";
            }
            else if ((msg == "!leave"))
            {

            }
            else
            {
                this.replyMessage = null;
            }
        }

        public string chatFormat(string msg)
        {
            return "« " + msg + " »";
        }

        public string slotsRemaining()
        {
            return "(" + (pugStatus.maxPlayers - pugStatus.numPlayers) + "/" + pugStatus.maxPlayers + " slots remaining)";
        }
    }
}
