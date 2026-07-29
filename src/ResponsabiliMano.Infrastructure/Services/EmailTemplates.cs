namespace ResponsabiliMano.Infrastructure.Services;

internal static class EmailTemplates
{
    public const string ProjectInviteSubject = "Convite — ResponsabiliMano";

    public static string ProjectInviteBody(string projectName, string inviteLink) => $"""
        <h2>Você foi convidado!</h2>
        <p>Olá! Você foi convidado para participar do projeto "{projectName}" no ResponsabiliMano.</p>
        <p>Clique no link abaixo para visualizar o projeto e aceitar o convite:</p>
        <p><a href="{inviteLink}">{inviteLink}</a></p>
        <p>Este convite expira em 7 dias.</p>
        """;

    public const string PasswordResetSubject = "Recuperação de Senha — ResponsabiliMano";

    public static string PasswordResetBody(string userName, string resetLink) => $"""
        <h2>Recuperação de Senha</h2>
        <p>Olá, {userName}!</p>
        <p>Você solicitou a redefinição de sua senha. Clique no link abaixo para definir uma nova senha:</p>
        <p><a href="{resetLink}">{resetLink}</a></p>
        <p>Este link expira em 1 hora.</p>
        <p>Se você não solicitou esta redefinição, ignore este e-mail.</p>
        """;

    public const string CheckInRequestSubject = "Hora do check-in — ResponsabiliMano";

    public static string CheckInRequestBody(string userName, string projectName, string link) => $"""
        <h2>Hora do check-in!</h2>
        <p>Olá, {userName}. Chegou a hora de registrar seu check-in do projeto "{projectName}".</p>
        <p>Clique no link abaixo para preencher:</p>
        <p><a href="{link}">{link}</a></p>
        """;

    public const string CheckInReminderSubject = "Lembrete de check-in — ResponsabiliMano";

    public static string CheckInReminderBody(string userName, string projectName, string link) => $"""
        <h2>Você ainda não fez seu check-in</h2>
        <p>Olá, {userName}. Este é um lembrete para registrar seu check-in do projeto "{projectName}".</p>
        <p>Clique no link abaixo para preencher:</p>
        <p><a href="{link}">{link}</a></p>
        """;
}
