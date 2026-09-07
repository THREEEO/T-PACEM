using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using Dapper;
using Microsoft.Data.Sqlite;

namespace T_PACE
{
    public partial class LoginWindow : Window
    {
        // URL da rota POST do seu Cloudflare Worker que faz a autenticação
        private static readonly string apiLoginUrl = "https://tpace-api.whyguiih.workers.dev/api/web/login";
        private const string VersaoAtualApp = "1.3.3"; // Controle a versão do seu app compilado aqui!
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
            txtUsuario.TextChanged += (s, e) => txtMensagem.Text = string.Empty;
            txtSenha.PasswordChanged += (s, e) => txtMensagem.Text = string.Empty;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtUsuario.Focus();
            txtUsuario.SelectAll();

            VerificarAtualizacao(); // Roda em segundo plano sem travar a tela
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private async void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text?.Trim() ?? string.Empty;
            string senha = txtSenha.Password ?? string.Empty;
            string entradaTroco = txtFundoTroco.Text.Trim().Replace("R$", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
            decimal.TryParse(entradaTroco, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal fundoTroco);

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(senha))
            {
                txtMensagem.Text = "Preencha o usuário e a senha.";
                return;
            }

            // Desativa o botão enquanto a API do Cloudflare responde
            btnOk.IsEnabled = false;
            txtMensagem.Text = "Autenticando...";

            try
            {
                using (var client = new HttpClient())
                {
                    var jsonRequest = JsonSerializer.Serialize(new { nome = usuario, senha = senha });
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(apiLoginUrl, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var resultado = JsonSerializer.Deserialize<LoginResponse>(responseString, options);

                        if (resultado != null && resultado.sucesso)
                        {
                            using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                            {
                                conn.Open();

                                // 1. NOVO: Sincroniza o usuário que veio da nuvem com o banco local
                                // Assim o banco local sempre reconhece o ID do usuário (evita erro de Foreign Key)
                                conn.Execute(@"
                                    INSERT INTO tb_usuarios (id, nome, nivel_acesso) 
                                    VALUES (@Id, @Nome, @NivelAcesso)
                                    ON CONFLICT(id) DO UPDATE SET 
                                        nome = excluded.nome,
                                        nivel_acesso = excluded.nivel_acesso;",
                                    new
                                    {
                                        Id = resultado.usuario.id,
                                        Nome = resultado.usuario.nome,
                                        NivelAcesso = resultado.usuario.nivel_acesso
                                    });

                                // 2. Registra os dados da sessão
                                Session.CurrentUserId = resultado.usuario.id;
                                Session.CurrentUserName = resultado.usuario.nome;

                                var now = DateTime.Now;
                                long sessaoId = conn.ExecuteScalar<long>(
                                    @"INSERT INTO tb_sessao_caixa (id_caixa, id_usuario, data_abertura, valor_fundo_troco, status)
                                       VALUES (@IdCaixa, @IdUsuario, @DataAbertura, @ValorFundoTroco, @Status);
                                       SELECT last_insert_rowid();",
                                    new { IdCaixa = 1, IdUsuario = Session.CurrentUserId, DataAbertura = now, ValorFundoTroco = fundoTroco, Status = 1 });

                                Session.CurrentSessaoCaixaId = Convert.ToInt32(sessaoId);
                            }

                            DialogResult = true;
                        }
                        else
                        {
                            txtMensagem.Text = "Erro ao processar login remoto.";
                            btnOk.IsEnabled = true;
                        }
                    }
                    else
                    {
                        txtMensagem.Text = "Usuário ou senha incorretos.";
                        btnOk.IsEnabled = true;
                    }
                }
            }
            catch (Exception)
            {
                // NOVO: Se a internet cair, tenta autenticar com os dados gravados no banco local
                if (FazerLoginLocal(usuario, senha))
                {
                    DialogResult = true;
                }
                else
                {
                    txtMensagem.Text = "Sem internet.";
                    btnOk.IsEnabled = true;
                }
            }
        }

        private bool FazerLoginLocal(string usuario, string senha)
        {
            try
            {
                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    // Busca se o usuário já fez login alguma vez na vida enquanto tinha internet
                    var userLocal = conn.QueryFirstOrDefault<UsuarioLogin>(
                        "SELECT id, nome, nivel_acesso FROM tb_usuarios WHERE nome = @Nome AND senha = @Senha",
                        new { Nome = usuario, Senha = senha });

                    if (userLocal != null)
                    {
                        Session.CurrentUserId = userLocal.id;
                        Session.CurrentUserName = userLocal.nome;

                        long sessaoId = conn.ExecuteScalar<long>(
                            @"INSERT INTO tb_sessao_caixa (id_caixa, id_usuario, data_abertura, valor_fundo_troco, status)
                               VALUES (@IdCaixa, @IdUsuario, @DataAbertura, @ValorFundoTroco, @Status);
                               SELECT last_insert_rowid();",
                            new { IdCaixa = 1, IdUsuario = Session.CurrentUserId, DataAbertura = DateTime.Now, ValorFundoTroco = 0m, Status = 1 });

                        Session.CurrentSessaoCaixaId = Convert.ToInt32(sessaoId);
                        return true;
                    }                                       
                }
            }
            catch { }
            return false;
        }
        private async void VerificarAtualizacao()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Faz a requisição de forma segura
                    var response = await client.GetAsync("https://tpace-api.whyguiih.workers.dev/api/app/versao");

                    // Só continua se a API respondeu com sucesso (Ignora os 404 e 500)
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using (var json = JsonDocument.Parse(jsonString))
                        {
                            string versaoNuvem = json.RootElement.GetProperty("versao").GetString();
                            string linkDownload = json.RootElement.GetProperty("link_download").GetString();

                            if (!string.IsNullOrEmpty(versaoNuvem))
                            {
                                // Converte os textos para o motor de versões do Windows
                                Version vNuvem = new Version(versaoNuvem);
                                Version vLocal = new Version(VersaoAtualApp);

                                // MATEMÁTICA: O botão SÓ aparece se a versão da nuvem for ESTRITAMENTE MAIOR
                                if (vNuvem > vLocal)
                                {
                                    btnAtualizar.Visibility = Visibility.Visible;
                                    btnAtualizar.Content = $" Atualização disponível: (v{versaoNuvem}) ";
                                    btnAtualizar.Tag = linkDownload;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Falha silenciosa: Sem internet, Cloudflare fora do ar, etc.
            }
        }

        private async void BtnAtualizar_Click(object sender, RoutedEventArgs e)
        {
            string link = btnAtualizar.Tag?.ToString();
            if (string.IsNullOrEmpty(link)) return;

            try
            {
                // Muda o visual do botão para dar feedback ao usuário
                btnAtualizar.Content = " Baixando atualização... ";
                btnAtualizar.IsEnabled = false;

                // 1. Caminhos ocultos para o download
                string tempPasta = System.IO.Path.GetTempPath();
                string zipPath = System.IO.Path.Combine(tempPasta, "tpace_update.zip");
                string batPath = System.IO.Path.Combine(tempPasta, "atualizador.bat");

                string appPath = AppDomain.CurrentDomain.BaseDirectory;
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

                // 2. Baixa o arquivo ZIP silenciosamente
                using (var client = new HttpClient())
                {
                    var bytes = await client.GetByteArrayAsync(link);
                    await System.IO.File.WriteAllBytesAsync(zipPath, bytes);
                }

                btnAtualizar.Content = " Instalando... ";

                // 3. Cria o script "fantasma" que vai sobrescrever os arquivos
                string script = $@"@echo off
timeout /t 2 /nobreak > NUL
powershell.exe -windowstyle hidden -Command ""Expand-Archive -Path '{zipPath}' -DestinationPath '{appPath}' -Force""
del ""{zipPath}""
start """" ""{exePath}""
del ""%~f0""
";
                System.IO.File.WriteAllText(batPath, script);

                // 4. Inicia o script de forma oculta e fecha o T-PACE atual!
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = batPath,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                System.Diagnostics.Process.Start(startInfo);

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível atualizar automaticamente. Detalhe: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                btnAtualizar.Content = " Erro. ";
                btnAtualizar.IsEnabled = true;
            }
        }
    } // <-- FIM DA CLASSE LoginWindow

    // As classes abaixo ficam FORA da LoginWindow, mas DENTRO do namespace T_PACE
    public class LoginResponse
    {
        public bool sucesso { get; set; }
        public UsuarioLogin usuario { get; set; } = new UsuarioLogin();
    }

    public class UsuarioLogin
    {
        public int id { get; set; }
        public string nome { get; set; } = string.Empty;
        public int nivel_acesso { get; set; }
    }

    public static class Session
    {
        public static int CurrentUserId { get; set; } = 0;
        public static string CurrentUserName { get; set; } = string.Empty;
        public static int CurrentSessaoCaixaId { get; set; } = 0;
    }
} // <-- FIM DO NAMESPACE T_PACE