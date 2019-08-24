using AutoMapper;
using Dapper;
using IdentityServer4.Dapper.Storage.Entities;
using IdentityServer4.Dapper.Storage.Options;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace IdentityServer4.Dapper.Storage.DataLayer
{
    public class DefaultPersistedGrantProvider : IPersistedGrantProvider
    {
        private DBProviderOptions _options;
        private readonly IMapper _mapper;
        private readonly string _connectionString;
        public DefaultPersistedGrantProvider(DBProviderOptions dBProviderOptions, ILogger<DefaultPersistedGrantProvider> logger)
        {
            this._options = dBProviderOptions ?? throw new ArgumentNullException(nameof(dBProviderOptions));
            _mapper = new MapperConfiguration(cfg => cfg.AddProfile<IdServerDapperMapperProfile>())
                .CreateMapper();
            _connectionString = dBProviderOptions.ConnectionString;
        }


        public async Task Add(PersistedGrant token)
        {
            var entity = _mapper.Map<PersistedGrants>(token);
            using (var con = new SqlConnection(_connectionString))
            {

                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var ret = await con.ExecuteAsync($"insert into {_options.DbSchema}.PersistedGrants ([Key],ClientId,CreationTime,Data,Expiration,SubjectId,[Type]) values (@Key,@ClientId,@CreationTime,@Data,@Expiration,@SubjectId,@Type)", entity, commandType: CommandType.Text, transaction: t);
                        if (ret != 1)
                        {
                            throw new Exception($"Error inserting into PersistedGrants, return value is {ret}");
                        }
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public async Task<PersistedGrant> Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }
            using (var connection = new SqlConnection(_connectionString))
            {

                var persistedGrant = await connection.QueryFirstOrDefaultAsync<Entities.PersistedGrants>($"select * from {_options.DbSchema}.PersistedGrants where [Key] = @Key", new { Key = key }, commandType: CommandType.Text);
                var model = _mapper.Map<PersistedGrant>(persistedGrant);
                return model;
            }


        }

        public async Task<IEnumerable<PersistedGrant>> GetAll(string subjectId)
        {
            return await GetAll(subjectId, null, null);

        }

        public async Task<IEnumerable<PersistedGrant>> GetAll(string subjectId, string clientId)
        {
            return await GetAll(subjectId, clientId, null);
        }

        public async Task<IEnumerable<PersistedGrant>> GetAll(string subjectId, string clientId, string type)
        {
            if (string.IsNullOrWhiteSpace(subjectId))
            {
                return null;
            }

            clientId = string.IsNullOrWhiteSpace(clientId) ? null : clientId;
            type = string.IsNullOrWhiteSpace(type) ? null : type;

            IEnumerable<Entities.PersistedGrants> persistedGrants = null;
            using (var connection = new SqlConnection(_connectionString))
            {

                persistedGrants = await connection.QueryAsync<Entities.PersistedGrants>($"select * from {_options.DbSchema}.PersistedGrants where (SubjectId = @SubjectId or @SubjectId is null) and (ClientId = @ClientId or @ClientId is null) and ([Type] = @Type or @Type is null)", new { SubjectId = subjectId, ClientId = clientId, Type = type }, commandType: CommandType.Text);
                var results = _mapper.Map<List<PersistedGrant>>(persistedGrants);
                return results;
            }


        }

        public async Task<int> QueryExpired(DateTime dateTime)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var count = await connection.ExecuteScalarAsync<int>($"select count(1) from {_options.DbSchema}.PersistedGrants p where p.Expiration < @UtcNow", new { UtcNow = dateTime },
                        commandType: CommandType.Text);

                //var count = connection.QuerySingleOrDefault<int>("select count(1) from PersistedGrants p where p.Expiration < @UtcNow", new { UtcNow = dateTime },  commandType: CommandType.Text);
                return count;

            }

        }

        public async Task Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }
            using (var connection = new SqlConnection(_connectionString))
            {

                connection.Open();
                using (var t = connection.BeginTransaction())
                {
                    try
                    {
                        var ret = await connection.ExecuteAsync($"delete from {_options.DbSchema}.PersistedGrants where [Key] = @Key", new { Key = key }, commandType: CommandType.Text, transaction: t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
                connection.Close();
            }
        }

        public async Task RemoveAll(string subjectId, string clientId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {

                connection.Open();
                using (var t = connection.BeginTransaction())
                {
                    try
                    {
                        var ret = await connection.ExecuteAsync($"delete from {_options.DbSchema}.PersistedGrants where SubjectId = @SubjectId and ClientId = @ClientId", new { SubjectId = subjectId, ClientId = clientId }, commandType: CommandType.Text, transaction: t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
                connection.Close();
            }
        }

        public async Task RemoveAll(string subjectId, string clientId, string type)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var t = connection.BeginTransaction())
                {
                    try
                    {
                        var ret = await connection.ExecuteAsync($"delete from {_options.DbSchema}.PersistedGrants where SubjectId = @SubjectId and ClientId = @ClientId and [Type] = @Type", new { SubjectId = subjectId, ClientId = clientId, Type = type }, commandType: CommandType.Text, transaction: t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
                connection.Close();
            }
        }

        public async Task RemoveRange(DateTime dateTime)
        {
            using (var connection = new SqlConnection(_connectionString))
            {

                connection.Open();

                using (var t = connection.BeginTransaction())
                {
                    try
                    {
                        var ret = await connection.ExecuteAsync($"delete from {_options.DbSchema}.PersistedGrants where Expiration < @UtcNow", new { UtcNow = dateTime }, commandType: CommandType.Text, transaction: t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
                connection.Close();
            }
        }

        public async Task Update(PersistedGrant token)
        {
            var dbGrant = await Get(token.Key);
            if (dbGrant == null)
            {
                throw new InvalidOperationException($"Can not find PersistedGrant with key={token.Key}.");
            }
            var entity = _mapper.Map<PersistedGrants>(token);
            using (var con = new SqlConnection(_connectionString))
            {

                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var ret = await con.ExecuteAsync($"update {_options.DbSchema}.PersistedGrants " +
                            $"set ClientId = @ClientId," +
                            $"[Data] = @Data, " +
                            $"Expiration = @Expiration, " +
                            $"SubjectId = @SubjectId, " +
                            $"[Type] = @Type " +
                            $"where [Key] = @Key", new
                            {
                                entity.Key,
                                entity.ClientId,
                                entity.Data,
                                entity.Expiration,
                                entity.SubjectId,
                                entity.Type
                            }, commandType: CommandType.Text, transaction: t);
                        if (ret != 1)
                        {
                            throw new Exception($"Error updating PersistedGrants, return value is  {ret}");
                        }
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
            }
        }

    }
}
