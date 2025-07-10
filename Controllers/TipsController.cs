using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using UAS_PAA.Helpers;
using UAS_PAA.Models;

namespace UAS_PAA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TipsController : ControllerBase
    {
        private string _constr;
        private IConfiguration _config;

        public TipsController(IConfiguration config)
        {
            _config = config;
            _constr = config.GetConnectionString("WebApiDatabase");
        }

        // POST Tips
        [HttpPost]
         [Authorize(Roles = "Admin")]
        public IActionResult TambahTips([FromBody] Tips tips)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(tips.Judul))
                    return BadRequest(new { message = "Judul tips tidak boleh kosong" });

                if (tips.Judul.Length > 84)
                    return BadRequest(new { message = "Judul tips tidak boleh lebih dari 84 karakter" });

                if (string.IsNullOrWhiteSpace(tips.Deskripsi))
                    return BadRequest(new { message = "Deskripsi tips tidak boleh kosong" });

                if (tips.Id_Users <= 0)
                    return BadRequest(new { message = "ID User harus berupa angka positif" });

                if (tips.Tanggal_Tips > DateTime.Now.AddYears(1))
                    return BadRequest(new { message = "Tanggal tips tidak valid" });


                using var conn = new NpgsqlConnection(_constr);
                conn.Open();
                var cmd = new NpgsqlCommand(@"
                    INSERT INTO Tips (judul, deskripsi, gambar, tanggal_tips, id_users)
                    VALUES (@judul, @deskripsi, @gambar, @tanggal, @id_users)
                    RETURNING id_tips;", conn);

                cmd.Parameters.AddWithValue("@judul", tips.Judul);
                cmd.Parameters.AddWithValue("@deskripsi", tips.Deskripsi);
                cmd.Parameters.AddWithValue("@gambar", tips.Gambar ?? "");
                cmd.Parameters.AddWithValue("@tanggal", tips.Tanggal_Tips.Date);
                cmd.Parameters.AddWithValue("@id_users", tips.Id_Users);

                var idTips = cmd.ExecuteScalar();
                return Ok(new { success = true, id_tips = idTips, message = "Tips berhasil ditambahkan" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error detail: " + ex.Message);
                return StatusCode(500, new { message = "Gagal menambahkan tips", error = ex.Message });
            }
        }

        // GET ALL Tips
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetAllTips()
        {
            try
            {
                using var conn = new NpgsqlConnection(_constr);
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    SELECT t.*, u.username 
                    FROM Tips t 
                    LEFT JOIN Users u ON t.id_users = u.id_users 
                    ORDER BY t.tanggal_tips DESC, t.id_tips DESC;", conn);

                using var reader = cmd.ExecuteReader();

                var list = new List<Dictionary<string, object>>();
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    list.Add(row);
                }

                return Ok(new { success = true, data = list, total = list.Count });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saat GET all tips: " + ex.Message);
                return StatusCode(500, new { message = "Gagal mengambil data tips", error = ex.Message });
            }
        }

        // PUT Tips
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateTips(int id, [FromBody] Tips tips)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tips.Judul))
                    return BadRequest(new { message = "Judul tips tidak boleh kosong" });

                if (tips.Judul.Length > 84)
                    return BadRequest(new { message = "Judul tips tidak boleh lebih dari 84 karakter" });

                if (string.IsNullOrWhiteSpace(tips.Deskripsi))
                    return BadRequest(new { message = "Deskripsi tips tidak boleh kosong" });

                if (tips.Tanggal_Tips > DateTime.Now.AddYears(1))
                    return BadRequest(new { message = "Tanggal tips tidak valid" });


                using var conn = new NpgsqlConnection(_constr);
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    UPDATE Tips 
                    SET judul = @judul, 
                        deskripsi = @deskripsi, 
                        gambar = @gambar, 
                        tanggal_tips = @tanggal
                    WHERE id_tips = @id 
                    RETURNING id_tips;", conn);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@judul", tips.Judul);
                cmd.Parameters.AddWithValue("@deskripsi", tips.Deskripsi);
                cmd.Parameters.AddWithValue("@gambar", tips.Gambar ?? "");
                cmd.Parameters.AddWithValue("@tanggal", tips.Tanggal_Tips.Date);

                var result = cmd.ExecuteScalar();

                if (result == null)
                {
                    return NotFound(new { message = $"Tips dengan id {id} tidak ditemukan" });
                }

                return Ok(new { success = true, message = "Tips berhasil diupdate", id_tips = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saat update tips: " + ex.Message);
                return StatusCode(500, new { message = "Gagal mengupdate tips", error = ex.Message });
            }
        }

        // DELETE Tips by ID
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteTips(int id)
        {
            using var conn = new NpgsqlConnection(_constr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var checkCmd = new NpgsqlCommand("SELECT id_tips FROM Tips WHERE id_tips = @id", conn, trans);
                checkCmd.Parameters.AddWithValue("@id", id);
                var exists = checkCmd.ExecuteScalar();

                if (exists == null)
                {
                    trans.Rollback();
                    return NotFound(new { message = $"Tips dengan id {id} tidak ditemukan" });
                }

                // Hapus tips
                var deleteCmd = new NpgsqlCommand("DELETE FROM Tips WHERE id_tips = @id", conn, trans);
                deleteCmd.Parameters.AddWithValue("@id", id);
                int rowsAffected = deleteCmd.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    trans.Rollback();
                    return StatusCode(500, new { message = "Gagal menghapus tips" });
                }

                var checkRemainingCmd = new NpgsqlCommand("SELECT COUNT(*) FROM Tips", conn, trans);
                var remainingCount = Convert.ToInt32(checkRemainingCmd.ExecuteScalar());

                // JIKA TIDAK ADA TIPS TERSISA, RESET SEQUENCE
                if (remainingCount == 0)
                {
                    var resetCmd = new NpgsqlCommand("ALTER SEQUENCE tips_id_tips_seq RESTART WITH 1;", conn, trans);
                    resetCmd.ExecuteNonQuery();
                }

                trans.Commit();

                var message = remainingCount == 0
                    ? $"Tips dengan id {id} berhasil dihapus. ID sequence direset ke 1 karena semua tips telah dihapus."
                    : $"Tips dengan id {id} berhasil dihapus.";

                return Ok(new
                {
                    success = true,
                    message = message,
                    remaining_tips = remainingCount
                });
            }
            catch (Exception ex)
            {
                trans.Rollback();
                Console.WriteLine("Error saat delete tips: " + ex.Message);
                return StatusCode(500, new { message = "Terjadi kesalahan saat menghapus tips", error = ex.Message });
            }
        }

        // Search Tips
        [HttpGet("search/{keyword}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult SearchTips(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return BadRequest(new { message = "Keyword pencarian tidak boleh kosong" });

                using var conn = new NpgsqlConnection(_constr);
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    SELECT t.*, u.username 
                    FROM Tips t 
                    LEFT JOIN Users u ON t.id_users = u.id_users 
                    WHERE LOWER(t.judul) LIKE LOWER(@keyword) 
                       OR LOWER(t.deskripsi) LIKE LOWER(@keyword)
                    ORDER BY t.tanggal_tips DESC, t.id_tips DESC;", conn);

                cmd.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                using var reader = cmd.ExecuteReader();

                var list = new List<Dictionary<string, object>>();
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    list.Add(row);
                }

                return Ok(new
                {
                    success = true,
                    data = list,
                    total = list.Count,
                    keyword = keyword
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saat search tips: " + ex.Message);
                return StatusCode(500, new { message = "Gagal mencari tips", error = ex.Message });
            }
        }
    }
}