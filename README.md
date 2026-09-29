# Checkpoint 2 — ExpenseHub

Checkpoint de C# em grupos de até 3 pessoas para construção de uma Application Programming Interface (API) corporativa de reembolsos.

O prazo de entrega é **13 de outubro de 2026**. O grupo deverá implementar autenticação, autorização, fluxo de aprovação e reprovação, pagamento simulado, histórico e testes unitários.

## Criar seu repositório

1. Clique em **Use this template**.
2. Selecione **Create a new repository**.
3. Crie um repositório **público** em uma das contas do grupo.
4. Adicione os demais integrantes como colaboradores.
5. Clone o repositório.

Não use fork. As issues permanecem neste repositório original como especificação comum da turma.

Os commits serão utilizados para avaliar a participação. Todos os membros do grupo
devem possuir mais de um commit no repositório.

## Fluxo de trabalho

Para cada issue:

1. leia os critérios no repositório original;
2. crie uma branch com o identificador, por exemplo `i06-ownership`;
3. implemente e valide a feature;
4. abra uma pull request no seu próprio repositório;
5. use um título como `I06 — Ownership e matriz de acesso`;
6. adicione na descrição uma referência completa, como `Racass/checkpoint-csharpracass-expensehub#6`;
7. não use `Closes`, `Fixes` ou `Resolves`, pois a issue original deve permanecer aberta;
8. conclua a auto-revisão e faça o merge.

## Estrutura inicial

```text
sources/
├── ExpenseHub.slnx
├── ExpenseHub.Api/
└── ExpenseHub.UnitTests/
```

A solução começa sem Identity, banco, domínio ou testes funcionais. Toda implementação avaliada deve ser criada por você.

## Comandos

```shell
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet test ./sources/ExpenseHub.slnx
dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
```

O endpoint inicial `GET /health` existe apenas para confirmar que a aplicação inicia.

## Documentação

- [Enunciado](docs/ENUNCIADO.md)
- [Requisitos e contratos](docs/REQUISITOS.md)
- [Rubrica](docs/RUBRICA.md)
- [Matriz de autorização](docs/MATRIZ-AUTORIZACAO.md)
- [Processo no GitHub](docs/PROCESSO-GITHUB.md)
- [Uso de Inteligência Artificial](docs/USO-DE-IA.md)
- [Regras do pipeline de qualidade](docs/code-quality-rules.md)

## Banco de dados

O ExpenseHub usa **SQLite** por meio do Entity Framework Core.

- **Provider e pacote:** `Microsoft.EntityFrameworkCore.Sqlite` (10.0.12). O pacote `Microsoft.EntityFrameworkCore.Design` é usado apenas pelas migrations.
- **Configuração:** a connection string está em `ConnectionStrings:ExpenseHub` no `appsettings.json` (`Data Source=expensehub.db`). O arquivo é criado na pasta de execução da API e não contém segredos. Para usar outro caminho, sobrescreva com a variável de ambiente `ConnectionStrings__ExpenseHub`.
- **Criação ou atualização do banco:** as migrations ficam em `sources/ExpenseHub.Api/Migrations`. Em ambiente `Development`, a API aplica as migrations ao iniciar. Para aplicar manualmente, use a ferramenta local `dotnet-ef`, restaurada pelo manifesto `dotnet-tools.json`:

```shell
dotnet tool restore
dotnet dotnet-ef database update --project ./sources/ExpenseHub.Api
```

- **Nova migration** depois de alterar as entidades:

```shell
dotnet dotnet-ef migrations add NomeDaMigration --project ./sources/ExpenseHub.Api --output-dir Migrations
```

- **Como iniciar a aplicação:** `dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj` (perfil `http`, `http://localhost:5245`). Confirme com `GET /health`.

Os arquivos `*.db`, `*.db-shm` e `*.db-wal` estão no `.gitignore`. O build e os testes unitários não dependem do banco.

### Entidades mínimas

| Entidade | Papel |
|---|---|
| `Expense` | Reembolso com dono, descrição, valor, data, categoria, estado e datas em UTC |
| `ExpenseCategory` | Categoria do reembolso, com quatro categorias iniciais criadas pela migration |
| `ExpenseHistory` | Histórico de ações: ator, horário, estado anterior e novo, justificativa |
| `PaymentRecord` | Pagamento simulado, no máximo um por reembolso |

Os estados (`ExpenseStatus`) são gravados como texto no banco: `Draft`, `Submitted`, `Approved`, `Rejected` e `Paid`.

## Autenticação e conta Admin

A API usa ASP.NET Core Identity com as tabelas no mesmo banco SQLite e autenticação por **token bearer**.

### Roles

`Admin`, `Employee`, `Approver`, `Finance` e `Auditor`. Elas são criadas pelo seed ao iniciar a aplicação, e nenhuma outra role é criada.

### Conta Admin inicial

O seed cria **uma única** conta Admin e pode ser executado várias vezes sem duplicar roles nem usuários. Se já existir algum usuário com a role Admin, nada é criado.

- O e-mail vem de `Seed:AdminEmail` no `appsettings.json` (`admin@expensehub.local`).
- A senha **não fica no repositório**. Configure com User Secrets antes de iniciar a API:

```shell
dotnet user-secrets set "Seed:AdminPassword" "<sua-senha>" --project ./sources/ExpenseHub.Api
```

Ou use a variável de ambiente `Seed__AdminPassword`. A senha segue a política padrão do Identity: pelo menos 6 caracteres, com letra maiúscula, minúscula, número e símbolo. Sem senha configurada, a API inicia normalmente, registra um aviso e não cria o Admin.

### Login

```http
POST /login
Content-Type: application/json

{ "email": "admin@expensehub.local", "password": "<sua-senha>" }
```

A resposta traz `accessToken`, válido por 1 hora. Envie-o nas rotas protegidas:

```http
GET /api/me
Authorization: Bearer <accessToken>
```

`GET /api/me` mostra id, e-mail e roles do token atual. As roles ficam gravadas no token: **depois de uma mudança de roles, o usuário precisa fazer login de novo** para o novo token refletir as roles atuais.

### Respostas de erro

Os erros usam `ProblemDetails`:

| Status | Quando |
|---|---|
| 400 | Entrada inválida (por exemplo, e-mail mal formatado no login) |
| 401 | Sem token, token inválido ou expirado, ou credenciais erradas no login |
| 403 | Autenticado, mas sem a role exigida pela rota |

Após 5 tentativas de login erradas seguidas, a conta fica bloqueada por 5 minutos.

## Cadastro e gerenciamento de roles

### Cadastro

```http
POST /register
Content-Type: application/json

{ "email": "ana@empresa.com", "password": "<senha>" }
```

- Cria o usuário **sem nenhuma role** e responde `201` com id, e-mail e `roles: []`.
- O cadastro **não aceita roles**: qualquer campo além de `email` e `password` (por exemplo `"roles"`) é recusado com `400`.
- E-mail inválido, e-mail já cadastrado ou senha fora da política do Identity também respondem `400`.

Enquanto não receber roles do Admin, o usuário consegue fazer login, mas não executa nenhuma operação protegida (`403`).

### Rotas do Admin

Todas exigem token de um usuário com a role `Admin`: sem token, `401`; com token sem Admin, `403`.

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/admin/users` | Lista usuários com suas roles |
| PUT | `/api/admin/users/{id}/roles` | Substitui as roles do usuário pelo conjunto enviado |

```http
PUT /api/admin/users/{id}/roles
Authorization: Bearer <token do Admin>
Content-Type: application/json

{ "roles": ["Employee", "Approver"] }
```

- O corpo é o **conjunto completo** desejado: roles ausentes são removidas e uma lista vazia remove todas.
- Só as cinco roles conhecidas são aceitas, sem diferenciar maiúsculas. Uma role desconhecida responde `400` e nada é alterado; nenhuma role nova é criada.
- Usuário inexistente responde `404`.
- O Admin **não pode remover a própria role Admin** (`409`), para não perder o acesso administrativo.

> **Importante:** as roles ficam gravadas no token. Depois de uma alteração de roles, o usuário precisa **fazer login de novo** para receber um token com as roles atualizadas. O token antigo continua válido, com as roles antigas, até expirar (1 hora).

O arquivo `sources/ExpenseHub.Api/ExpenseHub.Api.http` traz essas requisições prontas para o VS Code (extensão REST Client) ou o Visual Studio.

## Testes

Somente testes unitários escritos por você entram na nota. Testes de integração, end-to-end ou de interface são permitidos, mas opcionais e sem pontuação.

Os testes unitários devem executar sem banco, rede ou serviço externo.

## Entrega

Entregue:

- URL do repositório público;
- commit Secure Hash Algorithm (SHA) final;
- integração contínua executada;
- documentação atualizada.

O projeto deve compilar sem erros e ser entregue sem warnings para receber a pontuação integral de Qualidade de Código.
