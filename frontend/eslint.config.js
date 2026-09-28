import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([

  // Diz ao ESLint para ignorar a pasta dist/ (o código compilado final), pois não precisamos
  // analisar ficheiros gerados automaticamente.
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [ // Herda conjuntos de regras pré-definidos da comunidade (boas práticas padrão do JS e do TypeScript).
      js.configs.recommended,
      tseslint.configs.recommended,

      // Garante que estás a cumprir as regras estritas dos Hooks do React
      // (por exemplo, não chamar useState dentro de um if ou for).
      reactHooks.configs.flat.recommended,

      // Garante que os ficheiros .tsx exportem apenas componentes React, para que o Hot Module Replacement do Vite
      // funcione sem quebras de estado.
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      
      // Informa que variáveis globais do navegador (como window, document, fetch) são válidas e não devem
      // ser marcadas como erro de "variável não declarada".
      globals: globals.browser,
    },
  },
])
