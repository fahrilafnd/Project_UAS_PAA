namespace UAS_PAA.Models
{
    public class KegiatanPendampingan
    {
        public int Id_Kegiatan_Pendampingan { get; set; }
        public DateTime Tanggal_Kunjungan { get; set; } = DateTime.Now;
        public string Materi_Penyuluhan { get; set; }
        public string Kritik_Dan_Saran { get; set; }
        public int Id_Laporan_Lahan { get; set; }

    }
}
