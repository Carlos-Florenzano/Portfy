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

    function entrar(event: React.FormEvent) {
        event.preventDefault();

        if (!email || !senha) {
            setErro("Preencha o e-mail e a senha.");
            return;
        }

        setErro("");

        // Por enquanto, apenas entra no Dashboard.
        // Depois vamos conectar ao backend C#.
        onLogin();
    }

    return (
        <div className="login-container">
            <div className="login-card">

                <div className="login-logo">
                    <h1>PORTFY</h1>
                    <p>Gestão financeira e investimentos</p>
                </div>

                <form onSubmit={entrar}>

                    <div className="campo">
                        <label htmlFor="email">
                            E-mail
                        </label>

                        <input
                            id="email"
                            type="email"
                            placeholder="Digite seu e-mail"
                            value={email}
                            onChange={(event) =>
                                setEmail(event.target.value)
                            }
                        />
                    </div>

                    <div className="campo">
                        <label htmlFor="senha">
                            Senha
                        </label>

                        <input
                            id="senha"
                            type="password"
                            placeholder="Digite sua senha"
                            value={senha}
                            onChange={(event) =>
                                setSenha(event.target.value)
                            }
                        />
                    </div>

                    {erro && (
                        <p className="mensagem-erro">
                            {erro}
                        </p>
                    )}

                    <button type="submit">
                        Entrar
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