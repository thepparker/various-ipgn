namespace ipgnSteamPugInterface
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;

    public class settingsHandler
    {

        public string botIP;
        public int botPort;
        public string botPassword;
        public bool firstRun;

        public settingsHandler()
        {
            //load settings
            string filePath = Environment.CurrentDirectory + "\\ipgn_settings.txt";
            if (File.Exists(filePath))
            {
                StreamReader fileStream = new StreamReader(filePath);

                while (!fileStream.EndOfStream)
                {
                    string[] splitStream;
                    splitStream = fileStream.ReadLine().Split('=') ;
                    foreach (string setting in splitStream)
                    {
                        if (splitStream[0] == "botIP")
                            botIP = splitStream[1];
                        else if (splitStream[0] == "botPort")
                            botPort = Convert.ToInt32(splitStream[1]);
                        else if (splitStream[0] == "botPassword")
                            botPassword = splitStream[1];
                        else
                            Program.logToFile("Invalid setting '" + splitStream[0] + "' specified");
                    }
                }
                fileStream.Close();
                firstRun = false;
            }
            else
            {
                StreamWriter fileStream = new StreamWriter(filePath);
                fileStream.WriteLine("botIP=1.2.3.4");
                fileStream.WriteLine("botPort=1234");
                fileStream.WriteLine("botPassword=reindeer");

                fileStream.Close();

                botIP = "1.2.3.4";
                botPort = 1234;
                botPassword = "reindeer";
                firstRun = true;
            }
        }
    }
}
