// frontend/src/App.tsx (O Componente Raiz / Orquestrador):
// O App.tsx é o componente "pai" de toda a interface. No React, a UI é organizada como uma árvore,
// e o App fica no topo de tudo.

import { Ativos } from './components/Ativos';

function App() {
  return (
    <main>
      <h1>Portfy Dashboard</h1>
      <Ativos />
    </main>
  );
}

export default App;