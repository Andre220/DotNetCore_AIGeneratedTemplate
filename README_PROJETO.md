# 🚀 Template API - .NET 9 Reference Project

Um projeto de referência **COMPLETO** que demonstra **TODOS** os recursos modernos do ASP.NET Core 9.0, com exemplos práticos e documentação educacional.

## 📚 Conteúdo

- [Recursos Implementados](#-recursos-implementados)
- [Arquitetura](#-arquitetura)
- [Pré-requisitos](#-pré-requisitos)
- [Configuração](#️-configuração)
- [Executando o Projeto](#-executando-o-projeto)
- [Testando a API](#-testando-a-api)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Conceitos Demonstrados](#-conceitos-demonstrados)
- [Recursos do .NET 9](#-recursos-do-net-9)

## ✨ Recursos Implementados

### Arquitetura & Padrões
- ✅ **Clean Architecture** + **DDD** (Domain-Driven Design)
- ✅ **Vertical Slice Architecture** (features organizadas por funcionalidade)
- ✅ **CQRS Pattern** (separação entre Commands e Queries)
- ✅ **Repository Pattern** + **Unit of Work**
- ✅ **Result Pattern** (tratamento de erros sem exceptions)

### ASP.NET Core 9.0
- ✅ **JWT Authentication** (Bearer Token)
- ✅ **Rate Limiting** (.NET 7+) - Proteção contra DDoS
- ✅ **Output Caching** (.NET 7+) - Cache de respostas HTTP
- ✅ **Response Compression** (Gzip/Brotli)
- ✅ **API Versioning** (v1, v2, etc)
- ✅ **CORS** (Cross-Origin Resource Sharing)
- ✅ **Health Checks** + Dashboard UI

### Logging & Observability
- ✅ **Serilog** (Structured Logging)
- ✅ **OpenTelemetry** (Tracing, Metrics)
- ✅ **Request/Response Logging**

### Validação & Documentação
- ✅ **FluentValidation** (validação robusta)
- ✅ **OpenAPI/Swagger** (documentação interativa)
- ✅ **XML Comments** (documentação de código)

### Infraestrutura
- ✅ **PostgreSQL** + **Entity Framework Core**
- ✅ **Redis** (Cache Distribuído)
- ✅ **MailKit** (Envio de emails)
- ✅ **BCrypt** (Hash de senhas)

### Recursos Avançados (TODO)
- ⏳ **SignalR** (Real-time communication)
- ⏳ **gRPC** (High-performance RPC)
- ⏳ **Background Services** (IHostedService)
- ⏳ **Localization** (i18n)
- ⏳ **MediatR** (CQRS com pipeline)

## 🏗️ Arquitetura

O projeto segue **Clean Architecture** com camadas bem definidas:

```
┌─────────────────────────────────────────────┐
│              API Layer                      │
│  Controllers, Middleware, Configuration     │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│         Application Layer                   │
│  Use Cases, DTOs, Validators                │
│  (Vertical Slices: Features/Auth/Login/)    │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│            Domain Layer                     │
│  Entities, Value Objects, Interfaces        │
│  Business Rules, Domain Events              │
└─────────────────────────────────────────────┘
                    ↑
┌─────────────────────────────────────────────┐
│        Infrastructure Layer                 │
│  EF Core, Repositories, External Services   │
│  Redis, Email, JWT, etc                     │
└─────────────────────────────────────────────┘
```

### Vertical Slice Architecture

Cada feature está organizada verticalmente:

```
Features/
  Auth/
    Login/
      LoginCommand.cs
      LoginCommandHandler.cs
      LoginCommandValidator.cs
    Register/
      RegisterUserCommand.cs
      RegisterUserCommandHandler.cs
      RegisterUserCommandValidator.cs
  Users/
    GetUserById/
      GetUserByIdQuery.cs
      GetUserByIdQueryHandler.cs
```

## 📋 Pré-requisitos

- **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **PostgreSQL** - [Download](https://www.postgresql.org/download/)
- **Redis** (opcional) - [Download](https://redis.io/download)
- **Visual Studio 2022** ou **VS Code**

### Verificar instalação:

```bash
dotnet --version  # Deve mostrar 9.0.x
```

## ⚙️ Configuração

### 1. Clone o repositório

```bash
git clone <seu-repositorio>
cd TemplateApi
```

### 2. Configure o PostgreSQL

Crie o banco de dados:

```sql
CREATE DATABASE templateapi_dev;
```

### 3. Configure appsettings

Edite `src/TemplateApi.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=templateapi_dev;Username=postgres;Password=SUA_SENHA",
    "Redis": "localhost:6379,abortConnect=false"
  }
}
```

### 4. Execute as migrations

```bash
cd src/TemplateApi.Api
dotnet ef database update --project ../TemplateApi.Infrastructure
```

Se o comando acima não funcionar (migrations não criadas ainda), crie:

```bash
dotnet ef migrations add InitialCreate --project ../TemplateApi.Infrastructure --startup-project .
dotnet ef database update --project ../TemplateApi.Infrastructure --startup-project .
```

### 5. (Opcional) Configure Redis

Se não tiver Redis, a API usará cache em memória automaticamente.

Para instalar Redis no Windows:
- Use WSL2 ou Docker: `docker run -d -p 6379:6379 redis`

## 🚀 Executando o Projeto

### Modo Development

```bash
cd src/TemplateApi.Api
dotnet run
```

Ou use Visual Studio/Rider:
- F5 para debug
- Ctrl+F5 para executar sem debug

### Acessar a API

- **Swagger UI**: https://localhost:5001
- **Health Checks**: https://localhost:5001/health
- **Health UI**: https://localhost:5001/health-ui

## 🧪 Testando a API

### 1. Registrar um usuário

```bash
curl -X POST https://localhost:5001/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "João Silva",
    "email": "joao@example.com",
    "password": "Senha@123",
    "confirmPassword": "Senha@123"
  }'
```

**Resposta:**
```json
{
  "userId": 1,
  "email": "joao@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "message": "Usuário registrado com sucesso!"
}
```

### 2. Fazer Login

```bash
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "joao@example.com",
    "password": "Senha@123"
  }'
```

### 3. Acessar endpoints autenticados

Copie o token JWT e use no header:

```bash
curl -X GET https://localhost:5001/api/v1/auth/me \
  -H "Authorization: Bearer SEU_TOKEN_AQUI"
```

### 4. Usar Swagger

1. Acesse https://localhost:5001
2. Clique em "Authorize" (cadeado)
3. Cole o token JWT
4. Teste todos os endpoints interativamente!

## 📁 Estrutura do Projeto

```
TemplateApi/
├── src/
│   ├── TemplateApi.Api/           # Camada de apresentação
│   │   ├── Controllers/           # API Controllers
│   │   ├── Middleware/            # Middlewares customizados
│   │   ├── Program.cs             # ⭐ Configuração principal
│   │   └── appsettings.json       # Configurações
│   │
│   ├── TemplateApi.Application/   # Lógica de aplicação
│   │   ├── Features/              # ⭐ Vertical Slices (CQRS)
│   │   │   ├── Auth/
│   │   │   │   ├── Login/
│   │   │   │   └── Register/
│   │   │   └── Users/
│   │   └── Common/                # DTOs, Interfaces compartilhadas
│   │
│   ├── TemplateApi.Domain/        # Lógica de negócio
│   │   ├── Entities/              # ⭐ Entidades de domínio
│   │   │   ├── User.cs
│   │   │   ├── Product.cs
│   │   │   └── Order.cs
│   │   ├── Interfaces/            # Contratos (Repository, etc)
│   │   └── Common/                # BaseEntity, Result, ValueObjects
│   │
│   └── TemplateApi.Infrastructure/ # Implementação técnica
│       ├── Persistence/           # ⭐ EF Core, Repositórios
│       │   ├── ApplicationDbContext.cs
│       │   ├── Configurations/    # Fluent API
│       │   └── Repositories/
│       └── Services/              # ⭐ Email, Cache, JWT
│
├── README.md                      # Este arquivo
└── STUDY_GUIDE.md                 # Guia de estudo detalhado
```

## 💡 Conceitos Demonstrados

### 1. Clean Architecture

**Problema**: Código acoplado, difícil de testar e manter

**Solução**: Separação em camadas com dependências claras
- Domain não depende de ninguém
- Application depende apenas de Domain
- Infrastructure implementa contratos de Application
- API orquestra tudo

### 2. CQRS (Command Query Responsibility Segregation)

**Problema**: Lógica de leitura e escrita misturada

**Solução**: Separa operações
- **Commands**: Modificam estado (Create, Update, Delete)
- **Queries**: Apenas leem dados (Get, List)

### 3. Repository + Unit of Work

**Problema**: Acesso a dados espalhado, difícil de testar

**Solução**:
- Repository: Abstrai acesso a dados
- Unit of Work: Coordena transações

### 4. Vertical Slice Architecture

**Problema**: Features espalhadas por múltiplas pastas

**Solução**: Tudo relacionado a uma feature em uma pasta
- Fácil encontrar código
- Mudanças localizadas
- Onboarding mais rápido

## 🎯 Recursos do .NET 9

### Rate Limiting

Protege contra abuso:

```csharp
// Fixed Window: 100 requests por minuto
app.MapGet("/api/products")
   .RequireRateLimiting("fixed");
```

### Output Caching

Cache de respostas HTTP:

```csharp
app.MapGet("/api/products")
   .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));
```

### Minimal APIs

APIs sem controllers:

```csharp
app.MapGet("/", () => "Hello World!");
```

### JWT Authentication

Segurança baseada em tokens:

```csharp
[Authorize] // Exige token válido
public IActionResult SecureEndpoint() { }
```

## 📖 Aprendizado

Este projeto é **educacional**. Cada arquivo contém:

- ✅ Comentários detalhados explicando conceitos
- ✅ Exemplos práticos
- ✅ Comparações (quando usar X vs Y)
- ✅ Boas práticas
- ✅ Armadilhas comuns

**Arquivos importantes para estudar:**

1. `Program.cs` - Configuração completa da API
2. `ApplicationDbContext.cs` - EF Core com boas práticas
3. `User.cs` - Entidade de domínio rica
4. `RegisterUserCommand.cs` - CQRS pattern
5. `JwtService.cs` - Autenticação JWT

## 🤝 Contribuindo

Contribuições são bem-vindas! Abra uma issue ou PR.

## 📄 Licença

MIT License - use como quiser!

## 🙏 Agradecimentos

Projeto criado para ajudar a comunidade .NET brasileira a aprender práticas modernas de desenvolvimento.

---

**Feito com ❤️ para desenvolvedores .NET**
