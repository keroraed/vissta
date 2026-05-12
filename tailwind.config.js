/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        'navy': {
          DEFAULT: '#071426',
          light: '#0a1d37',
          dark: '#040b15'
        },
        'gold': '#D4AF73',
        'cream': '#F5F1EA'
      },
      fontFamily: {
        serif: ['"Playfair Display"', 'Georgia', 'serif'],
        sans: ['"Inter"', 'Helvetica', 'Arial', 'sans-serif'],
      }
    },
  },
  plugins: [],
}