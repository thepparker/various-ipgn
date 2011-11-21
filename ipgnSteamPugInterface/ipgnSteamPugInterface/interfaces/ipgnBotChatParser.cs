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

        public void parseMessage(string chatMessage)
        {
            string logMessage;

            if (this.IsGroupMsg)
                logMessage = (this.SenderName + " (" + this.ChatRoomName + "): " + chatMessage);
            else
                logMessage = (this.SenderName + " (PRIVATE): " + chatMessage);

            Program.logToWindow(logMessage);

            if (chatMessage == "!status")
            {
                if (pugStatus.detailed)
                {
                    this.replyMessage = "« Waiting for the pug to start on " + ipgnPugInterface.pugStatus.winMap + "»";
                }
                else if (pugStatus.inProgress)
                {
                    this.replyMessage = "« The game is currently in progress on »";
                }
            }
            else if (chatMessage == "!join")
                this.replyMessage = "You are now in the pug! (just kidding)";

            else
                this.replyMessage = null;
        }
    }
}
