namespace ipgnSteamPugInterface
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Text;
    using Steam4NET;

    class ipgnBotChatParser
    {
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

        public void parseMessage(string chatMessage)
        {
            string logMessage;

            if (this.IsGroupMsg)
                logMessage = (this.SenderName + " (" + this.ChatRoomName + "): " + chatMessage);
            else
                logMessage = (this.SenderName + " (PRIVATE): " + chatMessage);

            Program.logToWindow(logMessage);
            logToFile(logMessage);

            if (chatMessage == "!status")
                this.replyMessage = "Status is: no deal";

            else if (chatMessage == "!join")
                this.replyMessage = "You are now in the pug! (just kidding)";
            
            else
                this.replyMessage = null;
        }

        private void logToFile(string logMessage)
        {
            return;
        }
    }
}
