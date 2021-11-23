using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Luski_WSS
{
    internal class Program
    {
        public static WebSocketServer Server;
        public static bool ServerOnline = true;

        public class WSS : WebSocketBehavior
        {
            protected override void OnClose(CloseEventArgs e)
            {
                base.OnClose(e);
                try
                {
                    JObject data = new()
                    {
                        { "after", (int)Status.Offline },
                        { "before", Database.Read<int>("users", "wsstcp", ID, "status") },
                        { "id", Database.Read<long>("users", "wsstcp", ID, "id") }
                    };
                    JObject @out = new()
                    {
                        { "type", (int)DataType.Status_Update },
                        { "data", data }
                    };
                    Database.ExecuteNonQuery($"UPDATE users SET token = NULL::text, session_key=NULL::text, status={(int)Status.Offline}::integer, wsstcp=NULL::text WHERE wsstcp = '{ID}';");
                    Sessions.Broadcast(@out.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                try
                {
                    Console.WriteLine(e.Data);
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(e.Data);
                    if (!string.IsNullOrEmpty((string)data.key))
                    {
                        Console.WriteLine("Server try");
                        //check if it is the server
                        if ((string)data.key == HiddenInfo.Key)
                        {
                            Console.WriteLine("Server good");
                            Console.WriteLine(((SendType)(int)data.type).ToString());
                            switch ((SendType)(int)data.type)
                            {
                                case SendType.All:
                                    if ((DataType)(int)data.data.type == DataType.Message_Create)
                                    {
                                        string msg3 = (string)data.data.data.content;
                                        foreach (string item in Sessions.ActiveIDs.ToList())
                                        {
                                            string key = Database.Read<string>("users", "wsstcp", item, "session_key");
                                            if (!string.IsNullOrEmpty(key))
                                            {
                                                data.data.data.content = Convert.ToBase64String(Encryption.Encrypt(msg3, key));
                                                Sessions.SendTo(((object)data.data).ToString(), item);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Sessions.Broadcast(((object)data.data).ToString());
                                    }
                                    break;
                                case SendType.ID:
                                    Sessions.SendTo(((object)data.data).ToString(), (string)data.id);
                                    break;
                                case SendType.ID_Group:
                                    JArray users = (JArray)data.id_list;
                                    foreach (string user in users)
                                    {
                                        Sessions.SendTo(((object)data.data).ToString(), user);
                                    }
                                    break;
                                case SendType.Private:
                                    if ((DataType)(int)data.data.type == DataType.Call_Info)
                                    {
                                        CallManager.GetCall((long)data.data.id).UpdateUsers();
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        //might be call data or login sync
                        if (data.type is object && (int)data.type < (int)DataType.MAX && (int)data.type >= 0)
                        {
                            switch ((DataType)(int)data.type)
                            {
                                case DataType.Login:
                                    string token = (string)data.data.token;
                                    long? dbread = Database.Read<long?>("users", "login_token", token, "id");
                                    if (dbread != null)
                                    {
                                        string Token = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(dbread.ToString()))}.{Convert.ToBase64String(Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString()))}.{Convert.ToBase64String(Encoding.UTF8.GetBytes(new Random().Next(0, 1000000).ToString()))}.{Convert.ToBase64String(Encoding.UTF8.GetBytes(new Random().Next(0, 1000000).ToString()))}";
                                        Database.ExecuteNonQuery($"UPDATE users SET login_token = NULL::text, token='{Token}'::text, wsstcp='{ID}'::text WHERE id = '{dbread}';");
                                        JObject jobj = new()
                                        {
                                            { "type", (int)DataType.Login },
                                            { "token", Token }
                                        };
                                        Send(jobj.ToString());
                                    }
                                    break;
                                case DataType.Call_Data:// doo call data stuff
                                    CallManager.Call call = CallManager.GetCall((long)data.data.id);
                                    JObject calldata = new()
                                    {
                                        { "data", (string)data.data.data },
                                        { "from", Database.Read<long?>("users", "wsstcp", ID, "id") }
                                    };
                                    JObject @out = new()
                                    {
                                        { "type", (int)DataType.Call_Data },
                                        { "data", calldata }
                                    };
                                    foreach (string User in call.GetIds())
                                    {
                                        if (User == ID)
                                        {
                                            Sessions.SendTo(@out.ToString(), User);
                                        }
                                    }
                                    break;
                                case DataType.Key_Exchange:
                                    JObject keys = new()
                                    {
                                        { "key", (string)data.data.key },
                                        { "channel", (string)data.data.channel }
                                    };
                                    string id = Database.Read<string>("users", "id", ((long)data.data.to).ToString(), "wsstcp");
                                    if (!string.IsNullOrEmpty(id))
                                    {
                                        Sessions.SendTo(keys.ToString(), id);
                                    }
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public static async Task<int> Main(string[] args)
        {
            try
            {
                if (args == null || args.Length == 0) args = new string[2] { "10.100.0.153", "86" };//debug
                while (true)
                {
                    bool b = IsLocalIpAddress(IPAddress.Parse(args[0]));
                    if (b) break;
                }
                Server = new WebSocketServer(IPAddress.Parse(args[0]), int.Parse(args[1]), false);
                Server.AddWebSocketService<WSS>("/");
                Server.Start();
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return 0;
        }

        public static bool IsLocalIpAddress(IPAddress host)
        {
            try
            {
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                if (IPAddress.IsLoopback(host)) return true;
                foreach (IPAddress localIP in localIPs)
                {
                    if (host.Equals(localIP)) return true;
                }
            }
            catch { }
            return false;
        }
    }
}
