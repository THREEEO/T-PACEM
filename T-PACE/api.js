export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    const path = url.pathname.endsWith('/') ? url.pathname.slice(0, -1) : url.pathname;
    const method = request.method;

    const corsHeaders = {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, OPTIONS",
      "Access-Control-Allow-Headers": "Content-Type"
    };

    if (method === "OPTIONS") {
      return new Response(null, { headers: corsHeaders });
    }

    if (path === "/api/app/produtos") {
      const { results } = await env.DB.prepare("SELECT * FROM tb_produtos").all();
      return new Response(JSON.stringify(results), { headers: corsHeaders });
    }

    if (path === "/api/app/login" && method === "POST") {
      try {
        const body = await request.json();
        const stmt = env.DB.prepare("SELECT id, nome, nivel_acesso FROM tb_usuarios WHERE nome = ? AND senha = ?");
        const { results } = await stmt.bind(body.nome, body.senha).all();
        if (results.length > 0) {
          return new Response(JSON.stringify({ sucesso: true, usuario: results[0] }), { status: 200, headers: corsHeaders });
        } else {
          return new Response(JSON.stringify({ sucesso: false, erro: "Usuário ou senha incorretos!" }), { status: 401, headers: corsHeaders });
        }
      } catch (error) {
        return new Response(JSON.stringify({ sucesso: false, erro: "Erro no servidor." }), { status: 500, headers: corsHeaders });
      }
    }

    if (path === "/api/app/vendas" && method === "POST") {
        try {
            const body = await request.json();
            const stmts = [];
            const resVenda = await env.DB.prepare(`
                    INSERT INTO tb_vendas (id_sessao_caixa, id_cliente, data_hora, subtotal, desconto, total, status, id_cupom)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                `).bind(
                    body.id_sessao_caixa, 
                    body.id_cliente, 
                    body.data_hora, 
                    body.subtotal, 
                    body.desconto, 
                    body.total, 
                    body.status, 
                    body.id_cupom
                ).run();
            const cloudVendaId = resVenda.meta.last_row_id;
            for (const item of body.itens) {
                    stmts.push(env.DB.prepare(`
                        INSERT INTO tb_itens_venda (id_venda, id_produto, quantidade, preco_unitario, subtotal)
                        VALUES (?, ?, ?, ?, ?)
                    `).bind(cloudVendaId, item.id_produto, item.quantidade, item.preco_unitario, item.subtotal));
                    stmts.push(env.DB.prepare(`
                        UPDATE tb_produtos 
                        SET quantidade = quantidade - ? 
                        WHERE (codigo_geral IS NOT NULL AND codigo_geral = (SELECT codigo_geral FROM tb_produtos WHERE id = ?))
                           OR (codigo_geral IS NULL AND id = ?)
                    `).bind(item.quantidade, item.id_produto, item.id_produto));
                }
            for (const pag of body.pagamentos) {
                stmts.push(env.DB.prepare(`
                    INSERT INTO tb_pagamentos (id_venda, metodo, valor)
                    VALUES (?, ?, ?)
                `).bind(cloudVendaId, pag.metodo, pag.valor));
            }
            await env.DB.batch(stmts);
            return new Response(JSON.stringify({ sucesso: true, id_gerado: cloudVendaId }), { status: 201, headers: corsHeaders });
        } catch (error) {
            return new Response(JSON.stringify({ erro: "Erro ao sincronizar venda na nuvem.", detalhe: error.message }), { status: 500, headers: corsHeaders });
        }
    }

    if (path === "/api/app/sessoes" && method === "POST") {
        try {
            const body = await request.json();
            const stmts = [];
            for (const s of body) {
                stmts.push(env.DB.prepare(`
                    INSERT INTO tb_sessao_caixa (id_caixa, id_usuario, data_abertura, valor_fundo_troco, data_fechamento, status, valor_fechamento)
                    SELECT ?, ?, ?, ?, ?, ?, ?
                    WHERE NOT EXISTS (
                        SELECT 1 FROM tb_sessao_caixa WHERE data_abertura = ?
                    )
                `).bind(s.id_caixa, s.id_usuario, s.data_abertura, s.valor_fundo_troco, s.data_fechamento, s.status, s.valor_fechamento, s.data_abertura));
                stmts.push(env.DB.prepare(`
                    UPDATE tb_sessao_caixa
                    SET data_fechamento = ?, status = ?, valor_fechamento = ?
                    WHERE data_abertura = ?
                `).bind(s.data_fechamento, s.status, s.valor_fechamento, s.data_abertura));
            }
            await env.DB.batch(stmts);
            return new Response(JSON.stringify({ sucesso: true }), { status: 200, headers: corsHeaders });
        } catch (error) {
            return new Response(JSON.stringify({ erro: "Erro ao sincronizar sessões.", detalhe: error.message }), { status: 500, headers: corsHeaders });
        }
    }

    if (path === "/api/app/versao" && method === "GET") {
        try {
            const { results } = await env.DB.prepare("SELECT versao, link_download FROM tb_versao ORDER BY id DESC LIMIT 1").all();
            if (results.length > 0) {
                return new Response(JSON.stringify(results[0]), { status: 200, headers: corsHeaders });
            } else {
                return new Response(JSON.stringify({ erro: "Nenhuma versão encontrada" }), { status: 404, headers: corsHeaders });
            }
        } catch (error) {
            return new Response(JSON.stringify({ erro: "Erro ao buscar versão" }), { status: 500, headers: corsHeaders });
        }
    }

    if (path === "/api/app/trocas" && method === "POST") {
            try {
                const body = await request.json();
                const stmts = [];
                for (const t of body) {
                    stmts.push(env.DB.prepare(`
                        INSERT INTO tb_trocas (tipo_troca, data_troca, id_vendedor, produto_retornado, quantidade)
                        VALUES (?, ?, ?, ?, ?)
                    `).bind(t.tipo_troca, t.data_troca, t.id_vendedor, t.produto_retornado, t.quantidade));
                }
                await env.DB.batch(stmts);
                return new Response(JSON.stringify({ sucesso: true }), { status: 201, headers: corsHeaders });
            } catch (error) {
                return new Response(JSON.stringify({ erro: "Erro ao sincronizar trocas na nuvem.", detalhe: error.message }), { status: 500, headers: corsHeaders });
            }
        }

    if (path === "/api/web/login" && method === "POST") {
      try {
        const body = await request.json();
        const stmt = env.DB.prepare("SELECT id, nome, nivel_acesso FROM tb_usuarios WHERE nome = ? AND senha = ?");
        const { results } = await stmt.bind(body.nome, body.senha).all();
        if (results.length > 0) {
          return new Response(JSON.stringify({ sucesso: true, usuario: results[0] }), { status: 200, headers: corsHeaders });
        } else {
          return new Response(JSON.stringify({ sucesso: false, erro: "Usuário ou senha incorretos!" }), { status: 401, headers: corsHeaders });
        }
      } catch (error) {
        return new Response(JSON.stringify({ sucesso: false, erro: "Erro no servidor." }), { status: 500, headers: corsHeaders });
      }
    }

    if (path === "/api/web/dashboard" && method === "GET") {
      try {
        const sqlVendasHoje = `SELECT COALESCE(SUM(total), 0) AS total_vendas, COUNT(id) AS total_atendimentos FROM tb_vendas WHERE status = 'pago' AND DATE(data_hora) = DATE('now')`;
        const { results: resVendas } = await env.DB.prepare(sqlVendasHoje).all();
        const vendas = resVendas[0] || { total_vendas: 0, total_atendimentos: 0 };
        const sqlAlertas = `SELECT COUNT(id) AS total_alertas FROM tb_produtos WHERE quantidade <= quantidade_minima`;
        const { results: resAlertas } = await env.DB.prepare(sqlAlertas).all();
        const alertas = resAlertas[0] || { total_alertas: 0 };
        return new Response(JSON.stringify({ vendasHoje: vendas.total_vendas, atendimentosHoje: vendas.total_atendimentos, alertasEstoque: alertas.total_alertas }), { status: 200, headers: corsHeaders });
      } catch (error) {
        return new Response(JSON.stringify({ erro: "Erro ao consultar métricas do dashboard." }), { status: 500, headers: corsHeaders });
      }
    }

    if (path === "/api/web/graficos" && method === "GET") {
      try {
        const { results: resFat } = await env.DB.prepare(`SELECT DATE(data_hora) as data, SUM(total) as valor FROM tb_vendas WHERE status = 'pago' GROUP BY DATE(data_hora) ORDER BY data DESC LIMIT 7`).all();
        const { results: resPag } = await env.DB.prepare(`SELECT metodo, SUM(valor) as total FROM tb_pagamentos GROUP BY metodo`).all();
        const { results: resProd } = await env.DB.prepare(`SELECT p.nome, SUM(i.quantidade) as qtd FROM tb_itens_venda i JOIN tb_produtos p ON i.id_produto = p.id GROUP BY p.id ORDER BY qtd DESC LIMIT 5`).all();
        const { results: resFilial } = await env.DB.prepare(`SELECT f.nome_fantasia as nome, SUM(v.total) as valor FROM tb_vendas v JOIN tb_sessao_caixa s ON v.id_sessao_caixa = s.id JOIN tb_caixa c ON s.id_caixa = c.id JOIN tb_filiais f ON c.id_filial = f.id WHERE v.status = 'pago' GROUP BY f.id`).all();
        return new Response(JSON.stringify({ faturamento: resFat, pagamentos: resPag, produtos: resProd, filiais: resFilial }), { status: 200, headers: corsHeaders });
      } catch (error) {
        return new Response(JSON.stringify({ erro: "Erro ao gerar gráficos." }), { status: 500, headers: corsHeaders });
      }
    }

    if (method === "GET" && path === "/relatorios/giro") {
      try {
        const query = `
            SELECT p.id, p.nome, p.unidade_venda, p.quantidade as estoque_atual,
            COALESCE(SUM(iv.quantidade), 0) as volume_mes
            FROM tb_produtos p
            LEFT JOIN tb_itens_venda iv ON p.id = iv.id_produto
            LEFT JOIN tb_vendas v ON iv.id_venda = v.id
                 AND v.data_hora >= datetime('now', '-30 days')
                 AND v.status = 'pago'
            GROUP BY p.id
            ORDER BY volume_mes DESC
        `;
        const res = await env.DB.prepare(query).all();
        return new Response(JSON.stringify(res.results), { headers: corsHeaders });
      } catch (e) {
         return new Response(JSON.stringify({erro: e.message}), { status: 500, headers: corsHeaders });
       }
    }

    if (method === "GET" && path === "/relatorios/encalhados") {
      try {
        const query = `
            SELECT p.nome, p.quantidade, MAX(v.data_hora) as ultima_venda
            FROM tb_produtos p
            LEFT JOIN tb_itens_venda iv ON p.id = iv.id_produto
            LEFT JOIN tb_vendas v ON iv.id_venda = v.id AND v.status = 'pago'
            GROUP BY p.id
            HAVING ultima_venda IS NULL OR ultima_venda <= datetime('now', '-45 days')
        `;
        const res = await env.DB.prepare(query).all();
        return new Response(JSON.stringify(res.results), { headers: corsHeaders });
      } catch (e) {
         return new Response(JSON.stringify({erro: e.message}), { status: 500, headers: corsHeaders });
       }
    }

    if (method === "GET" && path === "/relatorios/trocas") {
      try {
        const query = `
            SELECT t.tipo_troca, t.data_troca, t.quantidade, p.nome
            FROM tb_trocas t
            LEFT JOIN tb_produtos p ON t.produto_retornado = p.codigo_barras
            ORDER BY t.data_troca DESC
        `;
        const res = await env.DB.prepare(query).all();
        return new Response(JSON.stringify(res.results), { headers: corsHeaders });
      } catch (e) {
         return new Response(JSON.stringify({erro: e.message}), { status: 500, headers: corsHeaders });
       }
    }

    if (method === "GET" && path === "/relatorios/desequilibrados") {
      try {
        const query = `
            SELECT nome, quantidade, quantidade_minima
            FROM tb_produtos
            WHERE quantidade <= (quantidade_minima / 3)
                OR quantidade >= (quantidade_minima * 2)
        `;
        const { results } = await env.DB.prepare(query).all();

        const desequilibrados = results.map(item => {
            const quant = item.quantidade || 0;
            const min = item.quantidade_minima || 10;
            const isExcesso = quant >= (min * 2);

            return {
                nome: item.nome,
                quantidade: quant,
                media: min,
                acao: isExcesso ? 'Promoção (Excesso)' : 'Repor (Escassez)',
                cor: isExcesso ? 'alerta' : 'alerta-baixo'
            };
        });
        return new Response(JSON.stringify(desequilibrados), { status: 200, headers: corsHeaders });
      } catch (e) {
        return new Response(JSON.stringify({ erro: e.message }), { status: 500, headers: corsHeaders });
      }
    }

    if (method === "GET" && path === "/relatorios/desempenho") {
      try {
        const inicio = url.searchParams.get("inicio") || "2000-01-01";
        const fim = url.searchParams.get("fim") || "2100-01-01";
        const queryGrafico = `
            SELECT date(data_hora) as data_venda, SUM(total) as faturamento_diario
            FROM tb_vendas
            WHERE status = 'pago' AND date(data_hora) BETWEEN date(?) AND date(?)
            GROUP BY date(data_hora)
            ORDER BY data_venda ASC
        `;
        const resGrafico = await env.DB.prepare(queryGrafico).bind(inicio, fim).all();

        const queryDetalhes = `
            SELECT
                v.id as id_venda,
                v.id_cupom,
                v.data_hora,
                v.subtotal as venda_subtotal,
                v.total as venda_total,
                u.nome as vendedor,
                GROUP_CONCAT(CAST(iv.quantidade AS INTEGER) || 'x ' || p.nome, ' | ') as produtos_comprados
            FROM tb_vendas v
            LEFT JOIN tb_sessao_caixa sc ON v.id_sessao_caixa = sc.id
            LEFT JOIN tb_usuarios u ON sc.id_usuario = u.id
            JOIN tb_itens_venda iv ON v.id = iv.id_venda
            JOIN tb_produtos p ON iv.id_produto = p.id
            WHERE v.status = 'pago' AND date(v.data_hora) BETWEEN date(?) AND date(?)
            GROUP BY v.id
            ORDER BY v.data_hora DESC
        `;
        const resDetalhes = await env.DB.prepare(queryDetalhes).bind(inicio, fim).all();
        return new Response(JSON.stringify({
            grafico: resGrafico.results,
            detalhes: resDetalhes.results,
            periodo: { inicio, fim }
        }), { status: 200, headers: corsHeaders });
      } catch (e) {
        return new Response(JSON.stringify({ erro: e.message }), { status: 500, headers: corsHeaders });
      }
    }

    if (method === "GET" && path === "/api/nfe/pendentes") {
      try {
        const { results } = await env.DB.prepare(
          "SELECT id FROM tb_vendas WHERE chave_nfe IS NULL ORDER BY id ASC"
        ).all();
        return new Response(JSON.stringify(results), { headers: corsHeaders });
      } catch (e) {
        return new Response(JSON.stringify({ error: e.message }), { status: 500, headers: corsHeaders });
      }
    }

    const matchVenda = path.match(/\/api\/nfe\/venda\/(\d+)/);
    if (method === "GET" && matchVenda) {
      const idVenda = matchVenda[1];
      try {
        const queryVenda = `
          SELECT
            v.id as id_pedido, v.total as total_final, v.subtotal as total_produtos, v.desconto as total_frete,
            c.cpf_cnpj as cpf, c.nome as cliente_nome, c.logradouro, c.numero, c.bairro, c.cidade, c.uf, c.cod_mun_ibge, c.cep
          FROM tb_vendas v
          LEFT JOIN tb_clientes c ON v.id_cliente = c.id
          WHERE v.id = ?
        `;
        const venda = await env.DB.prepare(queryVenda).bind(idVenda).first();
        if (!venda) return new Response(JSON.stringify({ error: "Venda não encontrada" }), { status: 404, headers: corsHeaders });

        const queryEmitente = `
          SELECT
            f.cnpj, f.nome_juridico as razao_social, f.inscricao_estadual as ie_emitente, f.crt as regime_tributario
          FROM tb_vendas v
          JOIN tb_sessao_caixa sc ON v.id_sessao_caixa = sc.id
          JOIN tb_caixa cx ON sc.id_caixa = cx.id
          JOIN tb_filiais f ON cx.id_filial = f.id
          WHERE v.id = ?
        `;
        const emitente = await env.DB.prepare(queryEmitente).bind(idVenda).first();

        const queryItens = `
          SELECT
            i.id_produto as produto_id, p.nome as nome_produto, i.quantidade, i.preco_unitario, i.subtotal,
            p.ncm, p.cest
          FROM tb_itens_venda i
          JOIN tb_produtos p ON i.id_produto = p.id
          WHERE i.id_venda = ?
        `;
        const itens = await env.DB.prepare(queryItens).bind(idVenda).all();

        return new Response(JSON.stringify({ pedido: venda, emitente: emitente, itens: itens.results }), { headers: corsHeaders });
      } catch (e) {
        return new Response(JSON.stringify({ error: e.message }), { status: 500, headers: corsHeaders });
      }
    }

    if (path === "/api/web/produtos" && method === "GET") {
      try {
        const { results } = await env.DB.prepare("SELECT * FROM tb_produtos").all();
        return new Response(JSON.stringify(results), { status: 200, headers: corsHeaders });
      } catch (error) {
        return new Response(JSON.stringify({ erro: "Erro ao carregar produtos." }), { status: 500, headers: corsHeaders });
      }
    }

    if (path === "/api/web/produtos" && method === "POST") {
      try {
        const body = await request.json();
        const stmt = env.DB.prepare(`
          INSERT INTO tb_produtos (
            nome, codigo_barras, preco_venda, quantidade, quantidade_minima, unidade_venda,
            custo, cest, aliquotas_imposto, ncm, valor_promocional, em_promocao, lote, validade, id_filial, foto, codigo_geral
          ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 1, ?, ?)
        `);
        await stmt.bind(
          body.nome || null, 
          body.codigo_barras || null, 
          body.preco_venda || 0, 
          body.quantidade || 0, 
          body.quantidade_minima || 0, 
          body.unidade_venda || 'Un',
          body.custo || null, 
          body.cest || null, 
          body.aliquotas_imposto || null, 
          body.ncm || null, 
          body.valor_promocional || null, 
          body.em_promocao || 0, 
          body.lote || null, 
          body.validade || null, 
          body.foto || null,
          body.codigo_geral || null
        ).run();

        return new Response(JSON.stringify({ sucesso: true }), { status: 201, headers: corsHeaders });
      } catch (error) {
        return new Response(JSON.stringify({ erro: "Erro ao adicionar produto.", detalhe: error.message }), { status: 500, headers: corsHeaders });
      }
    }

    if (path.startsWith("/api/web/produtos/") && method === "PUT") {
      try {
        const id = path.split('/').pop();
        const body = await request.json();
        const stmt = env.DB.prepare(`
          UPDATE tb_produtos
          SET nome = ?, codigo_barras = ?, preco_venda = ?, quantidade = ?, quantidade_minima = ?, unidade_venda = ?,
              custo = ?, cest = ?, aliquotas_imposto = ?, ncm = ?, valor_promocional = ?, em_promocao = ?, lote = ?, validade = ?, foto = ?, codigo_geral = ?
          WHERE id = ?
        `);
        await stmt.bind(
          body.nome || null, 
          body.codigo_barras || null, 
          body.preco_venda || 0, 
          body.quantidade || 0, 
          body.quantidade_minima || 0, 
          body.unidade_venda || 'Un',
          body.custo || null, 
          body.cest || null, 
          body.aliquotas_imposto || null, 
          body.ncm || null, 
          body.valor_promocional || null, 
          body.em_promocao || 0, 
          body.lote || null, 
          body.validade || null, 
          body.foto || null, 
          body.codigo_geral || null,
          id
        ).run();

        return new Response(JSON.stringify({ sucesso: true }), { status: 200, headers: corsHeaders });
      } catch (error) {
        return new Response(JSON.stringify({ erro: "Erro ao atualizar produto." }), { status: 500, headers: corsHeaders });
      }
    }

    if (path.startsWith("/api/web/produtos/") && method === "DELETE") {
      try {
        const id = path.split('/').pop();
        await env.DB.prepare("DELETE FROM tb_produtos WHERE id = ?").bind(id).run();
        return new Response(JSON.stringify({ sucesso: true }), { status: 200, headers: corsHeaders });
      } catch (error) {
        return new Response(JSON.stringify({ erro: "Erro ao excluir produto." }), { status: 500, headers: corsHeaders });
      }
    }

    if (path === "/api/web/upload" && method === "POST") {
      try {
        const formData = await request.formData();
        const file = formData.get("file");
        
        if (!file) {
          return new Response(JSON.stringify({ erro: "Nenhum arquivo enviado" }), { status: 400, headers: corsHeaders });
        }
        
        const fileName = file.name ? file.name.replace(/\s+/g, '_') : 'imagem_padrao.jpg';
        const nomeArquivo = `${Date.now()}_${fileName}`;
        
        const buffer = await file.arrayBuffer();
        
        await env.BUCKET_PRODUTOS.put(nomeArquivo, buffer, {
          httpMetadata: { contentType: file.type || 'image/jpeg' }
        });
        
        const urlPublica = `https://pub-cef0af4ec14641d69ab30f7d560f9387.r2.dev/${nomeArquivo}`;
        
        return new Response(JSON.stringify({ sucesso: true, url: urlPublica }), { status: 201, headers: corsHeaders });
      } catch (error) {
        return new Response(JSON.stringify({ erro: "Erro ao salvar no R2.", detalhe: error.message }), { status: 500, headers: corsHeaders });
      }
    }

    if (method === "GET" && path === "/api/web/funcionarios/pessoal") {
      try {
        const { results } = await env.DB.prepare(
          "SELECT id, nome_completo, data_nascimento, genero, raca, estado_civil, nacionalidade, naturalidade, cpf, orgao_emissor, email, telefone, contato_emergencia, pcd, escolaridade, formacao_academica, logradouro, numero, bairro, cidade, cep, complemento, status FROM tb_funcionarios ORDER BY id ASC"
        ).all();
        return new Response(JSON.stringify(results), { headers: corsHeaders });
      } catch (e) {
        return new Response(JSON.stringify({ error: "Erro GET Pessoal: " + e.message }), { status: 500, headers: corsHeaders });
      }
    }

    if (method === "GET" && path === "/api/web/funcionarios/profissional") {
      try {
        const { results } = await env.DB.prepare(
          "SELECT id_funcionario, data_admissao, tipo, cargo, nivel_senioridade, setor, gestor, tempo_empregado, modelo_trabalho, escala_trabalho, salario_base, tipo_remuneracao, banco, agencia, chave_pix, centro_custo, data_demissao, tipo_demissao, motivo_demissao FROM tb_funcionarios_complemento ORDER BY id_funcionario ASC"
        ).all();
        return new Response(JSON.stringify(results), { headers: corsHeaders });
      } catch (e) {
        return new Response(JSON.stringify({ error: "Erro GET Profissional: " + e.message }), { status: 500, headers: corsHeaders });
      }
    }

    if (method === "POST" && path === "/api/web/funcionarios") {
      try {
        const body = await request.json();

        const maxIdResult = await env.DB.prepare("SELECT MAX(id) as maxId FROM tb_funcionarios").first();
        const novoId = (maxIdResult && maxIdResult.maxId !== null ? maxIdResult.maxId : 0) + 1;

        const stmtFuncionario = env.DB.prepare(`
          INSERT INTO tb_funcionarios (
            id, nome_completo, data_nascimento, genero, raca, estado_civil, nacionalidade, 
            naturalidade, cpf, orgao_emissor, email, telefone, contato_emergencia, 
            pcd, escolaridade, formacao_academica, logradouro, numero, bairro, 
            cidade, cep, complemento, status
          ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        `);

        await stmtFuncionario.bind(
          novoId, body.nome_completo, body.data_nascimento, body.genero, body.raca, body.estado_civil, body.nacionalidade,
          body.naturalidade, body.cpf, body.orgao_emissor, body.email, body.telefone, body.contato_emergencia,
          body.pcd, body.escolaridade, body.formacao_academica, body.logradouro, body.numero, body.bairro,
          body.cidade, body.cep, body.complemento, body.status
        ).run();

        const stmtComplemento = env.DB.prepare(`
          INSERT INTO tb_funcionarios_complemento (
            id_funcionario, data_admissao, tipo, cargo, nivel_senioridade, 
            setor, gestor, tempo_empregado, modelo_trabalho, escala_trabalho, 
            salario_base, tipo_remuneracao, banco, agencia, chave_pix, centro_custo, 
            data_demissao, tipo_demissao, motivo_demissao
          ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        `);

        await stmtComplemento.bind(
          novoId, body.data_admissao, body.tipo, body.cargo, body.nivel_senioridade,
          body.setor, body.gestor, body.tempo_empregado, body.modelo_trabalho, body.escala_trabalho,
          body.salario_base, body.tipo_remuneracao, body.banco, body.agencia, body.chave_pix, body.centro_custo,
          body.data_demissao, body.tipo_demissao, body.motivo_demissao
        ).run();

        return new Response(JSON.stringify({ success: true, id: novoId }), { status: 201, headers: corsHeaders });
      } catch (e) { 
        return new Response(JSON.stringify({ error: "POST Funcionario: " + e.message }), { status: 500, headers: corsHeaders }); 
      }
    }
    if (method === "PUT" && path.startsWith("/api/web/funcionarios/")) {
      try {
        const id = path.split('/').pop();
        const body = await request.json();

        const stmtFuncionario = env.DB.prepare(`
          UPDATE tb_funcionarios SET 
            nome_completo=?, data_nascimento=?, genero=?, raca=?, estado_civil=?, nacionalidade=?, 
            naturalidade=?, cpf=?, orgao_emissor=?, email=?, telefone=?, contato_emergencia=?, 
            pcd=?, escolaridade=?, formacao_academica=?, logradouro=?, numero=?, bairro=?, 
            cidade=?, cep=?, complemento=?, status=?
          WHERE id = ?
        `);

        await stmtFuncionario.bind(
          body.nome_completo, body.data_nascimento, body.genero, body.raca, body.estado_civil, body.nacionalidade,
          body.naturalidade, body.cpf, body.orgao_emissor, body.email, body.telefone, body.contato_emergencia,
          body.pcd, body.escolaridade, body.formacao_academica, body.logradouro, body.numero, body.bairro,
          body.cidade, body.cep, body.complemento, body.status, id
        ).run();

        const checkComplemento = await env.DB.prepare("SELECT id FROM tb_funcionarios_complemento WHERE id_funcionario = ?").bind(id).first();

        if (checkComplemento) {
          const stmtComplemento = env.DB.prepare(`
            UPDATE tb_funcionarios_complemento SET 
              data_admissao=?, tipo=?, cargo=?, nivel_senioridade=?, 
              setor=?, gestor=?, tempo_empregado=?, modelo_trabalho=?, escala_trabalho=?, 
              salario_base=?, tipo_remuneracao=?, banco=?, agencia=?, chave_pix=?, centro_custo=?, 
              data_demissao=?, tipo_demissao=?, motivo_demissao=?
            WHERE id_funcionario = ?
          `);
          await stmtComplemento.bind(
            body.data_admissao, body.tipo, body.cargo, body.nivel_senioridade,
            body.setor, body.gestor, body.tempo_empregado, body.modelo_trabalho, body.escala_trabalho,
            body.salario_base, body.tipo_remuneracao, body.banco, body.agencia, body.chave_pix, body.centro_custo,
            body.data_demissao, body.tipo_demissao, body.motivo_demissao, id
          ).run();
        } else {
          const stmtComplemento = env.DB.prepare(`
            INSERT INTO tb_funcionarios_complemento (
              id_funcionario, data_admissao, tipo, cargo, nivel_senioridade, 
              setor, gestor, tempo_empregado, modelo_trabalho, escala_trabalho, 
              salario_base, tipo_remuneracao, banco, agencia, chave_pix, centro_custo, 
              data_demissao, tipo_demissao, motivo_demissao
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
          `);
          await stmtComplemento.bind(
            id, body.data_admissao, body.tipo, body.cargo, body.nivel_senioridade,
            body.setor, body.gestor, body.tempo_empregado, body.modelo_trabalho, body.escala_trabalho,
            body.salario_base, body.tipo_remuneracao, body.banco, body.agencia, body.chave_pix, body.centro_custo,
            body.data_demissao, body.tipo_demissao, body.motivo_demissao
          ).run();
        }

        return new Response(JSON.stringify({ success: true }), { status: 200, headers: corsHeaders });
      } catch (e) {
        return new Response(JSON.stringify({ error: "PUT Funcionario: " + e.message }), { status: 500, headers: corsHeaders });
      }
    }

    return new Response("Rota não encontrada: " + path, { status: 404, headers: corsHeaders });
  }
};