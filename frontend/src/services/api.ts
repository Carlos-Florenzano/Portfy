// O api.ts (localizado em frontend/src/services/api.ts) é a camada de abstração HTTP da aplicação.
// Ele atua como o "ramal de comunicação" exclusivo entre a interface do React e a Web API em C#.


// Importa a biblioteca Axios, que é o cliente HTTP responsável por enviar e receber dados pela rede usando
// Promises do JavaScript (async/await ou .then()).
import axios from 'axios';

// Instância apontando para a porta da API C# (.NET)
// Em vez de chamar o axios de forma genérica em vários arquivos do projeto,
// a função .create() instancia um cliente HTTP customizado e reutilizável.
// A palavra-chave export expõe esse cliente configurado para o restante da aplicação.
// Qualquer componente que precise buscar ou enviar dados para o backend só precisa fazer:
// import { api } from '../services/api';
export const api = axios.create({
  baseURL: 'http://localhost:5000/api', // Define o endereço base onde a API em C# está rodando.
});