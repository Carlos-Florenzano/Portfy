namespace Portfy.Services.Contracts;

/// Par (token, instante de expiração) devolvido pelo IAuthService. A expiração
/// também vai assinada dentro do token; aqui ela é só o valor para o cliente.
public sealed record TokenAcesso(string Token, DateTime ExpiraEm);
