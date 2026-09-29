# ExpenseHub

API REST de reembolsos corporativos, desenvolvida para o Checkpoint 2 de C# (FIAP). Funcionários registram despesas, aprovadores analisam os pedidos, o financeiro registra pagamentos e auditores consultam o histórico.

A especificação completa está em [`docs/`](docs/) e nas issues do repositório original [`Racass/checkpoint-csharpracass-expensehub`](https://github.com/Racass/checkpoint-csharpracass-expensehub/issues).

## Tecnologias

- .NET 10 e ASP.NET Core (controllers)
- ASP.NET Core Identity com autenticação por token bearer
- Entity Framework Core com SQLite
- MSTest para os testes unitários

## Estrutura

```text
sources/
├── ExpenseHub.slnx
├── ExpenseHub.Api/
│   ├── Auth/          roles, seed do Admin e regras de roles
│   ├── Controllers/   endpoints HTTP
│   ├── Data/          DbContext e conversões
│   ├── Domain/        entidades e estados do reembolso
│   ├── Dtos/          contratos de entrada e saída, com validação
│   ├── Migrations/    migrations do Entity Framework Core
│   ├── Services/      regras de negócio usadas pelos controllers
│   └── Validation/    atributos de validação próprios
└── ExpenseHub.UnitTests/
```

## Como executar

Pré-requisito: [.NET SDK 10](https://dotnet.microsoft.com/download).

1. Restaure as dependências e as ferramentas locais:

   ```shell
   dotnet restore ./sources/ExpenseHub.slnx
   dotnet tool restore
   ```

2. Configure a senha da conta Admin inicial (veja [Conta Admin inicial](#conta-admin-inicial)):

   ```shell
   dotnet user-secrets set "Seed:AdminPassword" "<sua-senha>" --project ./sources/ExpenseHub.Api
   ```

3. Compile, rode os testes e inicie a API:

   ```shell
   dotnet build ./sources/ExpenseHub.slnx
   dotnet test ./sources/ExpenseHub.slnx
   dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
   ```

A API sobe em `http://localhost:5245`. Confirme com `GET /health`, que responde `{"status":"ok"}`.

O arquivo [`ExpenseHub.Api.http`](sources/ExpenseHub.Api/ExpenseHub.Api.http) traz as requisições prontas para o VS Code (extensão REST Client) ou o Visual Studio.

## Banco de dados

- **Provider e pacote:** SQLite, com `Microsoft.EntityFrameworkCore.Sqlite` (10.0.12). O pacote `Microsoft.EntityFrameworkCore.Design` é usado apenas pelas migrations.
- **Configuração:** a connection string fica em `ConnectionStrings:ExpenseHub` no `appsettings.json` (`Data Source=expensehub.db`) e não contém segredos. O arquivo do banco é criado na pasta de execução da API. Para usar outro caminho, defina a variável de ambiente `ConnectionStrings__ExpenseHub`.
- **Criação e atualização:** ao iniciar, a API aplica as migrations pendentes e executa o seed. Para aplicar as migrations manualmente, use a ferramenta local `dotnet-ef`, declarada em `dotnet-tools.json`:

  ```shell
  dotnet dotnet-ef database update --project ./sources/ExpenseHub.Api
  ```

- **Nova migration** depois de alterar as entidades:

  ```shell
  dotnet dotnet-ef migrations add NomeDaMigration --project ./sources/ExpenseHub.Api --output-dir Migrations
  ```

Os arquivos `*.db`, `*.db-shm` e `*.db-wal` estão no `.gitignore`. O build e os testes unitários não dependem do banco.

### Entidades

| Entidade | Papel |
|---|---|
| `Expense` | Reembolso com proprietário, descrição, valor, data da despesa, categoria opcional, estado e datas de criação e alteração |
| `ExpenseCategory` | Categoria do reembolso. A migration inicial cria quatro categorias de referência: Transporte, Alimentação, Hospedagem e Outros |
| `ExpenseHistory` | Histórico: ação, reembolso, ator, instante, estado anterior e posterior, justificativa da reprovação e alterações feitas em Draft (`Changes`) |
| `PaymentRecord` | Pagamento simulado, com ator e instante; no máximo um por reembolso |

- Os estados (`ExpenseStatus`) são gravados como texto: `Draft`, `Submitted`, `Approved`, `Rejected` e `Paid`.
- Todos os instantes são gravados e devolvidos em UTC.
- Proprietário, ator do histórico e responsável pelo pagamento são chaves estrangeiras para o usuário do Identity.

## Autenticação e conta Admin

### Roles

`Admin`, `Employee`, `Approver`, `Finance` e `Auditor`. O seed cria as que faltarem ao iniciar a aplicação; nenhuma outra role é criada.

### Conta Admin inicial

O seed cria **uma única** conta Admin, vinculada à role `Admin`, e nenhum outro usuário. Ele pode ser executado várias vezes sem duplicar roles nem usuários: se já existir um usuário com a role Admin, nada é criado.

- O e-mail vem de `Seed:AdminEmail` no `appsettings.json` (`admin@expensehub.local`).
- A senha **não fica no repositório**. Configure com User Secrets ou com a variável de ambiente `Seed__AdminPassword`. Ela segue a política padrão do Identity: pelo menos 6 caracteres, com letra maiúscula, minúscula, número e símbolo.
- Sem senha configurada, a API inicia, registra um aviso no log e não cria o Admin.
- A conta Admin só é criada uma vez. Trocar o secret depois não altera a senha já gravada; para recriar, apague o arquivo `expensehub.db` e inicie a API de novo.

### Login

```http
POST /login
Content-Type: application/json

{ "email": "admin@expensehub.local", "password": "<sua-senha>" }
```

A resposta traz `accessToken`, válido por 1 hora. Envie-o nas rotas protegidas no cabeçalho `Authorization: Bearer <accessToken>`.

### Respostas de erro

Os erros usam `ProblemDetails`:

| Status | Quando |
|---|---|
| 400 | Entrada inválida |
| 401 | Sem token, token inválido, expirado ou emitido antes de uma alteração de roles; credenciais erradas no login |
| 403 | Autenticado, mas sem permissão para a operação |
| 404 | Recurso inexistente ou fora do escopo de leitura |
| 409 | Transição de estado incompatível ou repetida |

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

Enquanto não receber roles do Admin, o usuário consegue fazer login, mas recebe `403` nas operações protegidas.

### Rotas do Admin

Exigem token de um usuário com a role `Admin`: sem token, `401`; com token sem Admin, `403`.

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
- Só as cinco roles conhecidas são aceitas, escritas exatamente como acima. Uma role desconhecida responde `400` e nada é alterado; nenhuma role nova é criada.
- O Admin não pode remover a própria role Admin: a requisição responde `400` e nada é alterado.
- Usuário inexistente responde `404`.

> **Novo login obrigatório:** depois de uma alteração de roles, os tokens emitidos antes dela passam a receber `401`. O usuário precisa fazer login de novo para receber um token com as roles atuais.

## Reembolsos

Exigem token: sem token, `401`; com token sem a role exigida, `403`. Dono, estado, ator e horários são sempre definidos pelo servidor.

| Método | Rota | Role | Descrição |
|---|---|---|---|
| POST | `/api/expenses` | Employee | Cria um rascunho (`Draft`) do usuário autenticado; responde `201` |
| PUT | `/api/expenses/{id}` | Employee | Edita o próprio rascunho; responde `200` |
| POST | `/api/expenses/{id}/submit` | Employee | Envia o próprio rascunho (`Draft` para `Submitted`); responde `200` |
| GET | `/api/expenses` | Employee, Approver, Finance, Auditor | Lista os reembolsos visíveis, do mais recente para o mais antigo |
| GET | `/api/expenses/{id}` | Employee, Approver, Finance, Auditor | Consulta um reembolso visível |

```http
POST /api/expenses
Authorization: Bearer <token de um Employee>
Content-Type: application/json

{ "description": "Táxi do aeroporto ao hotel", "amount": 85.50, "expenseDate": "2026-09-20", "categoryId": 1 }
```

- **Validação (`400`):** descrição entre 10 e 500 caracteres; valor entre `0.01` e `2147483647`; data no formato `yyyy-MM-dd`, não futura em relação ao dia corrente em Brasília; `categoryId` opcional, mas precisa existir (1 Transporte, 2 Alimentação, 3 Hospedagem, 4 Outros).
- **Campos do servidor:** qualquer campo além dos quatro acima, como `ownerId`, `status` ou `createdAtUtc`, é recusado com `400`.
- **Edição:** o `PUT` envia o rascunho completo. Reembolso inexistente ou de outro usuário responde `404`; fora de `Draft`, `409`. Se nada mudar, responde `200` sem gravar histórico.
- **Visibilidade:** as roles acumulam e o resultado é a união dos escopos. Employee vê os próprios; Approver, os `Submitted`; Finance, os `Approved` e `Paid`; Auditor, todos. Admin sem outra role não tem acesso (`403`). O filtro é aplicado na consulta ao banco, e um reembolso fora do escopo responde `404`.
- **Envio:** só o dono envia, e só em `Draft`. Reembolso fora do escopo responde `404`; visível mas de outra pessoa, `403`; fora de `Draft` ou já enviado, `409`. O estado é usado como token de concorrência: se duas operações alterarem o mesmo reembolso ao mesmo tempo, a segunda responde `409` e não grava histórico.
- **Histórico:** a criação grava a ação `Created`, cada edição grava `Updated` e o envio grava `Submitted`. A edição registra as alterações no campo `Changes` no formato `campo: antes -> depois` (por exemplo `amount: 85.50 -> 171.00; categoryId: 1 -> null`). A alteração e o histórico são salvos na mesma operação.

## Testes

```shell
dotnet test ./sources/ExpenseHub.slnx
```

Os testes unitários ficam em `sources/ExpenseHub.UnitTests` e rodam sem banco, rede ou serviço externo.

## Processo de desenvolvimento

Cada issue do backlog central é implementada em uma branch própria (por exemplo `i03-user-roles`) e integrada por pull request, que cita a issue como `Racass/checkpoint-csharpracass-expensehub#N` e passa pelo workflow `code-quality`. O processo completo está em [PROCESSO-GITHUB.md](docs/PROCESSO-GITHUB.md).

## Documentação do checkpoint

- [Enunciado](docs/ENUNCIADO.md)
- [Requisitos e contratos](docs/REQUISITOS.md)
- [Rubrica](docs/RUBRICA.md)
- [Matriz de autorização](docs/MATRIZ-AUTORIZACAO.md)
- [Processo no GitHub](docs/PROCESSO-GITHUB.md)
- [Uso de Inteligência Artificial](docs/USO-DE-IA.md)
- [Regras do pipeline de qualidade](docs/code-quality-rules.md)
