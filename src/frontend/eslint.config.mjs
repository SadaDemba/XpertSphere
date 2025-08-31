import js from '@eslint/js'
import globals from 'globals'
import pluginVue from 'eslint-plugin-vue'
import pluginVueA11y from 'eslint-plugin-vuejs-accessibility'
import { defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript'
import prettierSkipFormatting from '@vue/eslint-config-prettier/skip-formatting'
import * as dotenv from 'dotenv'

dotenv.config();

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

    // Vue + TypeScript recommended (this handles the parsing correctly)
    vueTsConfigs.recommended,

    // Accessibility configuration for Vue
    {
        files: ['packages/**/*.vue'],
        plugins: {
            'vuejs-accessibility': pluginVueA11y
        },
        rules: {
            // Rules RGAA/WCAG strict for a11y
            'vuejs-accessibility/alt-text': 'error',
            'vuejs-accessibility/anchor-has-content': 'error',
            'vuejs-accessibility/aria-props': 'error',
            'vuejs-accessibility/aria-role': 'error',
            'vuejs-accessibility/aria-unsupported-elements': 'error',
            'vuejs-accessibility/click-events-have-key-events': 'error',
            'vuejs-accessibility/form-control-has-label': 'error',
            'vuejs-accessibility/heading-has-content': 'error',
            'vuejs-accessibility/iframe-has-title': 'error',
            'vuejs-accessibility/interactive-supports-focus': 'error',
            'vuejs-accessibility/label-has-for': ['error', {
                'required': {
                    'some': ['nesting', 'id']
                }
            }],
            'vuejs-accessibility/media-has-caption': 'error',
            'vuejs-accessibility/mouse-events-have-key-events': 'error',
            'vuejs-accessibility/no-access-key': 'error',
            'vuejs-accessibility/no-autofocus': 'error',
            'vuejs-accessibility/no-distracting-elements': 'error',
            'vuejs-accessibility/no-onchange': 'error',
            'vuejs-accessibility/no-redundant-roles': 'error',
            'vuejs-accessibility/role-has-required-aria-props': 'error',
            'vuejs-accessibility/tabindex-no-positive': 'error'
        }
    },


    {
        // Apply to all files in packages
        files: ['packages/src/*.{js,ts,vue,mjs}'],
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
            'vue/component-name-in-template-casing': ['error', 'kebab-case'],

            // TypeScript rules
            '@typescript-eslint/no-explicit-any': 'off',
            '@typescript-eslint/no-unused-vars': 'off',
            '@typescript-eslint/explicit-function-return-type': 'off',

            // General rules
            'prefer-const': 'error',
            'no-var': 'error',
            /*global process*/
            'no-console': process.env.NODE_ENV === 'production' ? 'warn' : 'off',
            'no-debugger': process.env.NODE_ENV === 'production' ? 'error' : 'off',
            'spaced-comment': ['error', 'always'],
            '@typescript-eslint/consistent-type-imports': [
                'error',
                { prefer: 'type-imports' }
            ],
            // Disable some strict rules for development
            'prefer-promise-reject-errors': 'off',
        }
    },

    prettierSkipFormatting
)