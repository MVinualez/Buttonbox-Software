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
        private readonly IMongoDatabase database;

        public DBConnection() {
            mongoDbUrl = ConfigurationManager.AppSettings["MongoDbUrl"];
            client = new MongoClient(mongoDbUrl);
            database = client.GetDatabase(ConfigurationManager.AppSettings["MongoDbName"]);
        }

        public List<BsonDocument> GetDocuments()
        {
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("data");
            var documents = collection.Find(new BsonDocument()).ToList();
            Console.WriteLine(documents);
            return documents;
        }

        public void AddDocument(BsonDocument document)
        {
            try
            {
                var collection = database.GetCollection<BsonDocument>("data");
                collection.InsertOne(document);
                Console.WriteLine("Document ajouté avec succès.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'ajout du document : {ex.Message}");
            }
        }
    }
}
