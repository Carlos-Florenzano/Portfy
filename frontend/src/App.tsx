import { useState } from "react";
import Login from "./Login";
import Cadastro from "./Cadastro";
import Dashboard from "./Dashboard";

function App() {
    const [tela, setTela] = useState<"login" | "cadastro" | "dashboard">(
        "login"
    );

    if (tela === "cadastro") {
        return (
            <Cadastro
                onVoltar={() => setTela("login")}
            />
        );
    }

    if (tela === "dashboard") {
        return <Dashboard />;
    }

    return (
        <Login
            onLogin={() => setTela("dashboard")}
            onCadastro={() => setTela("cadastro")}
        />
    );
}

export default App;