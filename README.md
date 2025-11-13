# 🚀 Template API - Enterprise ASP.NET Core Template

Um template completo e production-ready para APIs REST usando ASP.NET Core 9.0 com Clean Architecture e organização por Features.

## 📋 Índice

- [Arquitetura](#arquitetura)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Recursos Implementados](#recursos-implementados)
- [Como Usar](#como-usar)
- [Conceitos Explicados](#conceitos-explicados)

---

## 🏗️ Arquitetura

Este projeto segue os princípios de **Clean Architecture** com organização por **Features (Vertical Slices)**:

```
┌─────────────────────────────────────────────┐
│          TemplateApi.Api (Presentation)     │
│  Features: Users, Products, Auth, etc       │
│  Cada feature contém: Controller, Request,  │
│  Response, Validator, Handler               │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│      TemplateApi.Application (Use Cases)    │
│  DTOs, Validators, Mappers, Interfaces     │
│  CQRS Handlers, Business Logic              │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│       TemplateApi.Domain (Core)             │
│  Entities, Value Objects, Domain Events     │
│  Business Rules, Domain Services            │
└──────────────────────────────────────────────┘
                   ▲
┌──────────────────┴──────────────────────────┐
│    TemplateApi.Infrastructure (External)    │
│  Repository, UnitOfWork, EF Context        │
│  External Services, Cache, Logging          │
└─────────────────────────────────────────────┘
```

---

## 📁 Estrutura do Projeto

```
TemplateApi/
├── src/
│   ├── TemplateApi.Api/              # Camada de apresentação
│   │   ├── Features/                 # Organizado por features
│   │   │   ├── Users/
│   │   │   │   ├── v1/
│   │   │   │   │   ├── CreateUser/
│   │   │   │   │   ├── GetUser/
│   │   │   │   │   └── UpdateUser/
│   │   │   │   └── Models/
│   │   │   ├── Products/
│   │   │   └── Authentication/
│   │   ├── Shared/                   # Compartilhado entre features
│   │   │   ├── Middlewares/
│   │   │   ├── Filters/
│   │   │   ├── Extensions/
│   │   │   └── Configurations/
│   │   └── Program.cs
│   │
│   ├── TemplateApi.Application/       # Lógica de aplicação
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   ├── DTOs/
│   │   │   ├── Mappers/
│   │   │   └── Validators/
│   │   └── Features/                  # Use cases por feature
│   │
│   ├── TemplateApi.Domain/            # Core do negócio
│   │   ├── Common/
│   │   │   ├── BaseEntity.cs
│   │   │   ├── Result.cs
│   │   │   └── DomainEvent.cs
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Enums/
│   │   └── Interfaces/
│   │
│   └── TemplateApi.Infrastructure/    # Implementações externas
│       ├── Persistence/
│       │   ├── Context/
│       │   ├── Repositories/
│       │   └── Configurations/
│       ├── Services/
│       │   ├── Caching/
│       │   ├── Logging/
│       │   └── External/
│       └── Extensions/
│
└── tests/
    ├── TemplateApi.UnitTests/
    └── TemplateApi.IntegrationTests/
```

---

## ✨ Recursos Implementados

### 🧱 1. Arquitetura e Estrutura

- ✅ **Clean Architecture** com separação clara de responsabilidades
- ✅ **Vertical Slices** - organização por features
- ✅ **Options Pattern** - configurações fortemente tipadas
- ✅ **Dependency Injection** - extensões por camada
- ✅ **Result Pattern** - retornos padronizados

### 🧠 2. Padrões de Desenvolvimento

- ✅ **DTOs** - separação de modelos de domínio
- ✅ **AutoMapper** - mapeamento automático
- ✅ **FluentValidation** - validação declarativa
- ✅ **CQRS** - separação de comandos e queries
- ✅ **Repository Pattern** - abstração de persistência
- ✅ **Unit of Work** - controle de transações

### 🧩 3. Cross-cutting Concerns

- ✅ **Logging** - Serilog estruturado
- ✅ **Caching** - In-Memory e Redis
- ✅ **Authentication** - JWT com Refresh Token
- ✅ **Authorization** - Claims e Policies
- ✅ **Exception Handling** - middleware global
- ✅ **Response Wrapper** - formato padronizado

### 🧰 4. Infraestrutura

- ✅ **Entity Framework Core** - ORM
- ✅ **Migrations** - controle de schema
- ✅ **Audit Trail** - rastreamento de mudanças
- ✅ **Soft Delete** - exclusões lógicas
- ✅ **Database Seeding** - dados iniciais

### 🧑‍💻 5. API Features

- ✅ **API Versioning** - múltiplas versões
- ✅ **Swagger/OpenAPI** - documentação automática
- ✅ **Health Checks** - monitoramento
- ✅ **Rate Limiting** - proteção contra abuso
- ✅ **CORS** - controle de origem
- ✅ **Compression** - otimização de resposta

### 📊 6. Observabilidade

- ✅ **Structured Logging** - logs estruturados
- ✅ **Correlation ID** - rastreamento de requisições
- ✅ **Performance Metrics** - medição de tempo
- ✅ **Health Checks UI** - interface de status

---

## 🚀 Como Usar

### Pré-requisitos

- .NET 9.0 SDK
- SQL Server (ou ajustar para outro BD)
- Redis (opcional, para cache distribuído)

### Executar Localmente

```bash
# Restaurar dependências
dotnet restore

# Executar migrations
dotnet ef database update --project src/TemplateApi.Infrastructure

# Executar a API
cd src/TemplateApi.Api
dotnet run
```

### Acessar

- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`
- Health Checks: `http://localhost:5000/health`

---

## 📚 Conceitos Explicados

### 🎯 Por que Features (Vertical Slices)?

**Tradicional (Horizontal)**:
```
Controllers/ → todos os controllers
Services/ → todos os services
DTOs/ → todos os DTOs
```
❌ Dificulta encontrar tudo relacionado a uma funcionalidade
❌ Arquivos espalhados por múltiplas pastas

**Features (Vertical)**:
```
Users/
  ├── CreateUser/ → tudo sobre criar usuário
  ├── GetUser/ → tudo sobre buscar usuário
  └── UpdateUser/ → tudo sobre atualizar usuário
```
✅ Tudo relacionado junto
✅ Fácil de encontrar e manter
✅ Facilita trabalho em equipe

### 🎯 O que é Clean Architecture?

Separação em camadas com regra de dependência:

1. **Domain** (Core) - Regras de negócio puras
   - Sem dependências externas
   - Entidades, Value Objects, Interfaces

2. **Application** - Casos de uso
   - Depende apenas do Domain
   - DTOs, Validators, Handlers

3. **Infrastructure** - Implementações técnicas
   - Depende de Domain e Application
   - Banco de dados, APIs externas, Cache

4. **API** - Interface com o mundo
   - Depende de todas as outras
   - Controllers, Middlewares, Filters

**Regra de Ouro**: Dependências apontam SEMPRE para dentro (Domain no centro)

### 🎯 O que é Result Pattern?

Ao invés de lançar exceptions para fluxos esperados:

```csharp
// ❌ Ruim - exception para fluxo normal
public User GetUser(int id)
{
    var user = db.Find(id);
    if (user == null)
        throw new NotFoundException(); // Exception é caro!
    return user;
}

// ✅ Bom - resultado explícito
public Result<User> GetUser(int id)
{
    var user = db.Find(id);
    if (user == null)
        return Result<User>.Failure("User not found");
    return Result<User>.Success(user);
}
```

### 🎯 O que é Options Pattern?

Configurações fortemente tipadas:

```csharp
// ❌ Ruim - magic strings
var jwt = configuration["Jwt:Secret"];

// ✅ Bom - classe tipada
public class JwtOptions
{
    public string Secret { get; set; }
    public int ExpirationMinutes { get; set; }
}

services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
```

### 🎯 O que é CQRS?

Separação de leitura (Query) e escrita (Command):

```csharp
// Command - muda estado
public class CreateUserCommand
{
    public string Name { get; set; }
}

// Query - apenas lê
public class GetUserQuery
{
    public int Id { get; set; }
}
```

Benefícios:
- Otimizações específicas (queries podem ir direto no BD)
- Validações diferentes
- Escalabilidade (ler e escrever em BDs separados)

---

## 📖 Próximos Passos

1. Explore o código começando por `Program.cs`
2. Veja como uma feature está estruturada em `Features/Users/`
3. Entenda o fluxo: Controller → Handler → Repository → Database
4. Customize as configurations em `appsettings.json`
5. Adicione suas próprias features seguindo o padrão

---

## 🤝 Contribuindo

Este é um template de estudo. Sinta-se livre para adaptar ao seu projeto!

---

## 📄 Licença

MIT License - use livremente para aprender e construir!
