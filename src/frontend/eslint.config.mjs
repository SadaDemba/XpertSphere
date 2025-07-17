import js from '@eslint/js'
import globals from 'globals'
import pluginVue from 'eslint-plugin-vue'
import { defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript'
import prettierSkipFormatting from '@vue/eslint-config-prettier/skip-formatting'

export default defineConfigWithVueTs(
    {
        // Ignore patterns for monorepo
        ignores: [
            '**/dist/**',
            '**/node_modules/**',
            '**/.quasar/**',
            '**/coverage/**',
            '**/src-*/**',
        ]
    },

    // Base configurations
    js.configs.recommended,
    pluginVue.configs['flat/recommended'],

    // TypeScript configuration
    {
        files: ['packages/**/*.ts', 'packages/**/*.vue'],
        rules: {
            '@typescript-eslint/consistent-type-imports': [
                'error',
                { prefer: 'type-imports' }
            ],
        }
    },

    // Vue + TypeScript recommended (this handles the parsing correctly)
    vueTsConfigs.recommended,

    {
        // Apply to all files in packages
        files: ['packages/**/*.{js,ts,vue,mjs}'],
        languageOptions: {
            ecmaVersion: 'latest',
            sourceType: 'module',
            globals: {
                ...globals.browser,
                ...globals.node,
                process: 'readonly',
            }
        },

        // XpertSphere custom rules
        rules: {
            // Vue rules
            'vue/multi-word-component-names': 'off',
            'vue/component-definition-name-casing': ['error', 'PascalCase'],
            'vue/component-name-in-template-casing': ['error', 'PascalCase'],

            // TypeScript rules
            '@typescript-eslint/no-explicit-any': 'warn',
            '@typescript-eslint/no-unused-vars': 'error',
            '@typescript-eslint/explicit-function-return-type': 'off',

            // General rules
            'prefer-const': 'error',
            'no-var': 'error',
            'no-console': process.env.NODE_ENV === 'production' ? 'warn' : 'off',
            'no-debugger': process.env.NODE_ENV === 'production' ? 'error' : 'off',
            'spaced-comment': ['error', 'always'],

            // Disable some strict rules for development
            'prefer-promise-reject-errors': 'off',
        }
    },

    prettierSkipFormatting
)