namespace ipgnSteamPugInterface
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;
    using System.Threading;
    using Steam4NET;


    // Handling messages for group chat involves using a Virtual Table to call the functions.
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    delegate Int32 NativeGetChatRoomEntry(IntPtr thisobj, UInt64 steamIDchat, Int32 iChatID, ref UInt64 steamIDuser, byte[] pvData, Int32 cubData, ref EChatEntryType peChatEntryType);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    delegate string NativeGetChatRoomName(IntPtr thisobj, UInt64 steamIDchat);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    delegate bool NativeSendChatMsg(IntPtr thisobj, UInt64 steamIDchat, EChatEntryType eChatEntryType, byte[] pvMsgBody, Int32 cubMsgBody);

    class ipgnBotSteamInterface
    {

        //Statics to be used later
        private static string steamClientVersion = "SteamClient008";
        private static string clientEngineVersion = "CLIENTENGINE_INTERFACE_VERSION002";
        private static string steamFriendsEngineVersion = "SteamFriends002";
        private static string clientFriendsEngineVersion = "CLIENTFRIENDS_INTERFACE_VERSION001";

        //declarations for steam interface
        IClientEngine clientEngine;
        IClientFriends clientFriends;
        ISteamClient008 steamClient;
        ISteamFriends002 steamFriends;

        //class declarations
        private ipgnBotPugInterface ipgnPugInterface;
        private ipgnBotChatParser ipgnBotParser;

        //For our pipe and user checks
        int pipe;
        int user;

        //Declerations for native functions
        NativeGetChatRoomEntry getChatMsg;
        NativeGetChatRoomName getChatName;
        NativeSendChatMsg sendChatMsg;

        //Vars for group chats
        bool groupChatEnabled;
        string groupStatusMsg;

        //Dictionary for the date and time
        Dictionary<ulong, DateTime> sessionInfo;

        //declare callbacks for events
        Callback<FriendChatMsg_t> chatCallback; //When someone sends a message directly to the bot
        Callback<PersonaStateChange_t> stateCallback; //When someone changes their status (offline/away/etc)
        Callback<ChatRoomMsg_t> chatRoomCallback; //When someone posts a message in a chat room

        Callback<UserRequestingFriendship_t> friendRequestCallback; //We need to automatically add new friends
        Callback<FriendAdded_t> friendAddedCallback; //So we know if a friend hath been added
        Callback<FriendInvited_t> friendInvitedCallback; //debugging
        //we need to find a callback for when someone removes us from friends

        //This is executed on object init, setting up our callbacks
        public ipgnBotSteamInterface()
        {
            groupChatEnabled = false;

            sessionInfo = new Dictionary<ulong, DateTime>();

            ipgnBotParser = new ipgnBotChatParser();

            //Create the callbacks
            chatCallback = new Callback<FriendChatMsg_t>(chatMessage, FriendChatMsg_t.k_iCallback);
            stateCallback = new Callback<PersonaStateChange_t>(stateChange, PersonaStateChange_t.k_iCallback);
            chatRoomCallback = new Callback<ChatRoomMsg_t>(chatRoomMessage, ChatRoomMsg_t.k_iCallback);

            friendAddedCallback = new Callback<FriendAdded_t>(friendAdded, FriendAdded_t.k_iCallback);
            friendRequestCallback = new Callback<UserRequestingFriendship_t>(friendRequest, UserRequestingFriendship_t.k_iCallback);
            friendInvitedCallback = new Callback<FriendInvited_t>(friendInvited, FriendInvited_t.k_iCallback);
        }

        public void ipgnPugInterfacePass(ipgnBotPugInterface interfaceHandle)
        {
            ipgnPugInterface = interfaceHandle;
            ipgnBotParser.ipgnPugInterfacePass(interfaceHandle);
        }

        public bool GetSteamClient()
        {
            if (!Steamworks.Load())
                return false;

            steamClient = Steamworks.CreateInterface<ISteamClient008>(steamClientVersion);
            clientEngine = Steamworks.CreateInterface<IClientEngine>(clientEngineVersion);

            if (steamClient == null)
                return false;

            if (clientEngine == null)
                return false;

            Program.logToWindow("Successfully found the steam client");

            return true;
        }

        public bool GetPipe()
        {
            if (pipe != 0)
            {
                steamClient.ReleaseSteamPipe(pipe);
            }

            pipe = steamClient.CreateSteamPipe();

            if (pipe == 0)
                return false;

            Program.logToWindow("Successfully opened a steam pipe");

            return true;
        }

        public bool GetUser()
        {
            if (user != 0)
            {
                steamClient.ReleaseUser(pipe, user);
            }

            user = steamClient.ConnectToGlobalUser(pipe);

            if (user == 0)
                return false;

            Program.logToWindow("Found steam user");

            return true;
        }

        public bool GetInterface()
        {
            steamFriends = Steamworks.CastInterface<ISteamFriends002>(steamClient.GetISteamFriends(user, pipe, steamFriendsEngineVersion));

            if (steamFriends == null)
                return false;

            Program.logToWindow("Got ISteamFriends002 interface");

            clientFriends = Steamworks.CastInterface<IClientFriends>(clientEngine.GetIClientFriends(user, pipe, clientFriendsEngineVersion));

            if (clientFriends == null)
                return false;

            Program.logToWindow("Got IClientFriends interface");

            VTable vTable = new VTable(clientFriends.Interface);

            getChatMsg = vTable.GetFunc<NativeGetChatRoomEntry>(99);
            getChatName = vTable.GetFunc<NativeGetChatRoomName>(117);
            sendChatMsg = vTable.GetFunc<NativeSendChatMsg>(98);

            groupChatEnabled = true;
            groupStatusMsg = "Enabled with vTable offsets";

            CallbackDispatcher.SpawnDispatchThread(pipe);

            Program.logToWindow("Found steam interface. Listing current clans:");

            int numClans = steamFriends.GetClanCount();
            for (int i = 0; i < numClans; i++)
            {
                ulong clanId = steamFriends.GetClanByIndex(i);
                Program.logToWindow("Clan Num: " + i + " ID: " + clanId + " Name: " + steamFriends.GetClanName(clanId));
            }

            return true;
        }

        int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        public string GetGroupChatStatus()
        {
            if (groupChatEnabled)
                return "Working as intended";

            return groupStatusMsg ?? "Not enabled. (Requires restart)";
        }

        //all interfaces are a go, now callbacks and other necessary functions
        void stateChange(PersonaStateChange_t perState)
        {
            CSteamID stateChangeUId = new CSteamID(perState.m_ulSteamID);
            EPersonaState newState = steamFriends.GetFriendPersonaState(stateChangeUId);

            string uName = steamFriends.GetFriendPersonaName(stateChangeUId);
            //if (newState == EPersonaState.k_EPersonaStateOffline)
                //person is offline, remove from pug
                

            Program.logToWindow(uName + " (" + perState.m_ulSteamID + "/" + stateChangeUId + ") changed state to " + newState);
        }

        void friendAdded(FriendAdded_t addedFriend)
        {
            Program.logToWindow("New friend. Sending greeting");
            Thread.Sleep(14000); //waiting 14 seconds so we're sure this new fella is our friend
            CSteamID newFriendId = new CSteamID(addedFriend.m_ulSteamID);
            string newFriendName = steamFriends.GetFriendPersonaName(newFriendId);

            sendMessage(newFriendId, "Hello there, " + newFriendName + ". I am the steam chat half of the "
                + "iPGN TF2 PUG bot. Commands available to you are: none", false);
            sendMessage(newFriendId, "If you need assistance, don't hesitate to join us on IRC, "
                + "#tf2pug @ irc.gamesurge.net. A simple widget is available at http://tf2pug.ipgn.com.au/irc/", false);

            ulong ipgnClanId = 103582791430132508;

            clientFriends.InviteFriendToClan(newFriendId, ipgnClanId);
        }

        void friendRequest(UserRequestingFriendship_t userRequesting)
        {
            Program.logToWindow("new user requesting friendship:" + (CSteamID)userRequesting.m_ulSteamID);
            CSteamID friendRequestId = new CSteamID(userRequesting.m_ulSteamID);
            steamFriends.AddFriend(friendRequestId);
            clientFriends.AddFriend(friendRequestId);

            Program.logToWindow(steamFriends.GetFriendPersonaName(friendRequestId) + "requesting friendship. Added friend");
        }

        void friendInvited(FriendInvited_t invitedFriend)
        {
            return;
        }

        void chatMessage(FriendChatMsg_t chatMsg)
        {
            byte[] msgData = new byte[1024 * 4];
            EChatEntryType chatType = EChatEntryType.k_EChatEntryTypeChatMsg;

            int len = steamFriends.GetChatMessage(chatMsg.m_ulReceiver, (int)chatMsg.m_iChatID, msgData, msgData.Length, ref chatType);

            if (chatType == EChatEntryType.k_EChatEntryTypeTyping)
                return;

            len = Clamp(len, 1, msgData.Length);

            ipgnBotParser.IsGroupMsg = false;

            ipgnBotParser.Sender = new CSteamID(chatMsg.m_ulSender);
            ipgnBotParser.SenderName = steamFriends.GetFriendPersonaName(ipgnBotParser.Sender);

            ipgnBotParser.Receiver = new CSteamID(chatMsg.m_ulReceiver);
            ipgnBotParser.ReceiverName = steamFriends.GetFriendPersonaName(ipgnBotParser.Receiver);

            ipgnBotParser.Message = Encoding.UTF8.GetString(msgData, 0, len);
            ipgnBotParser.Message = ipgnBotParser.Message.Substring(0, ipgnBotParser.Message.Length - 1);
            ipgnBotParser.MessageType = chatType;
            ipgnBotParser.MessageTime = DateTime.Now;

            ipgnBotParser.parseMessage(ipgnBotParser.Message);
            if (!ipgnPugInterface.ipgnPugInterfaceSocket.Connected)
            {
                sendMessage(ipgnBotParser.Sender, "The bot is currently unavailable. Try again soon", false);
                return;
            }
            else if (ipgnBotParser.replyMessage != null)
                sendMessage(ipgnBotParser.Sender, ipgnBotParser.replyMessage, false);
        }

        void chatRoomMessage(ChatRoomMsg_t chatRoomMsg)
        {
            if (!groupChatEnabled)
                return;

            byte[] msgData = new byte[1024 * 4];
            EChatEntryType chatType = EChatEntryType.k_EChatEntryTypeInvalid;
            ulong chatter = 0;

            int len = getChatMsg(clientFriends.Interface, chatRoomMsg.m_ulSteamIDChat,(int)chatRoomMsg.m_iChatID, ref chatter, msgData, msgData.Length, ref chatType);

            len = Clamp(len, 1, msgData.Length);

            ipgnBotParser.IsGroupMsg = true;
            ipgnBotParser.ChatRoom = chatRoomMsg.m_ulSteamIDChat;
            ipgnBotParser.ChatRoomName = getChatName(clientFriends.Interface, ipgnBotParser.ChatRoom);

            ipgnBotParser.Sender = new CSteamID(chatRoomMsg.m_ulSteamIDUser);
            ipgnBotParser.SenderName = steamFriends.GetFriendPersonaName(ipgnBotParser.Sender);

            ipgnBotParser.Receiver = ipgnBotParser.Sender;
            ipgnBotParser.ReceiverName = ipgnBotParser.SenderName;

            ipgnBotParser.Message = Encoding.UTF8.GetString(msgData, 0, len);
            ipgnBotParser.Message = ipgnBotParser.Message.Substring(0, ipgnBotParser.Message.Length - 1);
            ipgnBotParser.MessageType = chatType;
            ipgnBotParser.MessageTime = DateTime.Now;

            //Now we leave the rest to the bot (parsing, logging, etc);
            ipgnBotParser.parseMessage(ipgnBotParser.Message);
            if (!ipgnPugInterface.ipgnPugInterfaceSocket.Connected)
            {
                sendMessage(ipgnBotParser.ChatRoom, "The bot is currently unavailable. Try again soon", true);
                return;
            }
            else if (ipgnBotParser.replyMessage != null)
                sendMessage(ipgnBotParser.ChatRoom, ipgnBotParser.replyMessage, true); 
        }

        public void sendMessage(CSteamID botTarget, string botMessage, bool IsGroupMsg)
        {
            if (IsGroupMsg)
            {
                sendChatMsg(clientFriends.Interface, botTarget, ipgnBotParser.MessageType, System.Text.Encoding.UTF8.GetBytes(botMessage), botMessage.Length + 1);
                Program.logToWindow("Sent message to groupchat" + botTarget + ": " + botMessage + " (Type: " + ipgnBotParser.MessageType + ")");
            }
            else
            {
                steamFriends.SendMsgToFriend(botTarget, EChatEntryType.k_EChatEntryTypeChatMsg, System.Text.Encoding.UTF8.GetBytes(botMessage), botMessage.Length + 1);
                Program.logToWindow("Sent private message to " + botTarget + ": " + botMessage + " (Type: " + EChatEntryType.k_EChatEntryTypeChatMsg + ")");
            }
        }
    }
}