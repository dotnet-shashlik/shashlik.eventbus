using MongoDB.Bson;

namespace ConsoleApp1;

public class Users
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset? Birthday { get; set; }
    public string Sid => Id.ToString();

    public override string ToString()
    {
        return $"{Id}---{Name}";
    }
}