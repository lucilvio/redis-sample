using System;
using System.Collections.Generic;
using System.Linq;

using Dapper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

using StackExchange.Redis;

namespace Lucilvio.Redis.Sample.Controllers
{
    public class FiltrosController : Controller
    {
        private readonly string _connectionString;
        private readonly IDatabase _redisDb;

        public FiltrosController(ConnectionMultiplexer redisConnection)
        {
            this._redisDb = redisConnection.GetDatabase();
            this._connectionString = "Server=localhost;Database=prd-portal-2;Trusted_Connection=True;MultipleActiveResultSets=true;";
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.Client, Duration = 1440, VaryByQueryKeys = new string[] { "" })]
        public IEnumerable<ListItem> Negocios()
        {
            return new List<ListItem>
            {
                new ListItem("1", "VENDA"),
                new ListItem("2", "ALUGUEL"),
                new ListItem("3", "TEMPORADA"),
                new ListItem("4", "LANCAMENTO")
            };
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.Client, Duration = 6000, VaryByQueryKeys = new string[] { "negocio" })]
        public IEnumerable<ListItem> Tipos(string negocio)
        {
            var chaveDoRedis = $"t{negocio}";
            var tiposDoCache = this._redisDb.SortedSetRangeByRankWithScores(chaveDoRedis, 0, -1);

            if (tiposDoCache == null || tiposDoCache.Length <= 0)
            {
                var tipos = this.PegarTipos(negocio);

                if (tipos == null || !tipos.Any())
                    return new List<ListItem>();

                this._redisDb.SortedSetAdd(chaveDoRedis, tipos.Select(t => new SortedSetEntry(t.Texto, Double.Parse(t.Valor))).ToArray());

                return tipos;
            }
            else
            {
                return tiposDoCache.Select(tc => new ListItem(tc.Score.ToString(), tc.Element));
            }
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.Client, Duration = 6000, VaryByQueryKeys = new string[] { "negocio", "tipo" })]
        public IEnumerable<ListItem> Ufs(string negocio, string tipo)
        {
            var itens = new List<ListItem>();

            var chaveDoRedis = $"u{negocio}{tipo}";
            var tiposDoCache = this._redisDb.SortedSetRangeByRankWithScores(chaveDoRedis, 0, -1);

            if (tiposDoCache == null || tiposDoCache.Length <= 0)
            {
                var ufs = this.PegarUfs(negocio, tipo);

                if (ufs == null || !ufs.Any())
                    return itens;

                itens = ufs.ToList();

                this._redisDb.SortedSetAdd(chaveDoRedis, ufs.Select(t => new SortedSetEntry(t.Texto, Double.Parse(t.Valor))).ToArray());
            }
            else
            {
                itens = tiposDoCache.Select(tc => new ListItem(tc.Score.ToString(), tc.Element)).ToList();
            }

            return itens;
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.Client, Duration = 6000, VaryByQueryKeys = new string[] { "negocio", "tipo", "uf" })]
        public IEnumerable<ListItem> Cidades(string negocio, string tipo, string uf)
        {
            var itens = new List<ListItem>();

            var chaveRedis = $"c{negocio}{tipo}{uf}";
            var tiposDoCache = this._redisDb.SortedSetRangeByRankWithScores(chaveRedis, 0, -1);

            if (tiposDoCache == null || tiposDoCache.Length <= 0)
            {
                var cidades = this.PegarCidades(negocio, tipo, uf);

                if (cidades == null || !cidades.Any())
                    return itens;

                itens = cidades.ToList();

                this._redisDb.SortedSetAdd(chaveRedis, cidades.Select(t => new SortedSetEntry(t.Texto, Double.Parse(t.Valor))).ToArray());
            }
            else
            {
                itens = tiposDoCache.Select(tc => new ListItem(tc.Score.ToString(), tc.Element)).ToList();
            }

            return itens;
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.Client, Duration = 6000, VaryByQueryKeys = new string[] { "negocio", "tipo", "uf", "cidade" })]
        public IEnumerable<ListItem> Bairros(string negocio, string tipo, string uf, string cidade)
        {
            var itens = new List<ListItem>();

            var chaveRedis = $"b{negocio}{tipo}{uf}{cidade}";
            var tiposDoCache = this._redisDb.SortedSetRangeByRankWithScores(chaveRedis, 0, -1);

            if (tiposDoCache == null || tiposDoCache.Length <= 0)
            {
                var bairros = this.PegarBairros(negocio, tipo, uf, cidade);

                if (bairros == null || !bairros.Any())
                    return itens;

                itens = bairros.ToList();

                this._redisDb.SortedSetAdd(chaveRedis, bairros.Select(t => new SortedSetEntry(t.Texto, Double.Parse(t.Valor))).ToArray());
            }
            else
            {
                itens = tiposDoCache.Select(tc => new ListItem(tc.Score.ToString(), tc.Element)).ToList();
            }

            return itens;
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.Client, Duration = 6000, VaryByQueryKeys = new string[] { "negocio", "tipo", "uf", "cidade", "bairro" })]
        public IEnumerable<ListItem> Quartos(string negocio, string tipo, string uf, string cidade, string bairro)
        {
            var itens = new List<ListItem>();

            var chaveRedis = $"q{negocio}{tipo}{uf}{cidade}{bairro}";
            var dadosDoCache = this._redisDb.SortedSetRangeByRankWithScores(chaveRedis, 0, -1);

            if (dadosDoCache == null || dadosDoCache.Length <= 0)
            {
                var quartos = this.PegarQuartos(negocio, tipo, uf, cidade, bairro);

                if (quartos == null || !quartos.Any())
                    return itens;

                itens = quartos.ToList();

                this._redisDb.SortedSetAdd(chaveRedis, quartos.Select(t => new SortedSetEntry(t.Texto, Double.Parse(t.Valor))).ToArray());
            }
            else
            {
                itens = dadosDoCache.Select(tc => new ListItem(tc.Score.ToString(), tc.Element)).ToList();
            }

            return itens;
        }

        private IEnumerable<ListItem> PegarQuartos(string negocio, string tipo, string uf, string cidade, string bairro)
        {
            if(string.IsNullOrEmpty(bairro))
                return new List<ListItem>();

            var query = $@"select quartos as valor, quartos as texto from Imoveis where IdDoNegocio={negocio} and IdDoTipo ={tipo} 
                and IdDaUf={uf} and IdDaCidade={cidade} and IdDoBairro={bairro} group by quartos";

            using var con = new SqlConnection(this._connectionString);
            return con.Query<ListItem>(query);
        }

        private IEnumerable<ListItem> PegarBairros(string negocio, string tipo, string uf, string cidade)
        {
            if(string.IsNullOrEmpty(cidade))
                return new List<ListItem>();

            var query = $@"select idDoBairro as valor, bairro as texto from Imoveis where IdDoNegocio = {negocio} 
                and IdDoTipo = {tipo} and IdDaUf = {uf} and IdDaCidade = {cidade} and idDoBairro <> 0 group by idDoBairro, bairro";

            using var con = new SqlConnection(this._connectionString);
            return con.Query<ListItem>(query);
        }

        private IEnumerable<ListItem> PegarCidades(string negocio, string tipo, string uf)
        {
            if(string.IsNullOrEmpty(uf))
                return new List<ListItem>();

            var query = $@"select idDaCidade as valor, cidade as texto from Imoveis where IdDoNegocio = {negocio} 
                and IdDoTipo = {tipo} and IdDaUf = {uf} and idDaCidade <> 0 group by idDaCidade, cidade";

            using var con = new SqlConnection(this._connectionString);
            return con.Query<ListItem>(query);
        }

        private IEnumerable<ListItem> PegarTipos(string negocio)
        {
            if(string.IsNullOrEmpty(negocio))
                return new List<ListItem>();

            var query = $@"select idDoTipo as valor, nomeDoTipo as texto from Imoveis where IdDoNegocio = {negocio} 
                and IdDoTipo <> 0 group by idDoTipo, nomeDoTipo";
            
            using var con = new SqlConnection(this._connectionString);
            return con.Query<ListItem>(query);
        }

        private IEnumerable<ListItem> PegarUfs(string negocio, string tipo)
        {
            if(string.IsNullOrEmpty(tipo))
                return new List<ListItem>();

            var query = $@"select idDaUf as valor, uf as texto from Imoveis where IdDoNegocio = {negocio} 
                and IdDoTipo = {tipo} and idDaUf <> 0 group by idDaUf, uf";

            using var con = new SqlConnection(this._connectionString);
            return con.Query<ListItem>(query);
        }
    }
}
