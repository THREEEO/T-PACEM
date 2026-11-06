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
        public ObservableCollection<PagamentoParcial> PagamentosParciais { get; set; }

        private decimal _descontoManualVenda = 0m;

        public MainWindow()
        {
            InitializeComponent();
            btnFinalizar.Click += (s, e) => AbrirTelaPagamento();
            txtBusca.KeyDown += TxtBusca_KeyDown;

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
            PagamentosParciais = new ObservableCollection<PagamentoParcial>();

            listaCupom.ItemsSource = Carrinho;
            listaPagamentos.ItemsSource = PagamentosParciais;

            Task.Run(async () =>
            {
                await SincronizacaoService.SincronizarVendasPendentesAsync();
                await SincronizacaoService.SincronizarTrocasPendentesAsync();
                await SincronizacaoService.SincronizarProdutosAsync();
            });
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (OverlayCancelamento.Visibility == Visibility.Visible)
                {
                    FecharTelaCancelamento();
                    e.Handled = true;
                }
                else if (OverlayBusca.Visibility == Visibility.Visible)
                {
                    FecharTelaBusca();
                    e.Handled = true;
                }
                else if (OverlayDesconto.Visibility == Visibility.Visible)
                {
                    FecharTelaDesconto();
                    e.Handled = true;
                }
                else if (OverlayPagamento.Visibility == Visibility.Visible)
                {
                    FecharTelaPagamento();
                    e.Handled = true;
                }
                else if (OverlayFechamento.Visibility == Visibility.Visible)
                {
                    FecharTelaFechamento();
                    e.Handled = true;
                }
                else if (OverlayTroca.Visibility == Visibility.Visible)
                {
                    FecharTelaTroca();
                    e.Handled = true;
                }
            }
        }

        private void BtnFecharMain_Click(object sender, RoutedEventArgs e) => AbrirTelaFechamento();
        private void BtnFecharCancelamento_Click(object sender, RoutedEventArgs e) => FecharTelaCancelamento();
        private void BtnFecharBusca_Click(object sender, RoutedEventArgs e) => FecharTelaBusca();
        private void BtnFecharDesconto_Click(object sender, RoutedEventArgs e) => FecharTelaDesconto();
        private void BtnFecharPagamento_Click(object sender, RoutedEventArgs e) => FecharTelaPagamento();
        private void BtnFecharFechamento_Click(object sender, RoutedEventArgs e) => FecharTelaFechamento();
        private void BtnFecharTroca_Click(object sender, RoutedEventArgs e) => FecharTelaTroca();

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

                if (entradaCodigo.Contains("x") || entradaCodigo.Contains("X"))
                {
                    string[] partes = entradaCodigo.Split(new char[] { 'x', 'X' }, 2);

                    if (int.TryParse(partes[0].Trim(), out int qtd) && qtd > 0)
                    {
                        quantidadeDesejada = qtd;
                        codigoFinal = partes[1].Trim();
                    }
                }

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

                    txtUltimoPreco.Text = $"R$ {(quantidadeDesejada * precoCobrado):N2}";

                    AtualizarTotais();
                    txtBusca.Clear();
                    txtBusca.Clear();

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
                                bitmap.Freeze();

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
                    MessageBox.Show("Produto não cadastrado!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtBusca.SelectAll();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro na busca: {ex.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

        private async void BtnBuscarCupom_Click(object sender, RoutedEventArgs e)
        {
            string cupom = txtBuscaCupomTroca.Text.Trim();
            if (string.IsNullOrEmpty(cupom)) return;

            System.Collections.Generic.List<ItemVendaTroca> itens;

            using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(DatabaseConfig.ConnectionString))
            {
                itens = conn.Query<ItemVendaTroca>(@"
                    SELECT v.id as id_venda, p.id as id_produto, p.codigo_barras, p.nome, iv.quantidade as quantidade_comprada, iv.preco_unitario
                    FROM tb_vendas v
                    JOIN tb_itens_venda iv ON v.id = iv.id_venda
                    JOIN tb_produtos p ON iv.id_produto = p.id
                    WHERE v.id_cupom = @Cupom", new { Cupom = cupom }).ToList();
            }

            if (itens.Count == 0)
            {
                try
                {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var resp = await client.GetStringAsync($"https://tpace-api.whyguiih.workers.dev/api/app/cupom?codigo={Uri.EscapeDataString(cupom)}");
                        var opcoes = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        opcoes.Converters.Add(new DecimalConverter());
                        var itensNuvem = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<ItemVendaTroca>>(resp, opcoes);
                        if (itensNuvem != null && itensNuvem.Count > 0)
                        {
                            itens = itensNuvem;
                        }
                    }
                }
                catch { }
            }

            if (itens.Count == 0)
            {
                MessageBox.Show("Cupom não encontrado ou não possui itens registrados.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                listaItensCupom.ItemsSource = null;
            }
            else
            {
                listaItensCupom.ItemsSource = itens;
                listaItensCupom.Focus();
                listaItensCupom.SelectedIndex = 0;
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
                        foreach (ItemVendaTroca selecionado in listaItensCupom.SelectedItems)
                        {
                            decimal valorItem = qtdTroca * selecionado.preco_unitario;
                            valorTotalDevolvido += valorItem;

                            conn.Execute(@"INSERT INTO tb_trocas (tipo_troca, data_troca, id_vendedor, produto_retornado, quantidade, sincronizado)
                                           VALUES (@Tipo, @DataHora, @Vendedor, @Codigo, @Qtd, 0)",
                                         new { Tipo = tipoStr, DataHora = DateTime.Now, Vendedor = Session.CurrentUserId, Codigo = selecionado.codigo_barras, Qtd = qtdTroca }, tx);

                            conn.Execute(@"UPDATE tb_produtos 
                                           SET quantidade = COALESCE(quantidade, 0) + @Qtd 
                                           WHERE id = @IdProduto
                                              OR codigo_barras = @Codigo
                                              OR (codigo_geral IS NOT NULL AND codigo_geral != 0 AND codigo_geral = (SELECT codigo_geral FROM tb_produtos WHERE id = @IdProduto OR codigo_barras = @Codigo LIMIT 1))",
                                         new { Qtd = qtdTroca, IdProduto = selecionado.id_produto, Codigo = selecionado.codigo_barras }, tx);
                        }

                        tx.Commit();
                    }
                }
                Task.Run(async () => await SincronizacaoService.SincronizarTrocasPendentesAsync());
                if (tipoStr == "troca")
                {
                    _descontoManualVenda += valorTotalDevolvido;
                    AtualizarTotais();
                    MessageBox.Show($"Troca registrada com sucesso.\nEstoque atualizado e desconto de R$ {valorTotalDevolvido:N2} aplicado.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Devolução registrada com sucesso.\nEstoque atualizado. Valor a estornar: R$ {valorTotalDevolvido:N2}", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                FecharTelaTroca();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao processar troca: {ex.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
            txtBusca.Focus();
        }

        private void txtBuscaNome_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string termo = txtBuscaNome.Text.Trim();

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

        private void txtBuscaNome_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                FecharTelaBusca();
                e.Handled = true;
            }
            else if (e.Key == Key.Down && listaResultadosBusca.Items.Count > 0)
            {
                e.Handled = true;

                listaResultadosBusca.Focus();
                listaResultadosBusca.SelectedIndex = 0;

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

        private void listaResultadosBusca_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SelecionarProdutoBusca();
            }
            else if (e.Key == Key.Escape)
            {
                txtBuscaNome.Focus();
            }
        }

        private void listaResultadosBusca_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelecionarProdutoBusca();
        }

        private void SelecionarProdutoBusca()
        {
            if (listaResultadosBusca.SelectedItem is Produto produtoSelecionado)
            {
                FecharTelaBusca();

                txtBusca.Text += produtoSelecionado.codigo_barras;

                txtBusca.CaretIndex = txtBusca.Text.Length;
                txtBusca.Focus();
            }
        }

        private void AbrirTelaDesconto()
        {
            if (Carrinho.Count == 0)
            {
                MessageBox.Show("Adicione itens ao cupom antes de aplicar um desconto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            OverlayDesconto.Visibility = Visibility.Visible;

            txtValorDesconto.Text = _descontoManualVenda > 0 ? _descontoManualVenda.ToString("N2") : "";
            txtValorDesconto.SelectAll();
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

            if (string.IsNullOrEmpty(entrada))
            {
                _descontoManualVenda = 0m;
                AtualizarTotais();
                FecharTelaDesconto();
                return;
            }

            bool isPorcentagem = entrada.Contains("%");

            entrada = entrada.Replace("R$", "").Replace("%", "").Replace(" ", "").Replace(",", ".");

            if (decimal.TryParse(entrada, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valorDigitado))
            {
                decimal subtotalBruto = Carrinho.Sum(i => i.PrecoUnitario * i.Quantidade);
                decimal descontosDePromocoes = Carrinho.Sum(i => i.DescontoUnitario * i.Quantidade);
                decimal totalAtualVenda = subtotalBruto - descontosDePromocoes;

                decimal valorDescontoFinal = 0m;

                if (isPorcentagem)
                {
                    valorDescontoFinal = totalAtualVenda * (valorDigitado / 100m);
                }
                else
                {
                    valorDescontoFinal = valorDigitado;
                }

                if (valorDescontoFinal > totalAtualVenda || valorDescontoFinal < 0)
                {
                    MessageBox.Show("O desconto não pode ser negativo ou ultrapassar o valor total da venda!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtValorDesconto.SelectAll();
                    return;
                }

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

        private decimal CalcularTotalAPagar()
        {
            decimal subtotalBruto = Carrinho.Sum(i => i.PrecoUnitario * i.Quantidade);
            decimal descontosDePromocoes = Carrinho.Sum(i => i.DescontoUnitario * i.Quantidade);
            decimal total = subtotalBruto - (descontosDePromocoes + _descontoManualVenda);
            return total < 0 ? 0m : total;
        }

        private void AbrirTelaPagamento()
        {
            if (Carrinho.Count == 0)
            {
                MessageBox.Show("Adicione itens ao cupom antes de finalizar a venda.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            PagamentosParciais.Clear();
            decimal totalAPagar = CalcularTotalAPagar();

            txtPagamentoTotal.Text = $"R$ {totalAPagar:N2}";
            AtualizarEstadoPagamento(preencherRestante: true);

            OverlayPagamento.Visibility = Visibility.Visible;
            cmbMetodoPagamento.SelectedIndex = 0;
            txtValorRecebido.SelectAll();
            txtValorRecebido.Focus();
        }

        private void AtualizarEstadoPagamento(bool preencherRestante)
        {
            decimal totalAPagar = CalcularTotalAPagar();
            decimal totalJaLancado = PagamentosParciais.Sum(p => p.Valor);
            decimal restante = Math.Max(0m, totalAPagar - totalJaLancado);
            decimal trocoAcumulado = PagamentosParciais.Sum(p => p.Troco);

            txtPagamentoTotal.Text = $"R$ {totalAPagar:N2}";
            txtPagamentoRestante.Text = $"R$ {restante:N2}";
            txtSemPagamentos.Visibility = PagamentosParciais.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (preencherRestante)
            {
                txtValorRecebido.Text = restante.ToString("N2");
            }

            AtualizarTrocoPreview();
        }

        private void AtualizarTrocoPreview()
        {
            decimal totalAPagar = CalcularTotalAPagar();
            decimal totalJaLancado = PagamentosParciais.Sum(p => p.Valor);
            decimal restante = Math.Max(0m, totalAPagar - totalJaLancado);
            decimal trocoAcumulado = PagamentosParciais.Sum(p => p.Troco);

            string entrada = txtValorRecebido.Text.Trim().Replace("R$", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
            if (decimal.TryParse(entrada, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valorDigitado))
            {
                decimal trocoAtual = restante > 0 && valorDigitado > restante ? valorDigitado - restante : 0m;
                txtTroco.Text = $"R$ {(trocoAcumulado + trocoAtual):N2}";
            }
            else
            {
                txtTroco.Text = $"R$ {trocoAcumulado:N2}";
            }
        }

        private void FecharTelaPagamento()
        {
            OverlayPagamento.Visibility = Visibility.Collapsed;
            txtBusca.Focus();
        }

        private void txtValorRecebido_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (txtTroco == null) return;
            AtualizarTrocoPreview();
        }

        private void BtnAdicionarPagamento_Click(object sender, RoutedEventArgs e)
        {
            AdicionarParcelaPagamento();
        }

        private void BtnRemoverPagamento_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is PagamentoParcial item)
            {
                PagamentosParciais.Remove(item);
                AtualizarEstadoPagamento(preencherRestante: true);
                txtValorRecebido.SelectAll();
                txtValorRecebido.Focus();
            }
        }

        private void BtnConfirmarPagamento_Click(object sender, RoutedEventArgs e)
        {
            ProcessarVenda();
        }

        private bool AdicionarParcelaPagamento()
        {
            decimal totalAPagar = CalcularTotalAPagar();
            decimal totalJaLancado = PagamentosParciais.Sum(p => p.Valor);
            decimal restante = Math.Max(0m, totalAPagar - totalJaLancado);

            if (restante <= 0m)
            {
                return true;
            }

            string entrada = txtValorRecebido.Text.Trim().Replace("R$", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
            if (!decimal.TryParse(entrada, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valorDigitado) || valorDigitado <= 0m)
            {
                MessageBox.Show("Informe um valor válido maior que zero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtValorRecebido.SelectAll();
                txtValorRecebido.Focus();
                return false;
            }

            string metodoSelecionado = ((System.Windows.Controls.ComboBoxItem)cmbMetodoPagamento.SelectedItem)?.Content?.ToString() ?? "dinheiro";
            decimal valorAbatido = valorDigitado > restante ? restante : valorDigitado;
            decimal trocoGerado = valorDigitado > restante ? valorDigitado - restante : 0m;

            PagamentosParciais.Add(new PagamentoParcial
            {
                Metodo = metodoSelecionado,
                Valor = valorAbatido,
                ValorRecebido = valorDigitado,
                Troco = trocoGerado
            });

            AtualizarEstadoPagamento(preencherRestante: true);
            txtValorRecebido.SelectAll();
            txtValorRecebido.Focus();
            return true;
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
            else if (e.Key == Key.Up)
            {
                if (cmbMetodoPagamento.SelectedIndex > 0)
                    cmbMetodoPagamento.SelectedIndex--;
                else
                    cmbMetodoPagamento.SelectedIndex = cmbMetodoPagamento.Items.Count - 1;

                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
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
            decimal totalAPagar = CalcularTotalAPagar();

            decimal totalJaLancado = PagamentosParciais.Sum(p => p.Valor);
            decimal restante = Math.Max(0m, totalAPagar - totalJaLancado);

            if (restante > 0m)
            {
                string entrada = txtValorRecebido.Text.Trim().Replace("R$", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
                decimal.TryParse(entrada, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valorDigitado);

                if (valorDigitado <= 0m)
                {
                    MessageBox.Show($"Ainda falta pagar R$ {restante:N2}. Digite o valor recebido para concluir ou dividir o pagamento.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtValorRecebido.SelectAll();
                    txtValorRecebido.Focus();
                    return;
                }

                if (valorDigitado < restante)
                {
                    // Adiciona automaticamente como pagamento parcial (dividido) e aguarda o restante
                    AdicionarParcelaPagamento();
                    return;
                }

                // Valor digitado cobre o restante: adiciona a parcela final e conclui a venda
                if (!AdicionarParcelaPagamento())
                {
                    return;
                }
            }

            if (PagamentosParciais.Count == 0)
            {
                string metodoPadrao = ((System.Windows.Controls.ComboBoxItem)cmbMetodoPagamento.SelectedItem)?.Content?.ToString() ?? "dinheiro";
                PagamentosParciais.Add(new PagamentoParcial
                {
                    Metodo = metodoPadrao,
                    Valor = totalAPagar,
                    ValorRecebido = totalAPagar,
                    Troco = 0m
                });
            }

            decimal valorTotalRecebido = PagamentosParciais.Sum(p => p.ValorRecebido);
            decimal trocoFinal = PagamentosParciais.Sum(p => p.Troco);
            var listaPagamentosFinal = PagamentosParciais.ToList();

            string codigoUnicoCupom = DateTime.Now.ToString("yyMMddHHmmssff");

            try
            {
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
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
                                IdCupom = codigoUnicoCupom
                            }, transaction);

                            foreach (var item in Carrinho)
                            {
                                var sqlItem = @"INSERT INTO tb_itens_venda (id_venda, id_produto, quantidade, preco_unitario, subtotal)
                                                VALUES (@IdVenda, @IdProduto, @Quantidade, @PrecoUnitario, @Subtotal);
                                                
                                                UPDATE tb_produtos 
                                                SET quantidade = COALESCE(quantidade, 0) - @Quantidade 
                                                WHERE id = @IdProduto
                                                   OR (codigo_geral IS NOT NULL AND codigo_geral != 0 AND codigo_geral = (SELECT codigo_geral FROM tb_produtos WHERE id = @IdProduto));";

                                connection.Execute(sqlItem, new
                                {
                                    IdVenda = idVenda,
                                    IdProduto = item.IdProduto,
                                    Quantidade = item.Quantidade,
                                    PrecoUnitario = item.PrecoUnitario,
                                    Subtotal = item.Total
                                }, transaction);
                            }

                            var sqlPagamento = @"INSERT INTO tb_pagamentos (id_venda, metodo, valor)
                                                 VALUES (@IdVenda, @Metodo, @Valor);";

                            foreach (var pag in listaPagamentosFinal)
                            {
                                connection.Execute(sqlPagamento, new
                                {
                                    IdVenda = idVenda,
                                    Metodo = pag.Metodo,
                                    Valor = pag.Valor
                                }, transaction);
                            }

                            transaction.Commit();

                            ImprimirRecibo(idVenda, codigoUnicoCupom, Carrinho.ToList(), subtotalBruto, descontoTotal, totalAPagar, listaPagamentosFinal, valorTotalRecebido, trocoFinal);

                            Task.Run(async () => await SincronizacaoService.SincronizarVendasPendentesAsync());
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                Carrinho.Clear();
                PagamentosParciais.Clear();
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
                MessageBox.Show($"Erro ao concluir venda. Detalhe: {ex.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    conn.Execute("UPDATE tb_sessao_caixa SET data_fechamento = @Data, status = 0, valor_fechamento = @Valor, sincronizado = 0 WHERE id = @Id",
                        new { Data = DateTime.Now, Valor = valorFechamento, Id = Session.CurrentSessaoCaixaId });
                }

                FecharTelaFechamento();
                await SairDoCaixa();
            }
        }

        private async Task SairDoCaixa()
        {
            this.IsEnabled = false;
            txtUltimoNome.Text = "Encerrando Turno...";
            txtUltimoDetalhes.Text = "Aguarde enquanto os dados são salvos na nuvem.";
            txtUltimoPreco.Text = "";

            await SincronizacaoService.SincronizarVendasPendentesAsync();
            await SincronizacaoService.SincronizarSessoesCaixaAsync();
            await SincronizacaoService.SincronizarTrocasPendentesAsync();

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

        private void ImprimirRecibo(long idVenda, string codigoUnicoCupom, System.Collections.Generic.List<ItemCupom> itens, decimal subtotal, decimal desconto, decimal total, System.Collections.Generic.List<PagamentoParcial> pagamentos, decimal valorRecebido, decimal troco)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                var doc = new System.Windows.Documents.FlowDocument();

                doc.PageWidth = 275;
                doc.PagePadding = new System.Windows.Thickness(0, 10, 0, 20);
                doc.FontFamily = new System.Windows.Media.FontFamily("Courier New");
                doc.Foreground = System.Windows.Media.Brushes.Black;

                doc.FontWeight = FontWeights.Bold;

                System.Windows.Media.TextOptions.SetTextFormattingMode(doc, System.Windows.Media.TextFormattingMode.Display);
                System.Windows.Media.TextOptions.SetTextRenderingMode(doc, System.Windows.Media.TextRenderingMode.Aliased);

                int colunas = 36;

                try
                {
                    var uri = new Uri("pack://application:,,,/nota.png", UriKind.Absolute);
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage(uri);
                    var img = new System.Windows.Controls.Image
                    {
                        Source = bitmap,
                        Width = 140,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new System.Windows.Thickness(0, 0, 0, 10)
                    };
                    System.Windows.Media.RenderOptions.SetBitmapScalingMode(img, System.Windows.Media.BitmapScalingMode.HighQuality);
                    doc.Blocks.Add(new System.Windows.Documents.BlockUIContainer(img));
                }
                catch { }

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

                var sb = new System.Text.StringBuilder();

                sb.AppendLine("DESCRIÇÃO                   TOTAL(R$)");

                foreach (var item in itens)
                {
                    sb.AppendLine();
                    string desc = item.Descricao.Length > colunas ? item.Descricao.Substring(0, colunas) : item.Descricao;
                    sb.AppendLine(desc);

                    string qtd = item.Quantidade.ToString("0.###");
                    string un = "UN";
                    string vlUn = item.PrecoUnitario.ToString("N2");
                    string tot = item.Total.ToString("N2");

                    string linhaValores = $"{qtd} {un} X {vlUn}";
                    sb.AppendLine(AlinharLinha(linhaValores, tot, colunas));
                }
                sb.AppendLine(new string('-', colunas));

                decimal qtdeTotalItens = itens.Sum(i => i.Quantidade);
                sb.AppendLine(AlinharLinha("Qtde. Total de Itens", qtdeTotalItens.ToString("0.###"), colunas));
                sb.AppendLine(AlinharLinha("Subtotal R$", subtotal.ToString("N2"), colunas));
                if (desconto > 0)
                    sb.AppendLine(AlinharLinha("Descontos R$", desconto.ToString("N2"), colunas));

                doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(sb.ToString())) { FontSize = 11, Margin = new Thickness(0), LineHeight = 16 });

                var pTotal = new System.Windows.Documents.Paragraph();
                pTotal.Margin = new Thickness(0, 2, 0, 10);
                pTotal.TextAlignment = TextAlignment.Left;

                pTotal.Inlines.Add(new System.Windows.Documents.Run("VALOR TOTAL  R$ ") { FontSize = 11 });
                pTotal.Inlines.Add(new System.Windows.Documents.Run(total.ToString("N2") + "\n") { FontWeight = FontWeights.Black, FontSize = 16 });
                doc.Blocks.Add(pTotal);

                var sbPagamento = new System.Text.StringBuilder();
                sbPagamento.AppendLine(AlinharLinha("FORMA PAGAMENTO", "VALOR PAGO R$", colunas));
                foreach (var pag in pagamentos)
                {
                    sbPagamento.AppendLine(AlinharLinha(pag.Metodo.ToUpper(), pag.ValorRecebido.ToString("N2"), colunas));
                }
                sbPagamento.AppendLine(AlinharLinha("Troco R$", troco.ToString("N2"), colunas));
                sbPagamento.AppendLine(new string('-', colunas));
                doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(sbPagamento.ToString())) { FontSize = 11, Margin = new Thickness(0), LineHeight = 16 });

                try
                {
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

        private string AlinharLinha(string esquerda, string direita, int totalColunas)
        {
            if (esquerda.Length + direita.Length >= totalColunas)
                esquerda = esquerda.Substring(0, totalColunas - direita.Length - 1);

            return esquerda + direita.PadLeft(totalColunas - esquerda.Length);
        }

        private System.Windows.Controls.Image GerarCodigoBarrasITF(string texto)
        {
            if (texto.Length % 2 != 0) texto = "0" + texto;

            string[] padroes = { "NNWWN", "WNNNW", "NWNNW", "WWNNN", "NNWNW", "WNWNN", "NWWNN", "NNNWW", "WNNWN", "NWNWN" };

            int barraMagra = 2;
            int barraLarga = 5;
            int altura = 70;

            int larguraTotal = (barraMagra * 4) + (barraLarga + barraMagra * 2);
            foreach (char c in texto)
            {
                string p = padroes[c - '0'];
                foreach (char peso in p) larguraTotal += peso == 'W' ? barraLarga : barraMagra;
            }

            var visual = new System.Windows.Media.DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(0, 0, larguraTotal, altura));

                int x = 0;

                context.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(x, 0, barraMagra, altura)); x += barraMagra * 2;
                context.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(x, 0, barraMagra, altura)); x += barraMagra * 2;

                for (int i = 0; i < texto.Length; i += 2)
                {
                    string p1 = padroes[texto[i] - '0'];
                    string p2 = padroes[texto[i + 1] - '0'];

                    for (int j = 0; j < 5; j++)
                    {
                        int wBar = p1[j] == 'W' ? barraLarga : barraMagra;
                        context.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(x, 0, wBar, altura));
                        x += wBar;

                        int wSpc = p2[j] == 'W' ? barraLarga : barraMagra;
                        x += wSpc;
                    }
                }

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
            public int id_produto { get; set; }
            public string codigo_barras { get; set; }
            public string nome { get; set; }
            public decimal quantidade_comprada { get; set; }
            public decimal preco_unitario { get; set; }
        }

        public class PagamentoParcial
        {
            public string Metodo { get; set; } = string.Empty;
            public decimal Valor { get; set; }
            public decimal ValorRecebido { get; set; }
            public decimal Troco { get; set; }
            public string MetodoFormatado => Metodo.ToUpper();
        }

        private void Rodape_F2_Click(object sender, MouseButtonEventArgs e) => AbrirTelaPagamento();
        private void Rodape_F3_Click(object sender, MouseButtonEventArgs e) => AbrirTelaCancelamento();
        private void Rodape_F4_Click(object sender, MouseButtonEventArgs e) => AbrirTelaBusca();
        private void Rodape_F5_Click(object sender, MouseButtonEventArgs e) => AbrirTelaDesconto();
        private void Rodape_F6_Click(object sender, MouseButtonEventArgs e) => AbrirTelaTroca();
        private void Rodape_F9_Click(object sender, MouseButtonEventArgs e) => AbrirTelaFechamento();

        public class ItemCupom : INotifyPropertyChanged
        {
            public int IdProduto { get; set; }

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