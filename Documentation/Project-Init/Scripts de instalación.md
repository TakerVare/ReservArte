# Scripts de instalación — ReservArte

Guía para generar el frontend **Vue 3 + Vite**. Los comandos **`npm`**, **`npx`** y **`docker`** funcionan igual en PowerShell y en Bash. Lo que **no** es intercambiable son los bloques que crean carpetas y escriben ficheros: en Windows se usa **PowerShell** (`Out-File`, here-strings `@"..."@`); en **macOS / Linux / Git Bash** usa los bloques **Bash** de cada paso.

| Paso | Contenido |
|------|-----------|
| 1 | Crear proyecto Vite (`vue-ts`) |
| 1b | SQL Server en Docker (desarrollo) |
| 2 | Dependencias npm |
| 3 | Estructura de carpetas |
| 4 | Tailwind + archivos de configuración |
| 5 | Estilos, API client, router, tipos |
| 6 | Componentes UI (Reka UI) |
| 7 | `index.html` + Redsys SDK |
| 8 | Comprobaciones finales |

---

## 📋 Paso 1 — Crear el Proyecto Vite

### PowerShell

Abre PowerShell en la carpeta padre donde quieras el proyecto.

```powershell
npm create vite@latest reservarte-web -- --template vue-ts
cd reservarte-web
```

### Bash (macOS / Linux / Git Bash)

```bash
npm create vite@latest reservarte-web -- --template vue-ts
cd reservarte-web
```

Quedará creada la base **Vue 3 + TypeScript + Vite** en la carpeta `reservarte-web`.

---

## 🐳 Paso 1b — SQL Server en Docker (desarrollo local)

Mismo comando conceptual en ambos entornos (ajusta contraseña y volumen).

### PowerShell

```powershell
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=TuPasswordSegura123!" -p 1433:1433 --name reservarte-sql -v reservarte_sqldata:/var/opt/mssql -d mcr.microsoft.com/mssql/server:2022-latest
```

### Bash

```bash
docker run -e 'ACCEPT_EULA=Y' -e 'MSSQL_SA_PASSWORD=TuPasswordSegura123!' -p 1433:1433 \
  --name reservarte-sql -v reservarte_sqldata:/var/opt/mssql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Cadena de conexión típica para la API .NET: `Server=localhost,1433;Database=ReservArte;User Id=sa;Password=...;TrustServerCertificate=True`

---

## 📦 Paso 2 — Instalar Dependencias

### PowerShell

```powershell
Write-Host "=== Instalando dependencias principales ===" -ForegroundColor Green
npm install vue-router pinia axios date-fns clsx tailwind-merge
npm install vee-validate @vee-validate/zod zod
npm install -D tailwindcss postcss autoprefixer
npm install -D tailwindcss-animate
npm install reka-ui
npm install lucide-vue-next
npm install @fullcalendar/core @fullcalendar/vue3 @fullcalendar/daygrid @fullcalendar/timegrid @fullcalendar/interaction
npm install -D prettier eslint-config-prettier eslint-plugin-prettier
npm install -D eslint-plugin-vue vue-eslint-parser @vue/eslint-config-typescript
Write-Host "=== Instalación completada ===" -ForegroundColor Green
```

### Bash

Ejecuta **dentro de** `reservarte-web`:

```bash
set -e
echo "=== Instalando dependencias principales ==="
npm install vue-router pinia axios date-fns clsx tailwind-merge
npm install vee-validate @vee-validate/zod zod
npm install -D tailwindcss postcss autoprefixer
npm install -D tailwindcss-animate
npm install reka-ui
npm install lucide-vue-next
npm install @fullcalendar/core @fullcalendar/vue3 @fullcalendar/daygrid @fullcalendar/timegrid @fullcalendar/interaction
npm install -D prettier eslint-config-prettier eslint-plugin-prettier
npm install -D eslint-plugin-vue vue-eslint-parser @vue/eslint-config-typescript
echo "=== Instalación completada ==="
```

---

## Paso 3 — Crear Estructura de Directorios

### PowerShell

```powershell
Write-Host "=== Creando estructura de carpetas ===" -ForegroundColor Green
Remove-Item -Recurse -Force src -ErrorAction SilentlyContinue
$folders = @(
   "src\\router", "src\\assets\\icons", "src\\assets\\images", "src\\assets\\fonts",
   "src\\components\\ui", "src\\components\\layouts", "src\\components\\common",
   "src\\components\\features\\auth",
   "src\\components\\features\\appointments\\AppointmentWizard",
   "src\\components\\features\\customers", "src\\components\\features\\employees",
   "src\\components\\features\\services", "src\\components\\features\\payments",
   "src\\components\\features\\dashboard", "src\\components\\features\\organization",
   "src\\components\\features\\public-booking",
   "src\\features\\appointments\\api", "src\\features\\appointments\\composables",
   "src\\features\\appointments\\types", "src\\features\\appointments\\utils",
   "src\\features\\auth\\api", "src\\features\\auth\\composables", "src\\features\\auth\\types",
   "src\\features\\customers\\api", "src\\features\\customers\\composables", "src\\features\\customers\\types",
   "src\\features\\employees\\api", "src\\features\\employees\\composables", "src\\features\\employees\\types",
   "src\\features\\services\\api", "src\\features\\services\\composables", "src\\features\\services\\types",
   "src\\features\\payments\\api", "src\\features\\payments\\composables",
   "src\\features\\payments\\services", "src\\features\\payments\\types",
   "src\\pages\\auth", "src\\pages\\dashboard", "src\\pages\\appointments", "src\\pages\\customers",
   "src\\pages\\employees", "src\\pages\\services", "src\\pages\\settings", "src\\pages\\public",
   "src\\pages\\errors",
   "src\\lib\\api", "src\\lib\\composables", "src\\lib\\utils", "src\\lib\\validations",
   "src\\stores", "src\\types", "src\\config", "src\\styles\\themes", "src\\tests\\utils", "src\\tests\\mocks"
)
foreach ($folder in $folders) {
   New-Item -ItemType Directory -Force -Path $folder | Out-Null
   Write-Host "Creado: $folder"
}
Write-Host "=== Estructura de carpetas creada ===" -ForegroundColor Green
```

### Bash

Desde la raíz del proyecto `reservarte-web`:

```bash
set -e
echo "=== Creando estructura de carpetas ==="
rm -rf src
mkdir -p "src/router" "src/assets/icons" "src/assets/images" "src/assets/fonts" "src/components/ui" "src/components/layouts" "src/components/common" "src/components/features/auth" "src/components/features/appointments/AppointmentWizard" "src/components/features/customers" "src/components/features/employees" "src/components/features/services" "src/components/features/payments" "src/components/features/dashboard" "src/components/features/organization" "src/components/features/public-booking" "src/features/appointments/api" "src/features/appointments/composables" "src/features/appointments/types" "src/features/appointments/utils" "src/features/auth/api" "src/features/auth/composables" "src/features/auth/types" "src/features/customers/api" "src/features/customers/composables" "src/features/customers/types" "src/features/employees/api" "src/features/employees/composables" "src/features/employees/types" "src/features/services/api" "src/features/services/composables" "src/features/services/types" "src/features/payments/api" "src/features/payments/composables" "src/features/payments/services" "src/features/payments/types" "src/pages/auth" "src/pages/dashboard" "src/pages/appointments" "src/pages/customers" "src/pages/employees" "src/pages/services" "src/pages/settings" "src/pages/public" "src/pages/errors" "src/lib/api" "src/lib/composables" "src/lib/utils" "src/lib/validations" "src/stores" "src/types" "src/config" "src/styles/themes" "src/tests/utils" "src/tests/mocks"
echo "=== Estructura de carpetas creada ==="
```

---

## Paso 4 — Crear Archivos de Configuración

### 4.0 — Tailwind (dependencias)

```bash
npm install -D tailwindcss postcss autoprefixer
```

*(Válido también en PowerShell.)*

### 4.1 — Inicializar Tailwind CSS

> Si falla la última versión de Tailwind, prueba una versión anterior acorde a la [documentación](https://tailwindcss.com/docs/installation).

```bash
npx tailwindcss init -p
```

### 4.2 — Crear archivos de configuración

#### PowerShell

Desde `reservarte-web`, pega el bloque completo.

```powershell
Write-Host "=== Creando archivos de configuración ===" -ForegroundColor Green
@"
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@components': path.resolve(__dirname, './src/components'),
      '@features': path.resolve(__dirname, './src/features'),
      '@pages': path.resolve(__dirname, './src/pages'),
      '@lib': path.resolve(__dirname, './src/lib'),
      '@stores': path.resolve(__dirname, './src/stores'),
      '@types': path.resolve(__dirname, './src/types'),
      '@assets': path.resolve(__dirname, './src/assets'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['vue', 'vue-router', 'pinia'],
        },
      },
    },
  },
})

"@ | Out-File -FilePath "vite.config.ts" -Encoding utf8
@"
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"],
      "@components/*": ["./src/components/*"],
      "@features/*": ["./src/features/*"],
      "@pages/*": ["./src/pages/*"],
      "@lib/*": ["./src/lib/*"],
      "@stores/*": ["./src/stores/*"],
      "@types/*": ["./src/types/*"],
      "@assets/*": ["./src/assets/*"]
    }
  },
  "include": ["src"],
  "references": [{ "path": "./tsconfig.node.json" }]
}

"@ | Out-File -FilePath "tsconfig.json" -Encoding utf8
@"
/** @type {import('tailwindcss').Config} */
export default {
  darkMode: ['class'],
  content: [
    './src/**/*.{vue,ts,tsx}',
  ],
  theme: {
    container: {
      center: true,
      padding: '2rem',
      screens: {
        '2xl': '1400px',
      },
    },
    extend: {
      colors: {
        border: 'hsl(var(--border))',
        input: 'hsl(var(--input))',
        ring: 'hsl(var(--ring))',
        background: 'hsl(var(--background))',
        foreground: 'hsl(var(--foreground))',
        primary: {
          DEFAULT: 'hsl(var(--primary))',
          foreground: 'hsl(var(--primary-foreground))',
        },
        secondary: {
          DEFAULT: 'hsl(var(--secondary))',
          foreground: 'hsl(var(--secondary-foreground))',
        },
        destructive: {
          DEFAULT: 'hsl(var(--destructive))',
          foreground: 'hsl(var(--destructive-foreground))',
        },
        muted: {
          DEFAULT: 'hsl(var(--muted))',
          foreground: 'hsl(var(--muted-foreground))',
        },
        accent: {
          DEFAULT: 'hsl(var(--accent))',
          foreground: 'hsl(var(--accent-foreground))',
        },
        popover: {
          DEFAULT: 'hsl(var(--popover))',
          foreground: 'hsl(var(--popover-foreground))',
        },
        card: {
          DEFAULT: 'hsl(var(--card))',
          foreground: 'hsl(var(--card-foreground))',
        },
      },
      borderRadius: {
        lg: 'var(--radius)',
        md: 'calc(var(--radius) - 2px)',
        sm: 'calc(var(--radius) - 4px)',
      },
      keyframes: {
        'accordion-down': {
          from: { height: 0 },
          to: { height: 'var(--radix-accordion-content-height)' },
        },
        'accordion-up': {
          from: { height: 'var(--radix-accordion-content-height)' },
          to: { height: 0 },
        },
      },
      animation: {
        'accordion-down': 'accordion-down 0.2s ease-out',
        'accordion-up': 'accordion-up 0.2s ease-out',
      },
    },
  },
  plugins: [require('tailwindcss-animate')],
}

"@ | Out-File -FilePath "tailwind.config.js" -Encoding utf8
Write-Host "Aliases: usar @/components segun vite.config.ts" -ForegroundColor Gray
@"
# API Configuration
VITE_API_BASE_URL=http://localhost:5000
VITE_API_TIMEOUT=30000
# App Configuration
VITE_APP_NAME=ReservArte
VITE_APP_URL=http://localhost:3000
# Redsys Configuration (Frontend)
VITE_REDSYS_ENVIRONMENT=test
VITE_REDSYS_SDK_URL=https://sis-t.redsys.es:25443/sis/NC/redsysV3.js
# Feature Flags
VITE_ENABLE_SAVED_CARDS=true
VITE_ENABLE_WHATSAPP=false
VITE_ENABLE_PUBLIC_BOOKING=true

"@ | Out-File -FilePath ".env.example" -Encoding utf8
Copy-Item ".env.example" ".env.development"
@"
{
  "semi": true,
  "trailingComma": "es5",
  "singleQuote": true,
  "printWidth": 100,
  "tabWidth": 2,
  "useTabs": false
}

"@ | Out-File -FilePath ".prettierrc" -Encoding utf8
@"
module.exports = {
  root: true,
  env: { browser: true, es2020: true },
  extends: [
    'eslint:recommended',
    'plugin:vue/vue3-recommended',
    'plugin:@typescript-eslint/recommended',
    'prettier',
  ],
  ignorePatterns: ['dist', '.eslintrc.cjs'],
  parser: 'vue-eslint-parser',
  parserOptions: {
    ecmaVersion: 'latest',
    parser: '@typescript-eslint/parser',
    sourceType: 'module',
  },
  plugins: ['vue', 'prettier'],
  rules: {
    'prettier/prettier': 'warn',
    '@typescript-eslint/no-explicit-any': 'warn',
    'vue/multi-word-component-names': 'off',
  },
}

"@ | Out-File -FilePath ".eslintrc.cjs" -Encoding utf8
Write-Host "Archivos de configuracion creados" -ForegroundColor Green
```

#### Bash

Desde `reservarte-web`, pega el bloque completo (`<< 'EOF'` evita que el shell expanda variables).

```bash
set -e

echo "=== Creando archivos de configuración ==="

cat > vite.config.ts << 'EOF'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@components': path.resolve(__dirname, './src/components'),
      '@features': path.resolve(__dirname, './src/features'),
      '@pages': path.resolve(__dirname, './src/pages'),
      '@lib': path.resolve(__dirname, './src/lib'),
      '@stores': path.resolve(__dirname, './src/stores'),
      '@types': path.resolve(__dirname, './src/types'),
      '@assets': path.resolve(__dirname, './src/assets'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['vue', 'vue-router', 'pinia'],
        },
      },
    },
  },
})
EOF

cat > tsconfig.json << 'EOF'
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"],
      "@components/*": ["./src/components/*"],
      "@features/*": ["./src/features/*"],
      "@pages/*": ["./src/pages/*"],
      "@lib/*": ["./src/lib/*"],
      "@stores/*": ["./src/stores/*"],
      "@types/*": ["./src/types/*"],
      "@assets/*": ["./src/assets/*"]
    }
  },
  "include": ["src"],
  "references": [{ "path": "./tsconfig.node.json" }]
}
EOF

cat > tailwind.config.js << 'EOF'
/** @type {import('tailwindcss').Config} */
export default {
  darkMode: ['class'],
  content: [
    './src/**/*.{vue,ts,tsx}',
  ],
  theme: {
    container: {
      center: true,
      padding: '2rem',
      screens: {
        '2xl': '1400px',
      },
    },
    extend: {
      colors: {
        border: 'hsl(var(--border))',
        input: 'hsl(var(--input))',
        ring: 'hsl(var(--ring))',
        background: 'hsl(var(--background))',
        foreground: 'hsl(var(--foreground))',
        primary: {
          DEFAULT: 'hsl(var(--primary))',
          foreground: 'hsl(var(--primary-foreground))',
        },
        secondary: {
          DEFAULT: 'hsl(var(--secondary))',
          foreground: 'hsl(var(--secondary-foreground))',
        },
        destructive: {
          DEFAULT: 'hsl(var(--destructive))',
          foreground: 'hsl(var(--destructive-foreground))',
        },
        muted: {
          DEFAULT: 'hsl(var(--muted))',
          foreground: 'hsl(var(--muted-foreground))',
        },
        accent: {
          DEFAULT: 'hsl(var(--accent))',
          foreground: 'hsl(var(--accent-foreground))',
        },
        popover: {
          DEFAULT: 'hsl(var(--popover))',
          foreground: 'hsl(var(--popover-foreground))',
        },
        card: {
          DEFAULT: 'hsl(var(--card))',
          foreground: 'hsl(var(--card-foreground))',
        },
      },
      borderRadius: {
        lg: 'var(--radius)',
        md: 'calc(var(--radius) - 2px)',
        sm: 'calc(var(--radius) - 4px)',
      },
      keyframes: {
        'accordion-down': {
          from: { height: 0 },
          to: { height: 'var(--radix-accordion-content-height)' },
        },
        'accordion-up': {
          from: { height: 'var(--radix-accordion-content-height)' },
          to: { height: 0 },
        },
      },
      animation: {
        'accordion-down': 'accordion-down 0.2s ease-out',
        'accordion-up': 'accordion-up 0.2s ease-out',
      },
    },
  },
  plugins: [require('tailwindcss-animate')],
}
EOF

cat > .env.example << 'EOF'
# API Configuration
VITE_API_BASE_URL=http://localhost:5000
VITE_API_TIMEOUT=30000
# App Configuration
VITE_APP_NAME=ReservArte
VITE_APP_URL=http://localhost:3000
# Redsys Configuration (Frontend)
VITE_REDSYS_ENVIRONMENT=test
VITE_REDSYS_SDK_URL=https://sis-t.redsys.es:25443/sis/NC/redsysV3.js
# Feature Flags
VITE_ENABLE_SAVED_CARDS=true
VITE_ENABLE_WHATSAPP=false
VITE_ENABLE_PUBLIC_BOOKING=true
EOF

cp .env.example .env.development

cat > .prettierrc << 'EOF'
{
  "semi": true,
  "trailingComma": "es5",
  "singleQuote": true,
  "printWidth": 100,
  "tabWidth": 2,
  "useTabs": false
}
EOF

cat > .eslintrc.cjs << 'EOF'
module.exports = {
  root: true,
  env: { browser: true, es2020: true },
  extends: [
    'eslint:recommended',
    'plugin:vue/vue3-recommended',
    'plugin:@typescript-eslint/recommended',
    'prettier',
  ],
  ignorePatterns: ['dist', '.eslintrc.cjs'],
  parser: 'vue-eslint-parser',
  parserOptions: {
    ecmaVersion: 'latest',
    parser: '@typescript-eslint/parser',
    sourceType: 'module',
  },
  plugins: ['vue', 'prettier'],
  rules: {
    'prettier/prettier': 'warn',
    '@typescript-eslint/no-explicit-any': 'warn',
    'vue/multi-word-component-names': 'off',
  },
}
EOF

echo "✓ Archivos de configuración creados"
```

---

## Paso 5 — Crear Archivos Base Esenciales

### PowerShell

```powershell
Write-Host "=== Creando archivos base esenciales ===" -ForegroundColor Green
@"
@tailwind base;
@tailwind components;
@tailwind utilities;
@layer base {
  :root {
    --background: 0 0% 100%;
    --foreground: 222.2 84% 4.9%;
    --card: 0 0% 100%;
    --card-foreground: 222.2 84% 4.9%;
    --popover: 0 0% 100%;
    --popover-foreground: 222.2 84% 4.9%;
    --primary: 262.1 83.3% 57.8%;
    --primary-foreground: 210 40% 98%;
    --secondary: 210 40% 96.1%;
    --secondary-foreground: 222.2 47.4% 11.2%;
    --muted: 210 40% 96.1%;
    --muted-foreground: 215.4 16.3% 46.9%;
    --accent: 210 40% 96.1%;
    --accent-foreground: 222.2 47.4% 11.2%;
    --destructive: 0 84.2% 60.2%;
    --destructive-foreground: 210 40% 98%;
    --border: 214.3 31.8% 91.4%;
    --input: 214.3 31.8% 91.4%;
    --ring: 262.1 83.3% 57.8%;
    --radius: 0.5rem;
  }
  .dark {
    --background: 222.2 84% 4.9%;
    --foreground: 210 40% 98%;
    --card: 222.2 84% 4.9%;
    --card-foreground: 210 40% 98%;
    --popover: 222.2 84% 4.9%;
    --popover-foreground: 210 40% 98%;
    --primary: 262.1 83.3% 57.8%;
    --primary-foreground: 210 40% 98%;
    --secondary: 217.2 32.6% 17.5%;
    --secondary-foreground: 210 40% 98%;
    --muted: 217.2 32.6% 17.5%;
    --muted-foreground: 215 20.2% 65.1%;
    --accent: 217.2 32.6% 17.5%;
    --accent-foreground: 210 40% 98%;
    --destructive: 0 62.8% 30.6%;
    --destructive-foreground: 210 40% 98%;
    --border: 217.2 32.6% 17.5%;
    --input: 217.2 32.6% 17.5%;
    --ring: 262.1 83.3% 57.8%;
  }
}
@layer base {
  * {
    @apply border-border;
  }
  body {
    @apply bg-background text-foreground;
  }
}

"@ | Out-File -FilePath "src\\styles\\globals.css" -Encoding utf8
@"
import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

"@ | Out-File -FilePath "src\\lib\\utils\\cn.ts" -Encoding utf8
@"
import axios from 'axios';
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  timeout: Number(import.meta.env.VITE_API_TIMEOUT) || 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});
// Request interceptor
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);
// Response interceptor
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);
export default apiClient;

"@ | Out-File -FilePath "src\\lib\\api\\client.ts" -Encoding utf8
@"
export const env = {
  API_BASE_URL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  API_TIMEOUT: Number(import.meta.env.VITE_API_TIMEOUT) || 30000,
  APP_NAME: import.meta.env.VITE_APP_NAME || 'ReservArte',
  APP_URL: import.meta.env.VITE_APP_URL || 'http://localhost:3000',
  REDSYS_ENVIRONMENT: import.meta.env.VITE_REDSYS_ENVIRONMENT || 'test',
  REDSYS_SDK_URL: import.meta.env.VITE_REDSYS_SDK_URL || 'https://sis-t.redsys.es:25443/sis/NC/redsysV3.js',
  ENABLE_SAVED_CARDS: import.meta.env.VITE_ENABLE_SAVED_CARDS === 'true',
  ENABLE_WHATSAPP: import.meta.env.VITE_ENABLE_WHATSAPP === 'true',
  ENABLE_PUBLIC_BOOKING: import.meta.env.VITE_ENABLE_PUBLIC_BOOKING === 'true',
} as const;

"@ | Out-File -FilePath "src\\config\\env.ts" -Encoding utf8
@"
import { defineComponent, h } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
const DashboardPage = defineComponent({
  name: 'DashboardPage',
  setup() {
    return () => h('div', 'Dashboard')
  },
})
const LoginPage = defineComponent({
  name: 'LoginPage',
  setup() {
    return () => h('div', 'Login')
  },
})
export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'dashboard', component: DashboardPage },
    { path: '/login', name: 'login', component: LoginPage },
  ],
})

"@ | Out-File -FilePath "src\\router\\index.ts" -Encoding utf8
@"
<template>
  <router-view />
</template>

"@ | Out-File -FilePath "src\\App.vue" -Encoding utf8
@"
import { createApp } from 'vue'
import App from './App.vue'
import { router } from './router'
import './styles/globals.css'
createApp(App).use(router).mount('#app')

"@ | Out-File -FilePath "src\\main.ts" -Encoding utf8
@"
// Tipos globales compartidos
export * from './models.types';
export * from './api.types';
export * from './enums';

"@ | Out-File -FilePath "src\\types\\index.ts" -Encoding utf8
@"
export enum AppointmentStatus {
  Pending = 'Pending',
  Confirmed = 'Confirmed',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
  NoShow = 'NoShow',
}
export enum UserRole {
  Admin = 'Admin',
  Manager = 'Manager',
  Employee = 'Employee',
  Customer = 'Customer',
}
export enum PaymentMethod {
  Card = 'Card',
  Cash = 'Cash',
  Transfer = 'Transfer',
  Bizum = 'Bizum',
}
export enum PaymentStatus {
  Pending = 'Pending',
  Authorized = 'Authorized',
  Captured = 'Captured',
  Failed = 'Failed',
  Refunded = 'Refunded',
}

"@ | Out-File -FilePath "src\\types\\enums.ts" -Encoding utf8
Write-Host "Archivos base creados" -ForegroundColor Green
```

### Bash

```bash
set -e

echo "=== Creando archivos base esenciales ==="

cat > src/styles/globals.css << 'EOF'
@tailwind base;
@tailwind components;
@tailwind utilities;
@layer base {
  :root {
    --background: 0 0% 100%;
    --foreground: 222.2 84% 4.9%;
    --card: 0 0% 100%;
    --card-foreground: 222.2 84% 4.9%;
    --popover: 0 0% 100%;
    --popover-foreground: 222.2 84% 4.9%;
    --primary: 262.1 83.3% 57.8%;
    --primary-foreground: 210 40% 98%;
    --secondary: 210 40% 96.1%;
    --secondary-foreground: 222.2 47.4% 11.2%;
    --muted: 210 40% 96.1%;
    --muted-foreground: 215.4 16.3% 46.9%;
    --accent: 210 40% 96.1%;
    --accent-foreground: 222.2 47.4% 11.2%;
    --destructive: 0 84.2% 60.2%;
    --destructive-foreground: 210 40% 98%;
    --border: 214.3 31.8% 91.4%;
    --input: 214.3 31.8% 91.4%;
    --ring: 262.1 83.3% 57.8%;
    --radius: 0.5rem;
  }
  .dark {
    --background: 222.2 84% 4.9%;
    --foreground: 210 40% 98%;
    --card: 222.2 84% 4.9%;
    --card-foreground: 210 40% 98%;
    --popover: 222.2 84% 4.9%;
    --popover-foreground: 210 40% 98%;
    --primary: 262.1 83.3% 57.8%;
    --primary-foreground: 210 40% 98%;
    --secondary: 217.2 32.6% 17.5%;
    --secondary-foreground: 210 40% 98%;
    --muted: 217.2 32.6% 17.5%;
    --muted-foreground: 215 20.2% 65.1%;
    --accent: 217.2 32.6% 17.5%;
    --accent-foreground: 210 40% 98%;
    --destructive: 0 62.8% 30.6%;
    --destructive-foreground: 210 40% 98%;
    --border: 217.2 32.6% 17.5%;
    --input: 217.2 32.6% 17.5%;
    --ring: 262.1 83.3% 57.8%;
  }
}
@layer base {
  * {
    @apply border-border;
  }
  body {
    @apply bg-background text-foreground;
  }
}
EOF

cat > src/lib/utils/cn.ts << 'EOF'
import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
EOF

cat > src/lib/api/client.ts << 'EOF'
import axios from 'axios';
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  timeout: Number(import.meta.env.VITE_API_TIMEOUT) || 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});
// Request interceptor
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);
// Response interceptor
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);
export default apiClient;
EOF

cat > src/config/env.ts << 'EOF'
export const env = {
  API_BASE_URL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  API_TIMEOUT: Number(import.meta.env.VITE_API_TIMEOUT) || 30000,
  APP_NAME: import.meta.env.VITE_APP_NAME || 'ReservArte',
  APP_URL: import.meta.env.VITE_APP_URL || 'http://localhost:3000',
  REDSYS_ENVIRONMENT: import.meta.env.VITE_REDSYS_ENVIRONMENT || 'test',
  REDSYS_SDK_URL: import.meta.env.VITE_REDSYS_SDK_URL || 'https://sis-t.redsys.es:25443/sis/NC/redsysV3.js',
  ENABLE_SAVED_CARDS: import.meta.env.VITE_ENABLE_SAVED_CARDS === 'true',
  ENABLE_WHATSAPP: import.meta.env.VITE_ENABLE_WHATSAPP === 'true',
  ENABLE_PUBLIC_BOOKING: import.meta.env.VITE_ENABLE_PUBLIC_BOOKING === 'true',
} as const;
EOF

cat > src/router/index.ts << 'EOF'
import { defineComponent, h } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
const DashboardPage = defineComponent({
  name: 'DashboardPage',
  setup() {
    return () => h('div', 'Dashboard')
  },
})
const LoginPage = defineComponent({
  name: 'LoginPage',
  setup() {
    return () => h('div', 'Login')
  },
})
export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'dashboard', component: DashboardPage },
    { path: '/login', name: 'login', component: LoginPage },
  ],
})
EOF

cat > src/App.vue << 'EOF'
<template>
  <router-view />
</template>
EOF

cat > src/main.ts << 'EOF'
import { createApp } from 'vue'
import App from './App.vue'
import { router } from './router'
import './styles/globals.css'
createApp(App).use(router).mount('#app')
EOF

cat > src/types/index.ts << 'EOF'
// Tipos globales compartidos
export * from './models.types';
export * from './api.types';
export * from './enums';
EOF

cat > src/types/enums.ts << 'EOF'
export enum AppointmentStatus {
  Pending = 'Pending',
  Confirmed = 'Confirmed',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
  NoShow = 'NoShow',
}
export enum UserRole {
  Admin = 'Admin',
  Manager = 'Manager',
  Employee = 'Employee',
  Customer = 'Customer',
}
export enum PaymentMethod {
  Card = 'Card',
  Cash = 'Cash',
  Transfer = 'Transfer',
  Bizum = 'Bizum',
}
export enum PaymentStatus {
  Pending = 'Pending',
  Authorized = 'Authorized',
  Captured = 'Captured',
  Failed = 'Failed',
  Refunded = 'Refunded',
}
EOF

echo "✓ Archivos base creados"
```

---

## Paso 6 — Componentes UI base (Vue)

> **Reka UI** no incluye un CLI como shadcn: importa los primitivos desde el paquete `reka-ui` según su documentación.

### PowerShell

```powershell
Write-Host "Revisa la documentacion de Reka UI (Button, Dialog, ...)" -ForegroundColor Green
Write-Host "Crea wrappers en src/components/ui/" -ForegroundColor Yellow
```

### Bash

```bash
echo "Revisa la documentacion de Reka UI (Button, Dialog, ...)"
echo "Crea wrappers en src/components/ui/"
```

---

## Paso 7 — Actualizar index.html

### PowerShell

```powershell
@"
<!doctype html>
<html lang="es">
  <head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/logo.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>ReservArte - Sistema de Gestión de Citas</title>
    <!-- Redsys InSite SDK -->
    <script src="https://sis-t.redsys.es:25443/sis/NC/redsysV3.js"></script>
  </head>
  <body>
    <div id="app"></div>
    <script type="module" src="/src/main.ts"></script>
  </body>
</html>

"@ | Out-File -FilePath "index.html" -Encoding utf8
Write-Host "index.html actualizado con SDK de Redsys" -ForegroundColor Green
```

### Bash

```bash
set -e

cat > index.html << 'EOF'
<!doctype html>
<html lang="es">
  <head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/logo.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>ReservArte - Sistema de Gestión de Citas</title>
    <!-- Redsys InSite SDK -->
    <script src="https://sis-t.redsys.es:25443/sis/NC/redsysV3.js"></script>
  </head>
  <body>
    <div id="app"></div>
    <script type="module" src="/src/main.ts"></script>
  </body>
</html>
EOF

echo "✓ index.html actualizado con SDK de Redsys"
```

---

## Paso 8 — Verificar instalación

### PowerShell

```powershell
if (Test-Path "package.json") { Write-Host "OK package.json" } else { Write-Host "FALTA package.json" }
if (Test-Path "node_modules") { Write-Host "OK node_modules" } else { Write-Host "FALTA node_modules - npm install" }
if (Test-Path "src\\App.vue") { Write-Host "OK src/App.vue" } else { Write-Host "FALTA estructura src" }
Write-Host "npm run dev | npm run build"
```

### Bash

```bash
echo "=== Verificando instalación ==="
if [ -f package.json ]; then echo "OK package.json"; else echo "FALTA package.json"; fi
if [ -d node_modules ]; then echo "OK node_modules"; else echo "FALTA node_modules (npm install)"; fi
if [ -f src/App.vue ]; then echo "OK src/App.vue"; else echo "FALTA estructura src"; fi
echo "Iniciar: npm run dev"
echo "Compilar: npm run build"
```

---

## Notas importantes

### PowerShell (Windows)

> **CMD** no ejecuta los bloques anteriores; usa PowerShell o **Git Bash** con los bloques Bash.

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Bash (macOS / Linux)

- Comprueba `node -v` y `npm -v`.
- `set -e` hace que el script falle ante el primer error; puedes quitarlo si lo prefieres.

### Errores comunes

- **npm no se reconoce:** instala Node.js desde [nodejs.org](https://nodejs.org).
- **Permisos (Windows):** PowerShell como administrador si hace falta.
- **ESLint / Vue:** comprueba `eslint-plugin-vue` y `vue-eslint-parser` en `devDependencies`.

### Próximos pasos

- Añadir componentes Reka UI (u otro kit Vue) según necesites.
- Crear stores de Pinia.
- Implementar composables de autenticación (`useAuth`, etc.).
- Crear componentes de formularios.
