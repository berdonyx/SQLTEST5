namespace ManufacturingApp.Models
{
    public class User
    {
        public int    UserID         { get; set; }
        public string Login          { get; set; }
        public string PasswordHash   { get; set; }
        public int    RoleID         { get; set; }
        public string RoleName       { get; set; }
        public bool   IsBlocked      { get; set; }
        public int    FailedAttempts { get; set; }

        public bool IsAdmin => RoleName == "Администратор";
    }
}
