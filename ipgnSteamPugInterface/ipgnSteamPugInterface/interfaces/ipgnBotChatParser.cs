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
            /*string logMessage;
            
            if (this.IsGroupMsg)
                logMessage = (this.SenderName + " (" + this.ChatRoomName + "): " + msg);
            else
                logMessage = (this.SenderName + " (PRIVATE): " + msg);

            Program.logToWindow(logMessage);
            */

            if (msg == "!status")
            {
                if (!pugStatus.isPug)
                {
                    this.replyMessage = "No pug in progress. Type !pug to start one";
                    return;
                }
                
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
            }
            else if ((msg == "!join") || (msg == "!j") || (msg == "!add"))
            {
                if (pugStatus.isPug)
                {
                    int playerIndex = ipgnPugInterface.GetPlayerIndex(this.Sender);

                    if (playerIndex >= 0)
                    {
                        if (this.IsGroupMsg)
                            this.replyMessage = this.SenderName + " is already in the pug";
                        else
                            this.replyMessage = "You are already in the pug";
                    }
                    else if (pugStatus.inProgress || pugStatus.detailed || pugStatus.mapVoting)
                    {
                        this.replyMessage = "There is already a pug in progress. Please wait for the next one";
                    }
                    else
                    {
                        if (ipgnPugInterface.addPlayer(this.Sender, this.SenderName))
                        {
                            if (this.IsGroupMsg)
                                this.replyMessage = this.SenderName + " has been added to the pug. " + slotsRemaining();
                            else
                                this.replyMessage = "You have been added to the pug. " + slotsRemaining();
                        }
                        else
                        {
                            this.replyMessage = "Unable to add you to the pug";
                        }
                    }
                }
                else
                {
                    this.replyMessage = "No pug in progress. Type !pug to start one";
                }

                return;
            }
            else if ((msg == "!leave") || (msg == "!l"))
            {
                if (pugStatus.inProgress || pugStatus.detailed || pugStatus.mapVoting)
                {
                    this.replyMessage = "It is too late to leave the pug. Please request a replacement once in the server.";
                }
                else
                {
                    if (ipgnPugInterface.removePlayer(Sender))
                    {
                        this.replyMessage = this.SenderName + " has left the pug. " + slotsRemaining();
                    }
                    else
                    {
                        this.replyMessage = "ERROR: Unable to remove " + this.SenderName + " from the pug.";
                    }
                }
                return;
            }
            else if (msg == "!map")
            {
                return;
            }
            else if (msg == "!details")
            {
                return;
            }
            else if (msg == "!players")
            {
                if (pugStatus.isPug)
                    this.replyMessage = currentPlayers();
                else
                    this.replyMessage = "No pug in progress. Type !pug to start one";
            }
            else
            {
                this.replyMessage = null;
                return;
            }
        }

        public string chatFormat(string msg)
        {
            return "<< " + msg + " >>";
        }

        public string slotsRemaining()
        {
            return "(" + (pugStatus.maxPlayers - pugStatus.numPlayers) + "/" + pugStatus.maxPlayers + " slots remaining)";
        }

        public string currentPlayers()
        {
            string steamPlayers = "";

            for (int i = 0; i < pugStatus.players.GetLength(0); i++)
            {
                if (pugStatus.players[i, 0] == null)
                    break;

                if (i == 0)
                    steamPlayers = pugStatus.players[i, 1] + ", ";
                else
                {
                    steamPlayers = steamPlayers + ", " + pugStatus.players[i, 1];
                }
            }

            return "STEAM: " + steamPlayers + " IRC: " + pugStatus.ircplayers;
        }
    }
}
