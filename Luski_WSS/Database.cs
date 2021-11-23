using Npgsql;
using System;

namespace Luski_WSS
{
    public class Database
    {
        public const string ConectionString = $"SSL Mode=Disable;Persist Security Info=True;Password={HiddenInfo.Database.Password};Username={HiddenInfo.Database.Role};Database={HiddenInfo.Database.DatabaseName};Host={HiddenInfo.Database.Address}";

        public static NpgsqlParameter CreateParameter(string name, object value)
        {
            return new NpgsqlParameter(name, value);
        }

        public static NpgsqlParameter CreateJsonParameter(string name, object value)
        {
            NpgsqlParameter b = new(name, value)
            {
                NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb
            };
            return b;
        }

        public static void Insert(string table, params NpgsqlParameter[] Parameters)
        {
            using NpgsqlConnection con = new(ConectionString);
            con.Open();
            using (NpgsqlCommand cmd = new())
            {
                cmd.Connection = con;
                string vals = "";
                foreach (NpgsqlParameter param in Parameters)
                {
                    vals += "@" + param.ParameterName + ", ";
                    cmd.Parameters.Add(param);
                }
                vals = vals.Remove(vals.Length - 2, 2);
                cmd.CommandText = $"INSERT INTO {table} ({vals.Replace("@", "")}) VALUES({vals})";
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
            con.Close();
        }

        public static NpgsqlConnection CreateConnection()
        {
            return new NpgsqlConnection(ConectionString);
        }

        public static void Update(string table, string condiction_column, string condiction_value, params NpgsqlParameter[] Parameters)
        {
            using NpgsqlConnection con = new(ConectionString);
            con.Open();
            using (NpgsqlCommand cmd = new())
            {
                cmd.Connection = con;
                string vals = "";
                foreach (NpgsqlParameter param in Parameters)
                {
                    vals += param.ParameterName + " = @" + param.ParameterName + ", ";
                    cmd.Parameters.Add(param);
                }
                vals = vals.Remove(vals.Length - 2, 2);
                cmd.CommandText = $"UPDATE {table} SET {vals} WHERE {condiction_column} = '{condiction_value}';";
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
            con.Close();
        }

        public static T Read<T>(string table, string condiction_column, string condiction_value, string returned_column)
        {
            return Read<T>($"SELECT {returned_column} FROM {table} WHERE {condiction_column} = '{condiction_value}';");
        }

        public static T Read<T>(string command)
        {
            using NpgsqlConnection con = new(ConectionString);
            con.Open();
            using NpgsqlCommand cmd = new();
            cmd.Connection = con;
            cmd.CommandText = command;
            if (cmd.ExecuteScalar() is DBNull)
            {
                con.Close();
                return default;
            }
            else
            {
                T bob = (T)cmd.ExecuteScalar();
                con.Close();
                return bob;
            }
        }

        public static void ExecuteNonQuery(string command)
        {
            using NpgsqlConnection connection = new(ConectionString);
            connection.Open();
            using (NpgsqlCommand cmd = new())
            {
                cmd.Connection = connection;
                cmd.CommandText = command;
                cmd.ExecuteNonQuery();
            }
            connection.Close();
        }
    }
}
