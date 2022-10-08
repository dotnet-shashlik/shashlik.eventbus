using System;

namespace CommonTestLogical.EfCore
{
    public class Users
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class UsersStringId
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }
    }
}