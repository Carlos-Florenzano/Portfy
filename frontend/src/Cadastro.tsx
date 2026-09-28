import { useState } from "react";
import "./Cadastro.css";

interface CadastroProps {
    onVoltar: () => void;
}

function Cadastro({ onVoltar }: CadastroProps) {
    const [nome, setNome] = useState("");
    const [email, setEmail] = useState("");
    const [senha, setSenha] = useState("");
    const [confirmarSenha, setConfirmarSenha] = useState("");
    const [erro, setErro] = useState("");

    function cadastrar(event: React.FormEvent) {
        event.preventDefault();

        if (!nome || !email || !senha || !confirmarSenha) {
            setErro("Preencha todos os campos.");
            return;
        }

        if (senha !== confirmarSenha) {
            setErro("As senhas não são iguais.");
            return;
        }

        setErro("");

        // Por enquanto o cadastro é apenas visual.
        // Depois vamos enviar esses dados para o backend C#.
        alert("Cadastro realizado com sucesso!");

        onVoltar();
    }

    return (
        <div className="cadastro-container">
            <div className="cadastro-card">

                <div className="cadastro-logo">
                    <h1>PORTFY</h1>
                    <p>Crie sua conta</p>
                </div>

                <form onSubmit={cadastrar}>

                    <div className="campo">
                        <label htmlFor="nome">Nome</label>

                        <input
                            id="nome"
                            type="text"
                            placeholder="Digite seu nome"
                            value={nome}
                            onChange={(event) => setNome(event.target.value)}
                        />
                    </div>

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

                    <div className="campo">
                        <label htmlFor="confirmarSenha">
                            Confirmar senha
                        </label>

                        <input
                            id="confirmarSenha"
                            type="password"
                            placeholder="Confirme sua senha"
                            value={confirmarSenha}
                            onChange={(event) =>
                                setConfirmarSenha(event.target.value)
                            }
                        />
                    </div>

                    {erro && (
                        <p className="mensagem-erro">
                            {erro}
                        </p>
                    )}

                    <button type="submit">
                        Criar conta
                    </button>

                </form>

                <p className="voltar-login">
                    Já possui uma conta?
                    <span onClick={onVoltar}>
                        Entrar
                    </span>
                </p>

            </div>
        </div>
    );
}

export default Cadastro;