﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace OpenWorldServer
{
    public static class JoiningsUtils
    {
        public static void LoginProcedures(ServerClient client, string data)
        {
            bool userPresent = false;

            client.username = data.Split('│')[1];
            client.password = data.Split('│')[2];

            string playerVersion = data.Split('│')[3];
            string joinMode = data.Split('│')[4];
            string playerMods = data.Split('│')[5];

            if (Server.savedClients.Find(fetch => fetch.username == client.username) != null)
            {
                client.isAdmin = Server.savedClients.Find(fetch => fetch.username == client.username).isAdmin;
            }
            else client.isAdmin = false;

            int devInt = 0;
            if (client.isAdmin || Server.allowDevMode) devInt = 1;

            int wipeInt = 0;
            if (client.toWipe) wipeInt = 1;

            int roadInt = 0;
            if (Server.usingRoadSystem) roadInt = 1;
            if (Server.usingRoadSystem && Server.aggressiveRoadMode) roadInt = 2;

            string name = Server.serverName;
            int countInt = Networking.connectedClients.Count;

            int chatInt = 0;
            if (Server.usingChat) chatInt = 1;

            int profanityInt = 0;
            if (Server.usingProfanityFilter) profanityInt = 1;

            int modVerifyInt = 0;
            if (Server.usingModVerification) modVerifyInt = 1;

            if (!CompareConnectingClientWithPlayerCount(client)) return;

            if (!CompareConnectingClientVersion(client, playerVersion)) return;

            if (!CompareConnectingClientWithWhitelist(client)) return;

            if (!ParseClientUsername(client)) return;

            void SendNewGameData()
            {
                PlayerUtils.SaveNewPlayerFile(client.username, client.password);

                float mmGC = Server.globeCoverage;
                string mmS = Server.seed;
                int mmOR = Server.overallRainfall;
                int mmOT = Server.overallTemperature;
                int mmOP = Server.overallPopulation;

                string settlementString = "";
                foreach (KeyValuePair<string, List<string>> pair in Server.savedSettlements)
                {
                    settlementString += pair.Key + ":" + pair.Value[0] + "»";
                }
                if (settlementString.Count() > 0) settlementString = settlementString.Remove(settlementString.Count() - 1, 1);

                Networking.SendData(client, "MapDetails│" + mmGC + "│" + mmS + "│" + mmOR + "│" + mmOT + "│" + mmOP + "│" + settlementString + "│" + devInt + "│" + wipeInt + "│" + roadInt + "│" + countInt + "│" + chatInt + "│" + profanityInt + "│" + modVerifyInt + "│" + name);
            }

            void SendLoadGameData()
            {
                string settlementString = "";
                foreach (KeyValuePair<string, List<string>> pair in Server.savedSettlements)
                {
                    if (pair.Value[0] == client.username) continue;
                    settlementString += pair.Key + ":" + pair.Value[0] + "»";
                }
                if (settlementString.Count() > 0) settlementString = settlementString.Remove(settlementString.Count() - 1, 1);

                string dataToSend = "UpdateSettlements│" + settlementString + "│" + devInt + "│" + wipeInt + "│" + roadInt + "│" + countInt + "│" + chatInt + "│" + profanityInt + "│" + modVerifyInt + "│" + name;

                if (client.giftString.Count() > 0)
                {
                    string giftsToSend = "";

                    foreach (string str in client.giftString)
                    {
                        giftsToSend += str + "»";
                    }
                    if (giftsToSend.Count() > 0) giftsToSend = giftsToSend.Remove(giftsToSend.Count() - 1, 1);

                    dataToSend += "│GiftedItems│" + giftsToSend;

                    client.giftString.Clear();
                }

                if (client.tradeString.Count() > 0)
                {
                    string tradesToSend = "";

                    foreach (string str in client.tradeString)
                    {
                        tradesToSend += str + "»";
                    }
                    if (tradesToSend.Count() > 0) tradesToSend = tradesToSend.Remove(tradesToSend.Count() - 1, 1);

                    dataToSend += "│TradedItems│" + tradesToSend;

                    client.tradeString.Clear();
                }

                foreach (ServerClient sc in Server.savedClients)
                {
                    if (sc.username == client.username)
                    {
                        sc.giftString.Clear();
                        sc.tradeString.Clear();
                        break;
                    }
                }

                SaveSystem.SaveUserData(client);

                Networking.SendData(client, dataToSend);
            }

            foreach (ServerClient savedClient in Server.savedClients)
            {
                if (savedClient.username.ToLower() == client.username.ToLower())
                {
                    userPresent = true;

                    client.username = savedClient.username;

                    if (savedClient.password == client.password)
                    {
                        if (!CompareClientIPWithBans(client)) return;

                        if (!CompareModsWithClient(client, playerMods)) return;

                        CompareConnectingClientWithConnecteds(client);

                        ConsoleUtils.UpdateTitle();

                        ConsoleUtils.LogToConsole("Player [" + client.username + "] " + "[" + ((IPEndPoint)client.tcp.Client.RemoteEndPoint).Address.ToString() + "] " + "Has Connected");

                        ServerUtils.RefreshClientCount(client);

                        if (joinMode == "NewGame")
                        {
                            SendNewGameData();
                            ConsoleUtils.LogToConsole("Player [" + client.username + "] Has Reset Game Progress");
                        }

                        else if (joinMode == "LoadGame")
                        {
                            PlayerUtils.GiveSavedDataToPlayer(client);
                            SendLoadGameData();
                        }
                    }

                    else
                    {
                        Networking.SendData(client, "Disconnect│WrongPassword");

                        client.disconnectFlag = true;
                        ConsoleUtils.LogToConsole("Player [" + client.username + "] Has Been Kicked For: [Wrong Password]");
                        return;
                    }

                    break;
                }
            }

            if (userPresent) return;

            else
            {
                if (!CompareClientIPWithBans(client)) return;

                if (!CompareModsWithClient(client, playerMods)) return;

                CompareConnectingClientWithConnecteds(client);

                ConsoleUtils.UpdateTitle();

                ConsoleUtils.LogToConsole("New Player [" + client.username + "] " + "[" + ((IPEndPoint)client.tcp.Client.RemoteEndPoint).Address.ToString() + "] " + "Has Connected For The First Time");

                PlayerUtils.SaveNewPlayerFile(client.username, client.password);

                if (joinMode == "NewGame")
                {
                    SendNewGameData();
                }

                else if (joinMode == "LoadGame")
                {
                    SendLoadGameData();
                    ConsoleUtils.LogToConsole("Player [" + client.username + "] Has Registered With Existing Save");
                }
            }
        }

        public static bool CompareModsWithClient(ServerClient client, string data)
        {
            if (client.isAdmin) return true;
            if (!Server.forceModlist) return true;

            string[] clientMods = data.Split('»');

            string flaggedMods = "";

            bool flagged = false;

            foreach (string clientMod in clientMods)
            {
                if (Server.whitelistedMods.Contains(clientMod)) continue;
                else if (Server.blacklistedMods.Contains(clientMod))
                {
                    flagged = true;
                    flaggedMods += clientMod + "»";
                }
                else if (!Server.enforcedMods.Contains(clientMod))
                {
                    flagged = true;
                    flaggedMods += clientMod + "»";
                }
            }

            foreach (string serverMod in Server.enforcedMods)
            {
                if (!clientMods.Contains(serverMod))
                {
                    flagged = true;
                    flaggedMods += serverMod + "»";
                }
            }

            if (flagged)
            {
                ConsoleUtils.LogToConsole("Player [" + client.username + "] " + "Doesn't Have The Required Mod Or Mod Files Mismatch!");
                flaggedMods = flaggedMods.Remove(flaggedMods.Count() - 1, 1);
                Networking.SendData(client, "Disconnect│WrongMods│" + flaggedMods);

                client.disconnectFlag = true;
                return false;
            }
            else return true;
        }

        public static void CompareConnectingClientWithConnecteds(ServerClient client)
        {
            foreach (ServerClient sc in Networking.connectedClients)
            {
                if (sc.username == client.username)
                {
                    if (sc == client) continue;

                    Networking.SendData(sc, "Disconnect│AnotherLogin");
                    sc.disconnectFlag = true;
                    break;
                }
            }
        }

        public static bool CompareConnectingClientWithWhitelist(ServerClient client)
        {
            if (!Server.usingWhitelist) return true;
            if (client.isAdmin) return true;

            foreach (string str in Server.whitelistedUsernames)
            {
                if (str == client.username) return true;
            }

            Networking.SendData(client, "Disconnect│Whitelist");
            client.disconnectFlag = true;
            ConsoleUtils.LogToConsole("Player [" + client.username + "] Tried To Join But Is Not Whitelisted");
            return false;
        }

        public static bool CompareConnectingClientVersion(ServerClient client, string clientVersion)
        {
            string latestVersion = "";

            try
            {
                WebClient wc = new WebClient();
                latestVersion = wc.DownloadString("https://raw.githubusercontent.com/TastyLollipop/OpenWorld/main/Latest%20Versions%20Cache");
                latestVersion = latestVersion.Split('│')[2].Replace("- Latest Client Version: ", "");
                latestVersion = latestVersion.Remove(0, 1);
                latestVersion = latestVersion.Remove(latestVersion.Count() - 1, 1);
            }
            catch { return true; }

            if (clientVersion == latestVersion) return true;
            else
            {
                Networking.SendData(client, "Disconnect│Version");
                client.disconnectFlag = true;
                ConsoleUtils.LogToConsole("Player [" + client.username + "] Tried To Join But Is Using Other Version");
                return false;
            }
        }

        public static bool CompareClientIPWithBans(ServerClient client)
        {
            foreach (KeyValuePair<string, string> pair in Server.bannedIPs)
            {
                if (pair.Key == ((IPEndPoint)client.tcp.Client.RemoteEndPoint).Address.ToString() || pair.Value == client.username)
                {
                    Networking.SendData(client, "Disconnect│Banned");
                    client.disconnectFlag = true;
                    ConsoleUtils.LogToConsole("Player [" + client.username + "] Tried To Join But Is Banned");
                    return false;
                }
            }

            return true;
        }

        public static bool CompareConnectingClientWithPlayerCount(ServerClient client)
        {
            if (client.isAdmin) return true;

            if (Networking.connectedClients.Count() >= Server.maxPlayers + 1)
            {
                Networking.SendData(client, "Disconnect│ServerFull");
                client.disconnectFlag = true;
                return false;
            }

            return true;
        }

        public static bool ParseClientUsername(ServerClient client)
        {
            if (string.IsNullOrWhiteSpace(client.username))
            {
                Networking.SendData(client, "Disconnect│Corrupted");
                client.disconnectFlag = true;
                return false;
            }

            if (!client.username.All(character => Char.IsLetterOrDigit(character) || character == '_' || character == '-'))
            {
                Networking.SendData(client, "Disconnect│Corrupted");
                client.disconnectFlag = true;
                return false;
            }

            else return true;
        }
    }
}