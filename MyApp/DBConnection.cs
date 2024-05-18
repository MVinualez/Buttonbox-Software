using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp
{
    public class DBConnection
    {
        private readonly string mongoDbUrl;
        private readonly MongoClient client;

        public DBConnection() {
            mongoDbUrl = ConfigurationManager.AppSettings["MongoDbUrl"];
            client = new MongoClient(mongoDbUrl);
        }
        public void connect()
        {
            IMongoDatabase database = client.GetDatabase(ConfigurationManager.AppSettings["MongoDbName"]);
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("data");

            // Afficher tous les documents de la collection
            var documents = collection.Find(new BsonDocument()).ToList();
            foreach (var doc in documents)
            {
                Console.WriteLine(doc.ToJson());
            }
        }
    }
}
