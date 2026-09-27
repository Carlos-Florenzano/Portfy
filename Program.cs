var builder = WebApplication.CreateBuilder(args);



// Configuração dos Controllers e Swagger:

// builder.Services.AddControllers():
// No Backend: Registra no container de injeção de dependência do .NET a capacidade de reconhecer, instanciar
// e mapear todas as classes que herdam de ControllerBase (como o CarteiraController e AporteController).
// Com o Frontend: É essa instrução que permite que o backend escute
// e entenda as requisições HTTP (GET, POST, PUT, DELETE) enviadas pela aplicação React (usando fetch ou axios)
// e devolva as respostas em formato JSON.
builder.Services.AddControllers();

// obs: O Swagger (hoje conhecido oficialmente como OpenAPI)
// é uma ferramenta que cria automaticamente uma documentação interativa para a Web API.
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



// Permite chamadas HTTP vindas do React (Vite)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowReactApp");
app.UseAuthorization();
app.MapControllers();

app.Run();