namespace UAS_PAA.Models
{
    public class UpdateLaporanRequest
    {
        public int Id_Laporan { get; set; }
        public HasilPanen? HasilPanen { get; set; }
        public MusimTanam? MusimTanam { get; set; }
        public KegiatanPendampingan? KegiatanPendampingan { get; set; }
        public InputProduksi? InputProduksi { get; set; }
        public KendalaDiLapangan? KendalaDiLapngan { get; set; }
        public CatatanTambahan? CatatanTambahan { get; set; }
        public LaporanGambar? LaporanGambar { get; set; }
    }
}
