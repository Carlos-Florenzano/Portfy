import { useState } from "react";
import "./Login.css";

interface LoginProps {
    onLogin: () => void;
    onCadastro: () => void;
}

function Login({ onLogin, onCadastro }: LoginProps) {
    const [email, setEmail] = useState("");
    const [senha, setSenha] = useState("");
    const [erro, setErro] = useState("");
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

            const resposta = await fetch("http://localhost:5000/api/usuario/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    login: email,
                    senha: senha
                })
            });

            const dados = await resposta.json();

            if (!resposta.ok) {
                setErro(dados.mensagem || dados.Mensagem || "Login ou senha incorretos.");
                return;
            }

            // Salva as credenciais do usuário logado na sessão local
            localStorage.setItem("usuarioLogado", JSON.stringify({
                id: dados.id ?? dados.Id,
                nome: dados.nome ?? dados.Nome,
                login: dados.login ?? dados.Login
            }));

            onLogin();
        } catch (err) {
            setErro("Não foi possível conectar com o backend.");
            console.error(err);
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

                    {erro && (
                        <p className="mensagem-erro">
                            {erro}
                        </p>
                    )}

                    <button type="submit" disabled={carregando}>
                        {carregando ? "Entrando..." : "Entrar"}
                    </button>

                </form>

                <p className="cadastro">
                    Ainda não possui uma conta?
                    <span onClick={onCadastro}>
                        Criar conta
                    </span>
                </p>

            </div>
        </div>
    );
}

export default Login;