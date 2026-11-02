import { useEffect, useState } from "react";
import Login from "./Login";
import Cadastro from "./Cadastro";
import Dashboard from "./Dashboard";
import { EVENTO_SESSAO_EXPIRADA, sessaoValida } from "./services/sessao";

type Tela = "login" | "cadastro" | "dashboard";

function App() {
    // A tela inicial segue a sessão local; a API continua sendo quem valida o token.
    const [tela, setTela] = useState<Tela>(() => (sessaoValida() ? "dashboard" : "login"));

    // Aviso de sucesso que atravessa a troca de tela (ex.: cadastro -> login).
    const [aviso, setAviso] = useState("");

    // Mensagem exibida no login quando o backend recusa o token (401).
    const [erroSessao, setErroSessao] = useState("");

    useEffect(() => {
        function aoExpirarSessao() {
            setAviso("");
            setErroSessao("Sua sessão expirou. Entre novamente.");
            setTela("login");
        }

        window.addEventListener(EVENTO_SESSAO_EXPIRADA, aoExpirarSessao);

        return () => window.removeEventListener(EVENTO_SESSAO_EXPIRADA, aoExpirarSessao);
    }, []);

    if (tela === "cadastro") {
        return (
            <Cadastro
                onVoltar={() => {
                    setAviso("");
                    setTela("login");
                }}
                onCadastrado={(mensagem) => {
                    setAviso(mensagem);
                    setTela("login");
                }}
            />
        );
    }

    if (tela === "dashboard") {
        return <Dashboard />;
    }

    return (
        <Login
            aviso={aviso}
            erroInicial={erroSessao}
            onLogin={() => {
                setAviso("");
                setErroSessao("");
                setTela("dashboard");
            }}
            onCadastro={() => {
                setAviso("");
                setErroSessao("");
                setTela("cadastro");
            }}
        />
    );
}

export default App;
