import { useEffect, useState } from "react";
import "./Dashboard.css";

interface Usuario {
  id: number;
  nome: string;
  salarioMensal: number;
}

function Dashboard() {
  // para teste depois vou apagar console.log("Dashboard foi carregado!"); 
  const [usuario, setUsuario] = useState<Usuario | null>(null);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");

  useEffect(() => {
    carregarUsuario();
  }, []);

  async function carregarUsuario() {
    try {
      setCarregando(true);
      setErro("");

      const resposta = await fetch("http://localhost:5000/api/usuario");

      if (!resposta.ok) {
        throw new Error("Não foi possível carregar os dados do usuário.");
      }

      const dados = await resposta.json();

      // Como o backend retorna uma lista de usuários, pegamos o mais recente ou o primeiro
      let usuarioEncontrado = null;
      if (Array.isArray(dados) && dados.length > 0) {
        usuarioEncontrado = dados[dados.length - 1];
      } else if (!Array.isArray(dados) && dados) {
        usuarioEncontrado = dados;
      }

      if (!usuarioEncontrado) {
        // Caso a lista de usuários no backend ainda esteja vazia
        setUsuario({
          id: 1,
          nome: "Investidor",
          salarioMensal: 0,
        });
        return;
      }

      const usuarioFormatado: Usuario = {
        id: usuarioEncontrado.id ?? usuarioEncontrado.Id ?? 1,
        nome: usuarioEncontrado.nome ?? usuarioEncontrado.Nome ?? "Investidor",
        salarioMensal: Number(usuarioEncontrado.salarioMensal ?? usuarioEncontrado.SalarioMensal ?? 0),
      };

      setUsuario(usuarioFormatado);
    } catch (error) {
      setErro("Não foi possível conectar com o backend. Verifique se a API está rodando na porta 5000.");
      console.error(error);
    } finally {
      setCarregando(false);
    }
  }

  function formatarMoeda(valor: number) {
    return (valor || 0).toLocaleString("pt-BR", {
      style: "currency",
      currency: "BRL",
    });
  }

  if (carregando) {
    return (
      <div className="dashboard-loading">
        <p>Carregando Portfy...</p>
      </div>
    );
  }

  if (erro || !usuario) {
    return (
      <div className="dashboard-error">
        <h2>Erro ao carregar o Portfy</h2>
        <p>{erro}</p>

        <button onClick={carregarUsuario}>
          Tentar novamente
        </button>
      </div>
    );
  }

  return (
    <div className="portfy">

      {/* MENU LATERAL */}
      <aside className="sidebar">

        <div className="logo">
          <span>PORT</span>FY
        </div>

        <nav className="menu">

          <a className="menu-item active" href="#">
            <span>⌂</span>
            Dashboard
          </a>

          <a className="menu-item" href="#">
            <span>💰</span>
            Salário e Orçamento
          </a>

          <a className="menu-item" href="#">
            <span>📈</span>
            Investimentos
          </a>

          <a className="menu-item" href="#">
            <span>💵</span>
            Aportes
          </a>

          <a className="menu-item" href="#">
            <span>📊</span>
            Relatórios
          </a>

        </nav>

        <div className="sidebar-footer">
          <span>Portfy</span>
          <small>Gestão financeira</small>
        </div>

      </aside>


      {/* CONTEÚDO PRINCIPAL */}
      <main className="main">

        {/* CABEÇALHO */}
        <header className="header">

          <div>
            <p className="header-subtitle">
              PAINEL FINANCEIRO
            </p>

            <h1>
              Olá, {usuario.nome}
            </h1>

            <p className="header-description">
              Acompanhe sua vida financeira em um só lugar.
            </p>
          </div>

          <div className="profile">
            <div className="profile-avatar">
              {usuario.nome ? usuario.nome.charAt(0).toUpperCase() : "U"}
            </div>

            <div>
              <strong>{usuario.nome}</strong>
              <small>Investidor</small>
            </div>
          </div>

        </header>


        {/* CARDS PRINCIPAIS */}
        <section className="cards">

          <div className="card">
            <div className="card-header">
              <span>Salário Mensal</span>
              <span className="card-icon">💼</span>
            </div>

            <h2>
              {formatarMoeda(usuario.salarioMensal)}
            </h2>

            <p className="card-description">
              Renda mensal cadastrada
            </p>
          </div>


          <div className="card">
            <div className="card-header">
              <span>Saldo Disponível</span>
              <span className="card-icon">💰</span>
            </div>

            <h2>
              R$ 0,00
            </h2>

            <p className="card-description">
              Aguardando dados da carteira
            </p>
          </div>


          <div className="card">
            <div className="card-header">
              <span>Patrimônio Total</span>
              <span className="card-icon">📊</span>
            </div>

            <h2>
              R$ 0,00
            </h2>

            <p className="card-description">
              Aguardando dados da carteira
            </p>
          </div>


          <div className="card">
            <div className="card-header">
              <span>Total Investido</span>
              <span className="card-icon">📈</span>
            </div>

            <h2>
              R$ 0,00
            </h2>

            <p className="card-description">
              Aguardando dados dos investimentos
            </p>
          </div>

        </section>


        {/* ÁREA INFERIOR */}
        <section className="dashboard-grid">

          {/* ORÇAMENTOS */}
          <div className="panel">

            <div className="panel-header">
              <div>
                <h2>Orçamentos</h2>
                <p>
                  Acompanhe suas categorias de gastos
                </p>
              </div>

              <button className="panel-button">
                Ver todos
              </button>
            </div>

            <div className="empty-state">

              <div className="empty-icon">
                💳
              </div>

              <h3>
                Nenhum orçamento carregado
              </h3>

              <p>
                Os orçamentos serão exibidos aqui
                quando o backend disponibilizar esses dados.
              </p>

            </div>

          </div>


          {/* INVESTIMENTOS */}
          <div className="panel">

            <div className="panel-header">
              <div>
                <h2>Investimentos</h2>
                <p>
                  Posições atuais da carteira
                </p>
              </div>

              <button className="panel-button">
                Ver carteira
              </button>
            </div>

            <div className="empty-state">

              <div className="empty-icon">
                📈
              </div>

              <h3>
                Nenhum investimento carregado
              </h3>

              <p>
                As posições da carteira serão
                exibidas aqui.
              </p>

            </div>

          </div>

        </section>


        {/* APORTES */}
        <section className="panel aportes-panel">

          <div className="panel-header">

            <div>
              <h2>Últimos Aportes</h2>

              <p>
                Histórico dos aportes realizados
              </p>
            </div>

            <button className="panel-button">
              Ver histórico
            </button>

          </div>


          <div className="empty-state">

            <div className="empty-icon">
              💵
            </div>

            <h3>
              Nenhum aporte registrado
            </h3>

            <p>
              Quando os aportes estiverem disponíveis
              na API, eles aparecerão nesta área.
            </p>

          </div>

        </section>

      </main>

    </div>
  );
}

export default Dashboard;