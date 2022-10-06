// See https://aka.ms/new-console-template for more information

using ConsoleApp1;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

var mongoClient = new MongoClient("mongodb://mongo:123123@localhost");
var mongoDatabase = mongoClient.GetDatabase("shashlik");
var mongoCollection = mongoDatabase.GetCollection<Users>("c1");
mongoCollection.InsertOne(new Users
{
    Id = ObjectId.GenerateNewId().ToString(),
    Name = "老王",
    Birthday = DateTimeOffset.Now.AddHours(-1)
});

var asyncCursor = await mongoCollection.FindAsync(r => r.Birthday < DateTimeOffset.Now);
asyncCursor.ToList().ForEach(Console.WriteLine);