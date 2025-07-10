using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UAS_PAA.Helpers;

namespace UAS_PAA.Models
{
    public class LoginContext
    {
        private string __constr; 
        private string __ErrorMsg; 

        public LoginContext(string pConstr)
        {
            __constr = pConstr;
        }

        public List<Login> Autentifikasi(string p_username, string p_password, IConfiguration p_config)
        {
            List<Login> list1 = new List<Login>();

            string query = @"
                            SELECT u.id_users, u.username, u.email, r.id_role, r.nama_role
                            FROM users u
                            JOIN role r ON u.id_role = r.id_role
                            WHERE u.username = @username AND u.password = crypt(@password, u.password)";

            SqlDbHelpers db = new SqlDbHelpers(this.__constr);

            try
            {
                NpgsqlCommand cmd = db.getNpgsqlCommand(query); 
                cmd.Parameters.AddWithValue("@username", p_username); 
                cmd.Parameters.AddWithValue("@password", p_password);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var user = new Login()
                        {
                            Id_Users = Convert.ToInt32(reader["id_users"]),
                            Username = reader["username"].ToString(),
                            Email = reader["email"].ToString(),
                            Id_Role = Convert.ToInt32(reader["id_role"]),
                            Nama_Role = reader["nama_role"].ToString(),
                            Token = GenerateJwtToken(
                                Convert.ToInt32(reader["id_users"]), 
                                reader["username"].ToString(),
                                reader["nama_role"].ToString(),
                                p_config
                            )
                        };

                        Console.WriteLine($"User found: {user.Username}");
                        Console.WriteLine($"Generated Token: {user.Token}");

                        list1.Add(user);
                    }

                }

                cmd.Dispose(); 
                db.closeConnection(); 
            }
            catch (Exception ex)
            {
                __ErrorMsg = ex.Message; 
                Console.WriteLine("Error: " + ex.Message);
            }

            if (list1.Count == 0) 
            {
                Console.WriteLine("No user found with given credentials.");
            }

            return list1; 
        }

        private string GenerateJwtToken(int userId, string username, string role, IConfiguration pConfig)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(pConfig["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("Id_Users", userId.ToString()), 
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()), 
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                pConfig["Jwt:Issuer"],
                pConfig["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(15),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
