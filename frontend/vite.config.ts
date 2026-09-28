// Esta linha importa o plugin oficial do React para o Vite, que ensina ao Vite exatamente como traduzir a sintaxe do 
// React (<div ...>) para JavaScript puro e como ativar o Hot Module Replacement (HMR).
import react from '@vitejs/plugin-react'

// Importa a função helper defineConfig da biblioteca do Vite.
// Em vez de exportar um objeto JS simples, usar defineConfig({...}) oferece autocompletar e validação de tipos no VS Code.
// Ao digitar propriedades dentro dele, o editor sugere automaticamente todas as opções válidas suportadas pelo Vite.
import { defineConfig } from 'vite'

// https://vite.dev/config/
// Exporta a configuração principal que o Vite irá ler ao executar comandos como npm run dev ou npm run build.   
export default defineConfig({
  plugins: [react()], // Aqui registas a lista de plugins ativos no projeto.
})
