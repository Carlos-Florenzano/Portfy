import { useEffect, useState } from "react";
import "./Dashboard.css";
import { usuarioService } from "./services/UsuarioService";
import type { Usuario } from "./services/UsuarioService";

function Dashboard() {
  const [usuario, setUsuario] = useState<Usuario | null>(null);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");

  // Estados para edição (UPDATE)
  const [editando, setEditando] = useState(false);
  const [novoNome, setNovoNome] = useState("");
  const [novoSalario, setNovoSalario] = useState("");

  useEffect(() => {
    carregarUsuario();
  }, []);

  // 1. READ: Busca o utilizador atual pelo ID salvo no login
  async function carregarUsuario() {
    try {
      setCarregando(true);
      setErro("");

      const dadosArmazenados = localStorage.getItem("usuarioLogado");
      const usuarioLogado = dadosArmazenados ? JSON.parse(dadosArmazenados) : null;

      if (!usuarioLogado?.id) {
        throw new Error("Nenhum usuário logado. Faça login novamente.");
      }

      const usuarioApi = await usuarioService.obterPorId(usuarioLogado.id);
      setUsuario(usuarioApi);
      setNovoNome(usuarioApi.nome);
      setNovoSalario(usuarioApi.salarioMensal.toString());
    } catch (error: any) {
      setErro("Não foi possível carregar os dados do usuário.");
      console.error(error);
    } finally {
      setCarregando(false);
    }
  }

  // 2. UPDATE: Atualiza nome e salário do utilizador
  async function salvarEdicao() {
    if (!usuario) return;

    try {
      await usuarioService.atualizar(usuario.id, {
        nome: novoNome,
        salarioMensal: Number(novoSalario) || 0
      });

      // Atualiza o estado local e fecha o modo de edição
      setUsuario({
        ...usuario,
        nome: novoNome,
        salarioMensal: Number(novoSalario) || 0
      });
      setEditando(false);
      alert("Dados atualizados com sucesso!");
    } catch (err) {
      alert("Erro ao atualizar os dados.");
      console.error(err);
    }
  }

  // 3. DELETE: Remove a conta do utilizador
  async function encerrarConta() {
    if (!usuario) return;

    const confirmou = window.confirm("Atenção: deseja realmente excluir sua conta? Esta ação não pode ser desfeita.");
    if (!confirmou) return;

    try {
      await usuarioService.excluir(usuario.id);
      localStorage.removeItem("usuarioLogado");
      alert("Conta excluída com sucesso.");
      window.location.reload(); // Redireciona de volta para a tela de login
    } catch (err) {
      alert("Erro ao excluir conta.");
      console.error(err);
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
        <button onClick={carregarUsuario}>Tentar novamente</button>
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
          <a className="menu-item active" href="#"><span>⌂</span> Dashboard</a>
          <a className="menu-item" href="#"><span>💰</span> Salário e Orçamento</a>
          <a className="menu-item" href="#"><span>📈</span> Investimentos</a>
          <a className="menu-item" href="#"><span>💵</span> Aportes</a>
          <a className="menu-item" href="#"><span>📊</span> Relatórios</a>
        </nav>

        <div className="sidebar-footer">
          <button 
            onClick={encerrarConta}
            style={{ background: "transparent", border: "none", color: "#e63946", cursor: "pointer", fontSize: "0.85rem" }}
          >
            Excluir Conta
          </button>
        </div>
      </aside>

      {/* CONTEÚDO PRINCIPAL */}
      <main className="main">
        {/* CABEÇALHO */}
        <header className="header">
          <div>
            <p className="header-subtitle">PAINEL FINANCEIRO</p>
            <h1>Olá, {usuario.nome}</h1>
            <p className="header-description">Acompanhe sua vida financeira em um só lugar.</p>
          </div>

          <div className="profile">
            <div className="profile-avatar">
              {usuario.nome ? usuario.nome.charAt(0).toUpperCase() : "U"}
            </div>

            <div>
              <strong>{usuario.nome}</strong>
              <small style={{ display: "block" }}>{usuario.login}</small>
              <button 
                onClick={() => setEditando(!editando)}
                style={{ background: "none", border: "none", color: "#007acc", cursor: "pointer", fontSize: "0.8rem", padding: 0 }}
              >
                {editando ? "Cancelar Edição" : "Editar Perfil"}
              </button>
            </div>
          </div>
        </header>

        {/* FORMULÁRIO DE EDIÇÃO DO USUÁRIO (UPDATE) */}
        {editando && (
          <section className="card card-edicao">
            <div className="card-header">
              <div>
                <span className="card-titulo-edicao">Configurações de Perfil</span>
                <h3 className="card-subtitulo-edicao">Editar Dados Cadastrais</h3>
              </div>
            </div>

            <div className="form-edicao-grid">
              <div className="campo-edicao">
                <label>Nome Completo</label>
                <input
                  type="text"
                  placeholder="Seu nome"
                  value={novoNome}
                  onChange={(e) => setNovoNome(e.target.value)}
                />
              </div>

              <div className="campo-edicao">
                <label>Salário Mensal (R$)</label>
                <input
                  type="number"
                  step="0.01"
                  placeholder="Ex: 2500"
                  value={novoSalario}
                  onChange={(e) => setNovoSalario(e.target.value)}
                />
              </div>

              <div className="acoes-edicao">
                <button type="button" onClick={salvarEdicao} className="btn-salvar">
                  Salvar Alterações
                </button>
              </div>
            </div>
          </section>
        )}

        {/* CARDS PRINCIPAIS */}
        <section className="cards">
          <div className="card">
            <div className="card-header">
              <span>Salário Mensal</span>
              <span className="card-icon">💼</span>
            </div>
            <h2>{formatarMoeda(usuario.salarioMensal)}</h2>
            <p className="card-description">Renda mensal cadastrada</p>
          </div>

          <div className="card">
            <div className="card-header">
              <span>Saldo Disponível</span>
              <span className="card-icon">💰</span>
            </div>
            <h2>R$ 0,00</h2>
            <p className="card-description">Aguardando dados da carteira</p>
          </div>

          <div className="card">
            <div className="card-header">
              <span>Patrimônio Total</span>
              <span className="card-icon">📊</span>
            </div>
            <h2>R$ 0,00</h2>
            <p className="card-description">Aguardando dados da carteira</p>
          </div>

          <div className="card">
            <div className="card-header">
              <span>Total Investido</span>
              <span className="card-icon">📈</span>
            </div>
            <h2>R$ 0,00</h2>
            <p className="card-description">Aguardando dados dos investimentos</p>
          </div>
        </section>

        {/* ÁREA INFERIOR */}
        <section className="dashboard-grid">
          <div className="panel">
            <div className="panel-header">
              <div>
                <h2>Orçamentos</h2>
                <p>Acompanhe suas categorias de gastos</p>
              </div>
              <button className="panel-button">Ver todos</button>
            </div>
            <div className="empty-state">
              <div className="empty-icon">💳</div>
              <h3>Nenhum orçamento carregado</h3>
              <p>Os orçamentos serão exibidos aqui quando o backend disponibilizar esses dados.</p>
            </div>
          </div>

          <div className="panel">
            <div className="panel-header">
              <div>
                <h2>Investimentos</h2>
                <p>Posições atuais da carteira</p>
              </div>
              <button className="panel-button">Ver carteira</button>
            </div>
            <div className="empty-state">
              <div className="empty-icon">📈</div>
              <h3>Nenhum investimento carregado</h3>
              <p>As posições da carteira serão exibidas aqui.</p>
            </div>
          </div>
        </section>

        {/* APORTES */}
        <section className="panel aportes-panel">
          <div className="panel-header">
            <div>
              <h2>Últimos Aportes</h2>
              <p>Histórico dos aportes realizados</p>
            </div>
            <button className="panel-button">Ver histórico</button>
          </div>
          <div className="empty-state">
            <div className="empty-icon">💵</div>
            <h3>Nenhum aporte registrado</h3>
            <p>Quando os aportes estiverem disponíveis na API, eles aparecerão nesta área.</p>
          </div>
        </section>
      </main>
    </div>
  );
}

export default Dashboard;