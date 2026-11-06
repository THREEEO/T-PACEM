using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.Sqlite;
using Dapper;

namespace T_PACE
{
    public class DecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string valor = reader.GetString();
                if (string.IsNullOrWhiteSpace(valor)) return 0m;
                if (decimal.TryParse(valor, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal result))
                    return result;
                return 0m;
            }
            if (reader.TokenType == JsonTokenType.Number) return reader.GetDecimal();
            return 0m;
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    public class DoubleNullConverter : JsonConverter<double?>
    {
        public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string valor = reader.GetString();
                if (string.IsNullOrWhiteSpace(valor)) return null;
                if (double.TryParse(valor, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
                    return result;
                return null;
            }
            if (reader.TokenType == JsonTokenType.Number) return reader.GetDouble();
            return null;
        }

        public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
        {
            if (value.HasValue) writer.WriteNumberValue(value.Value);
            else writer.WriteNullValue();
        }
    }

    public class BooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True) return true;
            if (reader.TokenType == JsonTokenType.False) return false;

            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32() == 1;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                string valor = reader.GetString();
                if (valor == "1" || valor?.ToLower() == "true") return true;
                if (valor == "0" || valor?.ToLower() == "false" || string.IsNullOrWhiteSpace(valor)) return false;
            }

            return false;
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }

    public class SessaoCaixaDTO
    {
        public long id { get; set; }
        public long id_caixa { get; set; }
        public long id_usuario { get; set; }
        public string data_abertura { get; set; }
        public decimal? valor_fundo_troco { get; set; }
        public string data_fechamento { get; set; }
        public long status { get; set; }
        public decimal? valor_fechamento { get; set; }
    }

    public static class SincronizacaoService
    {
        private static readonly string apiUrl = "https://tpace-api.whyguiih.workers.dev/api/app/produtos";

        public static async Task SincronizarProdutosAsync()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetStringAsync(apiUrl);

                    var opcoesJson = new JsonSerializerOptions();
                    opcoesJson.Converters.Add(new DecimalConverter());
                    opcoesJson.Converters.Add(new DoubleNullConverter());
                    opcoesJson.Converters.Add(new BooleanConverter());

                    var produtosCloudflare = JsonSerializer.Deserialize<List<Produto>>(response, opcoesJson);

                    if (produtosCloudflare != null && produtosCloudflare.Count > 0)
                    {
                        using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
                        {
                            connection.Open();

                            using (var tx = connection.BeginTransaction())
                            {
                                try
                                {
                                    foreach (var p in produtosCloudflare)
                                    {
                                        string sql = @"
                                            INSERT INTO tb_produtos (id, codigo_barras, nome, custo, preco_venda, ncm, cest, aliquotas_imposto, quantidade, valor_promocional, unidade_venda, em_promocao, lote, validade, id_filial, quantidade_minima, foto, codigo_geral)
                                             VALUES (@id, @codigo_barras, @nome, @custo, @preco_venda, @ncm, @cest, @aliquotas_imposto, @quantidade, @valor_promocional, @unidade_venda, @em_promocao, @lote, @validade, @id_filial, @quantidade_minima, @foto, @codigo_geral)
                                            ON CONFLICT(id) DO UPDATE SET 
                                                codigo_barras = excluded.codigo_barras,
                                                nome = excluded.nome,
                                                custo = excluded.custo,
                                                preco_venda = excluded.preco_venda,
                                                ncm = excluded.ncm,
                                                cest = excluded.cest,
                                                aliquotas_imposto = excluded.aliquotas_imposto,
                                                quantidade = excluded.quantidade,
                                                valor_promocional = excluded.valor_promocional,
                                                unidade_venda = excluded.unidade_venda,
                                                em_promocao = excluded.em_promocao,
                                                lote = excluded.lote,
                                                validade = excluded.validade,
                                                id_filial = excluded.id_filial,
                                                quantidade_minima = excluded.quantidade_minima,
                                                foto = excluded.foto,
                                                codigo_geral = excluded.codigo_geral;";

                                        connection.Execute(sql, p, transaction: tx);
                                    }

                                    var idsNuvem = string.Join(",", produtosCloudflare.Select(p => p.id));
                                    string sqlLimpeza = $@"
                                        DELETE FROM tb_produtos 
                                        WHERE id NOT IN ({idsNuvem}) 
                                        AND id NOT IN (SELECT DISTINCT id_produto FROM tb_itens_venda)
                                        AND codigo_barras NOT IN (SELECT DISTINCT produto_retornado FROM tb_trocas);";

                                    connection.Execute(sqlLimpeza, transaction: tx);

                                    tx.Commit();
                                }
                                catch
                                {
                                    tx.Rollback();
                                    throw;
                                }
                            }
                        }

                        string pastaImagens = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Imagens");
                        if (!System.IO.Directory.Exists(pastaImagens))
                        {
                            System.IO.Directory.CreateDirectory(pastaImagens);
                        }

                        foreach (var prod in produtosCloudflare)
                        {
                            if (!string.IsNullOrWhiteSpace(prod.foto))
                            {
                                try
                                {
                                    string urlLimpa = prod.foto.Replace("pub-https://", "pub-")
                                                               .Replace(".r2.cloudflarestorage.com/tpacem.r2.dev/", ".r2.dev/");

                                    string nomeArquivo = urlLimpa.Substring(urlLimpa.LastIndexOf('/') + 1);
                                    string caminhoLocal = System.IO.Path.Combine(pastaImagens, nomeArquivo);

                                    if (!System.IO.File.Exists(caminhoLocal))
                                    {
                                        var imageBytes = await client.GetByteArrayAsync(urlLimpa);
                                        await System.IO.File.WriteAllBytesAsync(caminhoLocal, imageBytes);
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
                catch (HttpRequestException) { }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Erro interno na sincronização do banco: {ex.Message}", "Erro de Sincronização", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
        }
        public static async Task SincronizarVendasPendentesAsync()
        {
            try
            {
                await SincronizarSessoesCaixaAsync();

                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();

                    var vendasPendentes = conn.Query("SELECT * FROM tb_vendas WHERE sincronizado = 0 OR sincronizado IS NULL").ToList();

                    if (vendasPendentes.Count == 0) return;

                    using (var client = new HttpClient())
                    {
                        foreach (var venda in vendasPendentes)
                        {
                            var itens = conn.Query("SELECT id_produto, quantidade, preco_unitario, subtotal FROM tb_itens_venda WHERE id_venda = @IdVenda", new { IdVenda = venda.id })
                                .Select(i => new { id_produto = i.id_produto, quantidade = i.quantidade, preco_unitario = i.preco_unitario, subtotal = i.subtotal }).ToList();

                            var pagamentos = conn.Query("SELECT metodo, valor FROM tb_pagamentos WHERE id_venda = @IdVenda", new { IdVenda = venda.id })
                                .Select(p => new { metodo = p.metodo, valor = p.valor }).ToList();

                            var objVendaNuvem = new
                            {
                                id = venda.id,
                                id_sessao_caixa = (long?)venda.id_sessao_caixa,
                                id_cliente = (long?)venda.id_cliente,
                                data_hora = Convert.ToDateTime(venda.data_hora).ToString("yyyy-MM-dd HH:mm:ss"),
                                subtotal = venda.subtotal,
                                desconto = venda.desconto,
                                total = venda.total,
                                status = venda.status,
                                id_cupom = venda.id_cupom,
                                pagamentos = pagamentos,
                                itens = itens
                            };

                            string json = JsonSerializer.Serialize(objVendaNuvem);
                            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                            var response = await client.PostAsync("https://tpace-api.whyguiih.workers.dev/api/app/vendas", content);

                            if (response.IsSuccessStatusCode)
                            {
                                conn.Execute("UPDATE tb_vendas SET sincronizado = 1 WHERE id = @Id", new { Id = venda.id });
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }
        public static async Task SincronizarSessoesCaixaAsync()
        {
            try
            {
                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    var sessoes = conn.Query("SELECT * FROM tb_sessao_caixa WHERE sincronizado = 0 OR sincronizado IS NULL").ToList();
                    if (sessoes.Count == 0) return;

                    using (var client = new HttpClient())
                    {
                        var listaSessoes = sessoes.Select(s => new
                        {
                            id_caixa = Convert.ToInt64(s.id_caixa),
                            id_usuario = Convert.ToInt64(s.id_usuario),
                            data_abertura = Convert.ToDateTime(s.data_abertura).ToString("yyyy-MM-dd HH:mm:ss"),
                            valor_fundo_troco = s.valor_fundo_troco != null ? Convert.ToDecimal(s.valor_fundo_troco) : 0m,
                            data_fechamento = string.IsNullOrWhiteSpace(s.data_fechamento?.ToString())
                                              ? null
                                              : Convert.ToDateTime(s.data_fechamento).ToString("yyyy-MM-dd HH:mm:ss"),
                            status = Convert.ToInt32(s.status),
                            valor_fechamento = string.IsNullOrWhiteSpace(s.valor_fechamento?.ToString())
                                               ? (decimal?)null
                                               : Convert.ToDecimal(s.valor_fechamento)
                        }).ToList();

                        string json = JsonSerializer.Serialize(listaSessoes);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        var response = await client.PostAsync("https://tpace-api.whyguiih.workers.dev/api/app/sessoes", content);

                        if (response.IsSuccessStatusCode)
                        {
                            foreach (var s in sessoes)
                            {
                                conn.Execute("UPDATE tb_sessao_caixa SET sincronizado = 1 WHERE id = @Id", new { Id = Convert.ToInt64(s.id) });
                            }
                        }
                    }
                }
            }
            catch { }
        }
        public static async Task SincronizarTrocasPendentesAsync()
        {
            try
            {
                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    var trocas = conn.Query("SELECT * FROM tb_trocas WHERE sincronizado = 0 OR sincronizado IS NULL").ToList();
                    if (trocas.Count == 0) return;

                    using (var client = new HttpClient())
                    {
                        var listaTrocas = trocas.Select(t => new
                        {
                            id_local = Convert.ToInt64(t.id_troca),
                            tipo_troca = t.tipo_troca?.ToString(),
                            data_troca = Convert.ToDateTime(t.data_troca).ToString("yyyy-MM-dd HH:mm:ss"),
                            id_vendedor = Convert.ToInt64(t.id_vendedor),
                            produto_retornado = t.produto_retornado?.ToString(),
                            quantidade = Convert.ToDecimal(t.quantidade)
                        }).ToList();

                        string json = JsonSerializer.Serialize(listaTrocas);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        var response = await client.PostAsync("https://tpace-api.whyguiih.workers.dev/api/app/trocas", content);

                        if (response.IsSuccessStatusCode)
                        {
                            foreach (var t in listaTrocas)
                            {
                                conn.Execute("UPDATE tb_trocas SET sincronizado = 1 WHERE id_troca = @Id", new { Id = t.id_local });
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}