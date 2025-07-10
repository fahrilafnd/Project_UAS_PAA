using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UAS_PAA.Helpers;
using UAS_PAA.Models;

namespace UAS_PAA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private string __constr;
        private IConfiguration __config;

        public LoginController(IConfiguration configuration)
        {
            __config = configuration;
            __constr = configuration.GetConnectionString("WebApiDatabase");
        }

        // LOGIN
        [HttpPost("login")]
        public IActionResult LoginUser([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Username dan password harus diisi." });
            }

            LoginContext context = new LoginContext(__constr);
            List<Login> listLogin = context.Autentifikasi(request.Username, request.Password, __config);

            if (listLogin == null || listLogin.Count == 0)
            {
                return Unauthorized(new { message = "Username atau password salah." });
            }

            var user = listLogin[0];
            return Ok(new { Token = user.Token });
        }

        // REGISTER
        [HttpPost("register")]
        public IActionResult RegisterUser([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new { message = "Semua field harus diisi." });
            }

            try
            {
                SqlDbHelpers db = new SqlDbHelpers(__constr);

                // Cek duplikat email atau username
                string checkQuery = "SELECT COUNT(*) FROM users WHERE email = @email OR username = @username";
                var checkCmd = db.getNpgsqlCommand(checkQuery);
                checkCmd.Parameters.AddWithValue("@email", request.Email);
                checkCmd.Parameters.AddWithValue("@username", request.Username);
                var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                checkCmd.Dispose();

                if (count > 0)
                {
                    db.closeConnection();
                    return Conflict(new { message = "Email atau username sudah terdaftar." });
                }

                
                string insertQuery = @"
                    INSERT INTO users (email, username, password, id_role)
                    VALUES (@email, @username, crypt(@password, gen_salt('bf')), 2)";

                var cmd = db.getNpgsqlCommand(insertQuery);
                cmd.Parameters.AddWithValue("@email", request.Email);
                cmd.Parameters.AddWithValue("@username", request.Username);
                cmd.Parameters.AddWithValue("@password", request.Password);
                cmd.ExecuteNonQuery();

                cmd.Dispose();
                db.closeConnection();

                return Ok(new { message = "Registrasi berhasil." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Terjadi kesalahan pada server.", error = ex.Message });
            }
        }
    }

    // LOGIN Req Model
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    // REGISTER Req Model
    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
