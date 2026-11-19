using System.Globalization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Portfy.Data;
using Portfy.Dtos.Usuario;
using Portfy.Filters;
using Portfy.Middleware;
using Portfy.Services;
using Portfy.Services.Contracts;
using Portfy.Services.Integracoes;

var builder = WebApplication.CreateBuilder(args);

// A connection string tem uma única origem: ConnectionStrings:DefaultConnection no appsettings.json.
// Se ela não existir, a API falha no startup em vez de usar um caminho mágico.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "A connection string 'DefaultConnection' não está configurada em appsettings.json.");

// AppDbContext (SQLite) registrado como Scoped: uma instância por requisição HTTP.
// O interceptor reaplica os PRAGMAs toda vez que o EF abre uma conexão
// (foreign_keys = ON, journal_mode = WAL, busy_timeout = 5000, synchronous = NORMAL),
// que é o que sustenta requisições concorrentes sem corromper o banco.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)
           .AddInterceptors(new SqlitePragmaInterceptor()));

// Configuração dos Controllers e Swagger:

// builder.Services.AddControllers():
// No Backend: Registra no container de injeção de dependência do .NET a capacidade de reconhecer, instanciar
// e mapear todas as classes que herdam de ControllerBase (como o CarteiraController e AporteController).
// Com o Frontend: É essa instrução que permite que o backend escute
// e entenda as requisições HTTP (GET, POST, PUT, DELETE) enviadas pela aplicação React (usando fetch ou axios)
// e devolva as respostas em formato JSON.
// O filtro registrado aqui executa os validadores FluentValidation de cada DTO
// antes da action e devolve 400 em ProblemDetails (arch §14.1).
builder.Services.AddControllers(options => options.Filters.Add<FluentValidationFilter>());

// Um validador por DTO, descobertos por assembly: sem registro manual por tipo.
builder.Services.AddValidatorsFromAssemblyContaining<CadastroRequestValidator>();

// Autenticação JWT (ADR-010). A chave vem de Auth:Jwt:Chave, nunca do repositório:
// JwtConfiguracao.Ler falha no startup se ela estiver ausente ou curta demais.
JwtConfiguracao jwt = JwtConfiguracao.Ler(builder.Configuration);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<IAuthService, AuthService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Sem mapeamento automático de claims: os controllers leem "sub" diretamente.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emissor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.ChaveDeAssinatura,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

// Rate limiting por IP: 10 tentativas de login por minuto (P1-g) e 60 consultas
// de mercado por minuto (P2-h). O excedente recebe 429 com Retry-After.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(PoliticasDeRateLimit.Login, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    // Cadastro é anônimo e cada chamada executa BCrypt (custo de CPU): sem limite
    // por IP, um cliente satura o servidor com cadastros em massa. Janela própria
    // para não gastar o orçamento de tentativas de login.
    options.AddPolicy(PoliticasDeRateLimit.Cadastro, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    // Rotas de mercado são públicas e cada falha de cache vira chamada a um
    // provedor com cota limitada; o limite por IP protege essa cota.
    options.AddPolicy(PoliticasDeRateLimit.Mercado, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    options.OnRejected = async (contexto, cancellationToken) =>
    {
        if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            contexto.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);
        }

        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Muitas requisições.",
            Detail = "Limite de requisições excedido. Tente novamente em instantes.",
            Instance = contexto.HttpContext.Request.Path,
        };

        await contexto.HttpContext.Response.WriteAsJsonAsync(
            problema,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    };
});

// Injeção de dependência: cada controller depende de uma interface do namespace
// Portfy.Services.Contracts, nunca de uma instância concreta. Todo serviço que usa
// o AppDbContext é Scoped, acompanhando o ciclo de vida da requisição; o gerador
// de variação, o hasher de senha e a emissão de token, sem estado, são Singleton.
int fatorDeTrabalho = builder.Configuration.GetValue(
    "Auth:Senha:FatorDeTrabalho",
    SenhaHasher.FatorDeTrabalhoPadrao);

builder.Services.AddSingleton<IGeradorDeVariacao, GeradorDeVariacaoAleatoria>();
builder.Services.AddSingleton<ISenhaHasher>(new SenhaHasher(fatorDeTrabalho));
builder.Services.AddScoped<IAtivoService, AtivoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ICarteiraService, CarteiraService>();
builder.Services.AddScoped<IOrcamentoService, OrcamentoService>();
builder.Services.AddScoped<IAporteService, AporteService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();

// Fontes de dados de mercado (ADR-020): BRAPI para a B3, FMP para ativos
// globais e fundamentos, Banco Central (SGS) para macroeconomia. As credenciais
// não ficam no repositório: vêm de Integracoes__TokenBrapi e Integracoes__ChaveFmp.
builder.Services.AdicionarIntegracoesDeMercado(builder.Configuration);

// As credenciais dessas APIs viajam na query string. O handler padrão do
// HttpClient registra a URL completa em nível Information, então o filtro
// rebaixa esse log: token não pode aparecer em log (arch §15).
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

// builder.Services.AddEndpointsApiExplorer():
// No Backend: Analisa todo o código do seu projeto e vasculha quais rotas,
// verbos HTTP e parâmetros existem nos seus Controllers.
// Com o Frontend/Swagger: Cria a estrutura de metadados ("o mapa da API") necessária
// para que ferramentas de documentação consigam entender a API sem você precisar escrever arquivos
// de configuração manuais.
builder.Services.AddEndpointsApiExplorer();

// builder.Services.AddSwaggerGen():
// No Backend: Ativa o gerador de especificações OpenAPI/Swagger.
// Ele pega as informações levantadas pelo AddEndpointsApiExplorer e gera o contrato da API em formato JSON/YAML.
// Para a integração: Permite que geradores de código do frontend leiam os tipos de dados do backend e criem interfaces
// TypeScript automaticamente, evitando que você precise digitar os modelos manualmente no React.
builder.Services.AddSwaggerGen();

// Permite chamadas HTTP vindas do React (Vite / Create React App)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

// Cria o portfy.db na primeira execução e aplica as migrations pendentes nas seguintes
// (EnsureCreated é proibido: o esquema só nasce de migration). O seed inicial entra
// uma única vez, então reiniciar a API não duplica nem sobrescreve dados.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// Diagnóstico de operação: diz no log, sem vazar segredo, qual fonte ficou sem
// credencial. Sem essa linha, a falta de chave só apareceria como 503 na primeira
// requisição de um usuário.
app.Services.AvisarFontesDeMercadoNaoConfiguradas();

// Executa a interface visual do Swagger apenas em ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ativa o middleware de CORS com a política definida acima
app.UseCors("AllowReactApp");

// Política única de erro (arch §14.2): exceção de qualquer action vira
// ProblemDetails, então nenhuma action precisa de try/catch.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRateLimiter();

// UseAuthentication antes de UseAuthorization: sem isso o token nunca é validado.
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposto para os testes de integração (WebApplicationFactory<Program>).
public partial class Program;
