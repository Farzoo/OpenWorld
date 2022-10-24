﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Verse;
using Verse.AI;

namespace OpenWorld
{
    public static class Networking
    {
        private static TcpClient connection;
        private static Stream s;
        private static NetworkStream ns;
        private static StreamWriter sw;
        private static StreamReader sr;

        public static string ip;
        public static string port;
        public static string username = "Offline User";
        public static string password;

        public static bool isTryingToConnect;
        public static bool isConnectedToServer;

        public static void TryConnectToServer()
        {
            isTryingToConnect = true;

            try
            {
                connection = new TcpClient(ip, int.Parse(port));
                s = connection.GetStream();
                sw = new StreamWriter(s);
                sr = new StreamReader(s);
                ns = connection.GetStream();

                isConnectedToServer = true;
                isTryingToConnect = false;

                Dialog_MPParameters.__instance.Close();

                Threading.GenerateThreads(1);
                ListenToServer();
            }

            catch
            {
                isTryingToConnect = false;
                Find.WindowStack.Add(new Dialog_MPTimeout());
            }
        }

        private static void ListenToServer()
        {
            Thread.Sleep(100);

            if (!Main._ParametersCache.isLoadingExistingGame) SendData("Connect│" + username + "│" + password + "│" + Main._ParametersCache.versionCode + "│" + "NewGame" + "│" + Main._MPGame.GetCompactedModList());
            else SendData("Connect│" + username + "│" + password + "│" + Main._ParametersCache.versionCode + "│" + "LoadGame" + "│" + Main._MPGame.GetCompactedModList());

            while (true)
            {
                if (!isConnectedToServer) return;

                Thread.Sleep(1);

                if (ns.DataAvailable)
                {
                    string data = sr.ReadLine();
                    data = Encryption.DecryptString(data);
                    Log.Message(data);

                    if (data != null)
                    {
                        if (data.StartsWith("Planet│"))
                        {
                            NetworkingHandler.PlanetHandle(data);
                        }

                        else if (data.StartsWith("Settlements│"))
                        {
                            NetworkingHandler.SettlementsHandle(data);
                        }

                        else if (data.StartsWith("Variables│"))
                        {
                            NetworkingHandler.VariablesHandle(data);
                        }

                        else if (data.StartsWith("PlayerList│"))
                        {
                            NetworkingHandler.PlayerListHandle(data);
                        }

                        else if (data == "NewGame│")
                        {
                            NetworkingHandler.NewGameHandle(data);
                        }

                        else if (data == "LoadGame│")
                        {
                            NetworkingHandler.LoadGameHandle(data);
                        }

                        else if (data.StartsWith("ChatMessage│"))
                        {
                            NetworkingHandler.ChatMessageHandle(data);
                        }

                        else if (data.StartsWith("Notification│"))
                        {
                            NetworkingHandler.NotificationHandle(data);
                        }

                        else if (data.StartsWith("Admin│"))
                        {
                            NetworkingHandler.AdminHandle(data);
                        }

                        else if (data.StartsWith("SettlementBuilder│"))
                        {
                            NetworkingHandler.SettlementBuilderHandle(data);
                        }

                        else if (data.StartsWith("│SentEvent│"))
                        {
                            NetworkingHandler.SentEventHandle(data);
                        }

                        else if (data.StartsWith("│SentTrade│"))
                        {
                            NetworkingHandler.SentTradeHandle(data);
                        }

                        else if (data.StartsWith("│SentBarter│"))
                        {
                            NetworkingHandler.SentBarterHandle(data);
                        }

                        else if (data.StartsWith("TradeRequest│"))
                        {
                            NetworkingHandler.TradeRequestHandle(data);
                        }

                        else if (data.StartsWith("BarterRequest│"))
                        {
                            NetworkingHandler.BarterRequestHandle(data);
                        }

                        else if (data.StartsWith("Spy│"))
                        {
                            NetworkingHandler.SpiedHandle(data);
                        }

                        else if (data.StartsWith("│SentSpy│"))
                        {
                            NetworkingHandler.SentSpyHandle(data);
                        }

                        else if (data.StartsWith("ForcedEvent│"))
                        {
                            NetworkingHandler.ForcedEventHandle(data);
                        }

                        else if (data.StartsWith("GiftedItems│"))
                        {
                            NetworkingHandler.GiftedHandle(data);
                        }

                        else if (data.StartsWith("FactionManagement│"))
                        {
                            NetworkingHandler.FactionManagementHandle(data);
                        }

                        else if (data.StartsWith("Disconnect│"))
                        {
                            NetworkingHandler.DisconnectHandle(data);
                        }
                    }
                }
            }
        }

        public static void SendData(string data)
        {
            try
            {
                sw.WriteLine(Encryption.EncryptString(data));
                sw.Flush();
            }

            catch { DisconnectFromServer(); }
        }

        public static void CheckConnection()
        {
            while(true)
            {
                if (!isConnectedToServer)
                {
                    DisconnectFromServer();
                    break;
                }

                Thread.Sleep(1000);

                SendData("Ping");

                Log.Message("Ping");
            }

            //while (true)
            //{
            //    if (!isConnectedToServer) break;

            //    Thread.Sleep(1000);

            //    try
            //    {
            //        if (!IsConnected(connection))
            //        {
            //            Find.WindowStack.Add(new Dialog_MPDisconnected());
            //            DisconnectFromServer();
            //            break;
            //        }
            //    }

            //    catch
            //    {
            //        Find.WindowStack.Add(new Dialog_MPDisconnected());
            //        DisconnectFromServer();
            //        break;
            //    }
            //}

            //bool IsConnected(TcpClient connection)
            //{
            //    try
            //    {
            //        TcpClient c = connection;

            //        if (c != null && c.Client != null && c.Client.Connected)
            //        {
            //            if (c.Client.Poll(0, SelectMode.SelectRead))
            //            {
            //                return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
            //            }
            //        }

            //        return true;
            //    }

            //    catch { return false; }
            //}
        }

        public static void DisconnectFromServer()
        {
            try { connection.Close(); }
            catch { }

            isConnectedToServer = false;
        }
    }
}