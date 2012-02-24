using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace SteamSelectorThreading
{
    class Program
    {
        //loading flag
        static bool doneLoading;

        //struct for storing game properties
        struct SteamItem
        {
            public string gameName;
            public string gameID;
            public string hoursPlayed;

        };

        //pause method to wait for a key to be pressed to display messages
        public static void pause()
        {
            Console.WriteLine("Press any key to continue . . . ");
            Console.ReadKey(true);

        }


        public static void loading()
        {   //loops while the xml hasn't fully been parsed displays .
            while (!doneLoading)
            {
                Console.Write(".");
                System.Threading.Thread.Sleep(50);
            }
        }

        static void Main(string[] args)
        {
            //initializes profile type and id
            string theId = "";
            int pType = 0;

            //if settings don't exist
            if (!File.Exists("settings.ini"))
            {
                //prompts for profile type. catches format exceptions, and loops until valid input is given 
                Console.WriteLine("First off, look at your Steam Community profile. \nDoes it look like steamcommunity.com/id/myCustomId?\nOr steammcommunity.com/profiles/123456789998462496843? \nEnter 1 if it contains id, 2 if it contains profiles:");
                try
                {
                    pType = Convert.ToInt32(Console.ReadLine());
                    if (pType != 1 && pType != 2)
                        throw new FormatException();
                }
                catch (FormatException FE)
                {
                    while (pType < 1 || pType > 2)
                    {
                        Console.WriteLine("Invalid Choice. 1 if your URL contains id, 2 if it contains profiles:");
                        try
                        {
                            pType = Convert.ToInt32(Console.ReadLine());
                        }
                        catch (FormatException f)
                        {
                            continue;
                        }
                    }
                }

                Console.Clear();

                //prompts for ids depending on what type of profile was chosen
                if (pType == 1)
                {
                    Console.WriteLine("OK. Now enter the last part of your custom Steam url.\nex : steamcommunity.com/id/ENTERTHISPARTHERE: ");
                    theId = Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("OK. Now Enter your the ID number found at the end of your Steam url.\nex : steamcommunity.com/profiles/123456489979897 (Enter the Numbers Only): ");
                    theId = Console.ReadLine();
                }

                //writes profile type and id to settings file. Throws an exception if the user doesn't have the correct permissions
                try
                {
                    TextWriter writer = new StreamWriter("settings.ini");
                    writer.WriteLine(theId);
                    writer.WriteLine(pType);
                    writer.Close();
                    Console.Clear();
                }
                catch (UnauthorizedAccessException UA)
                {
                    Console.WriteLine("Cannot write settings file.\nMake sure you have write permissions to directory containing this exe, or run this program as an administrator");
                    pause();
                    return;
                }
                Console.WriteLine("Settings Saved!");
                pause();
            }
            //if settings.ini does exist
            else
            {
                //reads from the id and profile type from the file
                TextReader reader = new StreamReader("settings.ini");
                theId = reader.ReadLine();
                pType = Convert.ToInt32(reader.ReadLine());
                reader.Close();

            }

            //sets the loading flag to false, starts the loading thread.
            Console.Write("Getting your games now...");
            doneLoading = false;
            Thread t = new Thread(loading);
            t.Start();

            //creates a new steam item list
            List<SteamItem> theList = new List<SteamItem>();

            Uri theAddress;

            //sets the uri to the correct address based on the profile type
            if (pType == 1)
            {
                theAddress = new Uri("http://steamcommunity.com/id/" + theId + "/games?xml=1?tab=all");

            }
            else
            {
                theAddress = new Uri("http://steamcommunity.com/profiles/" + theId + "/games?xml=1?tab=all");

            }
            string theResult;

            try
            {   //creates a new request to the site. Will throw an error if it can't connect
                HttpWebRequest theRequest = WebRequest.Create(theAddress) as HttpWebRequest;

                using (HttpWebResponse theResponse = theRequest.GetResponse() as HttpWebResponse)
                {
                    // Gets the response XML from the steam community  
                    StreamReader theReader = new StreamReader(theResponse.GetResponseStream());

                    //sets the response to a string
                    theResult = theReader.ReadToEnd();
                }
            }
            catch (WebException we)
            {
                doneLoading = true;
                Console.WriteLine("Network Error. Cannot connect to Steam Community. Make sure your internet and the Steam Community are both up");
                pause();
                return;

            }

            //new xml document and loads the xml from the site into it
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(theResult);

            //used as a flag to see if the user's profile is private
            XmlNodeList errorCheck = xmlDoc.GetElementsByTagName("error");

            //if there is an error node(happens if the profile is private) quits the program
            if (errorCheck[0] != null)
            {
                doneLoading = true;
                Console.WriteLine("Access Error. Your Steam Profile must be public to use this program.");
                pause();
                return;

            }

            //grabs all of the game nodes from the xml doc
            XmlNodeList theNodes = xmlDoc.GetElementsByTagName("game");

            foreach (XmlNode node in theNodes)
            {
                //creates a new steam item, gets the game name, appId, and hours played(if they exist, otherwise it'll set it to 0) from the node and adds it to the list
                SteamItem tempItem = new SteamItem();
                tempItem.gameName = node["name"].InnerText;
                tempItem.gameID = node["appID"].InnerText;
                if (node["hoursOnRecord"] == null)
                {
                    tempItem.hoursPlayed = "0";
                }
                else
                {
                    tempItem.hoursPlayed = node["hoursOnRecord"].InnerText;
                }
                theList.Add(tempItem);



            }

            //stops the loading process
            doneLoading = true;
            Console.WriteLine("DONE");
            pause();
            Console.Clear();




            //new random generator
            Random theGenerator = new Random();

            //toRun is the position of the steam item in the list to run, choice is the first choice to make(any game you own vs. those you've never played), innerChoice is second( for determining whether a game is dlc or not)
            int toRun = 0;
            int choice = 0;
            int innerChoice = 0;


            while (choice < 1 || choice > 2)
            {
                //prompts whether to select from any game you own or games you've never played. Catches the exception and restarts the loop if there's an error
                Console.Clear();
                Console.WriteLine("STEAM SELECTOR MACH 2 TURBO EDITION by Josh Bing");
                Console.WriteLine("1.Any Game You Own\n2.Games You've Never Played");
                try
                {
                    choice = Convert.ToInt32(Console.ReadLine());
                }
                catch (FormatException FE)
                {
                    continue;
                }
            }

            Console.Clear();
            //valid game flag
            bool valid = false;

            //any game they own
            if (choice == 1)
            {
                valid = false;
                //loops while not a valid game
                while (!valid)
                {
                    //gets a random index, displays the game to run, and prompts to reroll or play it. catches format exception as well
                    toRun = theGenerator.Next(0, theList.Count);
                    valid = true;
                    Console.WriteLine("You're about to play " + theList[toRun].gameName + "!\nIs this a DLC?\n1. Yes, Reroll Please.\n2. Nope, I'll play it.");
                    try
                    {
                        innerChoice = Convert.ToInt32(Console.ReadLine());
                    }
                    catch (FormatException fe)
                    {
                        valid = false;
                        Console.Clear();
                        continue;
                    }
                    //causes the loop to restart
                    if (innerChoice == 1)
                    {
                        valid = false;
                    }

                    Console.Clear();

                }


            }
            //just games they've never played
            if (choice == 2)
            {
                valid = false;
                //while not a valid choice
                while (!valid)
                {
                    //gets a random index, checks to see if there are any hours played. Will continue until it finds one that has no hours
                    toRun = theGenerator.Next(0, theList.Count);
                    if (theList[toRun].hoursPlayed.Equals("0"))
                    {
                        //functions exactly above(prompts to either reroll or break out of loop and play it)
                        valid = true;
                        Console.WriteLine("You're about to play " + theList[toRun].gameName + "!\nIs this a DLC?\n1. Yes, Reroll Please.\n2. Nope, I'll play it.");
                        try
                        {
                            innerChoice = Convert.ToInt32(Console.ReadLine());
                        }
                        catch (FormatException Fe)
                        {
                            valid = false;
                            Console.Clear();
                            continue;
                        }
                        if (innerChoice == 1)
                            valid = false;
                        Console.Clear();

                    }
                }

            }


            //creates a new process
            Process launchGame = new Process();

            //doesn't notify the program when the process terminates(not needed in this case, because I terminate it right after launch)
            launchGame.EnableRaisingEvents = false;

            //Basically, this will cause the process to look at the registry, see the steam protocol and send the signal to steam to launch the game
            //this method works for anything(for example, if you replaced the fileName with test.Doc, it'll attempt to open the local file test.Doc in whatever
            //word processor is installed
            launchGame.StartInfo.FileName = "steam://rungameid/" + theList[toRun].gameID;

            //starts the process to send the signal to steam and then closes it, as it's no longer needed once the signal has been sent
            launchGame.Start();
            launchGame.Close();

        }
    }
}
