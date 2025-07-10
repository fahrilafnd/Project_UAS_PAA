using Microsoft.AspNetCore.Mvc;
using UAS_PAA.Helpers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly string _constr;

    public UserController(IConfiguration config)
    {
        _config = config;
        _constr = config.GetConnectionString("WebApiDatabase");
    }

    [HttpGet("{id}")]
    public IActionResult GetUserById(int id)
    {
        try
        {
            SqlDbHelpers db = new SqlDbHelpers(_constr);
            string query = "SELECT id_users, username, email FROM users WHERE id_users = @id";
            var cmd = db.getNpgsqlCommand(query);
            cmd.Parameters.AddWithValue("@id", id);
            var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                db.closeConnection();
                return NotFound(new { message = "User tidak ditemukan." });
            }

            var user = new
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
            };

            db.closeConnection();
            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Terjadi kesalahan server.", error = ex.Message });
        }
    }

    [HttpPut("{id}/username")]
    public IActionResult UpdateUsername(int id, [FromBody] string newUsername)
    {
        try
        {
            SqlDbHelpers db = new SqlDbHelpers(_constr);
            string query = "UPDATE users SET username = @username WHERE id_users = @id";
            var cmd = db.getNpgsqlCommand(query);
            cmd.Parameters.AddWithValue("@username", newUsername);
            cmd.Parameters.AddWithValue("@id", id);
            int affected = cmd.ExecuteNonQuery();
            db.closeConnection();

            if (affected == 0)
                return NotFound(new { message = "User tidak ditemukan." });

            return Ok(new { message = "Username berhasil diperbarui." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Terjadi kesalahan server.", error = ex.Message });
        }
    }
}
