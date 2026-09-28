import { useState } from "react";
import "./Cadastro.css";

interface CadastroProps {
    onVoltar: () => void;
}

function Cadastro({ onVoltar }: CadastroProps) {
    const [nome, setNome] = useState("");
    const [email, setEmail] = useState("");
    const [salario, setSalario] = useState("");
    const [senha, setSenha] = useState("");
    const [confirmarSenha, setConfirmarSenha] = useState("");
    const [erro, setErro] = useState("");
    const [carregando, setCarregando] = useState(false);

    async function cadastrar(event: React.FormEvent) {
        event.preventDefault();

        if (!nome || !email || !senha || !confirmarSenha) {
            setErro("Preencha todos os campos obrigatórios.");
            return;
        }

        if (senha !== confirmarSenha) {
            setErro("As senhas não são iguais.");
            return;
        }

        try {
            setCarregando(true);
            setErro("");

            // 1. Cadastra o usuário no C#
            const resposta = await fetch("http://localhost:5000/api/usuario/cadastro", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    nome: nome,
                    login: email,
                    senha: senha,
                    salarioMensal: Number(salario) || 0
                })
            });

            const dados = await resposta.json();

            if (!resposta.ok) {
                setErro(dados.mensagem || dados.Mensagem || "Erro ao cadastrar.");
                return;
            }

            // 2. No UsuarioController, o login exige conta verificada.
            // Ativamos a conta para permitir o login direto.
            if (dados.id || dados.Id) {
                const idUsuario = dados.id ?? dados.Id;
                await fetch(`http://localhost:5000/api/usuario/${idUsuario}/verificar`, {
                    method: "POST"
                });
            }

            alert("Cadastro realizado com sucesso! Faça login para entrar.");
            onVoltar();
        } catch (err) {
            setErro("Não foi possível conectar com o backend.");
            console.error(err);
        } finally {
            setCarregando(false);
        }
    }

    return (
        <div className="cadastro-container">
            <div className="cadastro-card">

                <div className="cadastro-logo">
                    <h1><span>PORT</span>FY</h1>
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
                        <label htmlFor="email">E-mail (Login)</label>
                        <input
                            id="email"
                            type="email"
                            placeholder="Digite seu e-mail"
                            value={email}
                            onChange={(event) => setEmail(event.target.value)}
                        />
                    </div>

                    <div className="campo">
                        <label htmlFor="salario">Salário Mensal (R$)</label>
                        <input
                            id="salario"
                            type="number"
                            placeholder="Ex: 3500"
                            value={salario}
                            onChange={(event) => setSalario(event.target.value)}
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
                        <label htmlFor="confirmarSenha">Confirmar senha</label>
                        <input
                            id="confirmarSenha"
                            type="password"
                            placeholder="Confirme sua senha"
                            value={confirmarSenha}
                            onChange={(event) => setConfirmarSenha(event.target.value)}
                        />
                    </div>

                    {erro && (
                        <p className="mensagem-erro">
                            {erro}
                        </p>
                    )}

                    <button type="submit" disabled={carregando}>
                        {carregando ? "Cadastrando..." : "Criar conta"}
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