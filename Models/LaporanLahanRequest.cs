namespace UAS_PAA.Models
{
    public class LaporanLahanRequest
    {
        public int Id_Laporan_Lahan { get; set; }
        public HasilPanen? HasilPanen { get; set; }
        public MusimTanam? MusimTanam { get; set; }
        public KegiatanPendampingan? Pendampingan { get; set; }
        public KendalaDiLapangan? Kendala { get; set; }
        public CatatanTambahan? Catatan { get; set; }
        public InputProduksi? InputProduksi { get; set; }
        public List<LaporanGambar>? Gambar { get; set; }
    }
}
