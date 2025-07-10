using Microsoft.AspNetCore.Mvc;

namespace UAS_PAA.Models
{
    public class LahanWithImage
    {
        public string Nama_Lahan { get; set; }
        public double Luas_Lahan { get; set; }
        public string Satuan_Luas { get; set; }
        public string Koordinat { get; set; }
        public double Centroid_Lat { get; set; }
        public double Centroid_Lng { get; set; }
        public int Id_Users { get; set; }
        public string Polygon_Img { get; set; }
    }

}
