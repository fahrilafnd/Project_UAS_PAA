namespace UAS_PAA.Models
{
    public class Users
    {
        public int Id_Users { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Id_Role { get; set; } 
        public Role? Role { get; set; } 
    }

    public class Role
    {
        public int Id_Role { get; set; }
        public string Nama_Role { get; set; } = string.Empty;
    }
}