import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist', 'dev-dist', 'coverage', 'playwright-report', 'test-results']),
  {
    // Legacy JavaScript / JSX app code.
    files: ['**/*.{js,jsx}'],
    extends: [
      js.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
      parserOptions: {
        ecmaVersion: 'latest',
        ecmaFeatures: { jsx: true },
        sourceType: 'module',
      },
    },
    rules: {
      'no-unused-vars': ['error', { varsIgnorePattern: '^[A-Z_]' }],
    },
  },
  {
    // TypeScript / TSX (migrated + new code). Type-aware-light: uses the
    // typescript-eslint parser + recommended rules without a full type-check
    // project (keeps lint fast). `npm run typecheck` covers type correctness.
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2022,
      globals: globals.browser,
      parserOptions: {
        ecmaVersion: 'latest',
        ecmaFeatures: { jsx: true },
        sourceType: 'module',
      },
    },
    rules: {
      // Keep the existing convention: ignore intentionally-unused PascalCase/_CONST.
      'no-unused-vars': 'off',
      '@typescript-eslint/no-unused-vars': [
        'error',
        { varsIgnorePattern: '^[A-Z_]', argsIgnorePattern: '^_' },
      ],
    },
  },
  {
    // Architecture guard (Phase 13.2): pages / features / layouts consume UI
    // ONLY through the `src/shared/*` public facade — never directly from the
    // `src/components/*` implementation layer. This keeps `src/components`
    // internal and prevents new direct-import regressions. Test files are exempt
    // (they legitimately test/wrap the implementation). See
    // docs/frontend/phase13/SHARED_UI_ADOPTION_MAP.md.
    files: [
      'src/pages/**/*.{js,jsx,ts,tsx}',
      'src/features/**/*.{js,jsx,ts,tsx}',
      'src/layouts/**/*.{js,jsx,ts,tsx}',
    ],
    ignores: ['**/*.test.{js,jsx,ts,tsx}'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['**/components/*', '**/components/**', '*/components/*'],
              message:
                'Import UI through src/shared/* (the public facade), not directly from src/components/*. See docs/frontend/phase13/SHARED_UI_ADOPTION_MAP.md',
            },
          ],
        },
      ],
    },
  },
  {
    // Test files (vitest) + setup get vitest + node globals.
    files: ['**/*.test.{js,jsx,ts,tsx}', 'src/test/**/*.{js,jsx,ts,tsx}'],
    languageOptions: {
      globals: { ...globals.node, ...globals.vitest },
    },
    rules: {
      'react-refresh/only-export-components': 'off',
    },
  },
  {
    // Playwright E2E + config/tooling files run in node.
    files: ['e2e/**/*.{js,jsx,ts,tsx}', '*.config.{js,ts}', 'playwright.config.{js,ts}'],
    languageOptions: {
      globals: { ...globals.node },
    },
    rules: {
      'react-refresh/only-export-components': 'off',
    },
  },
])
