using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using TemplateApi.Application.Common.Interfaces;

namespace TemplateApi.Infrastructure.Services;

/// <summary>
/// CONCEITO: Email Service com MailKit
/// 
/// MailKit é uma biblioteca moderna e robusta para envio de emails.
/// Suporta SMTP, autenticação, TLS/SSL, anexos, HTML, etc.
/// 
/// CONFIGURAÇÃO:
/// - SMTP Server (ex: Gmail, SendGrid, Amazon SES)
/// - Port (587 para TLS, 465 para SSL, 25 para não seguro)
/// - Credenciais
/// 
/// IMPORTANTE:
/// ⚠️ NUNCA envie email sincronamente no handler HTTP!
/// - É lento (pode demorar segundos)
/// - Pode falhar e travar a request
/// - Use background job (Hangfire, Quartz) ou queue (RabbitMQ, Azure Queue)
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _smtpServer = configuration["Email:SmtpServer"] ?? "localhost";
        _smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        _smtpUsername = configuration["Email:SmtpUsername"] ?? "";
        _smtpPassword = configuration["Email:SmtpPassword"] ?? "";
        _fromEmail = configuration["Email:FromEmail"] ?? "noreply@templateapi.com";
        _fromName = configuration["Email:FromName"] ?? "Template API";
    }

    public async Task SendEmailAsync(
        string to, 
        string subject, 
        string body, 
        bool isHtml = true, 
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        if (isHtml)
            bodyBuilder.HtmlBody = body;
        else
            bodyBuilder.TextBody = body;

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        
        try
        {
            // Conectar ao servidor SMTP
            await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls, cancellationToken);
            
            // Autenticar
            if (!string.IsNullOrEmpty(_smtpUsername))
            {
                await client.AuthenticateAsync(_smtpUsername, _smtpPassword, cancellationToken);
            }
            
            // Enviar
            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }

    public async Task SendTemplatedEmailAsync(
        string to, 
        string templateName, 
        object data, 
        CancellationToken cancellationToken = default)
    {
        // Aqui você carregaria um template de email (HTML)
        // Poderia usar Razor, Handlebars, ou templates simples
        
        string htmlBody = templateName switch
        {
            "Welcome" => GenerateWelcomeEmail(data),
            "PasswordReset" => GeneratePasswordResetEmail(data),
            _ => throw new ArgumentException($"Template {templateName} não encontrado")
        };

        var subject = templateName switch
        {
            "Welcome" => "Bem-vindo ao Template API!",
            "PasswordReset" => "Redefinir sua senha",
            _ => "Template API"
        };

        await SendEmailAsync(to, subject, htmlBody, isHtml: true, cancellationToken);
    }

    public async Task SendConfirmationEmailAsync(
        string to, 
        string confirmationLink, 
        CancellationToken cancellationToken = default)
    {
        var subject = "Confirme seu email";
        var body = $@"
            <html>
            <body>
                <h2>Bem-vindo ao Template API!</h2>
                <p>Por favor, confirme seu email clicando no link abaixo:</p>
                <a href=""{confirmationLink}"">Confirmar Email</a>
                <p>Se você não se registrou, ignore este email.</p>
            </body>
            </html>";

        await SendEmailAsync(to, subject, body, isHtml: true, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(
        string to, 
        string resetLink, 
        CancellationToken cancellationToken = default)
    {
        var subject = "Redefinir sua senha";
        var body = $@"
            <html>
            <body>
                <h2>Redefinir Senha</h2>
                <p>Você solicitou redefinir sua senha. Clique no link abaixo:</p>
                <a href=""{resetLink}"">Redefinir Senha</a>
                <p>Este link expira em 1 hora.</p>
                <p>Se você não solicitou, ignore este email.</p>
            </body>
            </html>";

        await SendEmailAsync(to, subject, body, isHtml: true, cancellationToken);
    }

    private string GenerateWelcomeEmail(object data)
    {
        return @"
            <html>
            <body>
                <h1>Bem-vindo!</h1>
                <p>Obrigado por se registrar no Template API.</p>
            </body>
            </html>";
    }

    private string GeneratePasswordResetEmail(object data)
    {
        return @"
            <html>
            <body>
                <h1>Redefinir Senha</h1>
                <p>Clique no link para redefinir sua senha.</p>
            </body>
            </html>";
    }
}
