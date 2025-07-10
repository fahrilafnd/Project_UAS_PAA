namespace UAS_PAA.Models
{
    public class Tips
    {
        public int Id_Tips { get; set; }
        public DateTime Tanggal_Tips { get; set; } = DateTime.Now;
        public string Judul { get; set; }
        public string Deskripsi { get; set; }
        public string Gambar { get; set; }
        public int Id_Users { get; set; }
    }
}


