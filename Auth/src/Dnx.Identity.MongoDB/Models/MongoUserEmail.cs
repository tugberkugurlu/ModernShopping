namespace Dnx.Identity.MongoDB.Models
{
    public class MongoUserEmail : MongoUserContactRecord
    {
        public MongoUserEmail(string email) : base(email)
        {
        }
    }
}
