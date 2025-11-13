# 🎓 Guia de Estudos - TemplateApi

## 📖 Como Navegar Este Projeto

Este projeto foi criado como **material de estudo** com explicações conceituais detalhadas em TODOS os arquivos.

### 🎯 Ordem Recomendada de Estudo

#### 1️⃣ **COMECE PELO DOMAIN** (Core do negócio)
```
📂 TemplateApi.Domain/
├── Common/
│   ├── Result.cs ⭐ LEIA PRIMEIRO
│   │   └── Result Pattern - alternativa a exceptions
│   ├── BaseEntity.cs
│   │   └── Entidade base, Audit Trail, Soft Delete
│   └── DomainEvent.cs
│       └── Event-Driven Architecture, Observer Pattern
└── Entities/
    └── User.cs ⭐ EXEMPLO COMPLETO
        └── Entidade rica, Factory Methods, Regras de negócio
```

**CONCEITOS APRESENTADOS:**
- ✅ Result Pattern (por que não usar exceptions para tudo)
- ✅ Entity vs Value Object
- ✅ Soft Delete vs Hard Delete
- ✅ Domain Events
- ✅ Factory Methods
- ✅ Encapsulation (setters privados)

---

#### 2️⃣ **INTERFACES DO DOMAIN** (Contratos)
```
📂 TemplateApi.Domain/Interfaces/
├── IRepository.cs ⭐ LEIA
│   └── Repository Pattern - abstração de persistência
├── IUnitOfWork.cs ⭐ IMPORTANTE
│   └── Unit of Work - transações atômicas
└── IUserRepository.cs
    └── Repositório específico com queries customizadas
```

**CONCEITOS APRENDIDOS:**
- ✅ Repository Pattern (por que abstrair o banco)
- ✅ Unit of Work (tudo ou nada)
- ✅ Dependency Inversion Principle
- ✅ Interface Segregation

---

#### 3️⃣ **APPLICATION LAYER** (Casos de uso)
```
📂 TemplateApi.Application/Common/
├── DTOs/
│   └── UserDtos.cs ⭐ LEIA
│       └── Por que NUNCA expor entidades diretamente
├── Validators/
│   └── UserValidators.cs ⭐ LEIA
│       └── FluentValidation vs DataAnnotations
└── Interfaces/
    └── IInfrastructureServices.cs ⭐ LEIA
        └── Cache, Logging, Email, DateTime abstraídos
```

**CONCEITOS APRENDIDOS:**
- ✅ DTOs (Request vs Response)
- ✅ Fluent Validation
- ✅ Por que abstrair serviços de infraestrutura
- ✅ Testabilidade

---

#### 4️⃣ **INFRASTRUCTURE** (Implementações - a criar)
```
📂 TemplateApi.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Repository.cs (implementação genérica)
│   └── UnitOfWork.cs
├── Services/
│   ├── CacheService.cs (Redis ou Memory)
│   ├── LogService.cs (Serilog)
│   └── EmailService.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
        └── AddInfrastructure(services)
```

**CONCEITOS A APRENDER:**
- Entity Framework Core
- Repository implementation
- UnitOfWork implementation
- Dependency Injection setup

---

#### 5️⃣ **API LAYER** (Features - a criar)
```
📂 TemplateApi.Api/
├── Features/Users/v1/
│   ├── CreateUser/
│   │   ├── CreateUserController.cs
│   │   ├── CreateUserCommand.cs
│   │   └── CreateUserHandler.cs
│   └── GetUser/
│       └── ...
├── Shared/
│   ├── Middlewares/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   └── RequestLoggingMiddleware.cs
│   └── Filters/
│       └── ValidationFilter.cs
└── Program.cs
```

**CONCEITOS A APRENDER:**
- Feature-based organization
- CQRS (Commands vs Queries)
- Middlewares
- Filters
- API Versioning
- Swagger/OpenAPI

---

## 🎯 Conceitos Principais por Arquivo

### Result.cs - Result Pattern
**POR QUÊ?** Exceptions são caras e para fluxos esperados (usuário não encontrado) são exagero.

```csharp
// ❌ Ruim - exception para fluxo normal
public User GetUser(int id) {
    var user = _repo.Find(id);
    if (user == null)
        throw new NotFoundException(); // Caro!
}

// ✅ Bom - Result explícito
public Result<User> GetUser(int id) {
    var user = _repo.Find(id);
    if (user == null)
        return Result<User>.Failure("User not found");
    return Result<User>.Success(user);
}
```

### BaseEntity.cs - Audit Trail
**POR QUÊ?** Toda entidade precisa: Id, CreatedAt, UpdatedAt, IsDeleted

```csharp
// Todas as entidades herdam
public class User : BaseEntity {
    // Automaticamente tem:
    // - Id
    // - CreatedAt
    // - UpdatedAt
    // - IsDeleted (soft delete)
    // - MarkAsDeleted()
    // - Restore()
}
```

### DomainEvent.cs - Event-Driven
**POR QUÊ?** Desacoplar ações que devem acontecer após algo.

```csharp
// Ao criar usuário
var user = User.Create(name, email);
user.AddDomainEvent(new UserCreatedEvent(user.Id));

// Handlers reagem automaticamente:
// - SendWelcomeEmailHandler
// - CreateUserProfileHandler
// - NotifyAdminsHandler
// Sem precisar chamar explicitamente!
```

### Repository Pattern
**POR QUÊ?** Domínio não deve saber sobre SQL, EF, Dapper, etc.

```csharp
// Domain define O QUÊ precisa
public interface IUserRepository {
    Task<User> GetByEmailAsync(string email);
}

// Infrastructure define COMO
public class UserRepository : IUserRepository {
    public Task<User> GetByEmailAsync(string email) {
        return _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}

// Posso trocar EF por Dapper sem tocar no domínio!
```

### Unit of Work
**POR QUÊ?** Garantir que operações sejam atômicas (tudo ou nada).

```csharp
// ❌ Sem UoW - podem ficar pela metade
await _userRepo.AddAsync(user);
await _userRepo.SaveAsync();
await _profileRepo.AddAsync(profile);
await _profileRepo.SaveAsync(); // Se falhar aqui, user já foi salvo!

// ✅ Com UoW - tudo em uma transação
await _userRepo.AddAsync(user);
await _profileRepo.AddAsync(profile);
await _unitOfWork.SaveChangesAsync(); // Tudo ou nada!
```

### DTOs
**POR QUÊ?** NUNCA expor entidades diretamente.

```csharp
// ❌ Expõe PasswordHash, IsDeleted, etc
[HttpGet]
public User Get(int id) => _repo.GetById(id);

// ✅ Controla exatamente o que vai na API
[HttpGet]
public UserDto Get(int id) {
    var user = _repo.GetById(id);
    return new UserDto {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email
        // PasswordHash NÃO está aqui!
    };
}
```

### Fluent Validation
**POR QUÊ?** Melhor que DataAnnotations em todos os aspectos.

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserRequest> {
    public CreateUserValidator() {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (email) => !await _repo.EmailExistsAsync(email))
                .WithMessage("Email already exists");
    }
}
```

---

## 🚀 Próximos Passos

1. **Leia os arquivos na ordem sugerida**
2. **Entenda os PORQUÊS nos comentários**
3. **Experimente modificar o código**
4. **Crie suas próprias entidades seguindo o padrão**

---

## 📚 Recursos Adicionais

### Clean Architecture
- [Clean Architecture by Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

### Domain-Driven Design
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)

### Patterns
- Repository Pattern
- Unit of Work Pattern
- Result Pattern
- Factory Pattern
- Observer Pattern (Domain Events)

### Libraries
- FluentValidation
- AutoMapper
- Serilog
- EF Core

---

## ❓ FAQ - Perguntas Frequentes

### "Por que tantas interfaces?"
**R:** Testabilidade e desacoplamento. Posso trocar implementações sem mudar o código que usa.

### "Por que não usar DataAnnotations?"
**R:** FluentValidation é mais poderoso, testável e separa validação do modelo.

### "Por que DTOs e não expor entidades?"
**R:** Segurança, desacoplamento, versionamento, performance.

### "Por que Result ao invés de Exceptions?"
**R:** Performance e explicitação de que pode falhar.

### "Domain Events são necessários?"
**R:** Não para apps simples. Mas em apps maiores, desacoplam muito o código.

### "Preciso usar tudo isso?"
**R:** Depende do tamanho do projeto. Para CRUD simples, pode ser overkill. Para apps empresariais, economiza MUITO tempo a longo prazo.

---

## 💡 Dicas de Estudo

1. **Não tente aprender tudo de uma vez**
2. **Entenda um conceito por dia**
3. **Implemente exemplos próprios**
4. **Questione os "porquês"**
5. **Compare com código que você já viu**

---

## 🎯 Checklist de Aprendizado

- [ ] Entendi Result Pattern e quando usar
- [ ] Entendi diferença entre Entity e Value Object
- [ ] Entendi Repository Pattern e por quê
- [ ] Entendi Unit of Work e transações
- [ ] Entendi Domain Events e desacoplamento
- [ ] Entendi por que usar DTOs
- [ ] Entendi FluentValidation
- [ ] Entendi organização por Features
- [ ] Consigo criar uma nova entidade seguindo o padrão
- [ ] Consigo criar um novo feature completo

---

**Bons estudos! 🚀**
