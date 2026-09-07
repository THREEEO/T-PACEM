using Microsoft.Data.Sqlite;
using System.IO;
using Dapper;
using System; // Necessário para usar o System.Convert

namespace T_PACE
{
    // 1. Tradutor para converter Double em Decimal (Resolve o erro do F4)
    public class DecimalTypeHandler : SqlMapper.TypeHandler<decimal>
    {
        public override void SetValue(System.Data.IDbDataParameter parameter, decimal value)
        {
            parameter.Value = value;
        }

        public override decimal Parse(object value)
        {
            return Convert.ToDecimal(value);
        }
    }

    // 2. Tradutor para converter Double em Long (Resolve o erro que você acabou de reportar)
    public class LongFromDoubleTypeHandler : SqlMapper.TypeHandler<long>
    {
        public override void SetValue(System.Data.IDbDataParameter parameter, long value)
        {
            parameter.Value = value;
        }

        public override long Parse(object value)
        {
            return Convert.ToInt64(value);
        }
    }

    public static class DatabaseConfig
    {
        private static string dbPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "tpace_pdv.sqlite");
        public static string ConnectionString => $"Data Source={dbPath};";

        public static void InicializarBanco()
        {
            // Registra os TypeHandlers
            try
            {
                SqlMapper.AddTypeHandler(new LongFromDoubleTypeHandler());
                SqlMapper.AddTypeHandler(new DecimalTypeHandler());
            }
            catch
            {
                // se já registrado ou falhar por algum motivo, prosseguimos sem interromper
            }

            // 1. CRIA O BANCO SE ELE NÃO EXISTIR
            if (!File.Exists(dbPath))
            {
                // ... (O restante do seu código continua normalmente a partir daqui)
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();

                    // Script exato do seu arquivo tpace.sql no GitHub
                    string sqlGitHub = @"
                        PRAGMA defer_foreign_keys=TRUE;
                        
                        CREATE TABLE tb_filiais (id INTEGER PRIMARY KEY AUTOINCREMENT, cnpj VARCHAR(14) UNIQUE, nome_juridico VARCHAR(150), nome_fantasia VARCHAR(150), status BOOLEAN, inscricao_estadual VARCHAR(20), inscricao_municipal VARCHAR(20), crt TINYINT, caminho_certificado VARCHAR(255), senha_certificado VARCHAR(255), ambiente_nfe TINYINT, cep VARCHAR(8), logradouro VARCHAR(100), numero VARCHAR(10), complemento VARCHAR(50), bairro VARCHAR(60), cidade VARCHAR(100), cod_mun_ibge INTEGER, uf CHAR(2), telefone VARCHAR(15), email VARCHAR(100));
                        INSERT INTO tb_filiais (id,cnpj,nome_juridico,nome_fantasia,status,inscricao_estadual,inscricao_municipal,crt,caminho_certificado,senha_certificado,ambiente_nfe,cep,logradouro,numero,complemento,bairro,cidade,cod_mun_ibge,uf,telefone,email) VALUES(1,'00.000.000/0000-00','juridico','fantasia',1,'123/4567890','123456-7',4,'123','123',123,'95720-000','Rua Buarque de Macedo','123','n tem','centro','garibaldi',4308607,'rs','54999999999','mail@mail.co');
                        
                        CREATE TABLE tb_usuarios (id INTEGER PRIMARY KEY AUTOINCREMENT, nome VARCHAR(100), email VARCHAR(100) UNIQUE, senha VARCHAR(255), pin VARCHAR(255), nivel_acesso INTEGER, id_filial INTEGER REFERENCES tb_filiais(id));
                        INSERT INTO tb_usuarios (id,nome,email,senha,pin,nivel_acesso,id_filial) VALUES(1,'admin','mail@mail.co','admin','admin',0,NULL);
                        INSERT INTO tb_usuarios (id,nome,email,senha,pin,nivel_acesso,id_filial) VALUES(2,'gerente','mail2@mail.co','gerente','gerente',1,1);
                        
                        CREATE TABLE tb_clientes (id INTEGER PRIMARY KEY AUTOINCREMENT, nome VARCHAR(100), cpf_cnpj VARCHAR(14) UNIQUE, email VARCHAR(100), telefone VARCHAR(15), cep VARCHAR(8), logradouro VARCHAR(100), numero VARCHAR(10), bairro VARCHAR(60), cidade VARCHAR(100), complemento VARCHAR(60), cod_mun_ibge INTEGER, uf CHAR(2));
                        INSERT INTO tb_clientes (id,nome,cpf_cnpj,email,telefone,cep,logradouro,numero,bairro,cidade,complemento,cod_mun_ibge,uf) VALUES(1,'cliente','000.000.000-00','mail@mail.co','54999999999','99999-999','Avenida 2','123','pov moro no centro','garibaldi','to não',4308607,'rs');
                        
                        CREATE TABLE tb_fornecedores (id INTEGER PRIMARY KEY AUTOINCREMENT, cnpj VARCHAR(14) UNIQUE, nome_juridico VARCHAR(150), nome_fantasia VARCHAR(150), inscricao_estadual VARCHAR(20), cep VARCHAR(8), logradouro VARCHAR(100), numero VARCHAR(10), complemento VARCHAR(60), bairro VARCHAR(60), cidade VARCHAR(100), cod_mun_ibge INTEGER, uf CHAR(2), telefone VARCHAR(15), email VARCHAR(100), status BOOLEAN);
                        INSERT INTO tb_fornecedores (id,cnpj,nome_juridico,nome_fantasia,inscricao_estadual,cep,logradouro,numero,complemento,bairro,cidade,cod_mun_ibge,uf,telefone,email,status) VALUES(1,'00.000.000/0000-00','juridicof','fantasiaf','123/4567890','12345-000','rua x','123',NULL,'centro','barbosa',4304804,'erriessi','54000000000','mail@mai.co',1);
                        
                        CREATE TABLE tb_caixa (id INTEGER PRIMARY KEY AUTOINCREMENT, id_filial INTEGER, nome VARCHAR(30), status BOOLEAN, FOREIGN KEY(id_filial) REFERENCES tb_filiais(id));
                        INSERT INTO tb_caixa (id,id_filial,nome,status) VALUES(1,1,'caixa',1);
                        
                        CREATE TABLE tb_produtos (id INTEGER PRIMARY KEY AUTOINCREMENT, codigo_barras VARCHAR(20) UNIQUE, nome VARCHAR(150), custo DECIMAL(10,2), preco_venda DECIMAL(10,2), ncm VARCHAR(8), cest VARCHAR(7), aliquotas_imposto DECIMAL(5,2), quantidade DECIMAL(10,3), valor_promocional DECIMAL(10,2), unidade_venda VARCHAR(2) CHECK(unidade_venda IN ('Kg', 'Un', 'L', 'G', 'Mt', 'Cx')), em_promocao BOOLEAN, lote VARCHAR(50), validade DATE, id_filial INTEGER, quantidade_minima DECIMAL(10,3) DEFAULT 10, FOREIGN KEY(id_filial) REFERENCES tb_filiais(id));
                        INSERT INTO tb_produtos (id,codigo_barras,nome,custo,preco_venda,ncm,cest,aliquotas_imposto,quantidade,valor_promocional,unidade_venda,em_promocao,lote,validade,id_filial,quantidade_minima) VALUES(1,'123456789','produto',25,50,'2202.10.00',' 17.044.00',3,60,35,'Un',1,'45678',NULL,1,10);
                        
                        CREATE TABLE tb_entradas_xml (id INTEGER PRIMARY KEY AUTOINCREMENT, chave_nfe VARCHAR(44) UNIQUE, id_fornecedor INTEGER, data_entrada DATETIME, valor_total DECIMAL(10,2), id_filial INTEGER, FOREIGN KEY(id_fornecedor) REFERENCES tb_fornecedores(id), FOREIGN KEY(id_filial) REFERENCES tb_filiais(id));
                        CREATE TABLE tb_sessao_caixa (id INTEGER PRIMARY KEY AUTOINCREMENT, id_caixa INTEGER, id_usuario INTEGER, data_abertura DATETIME, valor_fundo_troco DECIMAL(10,2), data_fechamento DATETIME, status BOOLEAN, valor_fechamento DECIMAL(10,2), FOREIGN KEY(id_caixa) REFERENCES tb_caixa(id), FOREIGN KEY(id_usuario) REFERENCES tb_usuarios(id));
                        CREATE TABLE tb_vendas (id INTEGER PRIMARY KEY AUTOINCREMENT, id_sessao_caixa INTEGER, id_cliente INTEGER, data_hora DATETIME, subtotal DECIMAL(10,2), desconto DECIMAL(10,2), total DECIMAL(10,2), status VARCHAR(15) CHECK(status IN ('pago', 'cancelado', 'pendente')), chave_nfe VARCHAR(44), FOREIGN KEY(id_sessao_caixa) REFERENCES tb_sessao_caixa(id), FOREIGN KEY(id_cliente) REFERENCES tb_clientes(id));
                        CREATE TABLE tb_pagamentos (id INTEGER PRIMARY KEY AUTOINCREMENT, id_venda INTEGER, metodo VARCHAR(20) CHECK(metodo IN ('pix', 'dinheiro', 'cartão de crédito', 'cartão de débito')), valor DECIMAL(10,2), cod_autorizacao_tef VARCHAR(50), FOREIGN KEY(id_venda) REFERENCES tb_vendas(id));
                        CREATE TABLE tb_itens_venda (id INTEGER PRIMARY KEY AUTOINCREMENT, id_venda INTEGER, id_produto INTEGER, quantidade DECIMAL(10,3), preco_unitario DECIMAL(10,2), subtotal DECIMAL(10,2), FOREIGN KEY(id_venda) REFERENCES tb_vendas(id), FOREIGN KEY(id_produto) REFERENCES tb_produtos(id));
                    ";

                    connection.Execute(sqlGitHub);
                }
            }

            // 2. ATUALIZADOR AUTOMÁTICO DE TABELAS (Migrations Silenciosas)
            // Mesmo se o banco já existir, ele garante que as colunas novas sejam empurradas
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                // Tenta adicionar a coluna quantidade_minima na tabela produtos
                try { connection.Execute("ALTER TABLE tb_produtos ADD COLUMN quantidade_minima DECIMAL(10,3) DEFAULT 10;"); } catch { /* Ignora se já existir */ }

                // Tenta adicionar a coluna id_filial na tabela usuários
                try { connection.Execute("ALTER TABLE tb_usuarios ADD COLUMN id_filial INTEGER;"); } catch { /* Ignora se já existir */ }

                // Tenta adicionar a coluna id_filial na tabela usuários
                try { connection.Execute("ALTER TABLE tb_usuarios ADD COLUMN id_filial INTEGER;"); } catch { /* Ignora se já existir */ }

                // NOVO: Adiciona a coluna para gerenciar a fila de sincronização
                try { connection.Execute("ALTER TABLE tb_vendas ADD COLUMN sincronizado BOOLEAN DEFAULT 0;"); } catch { /* Ignora se já existir */ }

                // NOVO: Adiciona a coluna para gerenciar a fila da sessão do caixa
                try { connection.Execute("ALTER TABLE tb_sessao_caixa ADD COLUMN sincronizado BOOLEAN DEFAULT 0;"); } catch { }

                // NOVAS COLUNAS DE PRODUTO
                try { connection.Execute("ALTER TABLE tb_produtos ADD COLUMN foto TEXT;"); } catch { }
                try { connection.Execute("ALTER TABLE tb_produtos ADD COLUMN codigo_geral INTEGER;"); } catch { }

                // NOVO: Adiciona a coluna do Cupom Único na tabela de Vendas
                try { connection.Execute("ALTER TABLE tb_vendas ADD COLUMN id_cupom VARCHAR(25);"); } catch { }

                // NOVO: Cria a tabela de Trocas caso ela ainda não exista no banco local
                try
                { connection.Execute(@"CREATE TABLE IF NOT EXISTS tb_trocas (id_troca INTEGER PRIMARY KEY AUTOINCREMENT, tipo_troca VARCHAR(15) CHECK(tipo_troca IN ('troca', 'devolucao')), data_troca DATETIME DEFAULT CURRENT_TIMESTAMP, id_vendedor INTEGER, produto_retornado VARCHAR(20), quantidade DECIMAL(10,3) );"); } catch { }
                try { connection.Execute("ALTER TABLE tb_trocas ADD COLUMN sincronizado BOOLEAN DEFAULT 0;"); } catch { }
            }
        }
    }
}