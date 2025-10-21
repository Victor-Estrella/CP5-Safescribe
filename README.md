# SafeScribe

API ASP.NET Core para gestao de notas seguras com autenticacao JWT, controle de roles e blacklist de tokens para logout.

## Integrantes
- Eduarda Tiemi Akamini Machado - RM: 554756 
- Victor Henrique Estrella Carracci - RM: 556206

## Principais funcionalidades
- Registro, login e logout com tokens JWT que expiram em 1 hora.
- Controle de acesso por roles (`Leitor`, `Editor`, `Admin`) aplicado no pipeline de autorizacao.
- CRUD de notas em memoria com validacoes para proprietarios e administradores.
- Associacao automatica do `UserId` a partir da claim do token JWT, impedindo forjar notas para outros utilizadores.
- Middleware que consulta blacklist de tokens antes de autorizar cada request.
- Documentacao interactiva via Swagger em `/swagger`.

## Arquitetura em alto nivel
- `Program.cs` configura CORS, autenticacao JWT, autorizacao, Swagger e injecao de dependencias.
- `AuthController` expõe endpoints de autenticacao e interage com `ITokenService` para gerenciar utilizadores.
- `NotasController` concentra regras de negocio de notas sobre `INoteService`.
- `TokenService` guarda utilizadores em memoria, gera tokens e aplica hash de senha com BCrypt.
- `InMemoryTokenBlacklistService` preserva JTIs invalidados pelo logout ate o vencimento do token.

## Requisitos
- .NET SDK 9.0 ou superior.
- Opcional: ferramenta REST (curl, Thunder Client, Postman) para exercitar a API.

## Executando o projeto
1. Restaure dependencias: `dotnet restore`.
2. Rode a API: `dotnet run --project SafeScribe.csproj --launch-profile https`.
3. A API escuta por padrao em `https://localhost:7202` (HTTP em `http://localhost:5006`).
4. Acesse `https://localhost:7202/swagger` para explorar e testar endpoints com JWT.

## Fluxo de uso sugerido
1. `POST /api/v1/auth/registrar` cria utilizador novo informando `username`, `password` e `role` (padrão `Leitor`).
2. `POST /api/v1/auth/login` devolve o token JWT; copie o valor retornado.
3. Inclua o header `Authorization: Bearer {token}` nas chamadas seguintes.
4. Utilize `POST /api/v1/notas` para criar notas (role `Editor` ou `Admin`).
5. `GET /api/v1/notas/{id}` permite que proprietario ou administrador recupere a nota.
6. `PUT /api/v1/notas/{id}` atualiza notas (role `Editor` ou `Admin` e dono ou admin).
7. `DELETE /api/v1/notas/{id}` remove notas (somente `Admin`).
8. `POST /api/v1/auth/logout` invalida o token atual inserindo o JTI na blacklist.

## Matriz de endpoints
| Metodo | Rota | Autorizacao | Descricao |
|--------|------|-------------|-----------|
| POST | `/api/v1/auth/registrar` | Publico | Registra utilizador em memoria |
| POST | `/api/v1/auth/login` | Publico | Autentica e retorna token JWT |
| POST | `/api/v1/auth/logout` | Qualquer utilizador autenticado | Invalida token atual |
| POST | `/api/v1/notas` | Roles `Editor`, `Admin` | Cria nota vinculada ao utilizador autenticado |
| GET | `/api/v1/notas/{id}` | Qualquer autenticado | Consulta nota se for dono ou admin |
| PUT | `/api/v1/notas/{id}` | Roles `Editor`, `Admin` | Atualiza nota se for dono ou admin |
| DELETE | `/api/v1/notas/{id}` | Role `Admin` | Remove nota |

## Consideracoes sobre seguranca
- Utilize chave JWT unica por ambiente e rotacione-a periodicamente.
- Ative HTTPS em producao e restrinja origens CORS conforme necessidade.
- As credenciais ficam so em memoria; avalie persistencia real e protecao por hash forte.
