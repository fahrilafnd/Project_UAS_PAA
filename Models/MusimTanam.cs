namespace UAS_PAA.Models
{
    public class MusimTanam
    {
        public int Id_Musim_Tanam { get; set; }
        public DateTime Tanggal_Mulai_Tanam { get; set; } = DateTime.Now;
        public string Jenis_Tanaman { get; set; }
        public string Sumber_Benih { get; set; }
        public int Id_Laporan_Lahan { get; set; }
    }
}
