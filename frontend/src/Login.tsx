import { useState } from "react";
import "./Login.css";
import { mensagemDeErro } from "./services/api";
import { salvarSessao } from "./services/sessao";
import { usuarioService } from "./services/UsuarioService";

interface LoginProps {
    onLogin: () => void;
    onCadastro: () => void;
    aviso?: string;
    erroInicial?: string;
}

function Login({ onLogin, onCadastro, aviso, erroInicial }: LoginProps) {
    const [email, setEmail] = useState("");
    const [senha, setSenha] = useState("");
    const [erro, setErro] = useState(erroInicial ?? "");
    const [carregando, setCarregando] = useState(false);

    async function entrar(event: React.FormEvent) {
        event.preventDefault();

        if (!email || !senha) {
            setErro("Preencha o e-mail e a senha.");
            return;
        }

        try {
            setCarregando(true);
            setErro("");

            const dados = await usuarioService.login({
                login: email,
                senha: senha
            });

            // O token é a credencial; os dados do usuário são só cache de UI.
            salvarSessao({
                token: dados.token,
                expiraEm: dados.expiraEm,
                usuario: {
                    id: dados.usuario.id,
                    nome: dados.usuario.nome,
                    login: dados.usuario.login
                }
            });

            onLogin();
        } catch (falha: unknown) {
            // A mensagem já vem traduzida pelo interceptor de services/api.ts
            setErro(mensagemDeErro(falha));
        } finally {
            setCarregando(false);
        }
    }

    return (
        <div className="login-container">
            <div className="login-card">
                <div className="login-logo">
                    <h1><span>PORT</span>FY</h1>
                    <p>Gestão financeira e investimentos</p>
                </div>

                <form onSubmit={entrar}>
                    <div className="campo">
                        <label htmlFor="email">E-mail</label>
                        <input
                            id="email"
                            type="email"
                            placeholder="Digite seu e-mail"
                            value={email}
                            onChange={(event) => setEmail(event.target.value)}
                        />
                    </div>

                    <div className="campo">
                        <label htmlFor="senha">Senha</label>
                        <input
                            id="senha"
                            type="password"
                            placeholder="Digite sua senha"
                            value={senha}
                            onChange={(event) => setSenha(event.target.value)}
                        />
                    </div>

                    {aviso && !erro && <p className="mensagem-sucesso">{aviso}</p>}
                    {erro && <p className="mensagem-erro">{erro}</p>}

                    <button type="submit" disabled={carregando}>
                        {carregando ? "Entrando..." : "Entrar"}
                    </button>
                </form>

                <p className="cadastro">
                    Ainda não possui uma conta?
                    <span onClick={onCadastro}>Criar conta</span>
                </p>
            </div>
        </div>
    );
}

export default Login;
