using Npgsql;
using System.Data;

namespace UAS_PAA.Helpers
{
    public class SqlDbHelpers
    {
        private NpgsqlConnection connection;
        private readonly string __constr;

        public SqlDbHelpers(string pConstr)
        {
            __constr = pConstr;
            connection = new NpgsqlConnection(__constr);
        }

        public NpgsqlCommand getNpgsqlCommand(string query)
        {
            // Cek koneksi
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var cmd = new NpgsqlCommand(query, connection)
            {
                CommandType = CommandType.Text
            };

            return cmd;
        }

        public void closeConnection()
        {
            if (connection.State == ConnectionState.Open)
            {
                connection.Close();
            }
        }

        public List<Dictionary<string, object>> ReadList(string query, Dictionary<string, object>? parameters = null)
        {
            var result = new List<Dictionary<string, object>>();
            using var cmd = getNpgsqlCommand(query);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                result.Add(row);
            }

            return result;
        }

        // Tambahan helper method

        public List<Dictionary<string, object>> QueryList(string query, int id)
        {
            return ReadList(query, new Dictionary<string, object>
            {
                { "@id", id }
            });
        }

        public Dictionary<string, object>? QuerySingle(string query, Dictionary<string, object> parameters)
        {
            var list = ReadList(query, parameters);
            return list.FirstOrDefault();
        }
    }
}
