import { useState } from "react";
import Login from "./Login";
import Cadastro from "./Cadastro";
import Dashboard from "./Dashboard";

function App() {
    const [tela, setTela] = useState<"login" | "cadastro" | "dashboard">(
        "login"
    );

    // Aviso de sucesso que atravessa a troca de tela (ex.: cadastro -> login).
    const [aviso, setAviso] = useState("");

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
            onLogin={() => {
                setAviso("");
                setTela("dashboard");
            }}
            onCadastro={() => {
                setAviso("");
                setTela("cadastro");
            }}
        />
    );
}

export default App;
