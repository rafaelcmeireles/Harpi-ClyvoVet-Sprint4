using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace ClyvoVetApi.Infrastructure.Mongo
{
    /// <summary>Configuração do MongoDB (seção <c>Mongo</c> do appsettings).</summary>
    public class MongoSettings
    {
        public const string SectionName = "Mongo";

        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string Database { get; set; } = "clyvovet";
        public string MessagesCollection { get; set; } = "chat_messages";
    }

    /// <summary>
    /// Implementação concreta do padrão Repository sobre MongoDB para as mensagens do chat.
    /// O mapeamento BSON é feito aqui (e não nas classes de Domínio) para manter o Domínio
    /// livre de dependências de infraestrutura.
    /// </summary>
    public class MongoChatMessageRepository : IChatMessageRepository
    {
        private static readonly object MapLock = new();
        private static bool _mapped;

        private readonly IMongoCollection<ChatMessage> _collection;

        public MongoChatMessageRepository(IMongoClient client, IOptions<MongoSettings> options)
        {
            RegisterClassMap();

            var settings = options.Value;
            _collection = client
                .GetDatabase(settings.Database)
                .GetCollection<ChatMessage>(settings.MessagesCollection);

            // Índice para a consulta mais comum: mensagens de uma consulta em ordem cronológica.
            _collection.Indexes.CreateOne(new CreateIndexModel<ChatMessage>(
                Builders<ChatMessage>.IndexKeys.Ascending(m => m.ConsultationId).Ascending(m => m.SentAt)));
        }

        public Task AddAsync(ChatMessage message, CancellationToken ct = default) =>
            _collection.InsertOneAsync(message, cancellationToken: ct);

        public async Task<PagedResult<ChatMessage>> GetByConsultationAsync(
            int consultationId, PageQuery page, CancellationToken ct = default)
        {
            var filter = Builders<ChatMessage>.Filter.Eq(m => m.ConsultationId, consultationId);

            var total = (int)await _collection.CountDocumentsAsync(filter, cancellationToken: ct);

            var sort = page.Desc
                ? Builders<ChatMessage>.Sort.Descending(m => m.SentAt)
                : Builders<ChatMessage>.Sort.Ascending(m => m.SentAt);

            var items = await _collection.Find(filter)
                .Sort(sort)
                .Skip((page.Page - 1) * page.PageSize)
                .Limit(page.PageSize)
                .ToListAsync(ct);

            return new PagedResult<ChatMessage>(items, total);
        }

        private static void RegisterClassMap()
        {
            lock (MapLock)
            {
                if (_mapped || BsonClassMap.IsClassMapRegistered(typeof(ChatMessage)))
                    return;

                BsonClassMap.RegisterClassMap<ChatMessage>(map =>
                {
                    map.AutoMap();
                    map.MapIdMember(m => m.Id)
                        .SetSerializer(new StringSerializer(BsonType.ObjectId))
                        .SetIdGenerator(StringObjectIdGenerator.Instance);
                });

                _mapped = true;
            }
        }
    }

    /// <summary>
    /// Implementação em memória usada no ambiente de testes (sem necessidade de um servidor MongoDB).
    /// </summary>
    public class InMemoryChatMessageRepository : IChatMessageRepository
    {
        private readonly List<ChatMessage> _messages = new();
        private readonly object _lock = new();

        public Task AddAsync(ChatMessage message, CancellationToken ct = default)
        {
            lock (_lock)
            {
                message.Id = ObjectId.GenerateNewId().ToString();
                _messages.Add(message);
            }

            return Task.CompletedTask;
        }

        public Task<PagedResult<ChatMessage>> GetByConsultationAsync(
            int consultationId, PageQuery page, CancellationToken ct = default)
        {
            lock (_lock)
            {
                var query = _messages.Where(m => m.ConsultationId == consultationId);
                var ordered = page.Desc ? query.OrderByDescending(m => m.SentAt) : query.OrderBy(m => m.SentAt);
                var all = ordered.ToList();

                var items = all.Skip((page.Page - 1) * page.PageSize).Take(page.PageSize).ToList();
                return Task.FromResult(new PagedResult<ChatMessage>(items, all.Count));
            }
        }
    }
}