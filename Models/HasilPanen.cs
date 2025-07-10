namespace UAS_PAA.Models
{
    public class HasilPanen
    {
        public int Id_Hasil_Panen { get; set; }
        public DateTime Tanggal_Panen { get; set; } = DateTime.Now;
        public double Total_Hasil_Panen { get; set; }
        public string Satuan_Panen { get; set; }
        public string Kualitas { get; set; }
        public int Id_Laporan_Lahan { get; set; }
    }
}
