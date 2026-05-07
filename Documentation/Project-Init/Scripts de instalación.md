📋 PASO 1: Crear el Proyecto Vite

Abre PowerShell en la ubicación donde quieres crear el proyecto y ejecuta:



\# Crear proyecto con Vite

npm create vite@latest reservarte-web -- --template react-ts



\# Entrar al directorio

cd reservarte-web



\# Esto creará el proyecto base con React + TypeScript.



\##########################################################



📦 PASO 2: Instalar Dependencias

Script de instalación (copia todo y pégalo en PowerShell línea por línea):



\# ============================================

\# INSTALACIÓN DE DEPENDENCIAS - RESERVARTE

\# ============================================



Write-Host "=== Instalando dependencias principales ===" -ForegroundColor Green



\# Dependencias principales

npm install react-router-dom zustand axios date-fns clsx tailwind-merge



\# React Hook Form + Zod para formularios

npm install react-hook-form zod @hookform/resolvers



\# Tailwind CSS + plugins

npm install -D tailwindcss postcss autoprefixer

npm install -D tailwindcss-animate



\# shadcn/ui dependencies

npm install @radix-ui/react-dialog @radix-ui/react-dropdown-menu @radix-ui/react-slot

npm install @radix-ui/react-alert-dialog @radix-ui/react-avatar @radix-ui/react-checkbox

npm install @radix-ui/react-label @radix-ui/react-select @radix-ui/react-separator

npm install @radix-ui/react-tabs @radix-ui/react-toast @radix-ui/react-tooltip

npm install @radix-ui/react-popover @radix-ui/react-switch



\# Iconos (Lucide React)

npm install lucide-react



\# Calendario

npm install react-big-calendar



\# Types para date-fns y react-big-calendar

npm install -D @types/react-big-calendar



\# ESLint y Prettier (opcionales pero recomendados)

npm install -D prettier eslint-config-prettier eslint-plugin-prettier



Write-Host "`n=== Instalación completada ===" -ForegroundColor Green

Write-Host "Total de paquetes instalados. Revisa si hubo errores arriba." -ForegroundColor Yellow





\####################################################





PASO 3: Crear Estructura de Directorios

Script para crear carpetas (copia y pega en PowerShell):



\# ============================================

\# CREACIÓN DE ESTRUCTURA DE CARPETAS

\# ============================================



Write-Host "=== Creando estructura de carpetas ===" -ForegroundColor Green



\# Eliminar carpeta src existente y recrear

Remove-Item -Recurse -Force src -ErrorAction SilentlyContinue



\# Crear todas las carpetas necesarias

$folders = @(

&nbsp;   "src\\app",

&nbsp;   "src\\assets\\icons",

&nbsp;   "src\\assets\\images",

&nbsp;   "src\\assets\\fonts",

&nbsp;   

&nbsp;   "src\\components\\ui",

&nbsp;   "src\\components\\layouts",

&nbsp;   "src\\components\\common",

&nbsp;   

&nbsp;   "src\\components\\features\\auth",

&nbsp;   "src\\components\\features\\appointments\\AppointmentWizard",

&nbsp;   "src\\components\\features\\customers",

&nbsp;   "src\\components\\features\\employees",

&nbsp;   "src\\components\\features\\services",

&nbsp;   "src\\components\\features\\payments",

&nbsp;   "src\\components\\features\\dashboard",

&nbsp;   "src\\components\\features\\organization",

&nbsp;   "src\\components\\features\\public-booking",

&nbsp;   

&nbsp;   "src\\features\\appointments\\api",

&nbsp;   "src\\features\\appointments\\hooks",

&nbsp;   "src\\features\\appointments\\types",

&nbsp;   "src\\features\\appointments\\utils",

&nbsp;   

&nbsp;   "src\\features\\auth\\api",

&nbsp;   "src\\features\\auth\\hooks",

&nbsp;   "src\\features\\auth\\types",

&nbsp;   

&nbsp;   "src\\features\\customers\\api",

&nbsp;   "src\\features\\customers\\hooks",

&nbsp;   "src\\features\\customers\\types",

&nbsp;   

&nbsp;   "src\\features\\employees\\api",

&nbsp;   "src\\features\\employees\\hooks",

&nbsp;   "src\\features\\employees\\types",

&nbsp;   

&nbsp;   "src\\features\\services\\api",

&nbsp;   "src\\features\\services\\hooks",

&nbsp;   "src\\features\\services\\types",

&nbsp;   

&nbsp;   "src\\features\\payments\\api",

&nbsp;   "src\\features\\payments\\hooks",

&nbsp;   "src\\features\\payments\\services",

&nbsp;   "src\\features\\payments\\types",

&nbsp;   

&nbsp;   "src\\pages\\auth",

&nbsp;   "src\\pages\\dashboard",

&nbsp;   "src\\pages\\appointments",

&nbsp;   "src\\pages\\customers",

&nbsp;   "src\\pages\\employees",

&nbsp;   "src\\pages\\services",

&nbsp;   "src\\pages\\settings",

&nbsp;   "src\\pages\\public",

&nbsp;   "src\\pages\\errors",

&nbsp;   

&nbsp;   "src\\lib\\api",

&nbsp;   "src\\lib\\hooks",

&nbsp;   "src\\lib\\utils",

&nbsp;   "src\\lib\\validations",

&nbsp;   

&nbsp;   "src\\stores",

&nbsp;   "src\\types",

&nbsp;   "src\\config",

&nbsp;   "src\\styles\\themes",

&nbsp;   "src\\tests\\utils",

&nbsp;   "src\\tests\\mocks"

)



foreach ($folder in $folders) {

&nbsp;   New-Item -ItemType Directory -Force -Path $folder | Out-Null

&nbsp;   Write-Host "✓ Creado: $folder" -ForegroundColor Gray

}



Write-Host "`n=== Estructura de carpetas creada ===" -ForegroundColor Green





\#############################################################



PASO 4: Crear Archivos de Configuración



4.0 - Instalar Tailwind CSS antes de ejecutar el comando



npm install -D tailwindcss postcss autoprefixer





4.1 - Inicializar Tailwind CSS

\## Si da error la última versión, instalar la anterior

npx tailwindcss init -p





4.2 - Crear archivos de configuración

\# Copia y pega este script en PowerShell:



\# ============================================

\# CREACIÓN DE ARCHIVOS DE CONFIGURACIÓN

\# ============================================



Write-Host "=== Creando archivos de configuración ===" -ForegroundColor Green



\# ============= vite.config.ts =============

@"

import { defineConfig } from 'vite'

import react from '@vitejs/plugin-react'

import path from 'path'



export default defineConfig({

&nbsp; plugins: \[react()],

&nbsp; resolve: {

&nbsp;   alias: {

&nbsp;     '@': path.resolve(\_\_dirname, './src'),

&nbsp;     '@components': path.resolve(\_\_dirname, './src/components'),

&nbsp;     '@features': path.resolve(\_\_dirname, './src/features'),

&nbsp;     '@pages': path.resolve(\_\_dirname, './src/pages'),

&nbsp;     '@lib': path.resolve(\_\_dirname, './src/lib'),

&nbsp;     '@stores': path.resolve(\_\_dirname, './src/stores'),

&nbsp;     '@types': path.resolve(\_\_dirname, './src/types'),

&nbsp;     '@assets': path.resolve(\_\_dirname, './src/assets'),

&nbsp;   },

&nbsp; },

&nbsp; server: {

&nbsp;   port: 3000,

&nbsp;   proxy: {

&nbsp;     '/api': {

&nbsp;       target: 'http://localhost:5000',

&nbsp;       changeOrigin: true,

&nbsp;     },

&nbsp;   },

&nbsp; },

&nbsp; build: {

&nbsp;   outDir: 'dist',

&nbsp;   sourcemap: true,

&nbsp;   rollupOptions: {

&nbsp;     output: {

&nbsp;       manualChunks: {

&nbsp;         vendor: \['react', 'react-dom', 'react-router-dom'],

&nbsp;         ui: \['@radix-ui/react-dialog', '@radix-ui/react-dropdown-menu'],

&nbsp;       },

&nbsp;     },

&nbsp;   },

&nbsp; },

})

"@ | Out-File -FilePath "vite.config.ts" -Encoding utf8



\# ============= tsconfig.json =============

@"

{

&nbsp; "compilerOptions": {

&nbsp;   "target": "ES2020",

&nbsp;   "useDefineForClassFields": true,

&nbsp;   "lib": \["ES2020", "DOM", "DOM.Iterable"],

&nbsp;   "module": "ESNext",

&nbsp;   "skipLibCheck": true,

&nbsp;   "moduleResolution": "bundler",

&nbsp;   "allowImportingTsExtensions": true,

&nbsp;   "resolveJsonModule": true,

&nbsp;   "isolatedModules": true,

&nbsp;   "noEmit": true,

&nbsp;   "jsx": "react-jsx",

&nbsp;   "strict": true,

&nbsp;   "noUnusedLocals": true,

&nbsp;   "noUnusedParameters": true,

&nbsp;   "noFallthroughCasesInSwitch": true,

&nbsp;   "baseUrl": ".",

&nbsp;   "paths": {

&nbsp;     "@/\*": \["./src/\*"],

&nbsp;     "@components/\*": \["./src/components/\*"],

&nbsp;     "@features/\*": \["./src/features/\*"],

&nbsp;     "@pages/\*": \["./src/pages/\*"],

&nbsp;     "@lib/\*": \["./src/lib/\*"],

&nbsp;     "@stores/\*": \["./src/stores/\*"],

&nbsp;     "@types/\*": \["./src/types/\*"],

&nbsp;     "@assets/\*": \["./src/assets/\*"]

&nbsp;   }

&nbsp; },

&nbsp; "include": \["src"],

&nbsp; "references": \[{ "path": "./tsconfig.node.json" }]

}

"@ | Out-File -FilePath "tsconfig.json" -Encoding utf8



\# ============= tailwind.config.js =============

@"

/\*\* @type {import('tailwindcss').Config} \*/

export default {

&nbsp; darkMode: \['class'],

&nbsp; content: \[

&nbsp;   './pages/\*\*/\*.{ts,tsx}',

&nbsp;   './components/\*\*/\*.{ts,tsx}',

&nbsp;   './app/\*\*/\*.{ts,tsx}',

&nbsp;   './src/\*\*/\*.{ts,tsx}',

&nbsp; ],

&nbsp; theme: {

&nbsp;   container: {

&nbsp;     center: true,

&nbsp;     padding: '2rem',

&nbsp;     screens: {

&nbsp;       '2xl': '1400px',

&nbsp;     },

&nbsp;   },

&nbsp;   extend: {

&nbsp;     colors: {

&nbsp;       border: 'hsl(var(--border))',

&nbsp;       input: 'hsl(var(--input))',

&nbsp;       ring: 'hsl(var(--ring))',

&nbsp;       background: 'hsl(var(--background))',

&nbsp;       foreground: 'hsl(var(--foreground))',

&nbsp;       primary: {

&nbsp;         DEFAULT: 'hsl(var(--primary))',

&nbsp;         foreground: 'hsl(var(--primary-foreground))',

&nbsp;       },

&nbsp;       secondary: {

&nbsp;         DEFAULT: 'hsl(var(--secondary))',

&nbsp;         foreground: 'hsl(var(--secondary-foreground))',

&nbsp;       },

&nbsp;       destructive: {

&nbsp;         DEFAULT: 'hsl(var(--destructive))',

&nbsp;         foreground: 'hsl(var(--destructive-foreground))',

&nbsp;       },

&nbsp;       muted: {

&nbsp;         DEFAULT: 'hsl(var(--muted))',

&nbsp;         foreground: 'hsl(var(--muted-foreground))',

&nbsp;       },

&nbsp;       accent: {

&nbsp;         DEFAULT: 'hsl(var(--accent))',

&nbsp;         foreground: 'hsl(var(--accent-foreground))',

&nbsp;       },

&nbsp;       popover: {

&nbsp;         DEFAULT: 'hsl(var(--popover))',

&nbsp;         foreground: 'hsl(var(--popover-foreground))',

&nbsp;       },

&nbsp;       card: {

&nbsp;         DEFAULT: 'hsl(var(--card))',

&nbsp;         foreground: 'hsl(var(--card-foreground))',

&nbsp;       },

&nbsp;     },

&nbsp;     borderRadius: {

&nbsp;       lg: 'var(--radius)',

&nbsp;       md: 'calc(var(--radius) - 2px)',

&nbsp;       sm: 'calc(var(--radius) - 4px)',

&nbsp;     },

&nbsp;     keyframes: {

&nbsp;       'accordion-down': {

&nbsp;         from: { height: 0 },

&nbsp;         to: { height: 'var(--radix-accordion-content-height)' },

&nbsp;       },

&nbsp;       'accordion-up': {

&nbsp;         from: { height: 'var(--radix-accordion-content-height)' },

&nbsp;         to: { height: 0 },

&nbsp;       },

&nbsp;     },

&nbsp;     animation: {

&nbsp;       'accordion-down': 'accordion-down 0.2s ease-out',

&nbsp;       'accordion-up': 'accordion-up 0.2s ease-out',

&nbsp;     },

&nbsp;   },

&nbsp; },

&nbsp; plugins: \[require('tailwindcss-animate')],

}

"@ | Out-File -FilePath "tailwind.config.js" -Encoding utf8



\# ============= components.json (shadcn/ui) =============

@"

{

&nbsp; "`$schema": "https://ui.shadcn.com/schema.json",

&nbsp; "style": "default",

&nbsp; "rsc": false,

&nbsp; "tsx": true,

&nbsp; "tailwind": {

&nbsp;   "config": "tailwind.config.js",

&nbsp;   "css": "src/styles/globals.css",

&nbsp;   "baseColor": "slate",

&nbsp;   "cssVariables": true

&nbsp; },

&nbsp; "aliases": {

&nbsp;   "components": "@/components",

&nbsp;   "utils": "@/lib/utils"

&nbsp; }

}

"@ | Out-File -FilePath "components.json" -Encoding utf8



\# ============= .env.example =============

@"

\# API Configuration

VITE\_API\_BASE\_URL=http://localhost:5000

VITE\_API\_TIMEOUT=30000



\# App Configuration

VITE\_APP\_NAME=ReservArte

VITE\_APP\_URL=http://localhost:3000



\# Redsys Configuration (Frontend)

VITE\_REDSYS\_ENVIRONMENT=test

VITE\_REDSYS\_SDK\_URL=https://sis-t.redsys.es:25443/sis/NC/redsysV3.js



\# Feature Flags

VITE\_ENABLE\_SAVED\_CARDS=true

VITE\_ENABLE\_WHATSAPP=false

VITE\_ENABLE\_PUBLIC\_BOOKING=true

"@ | Out-File -FilePath ".env.example" -Encoding utf8



\# Copiar a .env.development

Copy-Item ".env.example" ".env.development"



\# ============= .prettierrc =============

@"

{

&nbsp; "semi": true,

&nbsp; "trailingComma": "es5",

&nbsp; "singleQuote": true,

&nbsp; "printWidth": 100,

&nbsp; "tabWidth": 2,

&nbsp; "useTabs": false

}

"@ | Out-File -FilePath ".prettierrc" -Encoding utf8



\# ============= .eslintrc.cjs =============

@"

module.exports = {

&nbsp; root: true,

&nbsp; env: { browser: true, es2020: true },

&nbsp; extends: \[

&nbsp;   'eslint:recommended',

&nbsp;   'plugin:@typescript-eslint/recommended',

&nbsp;   'plugin:react-hooks/recommended',

&nbsp;   'prettier',

&nbsp; ],

&nbsp; ignorePatterns: \['dist', '.eslintrc.cjs'],

&nbsp; parser: '@typescript-eslint/parser',

&nbsp; plugins: \['react-refresh', 'prettier'],

&nbsp; rules: {

&nbsp;   'react-refresh/only-export-components': \[

&nbsp;     'warn',

&nbsp;     { allowConstantExport: true },

&nbsp;   ],

&nbsp;   'prettier/prettier': 'warn',

&nbsp;   '@typescript-eslint/no-explicit-any': 'warn',

&nbsp; },

}

"@ | Out-File -FilePath ".eslintrc.cjs" -Encoding utf8



Write-Host "✓ Archivos de configuración creados" -ForegroundColor Green







\###########################################



\# PASO 5: Crear Archivos Base Esenciales



\# ============================================

\# CREACIÓN DE ARCHIVOS BASE

\# ============================================



Write-Host "`n=== Creando archivos base esenciales ===" -ForegroundColor Green



\# ============= src/styles/globals.css =============

@"

@tailwind base;

@tailwind components;

@tailwind utilities;



@layer base {

&nbsp; :root {

&nbsp;   --background: 0 0% 100%;

&nbsp;   --foreground: 222.2 84% 4.9%;

&nbsp;   --card: 0 0% 100%;

&nbsp;   --card-foreground: 222.2 84% 4.9%;

&nbsp;   --popover: 0 0% 100%;

&nbsp;   --popover-foreground: 222.2 84% 4.9%;

&nbsp;   --primary: 262.1 83.3% 57.8%;

&nbsp;   --primary-foreground: 210 40% 98%;

&nbsp;   --secondary: 210 40% 96.1%;

&nbsp;   --secondary-foreground: 222.2 47.4% 11.2%;

&nbsp;   --muted: 210 40% 96.1%;

&nbsp;   --muted-foreground: 215.4 16.3% 46.9%;

&nbsp;   --accent: 210 40% 96.1%;

&nbsp;   --accent-foreground: 222.2 47.4% 11.2%;

&nbsp;   --destructive: 0 84.2% 60.2%;

&nbsp;   --destructive-foreground: 210 40% 98%;

&nbsp;   --border: 214.3 31.8% 91.4%;

&nbsp;   --input: 214.3 31.8% 91.4%;

&nbsp;   --ring: 262.1 83.3% 57.8%;

&nbsp;   --radius: 0.5rem;

&nbsp; }



&nbsp; .dark {

&nbsp;   --background: 222.2 84% 4.9%;

&nbsp;   --foreground: 210 40% 98%;

&nbsp;   --card: 222.2 84% 4.9%;

&nbsp;   --card-foreground: 210 40% 98%;

&nbsp;   --popover: 222.2 84% 4.9%;

&nbsp;   --popover-foreground: 210 40% 98%;

&nbsp;   --primary: 262.1 83.3% 57.8%;

&nbsp;   --primary-foreground: 210 40% 98%;

&nbsp;   --secondary: 217.2 32.6% 17.5%;

&nbsp;   --secondary-foreground: 210 40% 98%;

&nbsp;   --muted: 217.2 32.6% 17.5%;

&nbsp;   --muted-foreground: 215 20.2% 65.1%;

&nbsp;   --accent: 217.2 32.6% 17.5%;

&nbsp;   --accent-foreground: 210 40% 98%;

&nbsp;   --destructive: 0 62.8% 30.6%;

&nbsp;   --destructive-foreground: 210 40% 98%;

&nbsp;   --border: 217.2 32.6% 17.5%;

&nbsp;   --input: 217.2 32.6% 17.5%;

&nbsp;   --ring: 262.1 83.3% 57.8%;

&nbsp; }

}



@layer base {

&nbsp; \* {

&nbsp;   @apply border-border;

&nbsp; }

&nbsp; body {

&nbsp;   @apply bg-background text-foreground;

&nbsp; }

}

"@ | Out-File -FilePath "src\\styles\\globals.css" -Encoding utf8



\# ============= src/lib/utils/cn.ts =============

@"

import { type ClassValue, clsx } from 'clsx';

import { twMerge } from 'tailwind-merge';



export function cn(...inputs: ClassValue\[]) {

&nbsp; return twMerge(clsx(inputs));

}

"@ | Out-File -FilePath "src\\lib\\utils\\cn.ts" -Encoding utf8



\# ============= src/lib/api/client.ts =============

@"

import axios from 'axios';



const apiClient = axios.create({

&nbsp; baseURL: import.meta.env.VITE\_API\_BASE\_URL || 'http://localhost:5000',

&nbsp; timeout: Number(import.meta.env.VITE\_API\_TIMEOUT) || 30000,

&nbsp; headers: {

&nbsp;   'Content-Type': 'application/json',

&nbsp; },

});



// Request interceptor

apiClient.interceptors.request.use(

&nbsp; (config) => {

&nbsp;   const token = localStorage.getItem('authToken');

&nbsp;   if (token) {

&nbsp;     config.headers.Authorization = ``Bearer `${token}``;

&nbsp;   }

&nbsp;   return config;

&nbsp; },

&nbsp; (error) => {

&nbsp;   return Promise.reject(error);

&nbsp; }

);



// Response interceptor

apiClient.interceptors.response.use(

&nbsp; (response) => response,

&nbsp; (error) => {

&nbsp;   if (error.response?.status === 401) {

&nbsp;     localStorage.removeItem('authToken');

&nbsp;     window.location.href = '/login';

&nbsp;   }

&nbsp;   return Promise.reject(error);

&nbsp; }

);



export default apiClient;

"@ | Out-File -FilePath "src\\lib\\api\\client.ts" -Encoding utf8



\# ============= src/config/env.ts =============

@"

export const env = {

&nbsp; API\_BASE\_URL: import.meta.env.VITE\_API\_BASE\_URL || 'http://localhost:5000',

&nbsp; API\_TIMEOUT: Number(import.meta.env.VITE\_API\_TIMEOUT) || 30000,

&nbsp; APP\_NAME: import.meta.env.VITE\_APP\_NAME || 'ReservArte',

&nbsp; APP\_URL: import.meta.env.VITE\_APP\_URL || 'http://localhost:3000',

&nbsp; REDSYS\_ENVIRONMENT: import.meta.env.VITE\_REDSYS\_ENVIRONMENT || 'test',

&nbsp; REDSYS\_SDK\_URL: import.meta.env.VITE\_REDSYS\_SDK\_URL || 'https://sis-t.redsys.es:25443/sis/NC/redsysV3.js',

&nbsp; ENABLE\_SAVED\_CARDS: import.meta.env.VITE\_ENABLE\_SAVED\_CARDS === 'true',

&nbsp; ENABLE\_WHATSAPP: import.meta.env.VITE\_ENABLE\_WHATSAPP === 'true',

&nbsp; ENABLE\_PUBLIC\_BOOKING: import.meta.env.VITE\_ENABLE\_PUBLIC\_BOOKING === 'true',

} as const;

"@ | Out-File -FilePath "src\\config\\env.ts" -Encoding utf8



\# ============= src/app/router.tsx =============

@"

import { createBrowserRouter } from 'react-router-dom';



// Placeholder pages

const DashboardPage = () => <div>Dashboard</div>;

const LoginPage = () => <div>Login</div>;



export const router = createBrowserRouter(\[

&nbsp; {

&nbsp;   path: '/',

&nbsp;   element: <DashboardPage />,

&nbsp; },

&nbsp; {

&nbsp;   path: '/login',

&nbsp;   element: <LoginPage />,

&nbsp; },

]);

"@ | Out-File -FilePath "src\\app\\router.tsx" -Encoding utf8



\# ============= src/app/App.tsx =============

@"

import { RouterProvider } from 'react-router-dom';

import { router } from './router';

import '../styles/globals.css';



function App() {

&nbsp; return <RouterProvider router={router} />;

}



export default App;

"@ | Out-File -FilePath "src\\app\\App.tsx" -Encoding utf8



\# ============= src/app/main.tsx =============

@"

import React from 'react';

import ReactDOM from 'react-dom/client';

import App from './App';



ReactDOM.createRoot(document.getElementById('root')!).render(

&nbsp; <React.StrictMode>

&nbsp;   <App />

&nbsp; </React.StrictMode>

);

"@ | Out-File -FilePath "src\\app\\main.tsx" -Encoding utf8



\# ============= src/types/index.ts =============

@"

// Tipos globales compartidos

export \* from './models.types';

export \* from './api.types';

export \* from './enums';

"@ | Out-File -FilePath "src\\types\\index.ts" -Encoding utf8



\# ============= src/types/enums.ts =============

@"

export enum AppointmentStatus {

&nbsp; Pending = 'Pending',

&nbsp; Confirmed = 'Confirmed',

&nbsp; InProgress = 'InProgress',

&nbsp; Completed = 'Completed',

&nbsp; Cancelled = 'Cancelled',

&nbsp; NoShow = 'NoShow',

}



export enum UserRole {

&nbsp; Admin = 'Admin',

&nbsp; Manager = 'Manager',

&nbsp; Employee = 'Employee',

&nbsp; Customer = 'Customer',

}



export enum PaymentMethod {

&nbsp; Card = 'Card',

&nbsp; Cash = 'Cash',

&nbsp; Transfer = 'Transfer',

&nbsp; Bizum = 'Bizum',

}



export enum PaymentStatus {

&nbsp; Pending = 'Pending',

&nbsp; Authorized = 'Authorized',

&nbsp; Captured = 'Captured',

&nbsp; Failed = 'Failed',

&nbsp; Refunded = 'Refunded',

}

"@ | Out-File -FilePath "src\\types\\enums.ts" -Encoding utf8



Write-Host "✓ Archivos base creados" -ForegroundColor Green



\#####################################################



\#  PASO 6: Instalar Componentes Base de shadcn/ui



Write-Host "`n=== Instalando componentes de shadcn/ui ===" -ForegroundColor Green

Write-Host "Esto instalará los componentes más comunes. Puedes agregar más después." -ForegroundColor Yellow



npx shadcn-ui@latest add button

npx shadcn-ui@latest add input

npx shadcn-ui@latest add card

npx shadcn-ui@latest add dialog

npx shadcn-ui@latest add dropdown-menu

npx shadcn-ui@latest add form

npx shadcn-ui@latest add label

npx shadcn-ui@latest add select

npx shadcn-ui@latest add table

npx shadcn-ui@latest add toast



Write-Host "`n✓ Componentes de shadcn/ui instalados" -ForegroundColor Green



\##############################



\# PASO 7: Actualizar index.html



\# ============= index.html =============

@"

<!doctype html>

<html lang="es">

&nbsp; <head>

&nbsp;   <meta charset="UTF-8" />

&nbsp;   <link rel="icon" type="image/svg+xml" href="/logo.svg" />

&nbsp;   <meta name="viewport" content="width=device-width, initial-scale=1.0" />

&nbsp;   <title>ReservArte - Sistema de Gestión de Citas</title>

&nbsp;   

&nbsp;   <!-- Redsys InSite SDK -->

&nbsp;   <script src="https://sis-t.redsys.es:25443/sis/NC/redsysV3.js"></script>

&nbsp; </head>

&nbsp; <body>

&nbsp;   <div id="root"></div>

&nbsp;   <script type="module" src="/src/app/main.tsx"></script>

&nbsp; </body>

</html>

"@ | Out-File -FilePath "index.html" -Encoding utf8



Write-Host "✓ index.html actualizado con SDK de Redsys" -ForegroundColor Green



\##################################################



\# PASO 8: Verificar Instalación



Write-Host "`n=== Verificando instalación ===" -ForegroundColor Green



\# Verificar que package.json existe

if (Test-Path "package.json") {

&nbsp;   Write-Host "✓ package.json encontrado" -ForegroundColor Green

} else {

&nbsp;   Write-Host "✗ package.json NO encontrado" -ForegroundColor Red

}



\# Verificar node\_modules

if (Test-Path "node\_modules") {

&nbsp;   Write-Host "✓ node\_modules encontrado" -ForegroundColor Green

} else {

&nbsp;   Write-Host "✗ node\_modules NO encontrado - ejecuta: npm install" -ForegroundColor Red

}



\# Verificar estructura src

if (Test-Path "src\\app\\App.tsx") {

&nbsp;   Write-Host "✓ Estructura src creada correctamente" -ForegroundColor Green

} else {

&nbsp;   Write-Host "✗ Estructura src incompleta" -ForegroundColor Red

}



Write-Host "`n=== Instalación completada ===" -ForegroundColor Cyan

Write-Host "`nPara iniciar el servidor de desarrollo:" -ForegroundColor Yellow

Write-Host "  npm run dev" -ForegroundColor White

Write-Host "`nPara compilar para producción:" -ForegroundColor Yellow

Write-Host "  npm run build" -ForegroundColor White







\###################################



Notas Importantes



PowerShell vs CMD: Estos scripts están optimizados para PowerShell. Si usas CMD, algunos comandos pueden necesitar ajustes.

Permisos: Si PowerShell no te deja ejecutar scripts, ejecuta primero:



powershell   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser



Errores comunes:



Si npm no se reconoce: Instala Node.js desde nodejs.org

Si hay errores de permisos: Ejecuta PowerShell como Administrador

Si falla npx shadcn-ui: Asegúrate que components.json existe





Próximos pasos después de la instalación:



Instalar componentes adicionales de shadcn/ui según necesites

Crear stores de Zustand

Implementar hooks de autenticación

Crear componentes de formularios

