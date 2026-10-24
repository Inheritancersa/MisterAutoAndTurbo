# Mister Auto and Turbo — Web (Staff)

ASP.NET Core MVC **staff / workshop** web app for Mister Auto and Turbo.

> **Security:** Real credentials are **not** included in this zip.  
> Use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or a local `appsettings.Development.json` (gitignored) before running.

## Stack

- .NET 8 / ASP.NET Core MVC  
- Entity Framework Core + Npgsql (PostgreSQL / Supabase)  
- Email via SMTP (e.g. Gmail)

## Upload to GitHub

1. Create a **new public repository** on GitHub (empty, no README if you prefer).  
2. Unzip this file.  
3. In the unzipped folder:

```bash
git init
git add .
git commit -m "Initial commit: Mister Auto and Turbo Web"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO.git
git push -u origin main
```

Or use GitHub’s **“uploading an existing file”** UI and drag the unzipped folder contents.

## Local run

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Supabase" "YOUR_CONNECTION_STRING"
dotnet user-secrets set "ConnectionStrings:ServiceRoleKey" "YOUR_KEY"
dotnet user-secrets set "Email:SmtpUser" "you@gmail.com"
dotnet user-secrets set "Email:SmtpPass" "your-app-password"
dotnet restore
dotnet run
```

Do not commit production passwords, service-role keys, or SMTP app passwords.
