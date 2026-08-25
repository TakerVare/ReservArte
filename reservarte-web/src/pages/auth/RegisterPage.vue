<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { Banner } from '@components/ui/banner';
import { RegisterForm } from '@components/ui/register-form';
import { useAuthStore } from '@stores/authStore';
import { register, fetchLegalVersions, AuthApiError } from '@features/auth/api/auth.api';
import type { LegalVersions } from '@features/auth/types/auth.types';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

const router = useRouter();
const authStore = useAuthStore();

const loading = ref(false);
const errorMessage = ref('');
const legalVersions = ref<LegalVersions | null>(null);
const versionsError = ref(false);

// Las versiones vigentes se piden al backend (fuente de verdad). El registro
// debe enviarlas en el consentimiento; el backend valida coincidencia. Si no
// se pueden cargar, no se permite registrar (no se puede consentir a ciegas).
onMounted(async () => {
  try {
    legalVersions.value = await fetchLegalVersions();
  } catch {
    versionsError.value = true;
  }
});

async function handleSubmit(payload: {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  password: string;
  acceptedTerms: boolean;
  acceptedPrivacy: boolean;
}) {
  if (!legalVersions.value) {
    errorMessage.value = 'No se han podido cargar los documentos legales. Recarga la página.';
    return;
  }
  errorMessage.value = '';
  loading.value = true;
  try {
    const result = await register({
      ...payload,
      // Se adjuntan las versiones vigentes cargadas al montar la página
      acceptedTermsVersion: legalVersions.value.termsVersion,
      acceptedPrivacyVersion: legalVersions.value.privacyVersion,
    });

    // El registro autentica automáticamente (devuelve tokens + user)
    authStore.login({
      user: result.user,
      accessToken: result.accessToken,
      refreshToken: result.refreshToken,
      mfaRequired: result.mfaRequired,
      mfaTicket: result.mfaTicket,
    });

    router.push({ name: 'my-appointments' });
  } catch (err) {
    errorMessage.value =
      err instanceof AuthApiError
        ? err.message
        : 'No se pudo completar el registro. Inténtalo de nuevo.';
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="flex min-h-screen flex-col">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />
    <main class="flex flex-1 items-center justify-center px-4 py-8">
      <div class="w-full max-w-[600px]">
        <RegisterForm
          :loading="loading"
          :error-message="versionsError
            ? 'No se han podido cargar los documentos legales. Recarga la página.'
            : errorMessage"
          @submit="handleSubmit"
        />
        <div class="mt-6 text-center">
          <RouterLink
            :to="{ name: 'login' }"
            class="font-sans text-sm text-muted-foreground underline-offset-2 hover:text-primary hover:underline"
          >
            ¿Ya tienes cuenta? Inicia sesión
          </RouterLink>
        </div>
      </div>
    </main>
  </div>
</template>