using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Portfy.Data;
using Portfy.Dtos.Usuario;
using Portfy.Filters;
using Portfy.Middleware;
using Portfy.Services;
using Portfy.Services.Contracts;

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

// Injeção de dependência: cada controller depende de uma interface do namespace
// Portfy.Services.Contracts, nunca de uma instância concreta. Todo serviço que usa
// o AppDbContext é Scoped, acompanhando o ciclo de vida da requisição; o gerador
// de variação e o hasher de senha, sem estado e sem banco, são Singleton.
builder.Services.AddSingleton<IGeradorDeVariacao, GeradorDeVariacaoAleatoria>();
builder.Services.AddSingleton<ISenhaHasher, SenhaHasher>();
builder.Services.AddScoped<IAtivoService, AtivoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ICarteiraService, CarteiraService>();
builder.Services.AddScoped<IOrcamentoService, OrcamentoService>();
builder.Services.AddScoped<IAporteService, AporteService>();

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

app.UseAuthorization();
app.MapControllers();

app.Run();
