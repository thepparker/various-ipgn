namespace ipgnSteamPugInterface
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Threading;
    using System.Windows.Forms;
    using System.Net;
    using Steam4NET;

    static class Program
    {
        public static ipgnBotSteamInterface ipgnSteamInterface;
        public static ipgnBotPugInterface ipgnPugInterface;
        public static settingsHandler ipgnBotSettings;

        static mainWindow ipgnBotWindow;

        public static string botIP;
        public static int botPort;
        public static string botInterfacePassword;

        public static void logToWindow(string logString)
        {
            ipgnBotWindow.Print("[" + DateTime.Now + "] " + logString);
            logToFile(logString);
        }

        public static void logToFile(string logMessage)
        {
            return;
        }

        [STAThread]

        static void Main()
        {
            bool firstProcess;

            Mutex ipgnBotMutex = new Mutex
            (
                true,
                "ipgnSteamBot_19899asdfffkk9921", //hopefully a mutex ID not currently present... if it is, well then shit
                out firstProcess
            );

            if (!firstProcess)
            {
                //An application using our mutex is already running (presumably the bot), therefore we need to end
                //this will show an error at some point 
                MessageBox.Show("The bot is already running. If you can't see the process, check task manager");
                Thread.Sleep(10000);
                Application.Exit();
                return;
            }
            ipgnSteamInterface = new ipgnBotSteamInterface();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.EnableVisualStyles();

            ipgnBotWindow = new mainWindow();
            ipgnBotWindow.Show();

            bool waited = false;

            if (!ipgnSteamInterface.GetSteamClient()) //check to see if a client is running or not
            {
                logToWindow("Can't find steam client, exiting");
                Thread.Sleep(10000);
                return;
            }

            if (!ipgnSteamInterface.GetPipe()) //we can't get a pipe going of the steam api... usually means steam is still starting or something
            {
                logToWindow("Steam is currently starting up or something similar. Waiting for it to be ready");

                waited = true;

                while (!ipgnSteamInterface.GetPipe())
                {
                    Application.DoEvents();
                    Thread.Sleep(2000);
                }

                //just a double check to make sure we've got the pipe
                if (!ipgnSteamInterface.GetPipe())
                {
                    logToWindow("Something is fucky (Copyright Bubbles, TPB) and we couldn't get the pipe even after steam startup");
                    Thread.Sleep(10000);
                    return;
                }
            }

            //Now we need to get our user
            while (!ipgnSteamInterface.GetUser())
            {
                Application.DoEvents();
                Thread.Sleep(2000);
                logToWindow("Trying to get current steam user");
            }

            if (waited)
            {
                //We had to wait to get the pipe, so we should probably wait for steam to actually start
                Thread.Sleep(10000);
            }

            //Now let's try the user again
            if (!ipgnSteamInterface.GetUser())
            {
                logToWindow("Unable to get the steam user. Exiting");
                Thread.Sleep(10000);
                return;
            }
            
            //Check for the steam friends interface
            if (!ipgnSteamInterface.GetInterface())
            {
                logToWindow("Unable to get the friends interface. This could indicate a major steam change. I hope you know how to fix this. Contact bladez @ ipgn");
                Thread.Sleep(10000);
                /*No but really, if this is the case you'll probably either have to contact prithu@iinet.net.au (bladez),
                 * or if you're reading this comment you have the source, so you are able to update to the latest
                 * version of the Open Steam Works API available at http://opensteamworks.org/ (Update the Steam4NET
                 * project and recompile)
                 */ 
                
                return;
            }

            if (waited)
            {
                Thread.Sleep(10000);
                logToWindow("Chances are steam is now running properly");
            }

            //steam interface loaded, now try pug interface

            ipgnPugInterface = new ipgnBotPugInterface();
            ipgnBotSettings = new settingsHandler();

            botIP = ipgnBotSettings.botIP;
            botPort = ipgnBotSettings.botPort;
            botInterfacePassword = ipgnBotSettings.botPassword;

            if (ipgnPugInterface.connectToPugBot(new IPEndPoint(IPAddress.Parse(botIP), botPort), botInterfacePassword))
            {
                Program.logToWindow("We're inside the initial connection check");
                int i = 0;
                while (!ipgnPugInterface.connectedToBot)
                {
                    if (i > 20)
                        break;
                    Application.DoEvents();
                    Thread.Sleep(10);
                    i++;
                }
            }

            if (!ipgnPugInterface.connectedToBot)
            {
                MessageBox.Show("No socket. Exiting");

                Application.Exit();
                return;
            }

            while (firstProcess)
            {
                //Infinitely loop so we actually have an application ;D
                Application.DoEvents();
                Thread.Sleep(10);
            }

            GC.KeepAlive(ipgnBotMutex);

        }
    }
}
