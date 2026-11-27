// Flat config for ESLint 9, migrated one-to-one from the former .eslintrc.json.
// No shared rule sets are enabled; only the formatting rules below apply.
// The import/* rules from the old file are dropped: eslint-plugin-import is not installed.
import babelParser from '@babel/eslint-parser';

export default [
  {
    ignores: ['src/data/**', 'src/reports/**'],
  },
  {
    files: ['**/*.js'],
    languageOptions: {
      parser: babelParser,
      parserOptions: {
        requireConfigFile: false,
      },
      sourceType: 'module',
      globals: {
        __ENV: 'readonly',
      },
    },
    rules: {
      'no-restricted-globals': 'off',
      'eol-last': 'error',
      indent: ['warn', 2, { SwitchCase: 1 }],
      quotes: ['warn', 'single', { avoidEscape: true, allowTemplateLiterals: true }],
      semi: ['warn', 'always', { omitLastInOneLineBlock: true }],
    },
  },
];
