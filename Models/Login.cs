namespace UAS_PAA.Models
{
    public class Login
    {
        public int Id_Users { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Id_Role { get; set; }
        public string Nama_Role { get; set; }
        public string Token { get; set; }
    }
}
