namespace Dapper.Accesa.Model
{
    public class UserNonDefaultConstructor
    {
        public UserNonDefaultConstructor(string userName)
        {
            UserName = userName;
        }

        public int Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}