import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'

// './index.css': É aqui que o CSS global é carregado na aplicação para ser aplicado em toda a árvore de componentes.
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render( // busca o root do html

  // <StrictMode>: Um envoltório exclusivo de desenvolvimento que executa verificações extras para alertar sobre efeitos colaterais
  // indesejados ou chamadas inseguras.
  <StrictMode>
    <App />
  </StrictMode>,
)
