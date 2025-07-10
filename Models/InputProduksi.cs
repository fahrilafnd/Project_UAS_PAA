namespace UAS_PAA.Models
{
    public class InputProduksi
    {
        public int Id_Input_Produksi { get; set; }
        public string Jenis_Pupuk { get; set; }
        public double Jumlah_Pupuk { get; set; }
        public string  Satuan_Pupuk { get; set; }
        public double Jumlah_Pestisida { get; set; }
        public string Satuan_Pestisida { get; set; }
        public string Teknik_Pengolahan_Tanah { get; set; }
        public int Id_Laporan_Lahan  { get; set; }
    }
}
