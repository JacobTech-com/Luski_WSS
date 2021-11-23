using System.Collections.Generic;
using System.Linq;

namespace Luski_WSS
{
    public static class CallManager
    {
        private static readonly Dictionary<long, List<string>> PeopleInCalls = new();

        public static Call GetCall(long id)
        {
            return new Call(id);
        }

        public class Call
        {
            private readonly long CallID;
            private List<string> People
            {
                get
                {
                    if (PeopleInCalls.ContainsKey(CallID)) return PeopleInCalls[CallID];
                    else return null;
                }
                set 
                {
                    PeopleInCalls[CallID] = value;
                }
            }
            public Call(long Id)
            {
                CallID = Id;
            }

            public IReadOnlyList<string> GetIds()
            {
                if (People != null) return People.AsReadOnly();
                else return null;
            }

            public void UpdateUsers()
            {
                string[] db = Database.Read<string[]>("channels", "id", CallID.ToString(), "call_members");
                if (db != null) db = System.Array.Empty<string>();
                People = db.ToList();
            }
        }
    }
}
