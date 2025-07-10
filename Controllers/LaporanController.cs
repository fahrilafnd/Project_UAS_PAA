using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
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
    public class LaporanController : ControllerBase
    {
        private string _constr;
        private IConfiguration _config;

        public LaporanController(IConfiguration config)
        {
            _constr = config.GetConnectionString("WebApiDatabase");
        }

        // POST LAHAN
        [HttpPost("lahan")]
        [Authorize(Roles = "User")]
        public IActionResult TambahLahan([FromBody] Lahan lahan)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(lahan.Nama_Lahan))
                    return BadRequest(new { message = "Nama lahan tidak boleh kosong" });

                if (string.IsNullOrWhiteSpace(lahan.Satuan_Luas))
                    return BadRequest(new { message = "Satuan luas tidak boleh kosong" });

                if (string.IsNullOrWhiteSpace(lahan.Koordinat))
                    return BadRequest(new { message = "Koordinat tidak boleh kosong" });

                if (lahan.Id_Users <= 0)
                    return BadRequest(new { message = "ID User harus berupa angka positif" });

                if (lahan.Luas_Lahan <= 0)
                    return BadRequest(new { message = "Luas lahan harus berupa angka positif" });

                if (lahan.Centroid_Lat is < -90 or > 90)
                    return BadRequest(new { message = "Latitude tidak valid (harus antara -90 hingga 90)" });

                if (lahan.Centroid_Lng is < -180 or > 180)
                    return BadRequest(new { message = "Longitude tidak valid (harus antara -180 hingga 180)" });

                using var conn = new NpgsqlConnection(_constr);
                conn.Open();
                var cmd = new NpgsqlCommand(@"
            INSERT INTO Lahan (nama_lahan, luas_lahan, satuan_luas, koordinat, centroid_lat, centroid_lng, id_users)
            VALUES (@nama, @luas, @satuan, @koordinat, @lat, @lng, @id_users)
            RETURNING id_lahan;", conn);

                cmd.Parameters.AddWithValue("@nama", lahan.Nama_Lahan);
                cmd.Parameters.AddWithValue("@luas", lahan.Luas_Lahan);
                cmd.Parameters.AddWithValue("@satuan", lahan.Satuan_Luas);
                cmd.Parameters.AddWithValue("@koordinat", lahan.Koordinat);
                cmd.Parameters.AddWithValue("@lat", lahan.Centroid_Lat);
                cmd.Parameters.AddWithValue("@lng", lahan.Centroid_Lng);
                cmd.Parameters.AddWithValue("@id_users", lahan.Id_Users);

                var idLahan = cmd.ExecuteScalar();
                return Ok(new { success = true, id_lahan = idLahan });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error detail: " + ex.Message);
                return StatusCode(500, new { message = "Gagal menambahkan lahan", error = ex.Message });
            }
        }

        // Polygon IMAGE
        [HttpPost("polygon-image")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> TambahLahanWithImage([FromBody] LahanWithImage lahan)
        {
            try
            {
                using var conn = new NpgsqlConnection(_constr);
                conn.Open();

                var cmd = new NpgsqlCommand(@"
            INSERT INTO Lahan 
            (nama_lahan, luas_lahan, satuan_luas, koordinat, centroid_lat, centroid_lng, id_users, polygon_img)
            VALUES 
            (@nama, @luas, @satuan, @koordinat, @lat, @lng, @id_users, @polygon_img)
            RETURNING id_lahan;", conn);

                cmd.Parameters.AddWithValue("@nama", lahan.Nama_Lahan);
                cmd.Parameters.AddWithValue("@luas", lahan.Luas_Lahan);
                cmd.Parameters.AddWithValue("@satuan", lahan.Satuan_Luas);
                cmd.Parameters.AddWithValue("@koordinat", lahan.Koordinat);
                cmd.Parameters.AddWithValue("@lat", lahan.Centroid_Lat);
                cmd.Parameters.AddWithValue("@lng", lahan.Centroid_Lng);
                cmd.Parameters.AddWithValue("@id_users", lahan.Id_Users);
                cmd.Parameters.AddWithValue("@polygon_img", lahan.Polygon_Img ?? "");

                var idLahan = cmd.ExecuteScalar();
                return Ok(new
                {
                    id_lahan = idLahan,
                    polygon_image = lahan.Polygon_Img
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Gagal menyimpan lahan", error = ex.Message });
            }
        }

        // POST POLYGON by ID
        [HttpPost("polygon/{id_lahan}")]
        [Authorize(Roles = "User")]
        public IActionResult SimpanPolygon(int id_lahan, [FromBody] List<KoordinatModel> koordinatList)
        {
            if (koordinatList == null || koordinatList.Count < 3)
                return BadRequest(new { message = "Minimal 3 titik koordinat diperlukan." });

            SqlDbHelpers db = new SqlDbHelpers(_constr);

            try
            {
                foreach (var k in koordinatList)
                {
                    var cmd = db.getNpgsqlCommand("INSERT INTO KoordinatLahan (id_lahan, latitude, longitude) VALUES (@id, @lat, @lng)");
                    cmd.Parameters.AddWithValue("@id", id_lahan);
                    cmd.Parameters.AddWithValue("@lat", k.Lat);
                    cmd.Parameters.AddWithValue("@lng", k.Lng);
                    cmd.ExecuteNonQuery();
                }

                db.closeConnection();
                return Ok(new { message = "Polygon berhasil disimpan." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        public class KoordinatModel
        {
            public double Lat { get; set; }
            public double Lng { get; set; }
        }

        // POST LAPORAN LAHAN
        [HttpPost]
        [Authorize(Roles = "User")]
        public IActionResult TambahLaporanLahan([FromBody] LaporanLahan laporan)
        {
            try
            {
                using var conn = new NpgsqlConnection(_constr);
                conn.Open();

                var cmd = new NpgsqlCommand(@"
            INSERT INTO laporanlahan (id_lahan, tanggal_laporan)
            VALUES (@id_lahan, @tanggal_laporan)
            RETURNING id_laporan_lahan;
        ", conn);

                cmd.Parameters.AddWithValue("@id_lahan", laporan.Id_Lahan);
                cmd.Parameters.AddWithValue("@tanggal_laporan", laporan.Tanggal_Laporan);

                var id = cmd.ExecuteScalar();
                return Ok(new { id_laporan_lahan = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST Laporan
        [HttpPost("laporan")]
        [Authorize(Roles = "User")]
        public IActionResult PostLaporan([FromBody] LaporanLahanRequest data)
        {
            using (var conn = new NpgsqlConnection(_constr))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        int idLaporan = data.Id_Laporan_Lahan;
                        if (idLaporan <= 0)
                            return BadRequest(new { message = "ID laporan tidak valid." });

                        // Hasil Panen
                        if (data.HasilPanen != null)
                        {
                            if (data.HasilPanen.Tanggal_Panen > DateTime.Now.AddYears(1) || data.HasilPanen.Total_Hasil_Panen <= 0)
                                return BadRequest(new { message = "Data hasil panen tidak valid." });

                            var cmd = new NpgsqlCommand(@"
                        INSERT INTO HasilPanen (tanggal_panen, total_hasil_panen, satuan_panen, kualitas, id_laporan_lahan)
                        VALUES (@tanggal, @total, @satuan, @kualitas, @id)", conn, transaction);
                            cmd.Parameters.AddWithValue("@tanggal", data.HasilPanen.Tanggal_Panen);
                            cmd.Parameters.AddWithValue("@total", data.HasilPanen.Total_Hasil_Panen);
                            cmd.Parameters.AddWithValue("@satuan", data.HasilPanen.Satuan_Panen);
                            cmd.Parameters.AddWithValue("@kualitas", data.HasilPanen.Kualitas ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@id", idLaporan);
                            cmd.ExecuteNonQuery();
                        }

                        // Musim Tanam
                        if (data.MusimTanam != null)
                        {
                            var mt = data.MusimTanam;
                            if (mt.Tanggal_Mulai_Tanam > DateTime.Now.AddYears(1) ||
                                string.IsNullOrWhiteSpace(mt.Jenis_Tanaman) || string.IsNullOrWhiteSpace(mt.Sumber_Benih))
                                return BadRequest(new { message = "Data musim tanam tidak valid." });

                            var cmd = new NpgsqlCommand(@"
                        INSERT INTO MusimTanam (tanggal_mulai_tanam, jenis_tanaman, sumber_benih, id_laporan_lahan)
                        VALUES (@tanggal, @jenis, @sumber, @id)", conn, transaction);
                            cmd.Parameters.AddWithValue("@tanggal", mt.Tanggal_Mulai_Tanam);
                            cmd.Parameters.AddWithValue("@jenis", mt.Jenis_Tanaman);
                            cmd.Parameters.AddWithValue("@sumber", mt.Sumber_Benih);
                            cmd.Parameters.AddWithValue("@id", idLaporan);
                            cmd.ExecuteNonQuery();
                        }

                        // Pendampingan
                        if (data.Pendampingan != null)
                        {
                            var pd = data.Pendampingan;
                            if (pd.Tanggal_Kunjungan > DateTime.Now.AddYears(1) || string.IsNullOrWhiteSpace(pd.Materi_Penyuluhan))
                                return BadRequest(new { message = "Data pendampingan tidak valid." });

                            var cmd = new NpgsqlCommand(@"
                        INSERT INTO KegiatanPendampingan (tanggal_kunjungan, materi_penyuluhan, kritik_dan_saran, id_laporan_lahan)
                        VALUES (@tanggal, @materi, @kritik, @id)", conn, transaction);
                            cmd.Parameters.AddWithValue("@tanggal", pd.Tanggal_Kunjungan);
                            cmd.Parameters.AddWithValue("@materi", pd.Materi_Penyuluhan);
                            cmd.Parameters.AddWithValue("@kritik", pd.Kritik_Dan_Saran ?? "");
                            cmd.Parameters.AddWithValue("@id", idLaporan);
                            cmd.ExecuteNonQuery();
                        }

                        // Kendala
                        if (data.Kendala != null)
                        {
                            if (string.IsNullOrWhiteSpace(data.Kendala.Deskripsi))
                                return BadRequest(new { message = "Deskripsi kendala tidak boleh kosong." });

                            var cmd = new NpgsqlCommand(@"
                        INSERT INTO KendalaDiLapangan (deskripsi, id_laporan_lahan)
                        VALUES (@deskripsi, @id)", conn, transaction);
                            cmd.Parameters.AddWithValue("@deskripsi", data.Kendala.Deskripsi);
                            cmd.Parameters.AddWithValue("@id", idLaporan);
                            cmd.ExecuteNonQuery();
                        }

                        // Catatan
                        if (data.Catatan != null)
                        {
                            if (string.IsNullOrWhiteSpace(data.Catatan.Deskripsi))
                                return BadRequest(new { message = "Deskripsi catatan tidak boleh kosong." });

                            var cmd = new NpgsqlCommand(@"
                        INSERT INTO CatatanTambahan (deskripsi, id_laporan_lahan)
                        VALUES (@deskripsi, @id)", conn, transaction);
                            cmd.Parameters.AddWithValue("@deskripsi", data.Catatan.Deskripsi);
                            cmd.Parameters.AddWithValue("@id", idLaporan);
                            cmd.ExecuteNonQuery();
                        }

                        // Input Produksi
                        if (data.InputProduksi != null)
                        {
                            var ip = data.InputProduksi;
                            if (string.IsNullOrWhiteSpace(ip.Jenis_Pupuk) ||
                                ip.Jumlah_Pupuk < 0 || ip.Jumlah_Pestisida < 0 ||
                                string.IsNullOrWhiteSpace(ip.Satuan_Pupuk) || string.IsNullOrWhiteSpace(ip.Satuan_Pestisida))
                                return BadRequest(new { message = "Data input produksi tidak valid." });

                            var cmd = new NpgsqlCommand(@"
                        INSERT INTO InputProduksi (jenis_pupuk, jumlah_pupuk, satuan_pupuk, jumlah_pestisida, satuan_pestisida, teknik_pengolahan_tanah, id_laporan_lahan)
                        VALUES (@jenis, @jumlah_pupuk, @satuan_pupuk, @jumlah_pestisida, @satuan_pestisida, @teknik, @id)", conn, transaction);
                            cmd.Parameters.AddWithValue("@jenis", ip.Jenis_Pupuk);
                            cmd.Parameters.AddWithValue("@jumlah_pupuk", ip.Jumlah_Pupuk);
                            cmd.Parameters.AddWithValue("@satuan_pupuk", ip.Satuan_Pupuk);
                            cmd.Parameters.AddWithValue("@jumlah_pestisida", ip.Jumlah_Pestisida);
                            cmd.Parameters.AddWithValue("@satuan_pestisida", ip.Satuan_Pestisida);
                            cmd.Parameters.AddWithValue("@teknik", ip.Teknik_Pengolahan_Tanah ?? "");
                            cmd.Parameters.AddWithValue("@id", idLaporan);
                            cmd.ExecuteNonQuery();
                        }

                        // Gambar
                        if (data.Gambar != null && data.Gambar.Count > 0)
                        {
                            foreach (var gbr in data.Gambar)
                            {
                                if (string.IsNullOrWhiteSpace(gbr.Url_Gambar))
                                    return BadRequest(new { message = "URL gambar tidak boleh kosong." });

                                var cmd = new NpgsqlCommand(@"
                            INSERT INTO LaporanGambar (url_gambar, id_laporan_lahan)
                            VALUES (@url, @id)", conn, transaction);
                                cmd.Parameters.AddWithValue("@url", gbr.Url_Gambar);
                                cmd.Parameters.AddWithValue("@id", idLaporan);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Commit semua jika berhasil
                        transaction.Commit();
                        return Ok(new { message = "Laporan berhasil ditambahkan secara lengkap." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, new { message = "Gagal menyimpan laporan: " + ex.Message });
                    }
                }
            }
        }

        //GET LAHAN
        [HttpGet("lahan")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetAllLahan()
        {
            try
            {
                using var conn = new NpgsqlConnection(_constr);
                conn.Open();

                var cmd = new NpgsqlCommand("SELECT * FROM Lahan ORDER BY id_lahan DESC;", conn);
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

                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saat GET all lahan: " + ex.Message);
                return StatusCode(500, new { message = "Gagal mengambil data lahan", error = ex.Message });
            }
        }


        // GET LAporan LENGAKP
        [HttpGet("laporan")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<List<Dictionary<string, object>>> GetAllLaporanLengkap()
        {
            SqlDbHelpers db = new SqlDbHelpers(_constr);
            var result = new List<Dictionary<string, object>>();

            var semuaId = db.ReadList("SELECT id_laporan_lahan FROM LaporanLahan");

            foreach (var row in semuaId)
            {
                int id_laporan = Convert.ToInt32(row["id_laporan_lahan"]);
                var laporan = new Dictionary<string, object>();

                laporan["id_laporan_lahan"] = id_laporan;
                laporan["hasilPanen"] = QueryList(db, "SELECT * FROM HasilPanen WHERE id_laporan_lahan = @id", id_laporan);
                laporan["musimTanam"] = QueryList(db, "SELECT * FROM MusimTanam WHERE id_laporan_lahan = @id", id_laporan);
                laporan["pendampingan"] = QueryList(db, "SELECT * FROM KegiatanPendampingan WHERE id_laporan_lahan = @id", id_laporan);
                laporan["kendala"] = QueryList(db, "SELECT * FROM KendalaDiLapangan WHERE id_laporan_lahan = @id", id_laporan);
                laporan["catatan"] = QueryList(db, "SELECT * FROM CatatanTambahan WHERE id_laporan_lahan = @id", id_laporan);
                laporan["inputProduksi"] = QueryList(db, "SELECT * FROM InputProduksi WHERE id_laporan_lahan = @id", id_laporan);
                laporan["gambar"] = QueryList(db, "SELECT * FROM LaporanGambar WHERE id_laporan_lahan = @id", id_laporan);

                result.Add(laporan);
            }

            db.closeConnection();
            return Ok(result);
        }


        // GET laporan lengkap by ID
        [HttpGet("laporan/{id_lahan}")]
        [Authorize(Roles = "User")]
        public IActionResult GetLaporanByLahan(int id_lahan)
        {
            SqlDbHelpers db = new SqlDbHelpers(_constr);

            try
            {
                var hasil = new Dictionary<string, object>();

                var laporan = db.QuerySingle(
                    "SELECT * FROM LaporanLahan WHERE id_lahan = @id_lahan",
                    new Dictionary<string, object> { { "@id_lahan", id_lahan } }
                );


                if (laporan == null)
                {
                    return Ok(new { message = "Belum ada laporan untuk lahan ini.", data = new { } });
                }

                int idLaporan = Convert.ToInt32(laporan["id_laporan_lahan"]);
                hasil["laporan_lahan"] = laporan;

                hasil["hasilPanen"] = QueryList(db, "SELECT * FROM HasilPanen WHERE id_laporan_lahan = @id", idLaporan);
                hasil["musimTanam"] = QueryList(db, "SELECT * FROM MusimTanam WHERE id_laporan_lahan = @id", idLaporan);
                hasil["pendampingan"] = QueryList(db, "SELECT * FROM KegiatanPendampingan WHERE id_laporan_lahan = @id", idLaporan);
                hasil["kendala"] = QueryList(db, "SELECT * FROM KendalaDiLapangan WHERE id_laporan_lahan = @id", idLaporan);
                hasil["catatan"] = QueryList(db, "SELECT * FROM CatatanTambahan WHERE id_laporan_lahan = @id", idLaporan);
                hasil["inputProduksi"] = QueryList(db, "SELECT * FROM InputProduksi WHERE id_laporan_lahan = @id", idLaporan);
                hasil["gambar"] = QueryList(db, "SELECT * FROM LaporanGambar WHERE id_laporan_lahan = @id", idLaporan);

                db.closeConnection();
                return Ok(hasil);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Gagal mengambil data laporan: " + ex.Message });
            }
        }

        private List<Dictionary<string, object>> QueryList(SqlDbHelpers db, string query, int id)
        {
            var list = new List<Dictionary<string, object>>();
            NpgsqlCommand cmd = db.getNpgsqlCommand(query);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                list.Add(dict);
            }
            reader.Close();
            return list;
        }

       

        // PUT laporan
        [HttpPut("laporan/{id}")]
        [Authorize(Roles = "User")]
        public IActionResult UpdateLaporan(int id, [FromBody] UpdateLaporanRequest req)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_constr))
                {
                    conn.Open();

                    // Update Musim Tanam
                    if (req.MusimTanam != null)
                    {
                        var cmd = new NpgsqlCommand(@"
                    INSERT INTO MusimTanam (tanggal_mulai_tanam, jenis_tanaman, sumber_benih, id_laporan_lahan)
                    VALUES (@tanggal, @jenis, @sumber, @id_laporan)
                    ON CONFLICT (id_laporan_lahan)
                    DO UPDATE SET tanggal_mulai_tanam = EXCLUDED.tanggal_mulai_tanam, jenis_tanaman = EXCLUDED.jenis_tanaman,
                    sumber_benih = EXCLUDED.sumber_benih
                ", conn);

                        cmd.Parameters.AddWithValue("@id_laporan", id);
                        cmd.Parameters.AddWithValue("@tanggal", req.MusimTanam.Tanggal_Mulai_Tanam);
                        cmd.Parameters.AddWithValue("@jenis", req.MusimTanam.Jenis_Tanaman);
                        cmd.Parameters.AddWithValue("@sumber", req.MusimTanam.Sumber_Benih);
                        cmd.ExecuteNonQuery();
                    }

                    // Update Input Produksi
                    if (req.InputProduksi != null)
                    {
                        var cmd = new NpgsqlCommand(@"
                    INSERT INTO InputProduksi (jenis_pupuk, jumlah_pupuk, satuan_pupuk, jumlah_pestisida, satuan_pestisida, teknik_pengolahan_tanah, id_laporan_lahan)
                    VALUES (@jenis_pupuk, @jumlah_pupuk, @satuan_pupuk, @jumlah_pestisida, @satuan_pestisida, @teknik, @id_laporan)
                    ON CONFLICT (id_laporan_lahan)
                    DO UPDATE SET jenis_pupuk = EXCLUDED.jenis_pupuk, jumlah_pupuk = EXCLUDED.jumlah_pupuk, satuan_pupuk = EXCLUDED.satuan_pupuk,
                    jumlah_pestisida = EXCLUDED.jumlah_pestisida, satuan_pestisida = EXCLUDED.satuan_pestisida, teknik_pengolahan_tanah = EXCLUDED.teknik_pengolahan_tanah
                ", conn);

                        cmd.Parameters.AddWithValue("@id_laporan", id);
                        cmd.Parameters.AddWithValue("@jenis_pupuk", req.InputProduksi.Jenis_Pupuk);
                        cmd.Parameters.AddWithValue("@jumlah_pupuk", req.InputProduksi.Jumlah_Pupuk);
                        cmd.Parameters.AddWithValue("@satuan_pupuk", req.InputProduksi.Satuan_Pupuk);
                        cmd.Parameters.AddWithValue("@jumlah_pestisida", req.InputProduksi.Jumlah_Pestisida);
                        cmd.Parameters.AddWithValue("@satuan_pestisida", req.InputProduksi.Satuan_Pestisida);
                        cmd.Parameters.AddWithValue("@teknik", req.InputProduksi.Teknik_Pengolahan_Tanah);
                        cmd.ExecuteNonQuery();
                    }

                    // Update Pendampingan
                    if (req.KegiatanPendampingan != null)
                    {
                        var cmd = new NpgsqlCommand(@"
                    INSERT INTO KegiatanPendampingan (tanggal_kunjungan, materi_penyuluhan, kritik_dan_saran, id_laporan_lahan)
                    VALUES (@tanggal, @materi, @kritik, @id_laporan)
                    ON CONFLICT (id_laporan_lahan)
                    DO UPDATE SET tanggal_kunjungan = EXCLUDED.tanggal_kunjungan, materi_penyuluhan = EXCLUDED.materi_penyuluhan, kritik_dan_saran = EXCLUDED.kritik_dan_saran
                ", conn);

                        cmd.Parameters.AddWithValue("@id_laporan", id);
                        cmd.Parameters.AddWithValue("@tanggal", req.KegiatanPendampingan.Tanggal_Kunjungan);
                        cmd.Parameters.AddWithValue("@materi", req.KegiatanPendampingan.Materi_Penyuluhan);
                        cmd.Parameters.AddWithValue("@kritik", req.KegiatanPendampingan.Kritik_Dan_Saran);
                        cmd.ExecuteNonQuery();
                    }

                    // Update Kendala
                    if (req.KendalaDiLapngan != null)
                    {
                        var cmd = new NpgsqlCommand(@"
                    INSERT INTO KendalaDiLapangan (deskripsi, id_laporan_lahan)
                    VALUES (@deskripsi, @id_laporan)
                    ON CONFLICT (id_laporan_lahan)
                    DO UPDATE SET deskripsi = EXCLUDED.deskripsi
                ", conn);

                        cmd.Parameters.AddWithValue("@id_laporan", id);
                        cmd.Parameters.AddWithValue("@deskripsi", req.KendalaDiLapngan.Deskripsi);
                        cmd.ExecuteNonQuery();
                    }

                    // Update Hasil Panen
                    if (req.HasilPanen != null)
                    {
                        var cmd = new NpgsqlCommand(@"
                    INSERT INTO HasilPanen (tanggal_panen, total_hasil_panen, satuan_panen, kualitas, id_laporan_lahan)
                    VALUES (@tanggal, @total, @satuan, @kualitas, @id_laporan)
                    ON CONFLICT (id_laporan_lahan)
                    DO UPDATE SET tanggal_panen = EXCLUDED.tanggal_panen, total_hasil_panen = EXCLUDED.total_hasil_panen, 
                    satuan_panen = EXCLUDED.satuan_panen, kualitas = EXCLUDED.kualitas
                ", conn);

                        cmd.Parameters.AddWithValue("@id_laporan", id);
                        cmd.Parameters.AddWithValue("@tanggal", req.HasilPanen.Tanggal_Panen);
                        cmd.Parameters.AddWithValue("@total", req.HasilPanen.Total_Hasil_Panen);
                        cmd.Parameters.AddWithValue("@satuan", req.HasilPanen.Satuan_Panen);
                        cmd.Parameters.AddWithValue("@kualitas", req.HasilPanen.Kualitas);
                        cmd.ExecuteNonQuery();
                    }

                    // Update Catatan
                    if (req.CatatanTambahan != null)
                    {
                        var cmd = new NpgsqlCommand(@"
                    INSERT INTO CatatanTambahan (deskripsi, id_laporan_lahan)
                    VALUES (@deskripsi, @id_laporan)
                    ON CONFLICT (id_laporan_lahan)
                    DO UPDATE SET deskripsi = EXCLUDED.deskripsi
                ", conn);

                        cmd.Parameters.AddWithValue("@id_laporan", id);
                        cmd.Parameters.AddWithValue("@deskripsi", req.CatatanTambahan.Deskripsi);
                        cmd.ExecuteNonQuery();
                    }

                    return Ok(new { message = "Laporan berhasil diupdate." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Gagal update laporan: " + ex.Message });
            }
        }

        // DELETE LAHAN by ID
        [HttpDelete("lahan/{id}")]
        [Authorize(Roles = "User")]
        public IActionResult DeleteLahan(int id)
        {
            using var conn = new NpgsqlConnection(_constr);
            conn.Open();

            using var trans = conn.BeginTransaction();

            try
            {
                var laporanIds = new List<int>();
                using (var cmd = new NpgsqlCommand("SELECT id_laporan_lahan FROM laporanlahan WHERE id_lahan = @id_lahan", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id_lahan", id);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        laporanIds.Add(reader.GetInt32("id_laporan_lahan"));
                    }
                }

                foreach (var laporanId in laporanIds)
                {
                    string[] deleteChildQueries =
                    {
                "DELETE FROM LaporanGambar WHERE id_laporan_lahan = @laporan_id",
                "DELETE FROM HasilPanen WHERE id_laporan_lahan = @laporan_id",
                "DELETE FROM MusimTanam WHERE id_laporan_lahan = @laporan_id",
                "DELETE FROM InputProduksi WHERE id_laporan_lahan = @laporan_id",
                "DELETE FROM KegiatanPendampingan WHERE id_laporan_lahan = @laporan_id",
                "DELETE FROM KendalaDiLapangan WHERE id_laporan_lahan = @laporan_id",
                "DELETE FROM CatatanTambahan WHERE id_laporan_lahan = @laporan_id"
            };

                    foreach (var query in deleteChildQueries)
                    {
                        using var cmd = new NpgsqlCommand(query, conn, trans);
                        cmd.Parameters.AddWithValue("@laporan_id", laporanId);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new NpgsqlCommand("DELETE FROM laporanlahan WHERE id_laporan_lahan = @laporan_id", conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@laporan_id", laporanId);
                        cmd.ExecuteNonQuery();
                    }
                }

                using (var cmd = new NpgsqlCommand("DELETE FROM KoordinatLahan WHERE id_lahan = @id_lahan", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id_lahan", id);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new NpgsqlCommand("DELETE FROM Lahan WHERE id_lahan = @id_lahan", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id_lahan", id);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        trans.Rollback();
                        return NotFound(new { message = $"Lahan dengan id {id} tidak ditemukan." });
                    }
                }

                trans.Commit();
                return Ok(new
                {
                    message = $"Lahan dengan id {id} beserta {laporanIds.Count} laporan terkait berhasil dihapus.",
                    deleted_reports_count = laporanIds.Count
                });
            }
            catch (Exception ex)
            {
                trans.Rollback();
                return StatusCode(500, new { message = "Terjadi kesalahan saat menghapus lahan: " + ex.Message });
            }
        }

        //DElete LAPORAN
        [HttpDelete("laporan/{id}")]
        [Authorize(Roles = "User")]
        public IActionResult DeleteLaporan(int id)
        {
            using var conn = new NpgsqlConnection(_constr);
            conn.Open();

            using var trans = conn.BeginTransaction();

            try
            {
                string[] deleteChildQueries =
                {
            "DELETE FROM LaporanGambar WHERE id_laporan_lahan = @id",
            "DELETE FROM HasilPanen WHERE id_laporan_lahan = @id",
            "DELETE FROM MusimTanam WHERE id_laporan_lahan = @id",
            "DELETE FROM InputProduksi WHERE id_laporan_lahan = @id",
            "DELETE FROM KegiatanPendampingan WHERE id_laporan_lahan = @id",
            "DELETE FROM KendalaDiLapangan WHERE id_laporan_lahan = @id",
            "DELETE FROM CatatanTambahan WHERE id_laporan_lahan = @id"
        };

                foreach (var query in deleteChildQueries)
                {
                    using var cmd = new NpgsqlCommand(query, conn, trans);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                // Hapus data utama laporan
                using (var cmd = new NpgsqlCommand("DELETE FROM laporanlahan WHERE id_laporan_lahan = @id", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        trans.Rollback();
                        return NotFound(new { message = $"Laporan dengan id {id} tidak ditemukan." });
                    }
                }

                var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM laporanlahan", conn, trans);
                var remainingCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (remainingCount == 0)
                {
                    var resetCmd = new NpgsqlCommand("ALTER SEQUENCE laporanlahan_id_laporan_lahan_seq RESTART WITH 1;", conn, trans);
                    resetCmd.ExecuteNonQuery();
                }

                trans.Commit();

                var message = remainingCount == 0
                    ? $"Laporan dengan id {id} berhasil dihapus. ID sequence direset ke 1 karena semua laporan telah dihapus."
                    : $"Laporan dengan id {id} berhasil dihapus.";

                return Ok(new { message = message, remaining_reports = remainingCount });
            }
            catch (Exception ex)
            {
                trans.Rollback();
                return StatusCode(500, new { message = "Terjadi kesalahan saat menghapus laporan: " + ex.Message });
            }
        }


    }
}