using Dapper;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace T_PACE
{
    public partial class MainWindow : Window
    {
        private ProdutoRepository _repositorio;
        public ObservableCollection<ItemCupom> Carrinho { get; set; }

        private decimal _descontoManualVenda = 0m;

        public MainWindow()
        {
            InitializeComponent();
            btnFinalizar.Click += (s, e) => AbrirTelaPagamento();
            txtBusca.KeyDown += TxtBusca_KeyDown;

            Task.Run(async () =>
            {
                await SincronizacaoService.SincronizarProdutosAsync();
                await SincronizacaoService.SincronizarVendasPendentesAsync();
                await SincronizacaoService.SincronizarTrocasPendentesAsync();
            });

            // ATUALIZANDO O PERFIL VISUAL
            if (!string.IsNullOrEmpty(Session.CurrentUserName))
            {
                txtNomeUsuario.Text = Session.CurrentUserName;
                txtIniciaisUsuario.Text = Session.CurrentUserName.Length > 1
                    ? Session.CurrentUserName.Substring(0, 2).ToUpper()
                    : Session.CurrentUserName.ToUpper();
            }

            try
            {
                DatabaseConfig.InicializarBanco();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao criar banco local: {ex.Message}");
            }

            _repositorio = new ProdutoRepository();
            Carrinho = new ObservableCollection<ItemCupom>();

            listaCupom.ItemsSource = Carrinho;
            txtBusca.KeyDown += TxtBusca_KeyDown;

            Task.Run(async () => await SincronizacaoService.SincronizarProdutosAsync());
        }

        // Adicionamos a palavra 'async' aqui na assinatura do método
        private async void TxtBusca_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string entradaCompleta = txtBusca.Text.Trim();
                if (!string.IsNullOrEmpty(entradaCompleta))
                {
                    BiparProduto(entradaCompleta);
                }
            }
            else if (e.Key == Key.F3)
            {
                AbrirTelaCancelamento();
            }
            else if (e.Key == Key.F4)
            {
                AbrirTelaBusca();
            }
            else if (e.Key == Key.F5)
            {
                AbrirTelaDesconto();
            }
            else if (e.Key == Key.F6)
            {
                AbrirTelaTroca();
            }
            else if (e.Key == Key.F2)
            {
                AbrirTelaPagamento();
            }
            // NOVO: Atalho F9 (Fechamento do Caixa)
            else if (e.Key == Key.F9)
            {
                AbrirTelaFechamento();
            }
        }

        private void BiparProduto(string entradaCodigo)
        {
            try
            {
                int quantidadeDesejada = 1;
                string codigoFinal = entradaCodigo;

                // 1. INTELIGÊNCIA DO MULTIPLICADOR (Ex: 5x123456)
                if (entradaCodigo.Contains("x") || entradaCodigo.Contains("X"))
                {
                    // Quebra a string em duas partes: o que vem antes do X e o que vem depois
                    string[] partes = entradaCodigo.Split(new char[] { 'x', 'X' }, 2);

                    // Verifica se a primeira parte é realmente um número
                    if (int.TryParse(partes[0].Trim(), out int qtd) && qtd > 0)
                    {
                        quantidadeDesejada = qtd;
                        codigoFinal = partes[1].Trim(); // O código de barras é o que restou
                    }
                }

                // 2. BUSCA NO BANCO
                var produto = _repositorio.BuscarPorCodigoDeBarras(codigoFinal);

                if (produto != null)
                {
                    decimal precoCheio = produto.preco_venda;
                    decimal descontoDoItem = 0m;

                    if (produto.em_promocao && produto.valor_promocional.HasValue)
                    {
                        decimal valorPromo = Convert.ToDecimal(produto.valor_promocional.Value);
                        if (valorPromo > 0 && valorPromo < precoCheio)
                        {
                            descontoDoItem = precoCheio - valorPromo;
                        }
                    }

                    decimal precoCobrado = precoCheio - descontoDoItem;
                    var itemExistente = Carrinho.FirstOrDefault(c => c.Codigo == produto.codigo_barras);

                    if (itemExistente != null)
                    {
                        // Soma a quantidadeNova com a que já estava no carrinho
                        itemExistente.Quantidade += quantidadeDesejada;
                        itemExistente.Total = itemExistente.Quantidade * precoCobrado;
                    }
                    else
                    {
                        Carrinho.Add(new ItemCupom
                        {
                            IdProduto = produto.id,
                            Codigo = produto.codigo_barras,
                            Descricao = produto.nome,
                            Quantidade = quantidadeDesejada,
                            PrecoUnitario = precoCheio,
                            DescontoUnitario = descontoDoItem,
                            Total = quantidadeDesejada * precoCobrado
                        });
                    }

                    txtUltimoNome.Text = produto.nome;
                    txtUltimoDetalhes.Text = $"Cód: {produto.codigo_barras}  |  Qtd: {quantidadeDesejada} un";

                    // Exibe o preço total cobrado pela quantidade inteira na tela principal
                    txtUltimoPreco.Text = $"R$ {(quantidadeDesejada * precoCobrado):N2}";

                    AtualizarTotais();
                    txtBusca.Clear();
                    txtBusca.Clear();

                    // --- CARREGAMENTO DA FOTO DO CACHE LOCAL INSTANTÂNEO ---
                    if (!string.IsNullOrEmpty(produto.foto))
                    {
                        string nomeArquivo = produto.foto.Substring(produto.foto.LastIndexOf('/') + 1);
                        string caminhoLocal = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Imagens", nomeArquivo);
                        if (System.IO.File.Exists(caminhoLocal))
                        {
                            try
                            {
                                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                                bitmap.BeginInit();
                                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                bitmap.UriSource = new Uri(caminhoLocal, UriKind.Absolute);
                                bitmap.EndInit();
                                bitmap.Freeze(); // Garante estabilidade na interface gráfica

                                // CORREÇÃO: Aplica rotação de 90° para compensar o formato de orientação do celular
                                var rotatedBitmap = new System.Windows.Media.Imaging.TransformedBitmap(bitmap, new System.Windows.Media.RotateTransform(90));
                                rotatedBitmap.Freeze();

                                imgProdutoFoto.Source = rotatedBitmap;
                                imgProdutoFoto.Visibility = Visibility.Visible;
                                txtFotoPlaceholder.Visibility = Visibility.Collapsed;
                            }
                            catch
                            {
                                imgProdutoFoto.Visibility = Visibility.Collapsed;
                                txtFotoPlaceholder.Text = "ERRO NA FOTO";
                                txtFotoPlaceholder.Visibility = Visibility.Visible;
                            }
                        }
                        else
                        {
                            imgProdutoFoto.Visibility = Visibility.Collapsed;
                            txtFotoPlaceholder.Text = "FOTO NÃO BAIXADA";
                            txtFotoPlaceholder.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        imgProdutoFoto.Visibility = Visibility.Collapsed;
                        txtFotoPlaceholder.Text = "SEM FOTO";
                        txtFotoPlaceholder.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    // Se não encontrou nem com nem sem multiplicador
                    MessageBox.Show("Produto não cadastrado!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtBusca.SelectAll();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro na busca: {ex.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // LÓGICA DE CANCELAMENTO (F3)
        // ==========================================
        private void AbrirTelaCancelamento()
        {
            if (Carrinho.Count == 0)
            {
                MessageBox.Show("Não há itens no cupom para cancelar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            OverlayCancelamento.Visibility = Visibility.Visible;
            txtBuscaCancelamento.Clear();
            txtBuscaCancelamento.Focus();

            txtBuscaCancelamento.KeyDown -= TxtBuscaCancelamento_KeyDown;
            txtBuscaCancelamento.KeyDown += TxtBuscaCancelamento_KeyDown;
        }

        private void TxtBuscaCancelamento_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string codigo = txtBuscaCancelamento.Text.Trim();
                if (!string.IsNullOrEmpty(codigo))
                {
                    CancelarProduto(codigo);
                }
            }
            else if (e.Key == Key.Escape)
            {
                FecharTelaCancelamento();
            }
        }

        private void FecharTelaCancelamento()
        {
            OverlayCancelamento.Visibility = Visibility.Collapsed;
            txtBusca.Focus();
        }

        private void CancelarProduto(string codigo)
        {
            var itemExistente = Carrinho.FirstOrDefault(c => c.Codigo == codigo);

            if (itemExistente != null)
            {
                itemExistente.Quantidade -= 1;

                if (itemExistente.Quantidade <= 0)
                {
                    Carrinho.Remove(itemExistente);
                }
                else
                {
                    itemExistente.Total = itemExistente.Quantidade * (itemExistente.PrecoUnitario - itemExistente.DescontoUnitario);
                }

                txtUltimoNome.Text = "ITEM CANCELADO";
                txtUltimoDetalhes.Text = $"Cód: {codigo} removido";
                txtUltimoPreco.Text = $"- R$ {itemExistente.PrecoUnitario - itemExistente.DescontoUnitario:N2}";

                AtualizarTotais();
                FecharTelaCancelamento();
            }
            else
            {
                MessageBox.Show("Este item não está no cupom atual!", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBuscaCancelamento.SelectAll();
            }
        }

        // ==========================================
        // LÓGICA DE TROCAS E DEVOLUÇÕES (F6)
        // ==========================================
        private void AbrirTelaTroca()
        {
            OverlayTroca.Visibility = Visibility.Visible;
            txtBuscaCupomTroca.Clear();
            listaItensCupom.ItemsSource = null;
            txtQtdTroca.Text = "1";
            cmbTipoTroca.SelectedIndex = 0;
            txtBuscaCupomTroca.Focus();
        }

        private void FecharTelaTroca()
        {
            OverlayTroca.Visibility = Visibility.Collapsed;
            txtBusca.Focus();
        }

        // CONTROLES DE TECLADO (Fluxo de navegação)
        private void TxtBuscaCupomTroca_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnBuscarCupom_Click(null, null);
            else if (e.Key == Key.Escape) FecharTelaTroca();
        }

        private void ListaItensCupom_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                txtQtdTroca.Focus();
                txtQtdTroca.SelectAll();
            }
            else if (e.Key == Key.Escape) FecharTelaTroca();
        }

        private void TxtQtdTroca_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                cmbTipoTroca.Focus();
            }
            else if (e.Key == Key.Escape) FecharTelaTroca();
        }

        private void CmbTipoTroca_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                BtnConfirmarTroca_Click(null, null);
            }
            else if (e.Key == Key.Escape) FecharTelaTroca();
        }

        // PROCESSAMENTO DE BANCO DE DADOS
        private void BtnBuscarCupom_Click(object sender, RoutedEventArgs e)
        {
            string cupom = txtBuscaCupomTroca.Text.Trim();
            if (string.IsNullOrEmpty(cupom)) return;

            using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(DatabaseConfig.ConnectionString))
            {
                var itens = conn.Query<ItemVendaTroca>(@"
                    SELECT v.id as id_venda, p.id as id_produto, p.codigo_barras, p.nome, iv.quantidade as quantidade_comprada, iv.preco_unitario
                    FROM tb_vendas v
                    JOIN tb_itens_venda iv ON v.id = iv.id_venda
                    JOIN tb_produtos p ON iv.id_produto = p.id
                    WHERE v.id_cupom = @Cupom", new { Cupom = cupom }).ToList();

                if (itens.Count == 0)
                {
                    MessageBox.Show("Cupom não encontrado ou não possui itens registrados.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    listaItensCupom.ItemsSource = null;
                }
                else
                {
                    listaItensCupom.ItemsSource = itens;
                    // Joga o foco pra lista para o usuário selecionar com as setas e espaço
                    listaItensCupom.Focus();
                    listaItensCupom.SelectedIndex = 0;
                }
            }
        }

        private void BtnConfirmarTroca_Click(object sender, RoutedEventArgs e)
        {
            if (listaItensCupom.SelectedItems.Count == 0)
            {
                MessageBox.Show("Selecione pelo menos um produto da lista (use o mouse, ou Espaço/Setas no teclado).", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtQtdTroca.Text, out decimal qtdTroca) || qtdTroca <= 0)
            {
                MessageBox.Show("A quantidade informada é inválida.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 1. Validação de Segurança: Garante que o cliente não tente devolver mais do que comprou
            foreach (ItemVendaTroca selecionado in listaItensCupom.SelectedItems)
            {
                if (qtdTroca > selecionado.quantidade_comprada)
                {
                    MessageBox.Show($"Você está tentando trocar {qtdTroca} unidades de '{selecionado.nome}', mas apenas {selecionado.quantidade_comprada} foram compradas neste cupom.\n\nReduza a quantidade informada ou troque os itens individualmente.", "Erro de Quantidade", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtQtdTroca.Focus();
                    return;
                }
            }

            string tipoStr = cmbTipoTroca.SelectedIndex == 0 ? "troca" : "devolucao";
            decimal valorTotalDevolvido = 0m;

            try
            {
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        // 2. Loop de processamento de múltiplos itens
                        foreach (ItemVendaTroca selecionado in listaItensCupom.SelectedItems)
                        {
                            decimal valorItem = qtdTroca * selecionado.preco_unitario;
                            valorTotalDevolvido += valorItem;

                            // CORREÇÃO DA HORA: Força o uso do DateTime.Now do C# em vez do UTC do SQLite
                            conn.Execute(@"INSERT INTO tb_trocas (tipo_troca, data_troca, id_vendedor, produto_retornado, quantidade)
                                           VALUES (@Tipo, @DataHora, @Vendedor, @Codigo, @Qtd)",
                                         new { Tipo = tipoStr, DataHora = DateTime.Now, Vendedor = Session.CurrentUserId, Codigo = selecionado.codigo_barras, Qtd = qtdTroca }, tx);

                            // CORREÇÃO DO ESTOQUE: Usa a mesma lógica avançada das Vendas (reconhece codigo_geral e múltiplos códigos)
                            conn.Execute(@"UPDATE tb_produtos 
                                           SET quantidade = quantidade + @Qtd 
                                           WHERE (codigo_geral IS NOT NULL AND codigo_geral = (SELECT codigo_geral FROM tb_produtos WHERE id = @IdProduto))
                                              OR (codigo_geral IS NULL AND id = @IdProduto)",
                                         new { Qtd = qtdTroca, IdProduto = selecionado.id_produto }, tx);
                        }

                        tx.Commit();
                    }
                }
                Task.Run(async () => await SincronizacaoService.SincronizarTrocasPendentesAsync());
                // 3. Resultado Financeiro
                if (tipoStr == "troca")
                {
                    _descontoManualVenda += valorTotalDevolvido;
                    AtualizarTotais();
                    MessageBox.Show($"Troca registrada com sucesso.\n\nDesconto de R$ {valorTotalDevolvido:N2} aplicado.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Devolução registrada com sucesso.\n\nValor a estornar: R$ {valorTotalDevolvido:N2}", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                FecharTelaTroca();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao processar troca: {ex.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // LÓGICA DE BUSCA POR NOME (F4)
        // ==========================================
        private void AbrirTelaBusca()
        {
            OverlayBusca.Visibility = Visibility.Visible;
            txtBuscaNome.Clear();
            listaResultadosBusca.ItemsSource = null;
            txtBuscaNome.Focus();
        }

        private void FecharTelaBusca()
        {
            OverlayBusca.Visibility = Visibility.Collapsed;
            txtBusca.Focus(); // Devolve o foco para o leitor de código de barras
        }

        // Pesquisa no banco enquanto o usuário digita
        private void txtBuscaNome_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string termo = txtBuscaNome.Text.Trim();

            // Só pesquisa a partir de 2 letras, para evitar sobrecarregar o banco
            if (termo.Length >= 2)
            {
                var resultados = _repositorio.BuscarPorNome(termo);
                listaResultadosBusca.ItemsSource = resultados;
            }
            else
            {
                listaResultadosBusca.ItemsSource = null;
            }
        }

        // Controle do teclado no campo de texto
        private void txtBuscaNome_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                FecharTelaBusca();
                e.Handled = true;
            }
            else if (e.Key == Key.Down && listaResultadosBusca.Items.Count > 0)
            {
                // Impede que o TextBox continue processando a seta
                e.Handled = true;

                // Move o foco para a lista e seleciona o primeiro item
                listaResultadosBusca.Focus();
                listaResultadosBusca.SelectedIndex = 0;

                // Força o "foco físico" no primeiro item da lista para as setas continuarem funcionando
                var item = (System.Windows.Controls.ListBoxItem)listaResultadosBusca.ItemContainerGenerator.ContainerFromIndex(0);
                item?.Focus();
            }
            else if (e.Key == Key.Enter && listaResultadosBusca.Items.Count > 0)
            {
                listaResultadosBusca.SelectedIndex = 0;
                SelecionarProdutoBusca();
                e.Handled = true;
            }
        }

        // Controle do teclado navegando dentro da lista
        private void listaResultadosBusca_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SelecionarProdutoBusca();
            }
            else if (e.Key == Key.Escape)
            {
                txtBuscaNome.Focus(); // Aperta Esc, devolve o foco pra digitar de novo
            }
        }

        // Para quem prefere usar o mouse (2 cliques)
        private void listaResultadosBusca_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelecionarProdutoBusca();
        }

        private void SelecionarProdutoBusca()
        {
            if (listaResultadosBusca.SelectedItem is Produto produtoSelecionado)
            {
                FecharTelaBusca();

                // Adiciona o código do produto ao lado do que já estiver no campo (ex: "5x")
                txtBusca.Text += produtoSelecionado.codigo_barras;

                // Joga o cursor piscante para o final do texto e devolve o foco
                txtBusca.CaretIndex = txtBusca.Text.Length;
                txtBusca.Focus();
            }
        }

        // ==========================================
        // LÓGICA DE DESCONTO GLOBAL (F5)
        // ==========================================
        private void AbrirTelaDesconto()
        {
            if (Carrinho.Count == 0)
            {
                MessageBox.Show("Adicione itens ao cupom antes de aplicar um desconto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            OverlayDesconto.Visibility = Visibility.Visible;

            // Se já houver um desconto aplicado antes, mostra ele. Se não, limpa.
            txtValorDesconto.Text = _descontoManualVenda > 0 ? _descontoManualVenda.ToString("N2") : "";
            txtValorDesconto.SelectAll(); // Seleciona tudo para o operador sobrescrever fácil
            txtValorDesconto.Focus();
        }

        private void FecharTelaDesconto()
        {
            OverlayDesconto.Visibility = Visibility.Collapsed;
            txtBusca.Focus();
        }

        private void txtValorDesconto_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                FecharTelaDesconto();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                AplicarDescontoManual();
                e.Handled = true;
            }
        }

        private void AplicarDescontoManual()
        {
            string entrada = txtValorDesconto.Text.Trim();

            // Se o operador limpou o campo e deu Enter, removemos o desconto
            if (string.IsNullOrEmpty(entrada))
            {
                _descontoManualVenda = 0m;
                AtualizarTotais();
                FecharTelaDesconto();
                return;
            }

            // Descobre se o operador digitou o símbolo de %
            bool isPorcentagem = entrada.Contains("%");

            // Limpa a string para pegar só o número puro (tira R$, %, espaços e arruma a vírgula)
            entrada = entrada.Replace("R$", "").Replace("%", "").Replace(" ", "").Replace(",", ".");

            if (decimal.TryParse(entrada, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valorDigitado))
            {
                // Descobre quanto a venda está custando agora (já abatendo os descontos de promoção)
                decimal subtotalBruto = Carrinho.Sum(i => i.PrecoUnitario * i.Quantidade);
                decimal descontosDePromocoes = Carrinho.Sum(i => i.DescontoUnitario * i.Quantidade);
                decimal totalAtualVenda = subtotalBruto - descontosDePromocoes;

                decimal valorDescontoFinal = 0m;

                if (isPorcentagem)
                {
                    // Regra de 3 básica: (Valor Total * Porcentagem) / 100
                    valorDescontoFinal = totalAtualVenda * (valorDigitado / 100m);
                }
                else
                {
                    // Se não tem %, é desconto em Reais direto
                    valorDescontoFinal = valorDigitado;
                }

                // Trava de segurança: O desconto não pode ser maior que a venda nem negativo
                if (valorDescontoFinal > totalAtualVenda || valorDescontoFinal < 0)
                {
                    MessageBox.Show("O desconto não pode ser negativo ou ultrapassar o valor total da venda!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtValorDesconto.SelectAll();
                    return;
                }

                // Aplica na variável global e atualiza a tela
                _descontoManualVenda = valorDescontoFinal;
                AtualizarTotais();
                FecharTelaDesconto();
            }
            else
            {
                MessageBox.Show("Valor inválido. Digite um número (ex: 10,50) ou uma porcentagem (ex: 15%).", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                txtValorDesconto.SelectAll();
            }
        }

        private void AtualizarTotais()
        {
            decimal subtotalBruto = Carrinho.Sum(i => i.PrecoUnitario * i.Quantidade);
            decimal descontosDePromocoes = Carrinho.Sum(i => i.DescontoUnitario * i.Quantidade);
            decimal descontosTotais = descontosDePromocoes + _descontoManualVenda;
            decimal totalAPagar = subtotalBruto - descontosTotais;

            txtSubtotal.Text = $"R$ {subtotalBruto:N2}";
            txtDesconto.Text = $"- R$ {descontosTotais:N2}";
            txtTotal.Text = $"R$ {totalAPagar:N2}";
        }

        private void txtBusca_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        // ==========================================
        // LÓGICA DE PAGAMENTO E FINALIZAÇÃO (F2)
        // ==========================================
        private void AbrirTelaPagamento()
        {
            if (Carrinho.Count == 0)
            {
                MessageBox.Show("Adicione itens ao cupom antes de finalizar a venda.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            decimal subtotalBruto = Carrinho.Sum(i => i.PrecoUnitario * i.Quantidade);
            decimal descontosDePromocoes = Carrinho.Sum(i => i.DescontoUnitario * i.Quantidade);
            decimal totalAPagar = subtotalBruto - (descontosDePromocoes + _descontoManualVenda);

            txtPagamentoTotal.Text = $"R$ {totalAPagar:N2}";
            txtValorRecebido.Text = totalAPagar.ToString("N2"); // Sugere o valor exato inicialmente
            txtTroco.Text = "R$ 0,00";

            OverlayPagamento.Visibility = Visibility.Visible;
            cmbMetodoPagamento.SelectedIndex = 0; // Reseta pro Dinheiro
            txtValorRecebido.SelectAll();
            txtValorRecebido.Focus();
        }

        private void FecharTelaPagamento()
        {
            OverlayPagamento.Visibility = Visibility.Collapsed;
            txtBusca.Focus();
        }

        private void txtValorRecebido_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            decimal subtotalBruto = Carrinho.Sum(i => i.PrecoUnitario * i.Quantidade);
            decimal descontosDePromocoes = Carrinho.Sum(i => i.DescontoUnitario * i.Quantidade);
            decimal totalAPagar = subtotalBruto - (descontosDePromocoes + _descontoManualVenda);

            string entrada = txtValorRecebido.Text.Trim().Replace("R$", "").Replace(" ", "").Replace(".", "").Replace(",", ".");

            if (decimal.TryParse(entrada, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valorRecebido))
            {
                decimal troco = valorRecebido - totalAPagar;
                txtTroco.Text = troco < 0 ? "R$ 0,00" : $"R$ {troco:N2}";
            }
            else
            {
                txtTroco.Text = "R$ 0,00";
            }
        }

        private void txtValorRecebido_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                FecharTelaPagamento();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                ProcessarVenda();
                e.Handled = true;
            }
            // NOVO: Setas para cima e para baixo trocam a forma de pagamento
            else if (e.Key == Key.Up)
            {
                // Se não estiver no primeiro item, sobe um. Se estiver, vai pro último.
                if (cmbMetodoPagamento.SelectedIndex > 0)
                    cmbMetodoPagamento.SelectedIndex--;
                else
                    cmbMetodoPagamento.SelectedIndex = cmbMetodoPagamento.Items.Count - 1;

                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                // Se não estiver no último item, desce um. Se estiver, vai pro primeiro.
                if (cmbMetodoPagamento.SelectedIndex < cmbMetodoPagamento.Items.Count - 1)
                    cmbMetodoPagamento.SelectedIndex++;
                else
                    cmbMetodoPagamento.SelectedIndex = 0;

                e.Handled = true;
            }
        }

        private void ProcessarVenda()
        {
            decimal subtotalBruto = Carrinho.Sum(i => i.PrecoUnitario * i.Quantidade);
            decimal descontosDePromocoes = Carrinho.Sum(i => i.DescontoUnitario * i.Quantidade);
            decimal descontoTotal = descontosDePromocoes + _descontoManualVenda;
            decimal totalAPagar = subtotalBruto - descontoTotal;

            string entrada = txtValorRecebido.Text.Trim().Replace("R$", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
            decimal.TryParse(entrada, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valorRecebido);
            if (valorRecebido < totalAPagar)
            {
                MessageBox.Show("O valor recebido é menor que o total da venda!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtValorRecebido.SelectAll();
                return;
            }

            // CALCULA O TROCO PARA SAIR NA NOTINHA
            decimal trocoFinal = valorRecebido - totalAPagar;
            // Se por acaso vir nulo, assume "dinheiro" como padrão
            string metodoSelecionado = ((System.Windows.Controls.ComboBoxItem)cmbMetodoPagamento.SelectedItem)?.Content?.ToString() ?? "dinheiro";

            // GERA CÓDIGO PAR DE 12 DÍGITOS (AnoMesDiaHoraMinutoSegundo)
            // Fica estreito na bobina, não precisa de zero extra (pois é par) e não se repete.
            string codigoUnicoCupom = DateTime.Now.ToString("yyMMddHHmmssff");

            try
            {
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    connection.Open();
                    // O BeginTransaction garante que as tabelas sejam salvas em lote (ou tudo funciona ou tudo cancela)
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Grava a Venda
                            // 1. Grava a Venda
                            var sqlVenda = @"INSERT INTO tb_vendas (id_sessao_caixa, id_cliente, data_hora, subtotal, desconto, total, status, id_cupom)
                                             VALUES (@Sessao, NULL, @DataHora, @Subtotal, @Desconto, @Total, 'pago', @IdCupom);
                                             SELECT last_insert_rowid();";

                            long idVenda = connection.ExecuteScalar<long>(sqlVenda, new
                            {
                                Sessao = Session.CurrentSessaoCaixaId,
                                DataHora = DateTime.Now,
                                Subtotal = subtotalBruto,
                                Desconto = descontoTotal,
                                Total = totalAPagar,
                                IdCupom = codigoUnicoCupom // SALVANDO NO BANCO LOCAL
                            }, transaction);

                            // 2. Grava os Itens e DÁ BAIXA NO ESTOQUE EM TODOS OS CÓDIGOS DE BARRAS IGUAIS
                            foreach (var item in Carrinho)
                            {
                                var sqlItem = @"INSERT INTO tb_itens_venda (id_venda, id_produto, quantidade, preco_unitario, subtotal)
                                                VALUES (@IdVenda, @IdProduto, @Quantidade, @PrecoUnitario, @Subtotal);
                                                
                                                UPDATE tb_produtos 
                                                SET quantidade = quantidade - @Quantidade 
                                                WHERE (codigo_geral IS NOT NULL AND codigo_geral = (SELECT codigo_geral FROM tb_produtos WHERE id = @IdProduto))
                                                   OR (codigo_geral IS NULL AND id = @IdProduto);";

                                connection.Execute(sqlItem, new
                                {
                                    IdVenda = idVenda,
                                    IdProduto = item.IdProduto,
                                    Quantidade = item.Quantidade,
                                    PrecoUnitario = item.PrecoUnitario,
                                    Subtotal = item.Total
                                }, transaction);
                            }

                            // 3. Grava o Pagamento

                            var sqlPagamento = @"INSERT INTO tb_pagamentos (id_venda, metodo, valor)
                                                 VALUES (@IdVenda, @Metodo, @Valor);";

                            connection.Execute(sqlPagamento, new
                            {
                                IdVenda = idVenda,
                                Metodo = metodoSelecionado,
                                Valor = totalAPagar
                            }, transaction);


                            transaction.Commit(); // Se chegou aqui, joga tudo pro arquivo do banco de vez!

                            // ==========================================
                            // IMPRIMIR O CUPOM DA VENDA
                            // ==========================================
                            ImprimirRecibo(idVenda, codigoUnicoCupom, Carrinho.ToList(), subtotalBruto, descontoTotal, totalAPagar, metodoSelecionado, valorRecebido, trocoFinal);

                            // CHAMA O SINCRONIZADOR DE FILA
                            Task.Run(async () => await SincronizacaoService.SincronizarVendasPendentesAsync());
                        }
                        catch (Exception)
                        {
                            transaction.Rollback(); // Deu erro? Cancela tudo pra não quebrar o banco
                            throw;
                        }
                    }
                }

                // Finalizou com sucesso! Limpa a tela pro próximo cliente
                Carrinho.Clear();
                _descontoManualVenda = 0m;
                txtUltimoNome.Text = "Caixa Livre";
                txtUltimoDetalhes.Text = " ";
                txtUltimoPreco.Text = "R$ 0,00";
                imgProdutoFoto.Visibility = Visibility.Collapsed;
                txtFotoPlaceholder.Visibility = Visibility.Visible;
                AtualizarTotais();
                FecharTelaPagamento();
            }
            catch (Exception ex)
            {
                // AGORA SIM ESTAMOS USANDO O "ex.Message"!
                // Se der erro ao vender, ele vai te falar exatamente o que quebrou!
                MessageBox.Show($"Erro ao concluir venda. Detalhe: {ex.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // LÓGICA DE SAIR DO CAIXA (F9) E FECHAMENTO
        // ==========================================
        private void AbrirTelaFechamento()
        {
            OverlayFechamento.Visibility = Visibility.Visible;
            txtValorFechamento.Text = "0,00";
            txtValorFechamento.SelectAll();
            txtValorFechamento.Focus();
        }

        private void FecharTelaFechamento()
        {
            OverlayFechamento.Visibility = Visibility.Collapsed;
            txtBusca.Focus();
        }

        private async void txtValorFechamento_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                FecharTelaFechamento();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                string entrada = txtValorFechamento.Text.Trim().Replace("R$", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
                decimal.TryParse(entrada, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valorFechamento);

                // Grava o fechamento no banco de dados local
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    conn.Execute("UPDATE tb_sessao_caixa SET data_fechamento = @Data, status = 0, valor_fechamento = @Valor, sincronizado = 0 WHERE id = @Id",
                        new { Data = DateTime.Now, Valor = valorFechamento, Id = Session.CurrentSessaoCaixaId });
                }

                FecharTelaFechamento();
                await SairDoCaixa(); // Usa o método original para limpar a tela e deslogar
            }
        }

        private async Task SairDoCaixa()
        {
            // Bloqueia a tela e avisa o operador
            this.IsEnabled = false;
            txtUltimoNome.Text = "Encerrando Turno...";
            txtUltimoDetalhes.Text = "Aguarde enquanto os dados são salvos na nuvem.";
            txtUltimoPreco.Text = "";

            // Força o envio das vendas e SESSÕES DE CAIXA pendentes
            await SincronizacaoService.SincronizarVendasPendentesAsync();
            await SincronizacaoService.SincronizarSessoesCaixaAsync();
            await SincronizacaoService.SincronizarTrocasPendentesAsync();

            // Limpa a Sessão de quem estava logado
            Session.CurrentUserId = 0;
            Session.CurrentUserName = string.Empty;
            Session.CurrentSessaoCaixaId = 0;

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            this.Hide();

            var login = new LoginWindow();
            bool? ok = login.ShowDialog();

            if (ok == true)
            {
                var novaMain = new MainWindow();
                Application.Current.MainWindow = novaMain;
                Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                novaMain.Show();
            }
            else
            {
                Application.Current.Shutdown();
            }
            this.Close();
        }

        // ==========================================
        // GERADOR E IMPRESSOR DE CUPOM TÉRMICO (NFC-e CORRIGIDO E ALINHADO)
        // ==========================================
        private void ImprimirRecibo(long idVenda, string codigoUnicoCupom, System.Collections.Generic.List<ItemCupom> itens, decimal subtotal, decimal desconto, decimal total, string metodoPagamento, decimal valorRecebido, decimal troco)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                var doc = new System.Windows.Documents.FlowDocument();

                // Reduzido para 275 e margens zeradas nas laterais para respeitar os 72mm da impressora térmica
                doc.PageWidth = 275;
                doc.PagePadding = new System.Windows.Thickness(0, 10, 0, 20);
                doc.FontFamily = new System.Windows.Media.FontFamily("Courier New");
                doc.Foreground = System.Windows.Media.Brushes.Black;

                // RESOLVE O PROBLEMA DE TEXTO FINO: Força TUDO a ser impresso em Negrito
                doc.FontWeight = FontWeights.Bold;

                System.Windows.Media.TextOptions.SetTextFormattingMode(doc, System.Windows.Media.TextFormattingMode.Display);
                System.Windows.Media.TextOptions.SetTextRenderingMode(doc, System.Windows.Media.TextRenderingMode.Aliased);

                // Reduzido para 36 colunas para caber na área de impressão sem quebrar a linha e estragar o alinhamento
                int colunas = 36;

                // 1. LOGO DA EMPRESA (Usando nota.png)
                try
                {
                    var uri = new Uri("pack://application:,,,/nota.png", UriKind.Absolute);
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage(uri);
                    var img = new System.Windows.Controls.Image
                    {
                        Source = bitmap,
                        Width = 140, // Tamanho da logo
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new System.Windows.Thickness(0, 0, 0, 10)
                    };
                    System.Windows.Media.RenderOptions.SetBitmapScalingMode(img, System.Windows.Media.BitmapScalingMode.HighQuality);
                    doc.Blocks.Add(new System.Windows.Documents.BlockUIContainer(img));
                }
                catch { }

                // 2. CABEÇALHO (Centralizado)
                var pCabecalho = new System.Windows.Documents.Paragraph();
                pCabecalho.TextAlignment = TextAlignment.Center;
                pCabecalho.LineHeight = 16;
                pCabecalho.Margin = new Thickness(0, 0, 0, 10);

                pCabecalho.Inlines.Add(new System.Windows.Documents.Run("CNPJ: 28.173.572/0001-91\nCNPJ: 52.356.963/0001-26\nCNPJ: 68.712.675/0001-05\n") { FontSize = 11 });
                pCabecalho.Inlines.Add(new System.Windows.Documents.Run("Rua Buarque de Macedo, 3158,\nCentro, Garibaldi-RS\n") { FontSize = 11 });
                pCabecalho.Inlines.Add(new System.Windows.Documents.Run(new string('-', colunas) + "\n") { FontSize = 11 });
                pCabecalho.Inlines.Add(new System.Windows.Documents.Run("DOCUMENTO AUXILIAR DA NOTA FISCAL\nDE CONSUMIDOR ELETRONICA\n") { FontWeight = FontWeights.Black, FontSize = 11 });
                pCabecalho.Inlines.Add(new System.Windows.Documents.Run(new string('-', colunas)) { FontSize = 11 });
                doc.Blocks.Add(pCabecalho);

                // 3. CORPO DO CUPOM (Sem código do produto)
                var sb = new System.Text.StringBuilder();

                sb.AppendLine("DESCRIÇÃO                   TOTAL(R$)");

                foreach (var item in itens)
                {
                    sb.AppendLine();
                    // Descrição na primeira linha
                    string desc = item.Descricao.Length > colunas ? item.Descricao.Substring(0, colunas) : item.Descricao;
                    sb.AppendLine(desc);

                    // Quantidade, UN e Preço na esquerda, Total na direita
                    string qtd = item.Quantidade.ToString("0.###");
                    string un = "UN";
                    string vlUn = item.PrecoUnitario.ToString("N2");
                    string tot = item.Total.ToString("N2");

                    string linhaValores = $"{qtd} {un} X {vlUn}";
                    sb.AppendLine(AlinharLinha(linhaValores, tot, colunas));
                }
                sb.AppendLine(new string('-', colunas));

                // Totais (Agora soma a quantidade real de itens, e não a quantidade de linhas!)
                decimal qtdeTotalItens = itens.Sum(i => i.Quantidade);
                sb.AppendLine(AlinharLinha("Qtde. Total de Itens", qtdeTotalItens.ToString("0.###"), colunas));
                sb.AppendLine(AlinharLinha("Subtotal R$", subtotal.ToString("N2"), colunas));
                if (desconto > 0)
                    sb.AppendLine(AlinharLinha("Descontos R$", desconto.ToString("N2"), colunas));

                doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(sb.ToString())) { FontSize = 11, Margin = new Thickness(0), LineHeight = 16 });

                // 4. VALOR A PAGAR EM DESTAQUE
                var pTotal = new System.Windows.Documents.Paragraph();
                pTotal.Margin = new Thickness(0, 2, 0, 10);
                pTotal.TextAlignment = TextAlignment.Left; // Alinha nativamente à direita, sem usar espaços falsos

                // Texto normal, número gigante e chamativo!
                pTotal.Inlines.Add(new System.Windows.Documents.Run("VALOR TOTAL  R$ ") { FontSize = 11 });
                pTotal.Inlines.Add(new System.Windows.Documents.Run(total.ToString("N2") + "\n") { FontWeight = FontWeights.Black, FontSize = 16 });
                doc.Blocks.Add(pTotal);

                // 5. FORMA DE PAGAMENTO E TROCO
                var sbPagamento = new System.Text.StringBuilder();
                sbPagamento.AppendLine(AlinharLinha("FORMA PAGAMENTO", "VALOR PAGO R$", colunas));
                sbPagamento.AppendLine(AlinharLinha(metodoPagamento.ToUpper(), valorRecebido.ToString("N2"), colunas));
                sbPagamento.AppendLine(AlinharLinha("Troco R$", troco.ToString("N2"), colunas));
                sbPagamento.AppendLine(new string('-', colunas));
                doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(sbPagamento.ToString())) { FontSize = 11, Margin = new Thickness(0), LineHeight = 16 });

                // 6. CÓDIGO DE BARRAS DA VENDA E RODAPÉ
                try
                {
                    // Gera o código de barras com a string recebida do momento exato da venda
                    var imgBarcode = GerarCodigoBarrasITF(codigoUnicoCupom);
                    doc.Blocks.Add(new System.Windows.Documents.BlockUIContainer(imgBarcode));

                    var pRodape = new System.Windows.Documents.Paragraph();
                    pRodape.Margin = new Thickness(0, 0, 0, 10);
                    pRodape.TextAlignment = TextAlignment.Center;

                    pRodape.Inlines.Add(new System.Windows.Documents.Run(codigoUnicoCupom + "\n") { FontSize = 12, FontWeight = FontWeights.Black });
                    pRodape.Inlines.Add(new System.Windows.Documents.Run($"\nData: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n") { FontSize = 11 });
                    pRodape.Inlines.Add(new System.Windows.Documents.Run("OBRIGADO PELA PREFERENCIA!") { FontWeight = FontWeights.Black, FontSize = 11 });
                    doc.Blocks.Add(pRodape);
                }
                catch { }

                printDialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator, $"Recibo T-PACE {idVenda}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível imprimir o cupom.\n\nDetalhe: {ex.Message}", "Erro de Impressão", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Função Matemática Auxiliar
        private string AlinharLinha(string esquerda, string direita, int totalColunas)
        {
            if (esquerda.Length + direita.Length >= totalColunas)
                esquerda = esquerda.Substring(0, totalColunas - direita.Length - 1);

            return esquerda + direita.PadLeft(totalColunas - esquerda.Length);
        }

        // ==========================================
        // GERADOR NATIVO DE CÓDIGO DE BARRAS (CODE 39)
        // ==========================================
        // NOVO GERADOR DE BARRAS: Interleaved 2 of 5 (Metade da largura, linhas nítidas e fortes)
        private System.Windows.Controls.Image GerarCodigoBarrasITF(string texto)
        {
            // O padrão ITF exige um número par de dígitos. Se for ímpar, adicionamos um zero.
            if (texto.Length % 2 != 0) texto = "0" + texto;

            string[] padroes = { "NNWWN", "WNNNW", "NWNNW", "WWNNN", "NNWNW", "WNWNN", "NWWNN", "NNNWW", "WNNWN", "NWNWN" };

            int barraMagra = 2; // Grosso o suficiente para a cabeça térmica não borrar
            int barraLarga = 5;
            int altura = 70;

            // Calcula a largura total da imagem com antecedência
            int larguraTotal = (barraMagra * 4) + (barraLarga + barraMagra * 2);
            foreach (char c in texto)
            {
                string p = padroes[c - '0'];
                foreach (char peso in p) larguraTotal += peso == 'W' ? barraLarga : barraMagra;
            }

            var visual = new System.Windows.Media.DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                // Pinta o fundo de branco
                context.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(0, 0, larguraTotal, altura));

                int x = 0;

                // Padrão de Início (Start: 4 linhas finas intercaladas)
                context.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(x, 0, barraMagra, altura)); x += barraMagra * 2;
                context.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(x, 0, barraMagra, altura)); x += barraMagra * 2;

                // A mágica: desenha os pares de números (um na barra preta, um no espaço em branco)
                for (int i = 0; i < texto.Length; i += 2)
                {
                    string p1 = padroes[texto[i] - '0'];     // Número 1 vira as barras pretas
                    string p2 = padroes[texto[i + 1] - '0']; // Número 2 vira os espaços brancos

                    for (int j = 0; j < 5; j++)
                    {
                        int wBar = p1[j] == 'W' ? barraLarga : barraMagra;
                        context.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(x, 0, wBar, altura));
                        x += wBar;

                        int wSpc = p2[j] == 'W' ? barraLarga : barraMagra;
                        x += wSpc;
                    }
                }

                // Padrão de Fim (Stop: Larga preta, Fina branca, Fina preta)
                context.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(x, 0, barraLarga, altura)); x += barraLarga + barraMagra;
                context.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(x, 0, barraMagra, altura)); x += barraMagra;
            }

            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(larguraTotal, altura, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(visual);

            var img = new System.Windows.Controls.Image
            {
                Source = rtb,
                Width = larguraTotal,
                Height = altura,
                Margin = new Thickness(0, 15, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(img, System.Windows.Media.BitmapScalingMode.NearestNeighbor);
            return img;
        }
        public class ItemVendaTroca
        {
            public long id_venda { get; set; }
            public int id_produto { get; set; } // <--- ADICIONE ESTA LINHA AQUI
            public string codigo_barras { get; set; }
            public string nome { get; set; }
            public decimal quantidade_comprada { get; set; }
            public decimal preco_unitario { get; set; }
        }

        // ==========================================
        // EVENTOS DE CLIQUE DO RODAPÉ
        // ==========================================
        private void Rodape_F2_Click(object sender, MouseButtonEventArgs e) => AbrirTelaPagamento();
        private void Rodape_F3_Click(object sender, MouseButtonEventArgs e) => AbrirTelaCancelamento();
        private void Rodape_F4_Click(object sender, MouseButtonEventArgs e) => AbrirTelaBusca();
        private void Rodape_F5_Click(object sender, MouseButtonEventArgs e) => AbrirTelaDesconto();
        private void Rodape_F6_Click(object sender, MouseButtonEventArgs e) => AbrirTelaTroca();
        private void Rodape_F9_Click(object sender, MouseButtonEventArgs e) => AbrirTelaFechamento();

        public class ItemCupom : INotifyPropertyChanged
        {
            public int IdProduto { get; set; } // <--- ADICIONE ESTA LINHA AQUI

            private int _quantidade;
            private decimal _total;

            public string Codigo { get; set; } = string.Empty;
            public string Descricao { get; set; } = string.Empty;
            public decimal PrecoUnitario { get; set; }
            public decimal DescontoUnitario { get; set; }

            public int Quantidade
            {
                get => _quantidade;
                set
                {
                    _quantidade = value;
                    OnPropertyChanged(nameof(Quantidade));
                }
            }

            public decimal Total
            {
                get => _total;
                set
                {
                    _total = value;
                    OnPropertyChanged(nameof(Total));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}