import type { Config } from 'tailwindcss';

const config: Config = {
  content: [
    './src/pages/**/*.{js,ts,jsx,tsx,mdx}',
    './src/components/**/*.{js,ts,jsx,tsx,mdx}',
    './src/app/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  theme: {
    extend: {
      colors: {
        dp: {
          blue: '#09318D',
          pink: '#F59ACC',
          white: '#FFFFFF',
          line: 'rgba(9, 49, 141, 0.16)',
          tint: 'rgba(9, 49, 141, 0.045)',
          blush: 'rgba(245, 154, 204, 0.24)',
          glass: 'rgba(255, 255, 255, 0.4)',
          green: '#107C41',
          'green-blush': 'rgba(16, 124, 65, 0.14)',
          amber: '#B25900',
          'amber-blush': 'rgba(220, 107, 0, 0.14)',
          red: '#C5221F',
          'red-blush': 'rgba(197, 34, 31, 0.14)',
        }
      },
      fontFamily: {
        sans: ['var(--font-sans)', '-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'Roboto', 'Arial', 'sans-serif'],
        mono: ['var(--mono)', 'ui-monospace', '"SFMono-Regular"', 'Consolas', 'Menlo', 'monospace'],
      },
      borderRadius: {
        card: '19px',
        pill: '99px',
      },
      boxShadow: {
        card: '0 12px 32px rgba(9, 49, 141, 0.12)',
        receipt: '0 14px 42px rgba(9, 49, 141, 0.12)',
        window: '0 10px 28px rgba(9, 49, 141, 0.07)',
      }
    },
  },
  plugins: [],
};

export default config;
