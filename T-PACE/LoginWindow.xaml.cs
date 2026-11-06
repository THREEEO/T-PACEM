using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Dapper;
using Microsoft.Data.Sqlite;

namespace T_PACE
{
    public partial class LoginWindow : Window
    {
        private static readonly string apiLoginUrl = "https://tpace-api.whyguiih.workers.dev/api/web/login";
        private const string VersaoAtualApp = "1.3.9";
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

            VerificarAtualizacao();
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
                    var response = await client.GetAsync("https://tpace-api.whyguiih.workers.dev/api/app/versao");

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using (var json = JsonDocument.Parse(jsonString))
                        {
                            string versaoNuvem = json.RootElement.GetProperty("versao").GetString()?.Trim().TrimStart('v', 'V');
                            string linkDownload = json.RootElement.GetProperty("link_download").GetString()?.Trim();

                            if (!string.IsNullOrEmpty(versaoNuvem) && !string.IsNullOrEmpty(linkDownload))
                            {
                                Version vNuvem = new Version(versaoNuvem);
                                Version vLocal = new Version(VersaoAtualApp.Trim().TrimStart('v', 'V'));

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
            }
        }

        private static bool EhArquivoZipValido(byte[] bytes)
        {
            return bytes != null && bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B;
        }

        private static async Task<byte[]> BaixarPacoteAtualizacaoAsync(string link)
        {
            using (var handler = new HttpClientHandler { AllowAutoRedirect = true, UseCookies = true })
            using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) })
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) T-PACE-Updater/1.0");

                var bytes = await client.GetByteArrayAsync(link);
                if (EhArquivoZipValido(bytes))
                {
                    return bytes;
                }

                // Caso o Google Drive retorne a página HTML de aviso de verificação de vírus para arquivos grandes/executáveis
                string html = Encoding.UTF8.GetString(bytes);

                string actionUrl = "https://drive.usercontent.google.com/download";
                var formMatch = Regex.Match(html, @"<form[^>]*id=""download-form""[^>]*action=""([^""]+)""", RegexOptions.IgnoreCase);
                if (!formMatch.Success)
                {
                    formMatch = Regex.Match(html, @"<form[^>]*action=""(https://drive\.usercontent\.google\.com/download[^""]*)""", RegexOptions.IgnoreCase);
                }
                if (formMatch.Success)
                {
                    actionUrl = System.Net.WebUtility.HtmlDecode(formMatch.Groups[1].Value);
                }

                var inputMatches = Regex.Matches(html, @"<input[^>]*type=""hidden""[^>]*name=""([^""]+)""[^>]*value=""([^""]*)""", RegexOptions.IgnoreCase);
                var queryParams = new List<string>();
                foreach (Match m in inputMatches)
                {
                    string name = Uri.EscapeDataString(m.Groups[1].Value);
                    string val = Uri.EscapeDataString(System.Net.WebUtility.HtmlDecode(m.Groups[2].Value));
                    queryParams.Add($"{name}={val}");
                }

                string urlConfirmada;
                if (queryParams.Count > 0)
                {
                    string separador = actionUrl.Contains("?") ? "&" : "?";
                    urlConfirmada = actionUrl + separador + string.Join("&", queryParams);
                }
                else
                {
                    var idMatch = Regex.Match(link, @"(?:[?&]id=|/file/d/)([a-zA-Z0-9_-]+)");
                    if (!idMatch.Success)
                    {
                        throw new InvalidOperationException("O link de download não retornou um arquivo ZIP válido.");
                    }
                    string fileId = idMatch.Groups[1].Value;
                    urlConfirmada = $"https://drive.usercontent.google.com/download?id={fileId}&export=download&confirm=t";
                }

                bytes = await client.GetByteArrayAsync(urlConfirmada);
                if (!EhArquivoZipValido(bytes))
                {
                    throw new InvalidOperationException("Não foi possível obter o pacote ZIP de atualização a partir do servidor.");
                }

                return bytes;
            }
        }

        private async void BtnAtualizar_Click(object sender, RoutedEventArgs e)
        {
            string link = btnAtualizar.Tag?.ToString();
            if (string.IsNullOrEmpty(link)) return;

            try
            {
                btnAtualizar.Content = " Baixando atualização... ";
                btnAtualizar.IsEnabled = false;

                string tempPasta = Path.GetTempPath();
                string zipPath = Path.Combine(tempPasta, "tpace_update.zip");
                string extractDir = Path.Combine(tempPasta, "tpace_update_extracted");
                string batPath = Path.Combine(tempPasta, "atualizador.bat");

                string appPath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                                 ?? Path.Combine(appPath, "T-PACE.exe");
                int currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;

                var bytes = await BaixarPacoteAtualizacaoAsync(link);
                await File.WriteAllBytesAsync(zipPath, bytes);

                btnAtualizar.Content = " Extraindo arquivos... ";

                if (Directory.Exists(extractDir))
                {
                    Directory.Delete(extractDir, true);
                }
                Directory.CreateDirectory(extractDir);

                ZipFile.ExtractToDirectory(zipPath, extractDir, true);

                // Se o ZIP tiver apenas uma subpasta raiz, usa essa subpasta como origem
                string sourceDir = extractDir;
                var topFiles = Directory.GetFiles(extractDir);
                var topDirs = Directory.GetDirectories(extractDir);
                if (topFiles.Length == 0 && topDirs.Length == 1)
                {
                    sourceDir = topDirs[0];
                }

                // Preserva o banco de dados local existente para não perder vendas/sessões locais
                string dbLocal = Path.Combine(appPath, "tpace_pdv.sqlite");
                string dbNoPacote = Path.Combine(sourceDir, "tpace_pdv.sqlite");
                if (File.Exists(dbLocal) && File.Exists(dbNoPacote))
                {
                    try { File.Delete(dbNoPacote); } catch { }
                }

                btnAtualizar.Content = " Instalando... ";

                string script = $@"@echo off
chcp 65001 > NUL
:waitloop
tasklist /FI ""PID eq {currentPid}"" 2>NUL | find /I ""{currentPid}"" >NUL
if ""%ERRORLEVEL%""==""0"" (
    timeout /t 1 /nobreak > NUL
    goto waitloop
)
timeout /t 1 /nobreak > NUL
xcopy ""{sourceDir}\*"" ""{appPath}\"" /E /H /C /I /Y > NUL
rmdir /S /Q ""{extractDir}"" 2>NUL
del /F /Q ""{zipPath}"" 2>NUL
start """" ""{exePath}""
del ""%~f0""
";
                File.WriteAllText(batPath, script, new UTF8Encoding(false));

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"\"{batPath}\"\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                System.Diagnostics.Process.Start(startInfo);

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível atualizar automaticamente. Detalhe: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                btnAtualizar.Content = " Tentar novamente ";
                btnAtualizar.IsEnabled = true;
            }
        }
    }

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
}