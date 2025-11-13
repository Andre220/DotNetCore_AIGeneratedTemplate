# 📝 Comandos Úteis - Template API

## 🔧 Entity Framework Core (Migrations)

### Criar nova migration
```bash
cd src/TemplateApi.Api
dotnet ef migrations add NomeDaMigration --project ../TemplateApi.Infrastructure --startup-project .
```

### Aplicar migrations ao banco
```bash
dotnet ef database update --project ../TemplateApi.Infrastructure --startup-project .
```

### Reverter última migration
```bash
dotnet ef database update NomeMigrationAnterior --project ../TemplateApi.Infrastructure --startup-project .
```

### Remover última migration (se não aplicada)
```bash
dotnet ef migrations remove --project ../TemplateApi.Infrastructure --startup-project .
```

### Ver SQL que será executado
```bash
dotnet ef migrations script --project ../TemplateApi.Infrastructure --startup-project .
```

### Dropar banco e recriar
```bash
dotnet ef database drop --project ../TemplateApi.Infrastructure --startup-project .
dotnet ef database update --project ../TemplateApi.Infrastructure --startup-project .
```

## 🏗️ Build & Run

### Restaurar dependências
```bash
dotnet restore
```

### Compilar
```bash
dotnet build
```

### Executar em Development
```bash
cd src/TemplateApi.Api
dotnet run
```

### Executar com watch (hot reload)
```bash
dotnet watch run
```

### Compilar Release
```bash
dotnet build -c Release
```

### Publicar para produção
```bash
dotnet publish -c Release -o ./publish
```

## 🧪 Testes

### Rodar todos os testes
```bash
dotnet test
```

### Com cobertura de código
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 📦 NuGet Packages

### Adicionar pacote
```bash
dotnet add package NomeDoPacote
```

### Atualizar pacote
```bash
dotnet add package NomeDoPacote --version X.Y.Z
```

### Listar pacotes desatualizados
```bash
dotnet list package --outdated
```

### Restaurar pacotes
```bash
dotnet restore
```

## 🐳 Docker

### Build da imagem
```bash
docker build -t templateapi:latest .
```

### Executar container
```bash
docker run -p 5001:8080 templateapi:latest
```

### Docker Compose (PostgreSQL + Redis + API)
```bash
docker-compose up -d
```

### Ver logs
```bash
docker-compose logs -f api
```

### Parar tudo
```bash
docker-compose down
```

## 🔍 Debugging & Logs

### Ver logs em tempo real
```bash
tail -f logs/log-20240101.txt
```

### Limpar logs
```bash
rm -rf logs/*.txt
```

## 🗄️ PostgreSQL

### Conectar ao banco
```bash
psql -h localhost -U postgres -d templateapi_dev
```

### Comandos SQL úteis
```sql
-- Listar tabelas
\dt

-- Descrever tabela
\d users

-- Ver todos os usuários
SELECT * FROM "Users";

-- Contar registros
SELECT COUNT(*) FROM "Users";

-- Limpar tabela
TRUNCATE TABLE "Users" CASCADE;
```

## 🔴 Redis

### Conectar ao Redis CLI
```bash
redis-cli
```

### Comandos Redis úteis
```
# Ver todas as chaves
KEYS *

# Ver valor de uma chave
GET chave

# Deletar chave
DEL chave

# Limpar tudo
FLUSHALL

# Monitorar comandos em tempo real
MONITOR
```

## 🧹 Limpeza

### Limpar bin e obj
```bash
dotnet clean
rm -rf **/bin **/obj
```

### Limpar tudo (incluindo packages)
```bash
git clean -xfd
```

## 🔐 Secrets (Desenvolvimento)

### Inicializar user secrets
```bash
cd src/TemplateApi.Api
dotnet user-secrets init
```

### Adicionar secret
```bash
dotnet user-secrets set "Jwt:SecretKey" "MeuTokenSecreto"
```

### Listar secrets
```bash
dotnet user-secrets list
```

### Remover secret
```bash
dotnet user-secrets remove "Jwt:SecretKey"
```

## 📊 Performance

### Ver dependências do projeto
```bash
dotnet list package --include-transitive
```

### Análise de código
```bash
dotnet format
```

### Security audit
```bash
dotnet list package --vulnerable
```

## 🌐 Testes de API (curl)

### Registrar usuário
```bash
curl -X POST https://localhost:5001/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Teste User",
    "email": "teste@example.com",
    "password": "Senha@123",
    "confirmPassword": "Senha@123"
  }'
```

### Login
```bash
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "teste@example.com",
    "password": "Senha@123"
  }'
```

### Acessar endpoint protegido
```bash
TOKEN="seu-token-aqui"
curl -X GET https://localhost:5001/api/v1/auth/me \
  -H "Authorization: Bearer $TOKEN"
```

### Listar usuários
```bash
curl -X GET "https://localhost:5001/api/v1/users?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

## 🚀 CI/CD (GitHub Actions)

### Testar localmente com act
```bash
act -j build
```

## 💡 Dicas

### Verificar versão do .NET
```bash
dotnet --version
dotnet --list-sdks
dotnet --list-runtimes
```

### Verificar ferramentas instaladas
```bash
dotnet tool list -g
```

### Instalar EF Core CLI global
```bash
dotnet tool install --global dotnet-ef
```

### Atualizar EF Core CLI
```bash
dotnet tool update --global dotnet-ef
```

### Ver informação do projeto
```bash
dotnet list reference
dotnet list package
```

---

**💡 TIP**: Adicione esses comandos ao seu `.bashrc` ou crie aliases para os mais usados!

```bash
# Exemplo de alias
alias drun='dotnet run'
alias dwatch='dotnet watch run'
alias dmigrate='dotnet ef database update'
```
